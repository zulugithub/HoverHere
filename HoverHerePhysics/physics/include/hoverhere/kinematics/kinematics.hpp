#pragma once

#include <array>
#include <cstddef>

#include "hoverhere/core/frames.hpp"
#include "hoverhere/core/math.hpp"
#include "hoverhere/params/mechanics_params.hpp"

// Rotor control kinematics (documentation chapters 3 and 4): swashplate / servo / pitch link mechanics of the main
// rotor and the tail rotor pitch slider, ported from Helicopter_Mainrotor_Mechanics.cs and
// Helicopter_Tailrotor_Mechanics.cs. All functions are pure and allocation free.
//
// Frame O is the rotor frame: origin at the hub, the shaft is the y-axis (y up for a main rotor), right handed. The
// shaft rotation angle Omega rotates the rotating parts about +y by A = rot_y(Omega) (frames::rot_y); a blade at
// azimuth psi points along (cos psi, 0, -sin psi).
//
// Servo angles sigma are right-hand angles about the servo shaft axis (the z axis of the servo frame LA_i); the neutral
// position sigma = 0 is r_BiAi_LAi as exported from Blender. Note: Helicopter_Mainrotor_Mechanics.cs obtains sigma in
// step 1 as such a right-hand angle but rotates the arm with Az_LR(sigma) = rot_z(-sigma) in step 2, so the C# chain
// step 1 -> step 2 mirrors the arm (the -15 deg / -0.0065 m gains in Helicopter_ODE.cs hide that). Here both steps use the
// right-hand angle, so step 2 inverts step 1.
//
// Main rotor, the three steps of the documentation:
//   step 1  swashplate (roll, pitch, collective)  ->  servo angles                 (iterative, needed by the FBL controller)
//   --      servo dynamics PT1 on the servo angles                                  (between RK4 steps, pt1_step)
//   step 2  servo angles  ->  swashplate pose                                       (iterative, direct input for Rotorflight / NEXUS-XR)
//   step 3  swashplate pose, Omega  ->  blade pitch angles                          (iterative)
//   step 4  rotating swashplate drivers F-G-H                                       (analytical, visualization only)
// and the ideal linear CCPM mixer as a replacement for models without linkage geometry.
namespace hoverhere::kinematics {

using math::Mat3;
using math::Quat;
using math::Vec3;
using params::mechanics::MainrotorMechanics;
using params::mechanics::TailrotorMechanics;

inline constexpr std::size_t kServoCount = 3;
inline constexpr std::size_t kRotatingLimbCount = 2;
inline constexpr std::size_t kVirtualBladeCount = 10;  // azimuth stations of the rotor model (Helicopter_Mainrotor_Mechanics.cs)

struct IterationOptions {
    int max_iterations = 30;
    Real tolerance = 1e-13;  // [m] stop when no joint moves more than this between two iterations
};

// Exact discretization of a first order lag y' = (u - y) / T over dt (u constant). T <= 0 passes u through. Call it
// between the RK4 steps with the servo angle commands, never inside a stage.
[[nodiscard]] Real pt1_step(Real y, Real u, Real dt, Real time_constant) noexcept;

// ---------------------------------------------------------------------------------------------------------------
// main rotor
// ---------------------------------------------------------------------------------------------------------------
// Swashplate command as the controller produces it: tilt about the x-axis (roll) and about the z-axis (pitch), and the
// stroke of the swashplate centre along the shaft. The tilt is A^LKO = Ax(roll) Az(pitch), i.e. the swashplate normal is
// n = (-sin(pitch) cos(roll), cos(pitch) cos(roll), sin(roll)).
struct SwashplateCommand {
    Real roll = 0;        // [rad]
    Real pitch = 0;       // [rad]
    Real collective = 0;  // [m] offset of K from its neutral position r_KO_O.y
};

struct Step1Result {
    std::array<Real, kServoCount> sigma;           // [rad] servo angles
    std::array<Vec3, kServoCount> r_BiO_O;         // [m] servo arm ball joints
    std::array<Vec3, kServoCount> r_CiO_O;         // [m] swashplate ball joints of the commanded pose
    Real residual;                                 // [m] largest rod length error |l(B_i C_i) - l_CiBi|
    bool converged;
};

struct SwashplatePose {
    Vec3 r_KO_O;                                   // [m] centre of the swashplate on the shaft axis
    Vec3 normal_O;                                 // swashplate normal e_yLK^O
    Mat3 A_OLK;                                    // stationary swashplate frame (C_s planar joint: n_x = 0)
    std::array<Vec3, kServoCount> r_BiO_O;
    std::array<Vec3, kServoCount> r_CiO_O;
    Real residual;                                 // [m] largest rod length error
    bool converged;
};

struct BladeLinkage {
    Real beta;             // [rad] blade pitch angle (positive: the pitch link point E_i above the blade plane)
    Vec3 r_DiO_O;          // [m] ball joint on the rotating swashplate
    Vec3 r_EiO_O;          // [m] ball joint on the blade pitch arm
    Vec3 n_blade_O;        // blade axis (feathering axis)
    Real residual;         // [m] rod length error |l(D_i E_i) - l_EiDi|
};

struct RotatingBlades {
    Mat3 A_OLJ;            // rotating swashplate frame at the shaft angle
    std::array<BladeLinkage, kRotatingLimbCount> blade;
};

struct DriverLimb {
    Vec3 r_FiO_O;
    Vec3 r_GiO_O;
    Vec3 r_HiO_O;
    bool reachable;        // the two driver arms can reach each other (circle intersection exists)
};

// Step 1 (section 3.3): servo angles for the commanded swashplate pose.
[[nodiscard]] Step1Result step1_swashplate_to_servos(const MainrotorMechanics& m, const SwashplateCommand& command, const IterationOptions& options = {}) noexcept;

// Step 2 (section 3.4): swashplate pose for given servo angles (forward kinematics, three rods meeting the plane constraint).
[[nodiscard]] SwashplatePose step2_servos_to_swashplate(const MainrotorMechanics& m, const std::array<Real, kServoCount>& sigma, const IterationOptions& options = {}) noexcept;

// The commanded (roll, pitch, collective) a swashplate pose corresponds to; inverse of the definition in step 1.
[[nodiscard]] SwashplateCommand swashplate_command_from_pose(const MainrotorMechanics& m, const SwashplatePose& pose) noexcept;

// Step 3 (section 3.5): pitch angle of the blade that is at the azimuth `omega_blade` while the swashplate rotates with the shaft
// angle `omega_swash`, using rotating limb `limb` (the physical blades have omega_blade = Omega + offset, the virtual
// rotor model blades have omega_blade = omega_swash = psi and limb 0).
[[nodiscard]] BladeLinkage step3_blade(const MainrotorMechanics& m, const SwashplatePose& pose, Real omega_swash, Real omega_blade, std::size_t limb, const IterationOptions& options = {}) noexcept;

// Both physical blades of the rotor head at the shaft angle Omega (blade i is offset by 360 deg / blades_count * i).
[[nodiscard]] RotatingBlades step3_swashplate_to_blades(const MainrotorMechanics& m, const SwashplatePose& pose, Real Omega, const IterationOptions& options = {}) noexcept;

// Blade pitch for blade 0 at the azimuth psi (for the azimuth stations of the BEMT rotor model).
[[nodiscard]] Real step3_pitch_at_azimuth(const MainrotorMechanics& m, const SwashplatePose& pose, Real psi, const IterationOptions& options = {}) noexcept;

// The kVirtualBladeCount stations psi_c = c 360 deg / count of the rotor model; independent of the shaft angle.
[[nodiscard]] std::array<Real, kVirtualBladeCount> step3_virtual_blade_angles(const MainrotorMechanics& m, const SwashplatePose& pose, const IterationOptions& options = {}) noexcept;

// Step 4 (section 3.6, visualization only): rotation driver arms of the rotating swashplate.
[[nodiscard]] std::array<DriverLimb, kRotatingLimbCount> step4_driver_visuals(const MainrotorMechanics& m, const SwashplatePose& pose, const RotatingBlades& blades, Real Omega) noexcept;

// ---------------------------------------------------------------------------------------------------------------
// ideal linear CCPM mixer
// ---------------------------------------------------------------------------------------------------------------
// Linearization of the whole mechanism for models that have no linkage geometry (and as a cheap model). With
// gamma_i the azimuth of servo i around the shaft, h(gamma) = collective + a (pitch cos gamma + roll sin gamma) the
// small angle height of the swashplate over the servo ball joint,
//   servo i   sigma_i = servo_offset_i + k_collective collective + k_tilt (pitch cos gamma_i + roll sin gamma_i)
//   blade     beta(psi) = beta_0 + c_collective collective + c_cyclic (pitch cos(psi + phi) + roll sin(psi + phi))
struct CcpmMixer {
    std::array<Real, kServoCount> gamma;         // [rad] servo azimuths
    std::array<Real, kServoCount> servo_offset;  // [rad] servo angles of the neutral swashplate
    Real k_collective;                           // [rad/m]
    Real k_tilt;                                 // [rad/rad]
    Real beta_0;                                 // [rad]
    Real c_collective;                           // [rad/m]
    Real c_cyclic;                               // [rad/rad]
    Real phi;                                    // [rad] phase advance of the cyclic pitch against the swashplate tilt
};

// 120 degree CCPM of a Logo 600 sized head (arm 17 mm, ball joints on a 36 mm circle), no phase advance.
[[nodiscard]] CcpmMixer make_default_ccpm_mixer() noexcept;

// Fits the mixer to the small signal response of the detailed mechanism (finite differences around the neutral pose).
[[nodiscard]] CcpmMixer calibrate_ccpm_mixer(const MainrotorMechanics& m) noexcept;

// Swashplate -> servos (demixing of the controller output, the linear counterpart of step 1).
[[nodiscard]] std::array<Real, kServoCount> ccpm_servos(const CcpmMixer& mixer, const SwashplateCommand& command) noexcept;

// Servos -> swashplate (mixing, the linear counterpart of step 2): exact inverse of ccpm_servos.
[[nodiscard]] SwashplateCommand ccpm_swashplate(const CcpmMixer& mixer, const std::array<Real, kServoCount>& sigma) noexcept;

// Swashplate -> blade pitch at the azimuth psi (the linear counterpart of step 3).
[[nodiscard]] Real ccpm_pitch_at_azimuth(const CcpmMixer& mixer, const SwashplateCommand& command, Real psi) noexcept;

// ---------------------------------------------------------------------------------------------------------------
// tail rotor
// ---------------------------------------------------------------------------------------------------------------
struct TailrotorBlade {
    Real beta;                 // [rad] blade pitch angle
    Vec3 r_HiO_O;              // [m] pitch link point on the blade
    Vec3 r_GiO_O;              // [m] hinge on the pitch plate
    Vec3 r_IiO_O;              // [m] pitch arm point of the blade
    Vec3 n_blade_O;            // blade axis
    Vec3 normal_hinge_axis;
};

struct TailrotorPose {
    Vec3 r_BO_O;               // [m] servo arm ball joint
    Vec3 r_CO_O;               // [m] connection of the long rod to the lever
    Vec3 r_EO_O;               // [m] lever end at the control sleeve
    Vec3 r_KO_O;               // [m] control sleeve / pitch plate on the shaft axis
    Real lever_angle;          // [rad] rotation of the lever about its axis
    Real stroke;               // [m] axial position of the sleeve relative to r_KO_O of the parameters
    Real alpha;                // [rad] small rotation of the pitch plate relative to the blade
    bool reachable;            // the rod length / lever geometry has a solution
    std::array<TailrotorBlade, kRotatingLimbCount> blade;
};

// Analytical forward kinematics of chapter 4: servo angle sigma and shaft angle Omega -> blade pitch (all blades share beta).
[[nodiscard]] TailrotorPose tailrotor_forward_kinematics(const TailrotorMechanics& m, Real sigma, Real Omega) noexcept;

}  // namespace hoverhere::kinematics
