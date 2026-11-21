#pragma once

#include <array>

#include "hoverhere/aero/rotor_common.hpp"
#include "hoverhere/params/model_params.hpp"

// Air loads on the fuselage and on the fins / wings including the interaction with the rotor wakes (port of Fuselage_Model
// and Wing_Model of Helicopter_ODE.cs). Pure functions of (kinematics, wind, wake): usable in every RK4 stage.
//
// The wake of a rotor depends on the loads of the fuselage (parasite power of the rotor torque) and the fuselage depends on the
// wake. The loop is broken with a delay of one step: the wake velocities passed to the aerodynamics are the ones published
// by the rotor filter states (ClassicRotorFilterState::wake_velocity, BemtFilterState::wake_velocity).
namespace hoverhere::aero {

// Wake velocities of the rotors at the fuselage [m/s]; positive = the rotor blows air away from the aircraft's top (downwash of a
// lifting main rotor, along -y of the body frame), like Vi of the rotor models.
struct RotorWake {
    Real mainrotor = 0;
    Real tailrotor = 0;   // second rotor of a tandem, or the sideways wake of the tail rotor
    Real propeller = 0;   // wake along +x of the body frame (pusher propeller)
};

struct AerodynamicsConfig {
    params::si::Fuselage fuselage{};
    params::si::Wing horizontal_fin{};
    params::si::Wing vertical_fin{};
    params::si::Wing horizontal_wing_left{};
    params::si::Wing horizontal_wing_right{};
};
[[nodiscard]] AerodynamicsConfig make_aerodynamics_config(const params::si::HelicopterParams& helicopter) noexcept;

struct SurfaceLoads {
    Vec3 force_L{{0, 0, 0}};   // [N]
    Vec3 torque_L{{0, 0, 0}};  // [Nm] about the centre of mass
};

struct AerodynamicsResult {
    Vec3 force_O{{0, 0, 0}};           // [N] sum of all surfaces, world frame
    Vec3 torque_L{{0, 0, 0}};          // [Nm] sum, body frame, about the centre of mass
    Vec3 fuselage_force_L{{0, 0, 0}};  // [N] drag of the fuselage alone (input of the rotor parasite power)
    // loads of the single surfaces (zero for surfaces that do not exist): horizontal fin, vertical fin, left wing, right wing
    std::array<SurfaceLoads, 4> surface{};
};

// Fuselage drag: quadratic in the air velocity along each body axis; the vertical axis sees the rotor wakes.
[[nodiscard]] Vec3 fuselage_force_L(const params::si::Fuselage& fuselage, Real rho_air, const Vec3& air_velocity_L, const RotorWake& wake) noexcept;

// Lift of a fin or wing with stall: lift curve slope below the stall angle, flat plate drag above. The surface sees the rotor
// wakes where they hit it (only blowing away from the top, scaled by the downwash factors of the surface).
[[nodiscard]] SurfaceLoads wing_loads(const params::si::Wing& wing, Real rho_air, const BodyKinematics& body, const Vec3& wind_O, const RotorWake& wake) noexcept;

[[nodiscard]] AerodynamicsResult aerodynamics_evaluate(const AerodynamicsConfig& cfg, const BodyKinematics& body, const Vec3& wind_O, Real rho_air, const RotorWake& wake) noexcept;

}  // namespace hoverhere::aero
