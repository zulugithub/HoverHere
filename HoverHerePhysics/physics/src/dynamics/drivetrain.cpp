#include "hoverhere/dynamics/drivetrain.hpp"

#include <algorithm>
#include <cmath>

#include "hoverhere/core/frames.hpp"
#include "hoverhere/numerics/smooth_step.hpp"

namespace hoverhere::dynamics {

namespace {

using numerics::sign0;
using numerics::smooth_step;
using numerics::smooth_step_rise_fall;

constexpr Real kDefaultTransmissionEfficiency = 0.8;  // Helicopter_ODE.cs
constexpr Real kStandstillFrictionPerMass = 0.1;      // [Nm/kg] Helicopter_ODE.cs, friction of the standing main rotor

Real direction(const DrivetrainParams& p) noexcept
{
    return p.invert_mainrotor_rotation_direction ? Real{-1} : Real{1};
}

struct Freewheel {
    Real torque_mr;  // [Nm] transmitted torque on the main rotor shaft (>= 0 in the driving direction)
    bool open;
};

// The freewheel is a spring-damper that only transmits torque in the driving direction: it is open while the motor
// side lags behind the rotor (angle difference with the wrong sign) and when the damper alone would pull the rotor.
Freewheel freewheel(const DrivetrainParams& p, const DrivetrainState& x) noexcept
{
    const Real dir = direction(p);
    const Real delta_angle = x[drv::Omega_mo] / p.n_mo2mr - x[drv::Omega_mr];
    const Real delta_speed = x[drv::omega_mo] / p.n_mo2mr - x[drv::omega_mr];
    const Real torque = p.k_freewheel * delta_angle + p.d_freewheel * delta_speed;
    if (delta_angle * dir < 0 || torque * dir < 0) {
        return {0, true};
    }
    return {torque, false};
}

}  // namespace

DrivetrainParams make_drivetrain_params(const params::si::HelicopterParams& h) noexcept
{
    DrivetrainParams p{};
    p.R_a = h.brushless.R_a;
    p.Ri = h.accumulator.Ri;
    p.B_M = h.brushless.B_M;
    p.k = h.brushless.k;
    p.J_momg_mo = h.transmission.J_momg_mo;
    p.J_mrtrpr_mr = h.transmission.J_mrtrpr_mr;
    p.n_mo2mr = h.transmission.n_mo2mr;
    p.n_mr2tr = h.transmission.n_mr2tr;
    p.n_mr2pr = h.transmission.n_mr2pr;
    p.k_freewheel = h.transmission.k_freewheel;
    p.d_freewheel = h.transmission.d_freewheel;
    p.eta = kDefaultTransmissionEfficiency;
    p.invert_mainrotor_rotation_direction = h.transmission.invert_mainrotor_rotation_direction;
    p.Kp = h.governor.Kp;
    p.Ki = h.governor.Ki;
    p.saturation_min = h.governor.saturation_min;
    p.saturation_max = h.governor.saturation_max;
    p.mainrotor_coulomb_friction = kStandstillFrictionPerMass * h.mass_total;
    p.motor_extra_damping = 0.001;
    p.motor_off_damping = 0.9;
    return p;
}

bool is_valid(const DrivetrainParams& p) noexcept
{
    return p.R_a + p.Ri > 0 && p.J_momg_mo > 0 && p.J_mrtrpr_mr > 0 && p.n_mo2mr != 0 && p.n_mr2tr != 0 && p.n_mr2pr != 0 && p.eta > 0 &&
           p.k_freewheel >= 0 && p.d_freewheel >= 0 && p.saturation_max >= p.saturation_min;
}

GovernorOutput governor_pi(const DrivetrainParams& p, const Real omega_mo_target, const Real omega_mo, const Real integral) noexcept
{
    const Real error = omega_mo_target - omega_mo;
    GovernorOutput out{};
    out.V_s_raw = p.Kp * error + p.Ki * integral;
    out.V_s_out = out.V_s_raw;
    out.saturated = false;

    // the limits are positive voltages in the driving direction, so a reversed rotor mirrors them
    const Real dir = direction(p);
    const Real driving = dir * out.V_s_raw;
    if (driving > p.saturation_max) {
        out.V_s_out = dir * p.saturation_max;
        out.saturated = true;
    }
    else if (driving <= p.saturation_min) {
        out.V_s_out = dir * p.saturation_min;
        out.saturated = true;
    }
    out.integral_rate = out.saturated ? 0 : error;
    return out;
}

Real motor_speed_target(const DrivetrainParams& p, const Real mainrotor_speed_target) noexcept
{
    return direction(p) * p.n_mo2mr * mainrotor_speed_target;
}

Real soft_start_step(const Real current, const Real target, const Real dt, const Real factor) noexcept
{
    const Real step = target * dt * factor;
    if (target > 0) {
        return current >= target ? target : std::min(current + step, target);
    }
    if (target < 0) {
        return current <= target ? target : std::max(current + step, target);
    }
    return 0;
}

bool is_freewheeling(const DrivetrainParams& p, const DrivetrainState& x) noexcept
{
    return freewheel(p, x).open;
}

DrivetrainEvaluation evaluate_drivetrain(const DrivetrainParams& p, const DrivetrainInputs& in, const DrivetrainState& x) noexcept
{
    DrivetrainEvaluation e{};
    const Real omega_mo = x[drv::omega_mo];
    const Real omega_mr = x[drv::omega_mr];

    // governor (or the voltage of an external controller)
    GovernorOutput gov = governor_pi(p, in.omega_mo_target, omega_mo, x[drv::governor_int]);
    if (in.voltage_override) {
        gov = {*in.voltage_override, *in.voltage_override, false, 0};
    }
    e.V_s_raw = gov.V_s_raw;
    e.V_s_out = gov.V_s_out;
    e.saturated = gov.saturated;

    // loads of system 2 w.r.t. the main rotor shaft
    const Real T_tailrotor_mr = in.torque_tailrotor_tr / p.n_mr2tr / p.eta;
    const Real T_propeller_mr = in.torque_propeller_pr / p.n_mr2pr / p.eta;
    const Real rpm = frames::kRpmToRadPerSec;
    const Real T_friction_mr = -sign0(omega_mr) * p.mainrotor_coulomb_friction *
                               smooth_step_rise_fall(std::abs(omega_mr), 0, 0, 0.01 * rpm, 1, 300 * rpm, 400 * rpm, 0);
    e.T_load_mr = in.torque_mainrotor_mr + T_tailrotor_mr + T_propeller_mr + T_friction_mr;

    // freewheel and actio = reactio on the motor side
    const Freewheel fw = freewheel(p, x);
    e.freewheeling = fw.open;
    e.T_freewheel_mr = fw.torque_mr;
    const Real T_freewheel_mo = -fw.torque_mr / p.n_mo2mr / p.eta;

    // system 1: brushless motor
    const Real R_all = p.R_a + p.Ri;
    const Real friction_ramp = smooth_step(std::abs(omega_mo), -1, -1, 1, 1);  // 0 at standstill, 1 above 1 rad/s
    Real domega_mo;
    if (in.motor_enabled) {
        domega_mo = T_freewheel_mo / p.J_momg_mo + p.k / (p.J_momg_mo * R_all) * e.V_s_out -
                    (friction_ramp * p.B_M / p.J_momg_mo + p.k * p.k / (p.J_momg_mo * R_all)) * omega_mo - p.motor_extra_damping * omega_mo;
        e.I_mo = e.V_s_out / R_all - p.k / R_all * omega_mo;
        e.V_mo = e.V_s_out - p.Ri * e.I_mo;
        e.P_mo_el = e.I_mo * e.V_mo;
        e.P_mo_mech = omega_mo * T_freewheel_mo;
        e.eta_mo = e.P_mo_el > 1e-6 ? e.P_mo_mech / e.P_mo_el : 0;
    }
    else {
        domega_mo = T_freewheel_mo / p.J_momg_mo - sign0(omega_mo) * friction_ramp * p.B_M / p.J_momg_mo - p.motor_off_damping * omega_mo -
                    sign0(omega_mo) * smooth_step_rise_fall(std::abs(omega_mo), 0, 0, 0.5, 10, 20, 40, 0);
        e.I_mo = e.V_mo = e.P_mo_el = e.P_mo_mech = e.eta_mo = 0;
    }

    // system 2: main rotor shaft
    const Real domega_mr = (fw.torque_mr + e.T_load_mr) / p.J_mrtrpr_mr;

    e.derivative = DrivetrainState{domega_mo, omega_mo, domega_mr, omega_mr, in.motor_enabled ? gov.integral_rate : 0};
    return e;
}

DrivetrainState drivetrain_derivative(const DrivetrainParams& p, const DrivetrainInputs& in, const DrivetrainState& x) noexcept
{
    return evaluate_drivetrain(p, in, x).derivative;
}

void drivetrain_post_step(const DrivetrainParams& p, const DrivetrainInputs& in, DrivetrainState& x) noexcept
{
    if ((x[drv::Omega_mo] / p.n_mo2mr - x[drv::Omega_mr]) * direction(p) < 0) {
        x[drv::Omega_mo] = x[drv::Omega_mr] * p.n_mo2mr;
    }
    if (!in.motor_enabled) {
        x[drv::governor_int] = 0;
    }
}

}  // namespace hoverhere::dynamics
