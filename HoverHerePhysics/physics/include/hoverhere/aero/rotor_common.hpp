#pragma once

#include <cstdint>
#include <random>

#include "hoverhere/core/frames.hpp"
#include "hoverhere/core/math.hpp"
#include "hoverhere/dynamics/rigid_body.hpp"
#include "hoverhere/params/model_params.hpp"

// Building blocks shared by the rotor models (classic and BEMT) and the fuselage aerodynamics. Conventions:
//   O   inertial frame, y up (gravity along -y), right handed;  L_H  body frame at the centre of mass;  L_R  rotor frame
//   (y = shaft axis, orientation oriLH relative to L_H, S123 Euler angles).
//   Induced velocity Vi is the speed of the wake along the negative rotor axis (-y of the rotor frame): positive for a
//   lifting rotor, whose thrust points along +y, i.e. downwash of a main rotor. It has the sign of the thrust. The Unity
//   code returns v_i with the opposite sign from the classic model (v_i = -Vi).
//   Time dependent state (filters, integrators, random numbers) only changes in the post_step() functions, once per full step.
namespace hoverhere::aero {

using math::Mat3;
using math::Quat;
using math::Vec3;

enum class RotorKind : int { Mainrotor = 0, Tailrotor = 1, Propeller = 2 };

[[nodiscard]] params::si::Rotor as_rotor(const params::si::Mainrotor& rotor) noexcept;

// Fuselage kinematics as the rotors and the aerodynamics see them.
struct BodyKinematics {
    Vec3 position_O;    // [m]   centre of mass
    Mat3 A_OL;          // [-]   body -> world
    Vec3 velocity_O;    // [m/s] centre of mass
    Vec3 velocity_L;    // [m/s] centre of mass, in the body frame
    Vec3 omega_L;       // [rad/s]
};
[[nodiscard]] BodyKinematics body_kinematics(const dynamics::RigidBodyState& x) noexcept;

// Constant placement of a rotor in the body frame.
struct RotorGeometry {
    Vec3 position_L;    // [m] hub
    Mat3 A_LR;          // [-] rotor frame -> body frame, A_RL_S123(oriLH)
    Vec3 axis_L;        // shaft direction (rotor y-axis) in the body frame
};
[[nodiscard]] RotorGeometry make_rotor_geometry(const params::si::Rotor& rotor) noexcept;

// Wind velocity in the world frame (direction about the vertical axis, speed, vertical component).
[[nodiscard]] Vec3 wind_velocity_O(const params::si::Weather& weather) noexcept;

// Smooth saturation of x at +-limit with a quadratic transition of half width `transition` (Helper.Limit_Symetric).
[[nodiscard]] Real limit_symmetric(Real x, Real limit, Real transition) noexcept;

// Restoring stiffness of the hub (O-ring) that grows quadratically beyond `start_nonlinearity` (Nonlinear_Stiffness).
[[nodiscard]] Real nonlinear_hub_stiffness(Real stiffness, Real start_nonlinearity, Real flapping_angle) noexcept;

// ----------------------------------------------------------------------------------------------------------------
// ground effect (Padfield, Helicopter Flight Dynamics, eq. 3.218 without the discontinuity at h = R / 4)
// ----------------------------------------------------------------------------------------------------------------
struct GroundEffectInput {
    Real hub_distance_to_ground = 1e9;  // [m]
    Vec3 surface_normal_O{{0, 1, 0}};   // unit normal of the ground below the rotor, pointing up
};

// `downwash_direction_O`: unit vector of the wake direction (opposite to the thrust).
// Returns the thrust multiplier (>= 1); 1 when the wake does not blow towards the surface.
[[nodiscard]] Real ground_effect_factor(Real rotor_radius, const GroundEffectInput& ground, const Vec3& downwash_direction_O,
                                        Real horizontal_speed, Real induced_velocity) noexcept;

// ----------------------------------------------------------------------------------------------------------------
// deterministic random numbers for the turbulence (mt19937_64 is specified bit exactly, the distributions of the standard
// library are not)
// ----------------------------------------------------------------------------------------------------------------
class RotorRng {
public:
    explicit RotorRng(const std::uint64_t seed = 20240427) : engine_(seed) {}
    [[nodiscard]] Real symmetric() noexcept  // uniform in [-1, 1)
    {
        return static_cast<Real>(engine_() >> 11) * (2.0 / 9007199254740992.0) - 1.0;
    }

private:
    std::mt19937_64 engine_;
};

// ----------------------------------------------------------------------------------------------------------------
// turbulence of the rotor wake: random force and torque on the fuselage that change with `frequency`, the torque is
// smoothed. The strength depends on how fast the helicopter rotates (more) and translates (less) and on the strength of
// the vortex ring state.
// ----------------------------------------------------------------------------------------------------------------
struct TurbulenceConfig {
    Real rotational_velocity_limit = 4;      // [rad/s]
    Real translational_velocity_limit = 10;  // [m/s]
    Real frequency = 5;                      // [1/s]
    Vec3 force_strength{{2, 12, 2}};         // [N]  amplitude per body axis
    Vec3 torque_strength{{50, 2, 50}};       // [Nm] amplitude per body axis
    // The torque noise is smoothed with a critically damped filter. The Unity code smoothed it with Vector3.SmoothDamp(0.3 s) but never wrote
    // the filter velocity back and called it four times per RK4 step, which made the torque follow its target about 40 000 times slower than
    // intended (an effective lag of about 12 s at the 1 ms step, an rms of 5 % of the amplitude). The tuning parameters of the parameter
    // files were set with that behaviour; this time constant reproduces the same rms of the torque (5.2 % of the amplitude at the default
    // draw frequency of 5 Hz) with a correctly working filter.
    Real torque_smooth_time = 11.7;          // [s]
    Real torque_max_speed = 1000;            // [Nm/s]
};
[[nodiscard]] TurbulenceConfig make_turbulence_config(const params::si::Turbulence& p) noexcept;

struct TurbulenceState {
    Vec3 force_L{{0, 0, 0}};           // [N]  held between two draws
    Vec3 torque_L{{0, 0, 0}};          // [Nm] smoothed
    Vec3 torque_target_L{{0, 0, 0}};   // [Nm]
    Vec3 torque_velocity{{0, 0, 0}};   // [Nm/s] state of the smoothing filter
    Real elapsed_old = 0;              // [s] position within the turbulence period at the last step
};

// Strength in [0, 1]: `base_strength` (e.g. the vortex ring state) plus the contribution of the helicopter motion.
[[nodiscard]] Real turbulence_strength(const TurbulenceConfig& c, const Vec3& omega_L, Real translational_speed, Real base_strength) noexcept;

// Once per full step with the time at the end of the step.
void turbulence_post_step(const TurbulenceConfig& c, TurbulenceState& s, Real strength, Real time, Real dt, RotorRng& rng) noexcept;

}  // namespace hoverhere::aero
