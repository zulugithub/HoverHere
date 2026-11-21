#pragma once

#include <cstddef>

#include "hoverhere/aero/rotor_common.hpp"
#include "hoverhere/kinematics/kinematics.hpp"
#include "hoverhere/numerics/rk4.hpp"
#include "hoverhere/params/model_params.hpp"

// Classic (analytical, no blade elements) rotor model of the main rotor, the tail rotor and the propeller: thrust and
// induced velocity after Heffley / Cai et al. (iterative), profile / induced / parasite / climb power based shaft
// torque, tip-path-plane flapping dynamics with hub stiffness, vortex ring state, wake turbulence and ground effect
// (port of Helicopter_Rotor_Physics.Rotor_Thrust_and_Torque_with_Precalculations).
//
// Time structure:
//   classic_rotor_evaluate()   pure function of (config, filter state, inputs); safe inside every RK4 stage. The only
//                              rotor related RK4 state is the flapping angle pair (a_s, b_s), see FlappingState.
//   classic_rotor_post_step()  once per full step: vortex ring strength, turbulence random numbers and smoothing.
//   limit_flapping_at_low_rpm()  state projection between two steps (decay of the flapping angles at low rotor speed).
// Control input: the effective blade pitch angles (RotorControl), from the ideal CCPM mixer (main rotor,
// classic_control_from_ccpm) or from the tail rotor kinematics (classic_control_from_tailrotor).
namespace hoverhere::aero {

// Collective and cyclic blade pitch as the rotor feels them. Theta_cyc_a_s / Theta_cyc_b_s are the steady state flapping
// angles that a longitudinal (about the rotor z-axis) / lateral (about the rotor x-axis) cyclic input produces.
struct RotorControl {
    Real Theta_col = 0;        // [rad]
    Real Theta_cyc_a_s = 0;    // [rad]
    Real Theta_cyc_b_s = 0;    // [rad]
};

// Effective pitch angles of the ideal linear CCPM mixer for a swashplate command: collective pitch is the mean of the blade
// pitch, a swashplate tilt gives the flapping angle with the same sign: roll -> b_s, pitch -> a_s.
[[nodiscard]] RotorControl classic_control_from_ccpm(const kinematics::CcpmMixer& mixer, const kinematics::SwashplateCommand& command) noexcept;
// Same from servo angles (mixing step of the ideal CCPM).
[[nodiscard]] RotorControl classic_control_from_ccpm_servos(const kinematics::CcpmMixer& mixer, const std::array<Real, kinematics::kServoCount>& sigma) noexcept;
// The tail rotor has collective pitch only. The kinematic angle beta of the analytical tail kinematics is measured from the
// neutral pitch link position and is positive for a servo angle that decreases the thrust of the rotor (the sign of
// Helicopter_Tailrotor_Mechanics.cs is marked "Todo > or <" there); Theta_col = Theta_col_0 - beta then increases with the
// stick like the classic Theta_col = K_col * deflection + Theta_col_0, and the throw of 30 deg of the servo is the K_col of
// the tail rotor (the Logo 600 mechanism turns the blades by 0.8 deg per degree of servo).
[[nodiscard]] RotorControl classic_control_from_tailrotor(const kinematics::TailrotorPose& pose, Real theta_col_0) noexcept;

// RK4 state of one rotor: tip-path-plane flapping angles.
namespace flap {
inline constexpr std::size_t a_s = 0;  // [rad] about the rotor z-axis (longitudinal)
inline constexpr std::size_t b_s = 1;  // [rad] about the rotor x-axis (lateral)
}  // namespace flap
using FlappingState = numerics::StateVector<2>;

// The vortex ring state and the turbulence advance in steps of the Unity physics (about 1 ms): the rate parameters of the
// parameter files are "per step". They are scaled with dt / kUnityReferenceStep to be independent of the step size.
inline constexpr Real kUnityReferenceStep = 0.001;  // [s]

struct ClassicRotorConfig {
    RotorKind kind = RotorKind::Mainrotor;
    bool flapping = true;
    bool vortex_ring_state = true;
    bool turbulence = true;
    bool ground_effect = true;
    Real mass_total = 0;               // [kg] part of the helicopter mass this rotor carries (climb power); half for tandem rotors
    Real gravity = 9.81;               // [m/s^2]
    Real omega_reference = 0;          // [rad/s] nominal rotor speed (governor target of bank 0): flapping fades out below 70 %, is off below 10 %
    params::si::Rotor rotor{};
    params::si::Flapping hub{};
    params::si::RotorInflowTuning inflow{};
    params::si::VortexRingState vortex_ring{};
    TurbulenceConfig turbulence_config{};
};

// Configuration as the Unity ODE uses it: main rotor with everything on, tail rotor and propeller of a single rotor
// helicopter without flapping / vortex ring / turbulence / ground effect (all on for the second rotor of a tandem).
[[nodiscard]] ClassicRotorConfig make_classic_rotor_config(const params::si::ModelParams& p, RotorKind kind) noexcept;

// Slow filters and random disturbances of the rotor, advanced only by classic_rotor_post_step().
struct ClassicRotorFilterState {
    Real vrs_strength = 0;             // [0, 1] development of the vortex ring state
    TurbulenceState turbulence{};
    Real wake_velocity = 0;            // [m/s] induced velocity of the last step, published for the fuselage aerodynamics (one step delay)
};

struct ClassicRotorInputs {
    BodyKinematics body{};
    Vec3 wind_O{{0, 0, 0}};            // [m/s] wind velocity in the world frame
    Real rho_air = 1.27;               // [kg/m^3]
    Real omega_shaft = 0;              // [rad/s]
    RotorControl control{};
    FlappingState flapping{};          // [rad] RK4 state
    Vec3 force_fuselage_L{{0, 0, 0}};  // [N] air drag on the fuselage (parasite power)
    GroundEffectInput ground{};
    bool freewheeling = false;         // the rotor is not driven: no shaft torque on the fuselage
};

struct ClassicRotorOutputs {
    Real thrust = 0;                   // [N] along the shaft axis
    Real torque = 0;                   // [Nm] aerodynamic torque on the rotor about the shaft, signed like the rotation (negative when driven)
    Real induced_velocity = 0;         // [m/s] wake speed along the thrust direction, positive = downwash
    Vec3 force_O{{0, 0, 0}};           // [N] on the fuselage, world frame (thrust, vortex ring and ground effect applied)
    Vec3 torque_L{{0, 0, 0}};          // [Nm] on the fuselage about the centre of mass, body frame (shaft torque, hub offset, hub stiffness, turbulence)
    FlappingState dflapping{};         // [rad/s] derivative of the RK4 state
    Real flapping_tau = 0;             // [s] flapping time constant
    Real P_profile = 0, P_induced = 0, P_parasite = 0, P_climb = 0;  // [W]
    Real horizontal_speed = 0;         // [m/s] air speed in the rotor plane
    Real vertical_speed = 0;           // [m/s] air speed along the thrust, positive when climbing
    Real hub_speed = 0;                // [m/s] total air speed at the hub
    Real vrs_strength = 0;             // [0, 1] state used by this evaluation
    Real ground_effect_factor = 1;     // [-]
    Real turbulence_strength = 0;      // [0, 1] strength used by the next random draw
};

[[nodiscard]] ClassicRotorOutputs classic_rotor_evaluate(const ClassicRotorConfig& cfg, const ClassicRotorFilterState& state, const ClassicRotorInputs& in) noexcept;

// Once per full step. `out` are the outputs of the evaluation at the start of the step, `time` is the time at its end.
void classic_rotor_post_step(const ClassicRotorConfig& cfg, ClassicRotorFilterState& state, const ClassicRotorInputs& in,
                             const ClassicRotorOutputs& out, Real time, Real dt, RotorRng& rng) noexcept;

// Between two steps: at low rotor speed the flapping angles decay linearly to zero (0.003 rad per 1 ms), and are zero below 60 rpm.
void limit_flapping_at_low_rpm(FlappingState& flapping, Real omega_shaft, Real omega_reference, Real dt) noexcept;

}  // namespace hoverhere::aero
