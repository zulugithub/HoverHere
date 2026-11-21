#pragma once

#include <cstddef>
#include <optional>

#include "hoverhere/core/types.hpp"
#include "hoverhere/numerics/rk4.hpp"
#include "hoverhere/params/model_params.hpp"

// Drivetrain and governor (documentation chapter 6): brushless motor with its speed governor, the freewheel with
// torsional stiffness and damping, and the main rotor shaft that also carries the tail rotor and the propeller.
//
//   system 1 (w.r.t. the motor shaft)      J_momg omega_mo' = T_f^mo + k/R V_s - (B_M + k^2/R) omega_mo
//   system 2 (w.r.t. the main rotor shaft) J_mrtrpr omega_mr' = T_f^mr + T_load^mr
//   freewheel                              T_f^mr = k_fw (Omega_mg - Omega_mr) + d_fw (omega_mg - omega_mr)
//                                          while the motor side drives the rotor, otherwise 0
//   governor                               V_s = Kp (omega_target - omega_mo) + Ki integral(omega_target - omega_mo)
//
// The state has two angles, two speeds and the governor integral (five values, a "three part" model: motor, freewheel,
// rotor shaft). The derivative is a pure function of (parameters, inputs, state) and can be used inside an RK4 stage.
// Everything that is not smooth or not a derivative (soft start of the setpoint, the angle reset of the open freewheel,
// the integrator reset of a stopped motor) happens between the steps in drivetrain_post_step() / soft_start_step().
namespace hoverhere::dynamics {

inline constexpr std::size_t kDrivetrainStateSize = 5;

namespace drv {
inline constexpr std::size_t omega_mo = 0;       // [rad/s] motor speed
inline constexpr std::size_t Omega_mo = 1;       // [rad] motor angle
inline constexpr std::size_t omega_mr = 2;       // [rad/s] main rotor speed
inline constexpr std::size_t Omega_mr = 3;       // [rad] main rotor angle
inline constexpr std::size_t governor_int = 4;   // [rad] integral of the governor speed error
}  // namespace drv

using DrivetrainState = numerics::StateVector<kDrivetrainStateSize>;

struct DrivetrainParams {
    // motor and battery
    Real R_a;          // [ohm] motor, connector and controller resistance
    Real Ri;           // [ohm] battery inner resistance
    Real B_M;          // [Nm/(rad/s)] motor friction
    Real k;            // [V/(rad/s)] motor constant (back-emf = torque constant)
    // inertias and gears
    Real J_momg_mo;    // [kg m^2] motor + main gear, w.r.t. the motor shaft
    Real J_mrtrpr_mr;  // [kg m^2] main rotor + tail rotor + propeller, w.r.t. the main rotor shaft
    Real n_mo2mr;      // [-] gear ratio motor -> main rotor (omega_mo = n omega_mr)
    Real n_mr2tr;      // [-] gear ratio main rotor -> tail rotor
    Real n_mr2pr;      // [-] gear ratio main rotor -> propeller
    Real k_freewheel;  // [Nm/rad]
    Real d_freewheel;  // [Nm/(rad/s)]
    Real eta;          // [-] transmission efficiency (Helicopter_ODE.cs uses 0.8, the documentation says about 0.9)
    bool invert_mainrotor_rotation_direction;
    // governor
    Real Kp;
    Real Ki;
    Real saturation_min;  // [V]
    Real saturation_max;  // [V]
    // empirical terms of the Unity model (kept for parity)
    Real mainrotor_coulomb_friction;  // [Nm] friction torque of the standing rotor (0.1 * mass_total)
    Real motor_extra_damping;         // [1/s] additional damping of the driven motor (0.001)
    Real motor_off_damping;           // [1/s] damping of the motor with the controller switched off (0.9)
};

[[nodiscard]] DrivetrainParams make_drivetrain_params(const params::si::HelicopterParams& helicopter) noexcept;
[[nodiscard]] bool is_valid(const DrivetrainParams& p) noexcept;

struct DrivetrainInputs {
    Real omega_mo_target = 0;           // [rad/s] governor setpoint at the motor shaft (signed, see motor_speed_target)
    bool motor_enabled = true;          // false: controller off, the motor only brakes
    // Bypasses the PI controller (e.g. the ESC of a flight controller model): applied voltage [V], signed like the
    // motor speed, no saturation and no integrator.
    std::optional<Real> voltage_override;
    // Torques that the rotors apply to their shafts about the shaft axis, signed like the shaft rotation (-F_torquesumLD_LD.y
    // in Helicopter_ODE.cs): an aerodynamic load opposes the rotation, so it is negative for a driven rotor.
    Real torque_mainrotor_mr = 0;       // [Nm] at the main rotor shaft
    Real torque_tailrotor_tr = 0;       // [Nm] at the tail rotor shaft
    Real torque_propeller_pr = 0;       // [Nm] at the propeller shaft
};

// Results that belong to one evaluation of the derivative (for the logger, the sound and the post processing).
struct DrivetrainEvaluation {
    DrivetrainState derivative;
    Real V_s_raw;            // [V] governor output before the saturation
    Real V_s_out;            // [V] voltage that is applied to the motor
    bool saturated;          // governor output is limited (the integrator is frozen)
    bool freewheeling;       // the motor side runs slower than the rotor: no torque transmitted
    Real T_freewheel_mr;     // [Nm] freewheel torque on the main rotor shaft
    Real T_load_mr;          // [Nm] total load torque on the main rotor shaft (including friction)
    Real I_mo;               // [A]
    Real V_mo;               // [V]
    Real P_mo_el;            // [W]
    Real P_mo_mech;          // [W]
    Real eta_mo;             // [-]
};

struct GovernorOutput {
    Real V_s_raw;
    Real V_s_out;
    bool saturated;
    Real integral_rate;  // d(integral)/dt: the speed error, 0 while saturated (anti windup)
};

// PI speed governor with saturation and anti windup (the governor interface of the drivetrain). `direction` follows
// invert_mainrotor_rotation_direction.
[[nodiscard]] GovernorOutput governor_pi(const DrivetrainParams& p, Real omega_mo_target, Real omega_mo, Real integral) noexcept;

// Motor speed setpoint from the main rotor speed setpoint [rad/s]: signed by the rotation direction, multiplied by the gear ratio.
[[nodiscard]] Real motor_speed_target(const DrivetrainParams& p, Real mainrotor_speed_target) noexcept;

// Linear ramp of the setpoint from 0 (motor start) to the target, reaching it after 1 / factor seconds. Call once per step.
[[nodiscard]] Real soft_start_step(Real current, Real target, Real dt, Real factor) noexcept;

[[nodiscard]] DrivetrainEvaluation evaluate_drivetrain(const DrivetrainParams& p, const DrivetrainInputs& in, const DrivetrainState& x) noexcept;

[[nodiscard]] DrivetrainState drivetrain_derivative(const DrivetrainParams& p, const DrivetrainInputs& in, const DrivetrainState& x) noexcept;

// True while the freewheel is open (Omega_mg - Omega_mr has the wrong sign for the driving direction).
[[nodiscard]] bool is_freewheeling(const DrivetrainParams& p, const DrivetrainState& x) noexcept;

// Between two RK4 steps: while the freewheel is open the motor angle is pulled to the rotor angle so that the angles
// agree when the freewheel locks again; a disabled motor resets the governor integral.
void drivetrain_post_step(const DrivetrainParams& p, const DrivetrainInputs& in, DrivetrainState& x) noexcept;

}  // namespace hoverhere::dynamics
