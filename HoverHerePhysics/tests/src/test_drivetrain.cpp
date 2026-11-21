#include <algorithm>
#include <cmath>

#include <doctest/doctest.h>

#include "hoverhere/dynamics/drivetrain.hpp"
#include "hoverhere/numerics/rk4.hpp"
#include "hoverhere/params/params.hpp"

using hoverhere::Real;
using namespace hoverhere::dynamics;
namespace num = hoverhere::numerics;

namespace {

constexpr Real kRotorSpeed = 120.0;  // [rad/s]

// A 600 size electric helicopter, hand set so that the tests do not depend on the default parameter file.
DrivetrainParams electric_helicopter(const bool inverted = false)
{
    DrivetrainParams p{};
    p.R_a = 0.03;
    p.Ri = 0.02;
    p.B_M = 1e-4;
    p.k = 0.02;
    p.J_momg_mo = 6e-5;
    p.J_mrtrpr_mr = 0.05;
    p.n_mo2mr = 6.0;
    p.n_mr2tr = 4.5;
    p.n_mr2pr = 1.0;
    p.k_freewheel = 500;
    p.d_freewheel = 0.5;
    p.eta = 0.8;
    p.invert_mainrotor_rotation_direction = inverted;
    p.Kp = 0.1;
    p.Ki = 0.5;
    p.saturation_min = 0;
    p.saturation_max = 22;
    p.mainrotor_coulomb_friction = 0.3;
    p.motor_extra_damping = 0.001;
    p.motor_off_damping = 0.9;
    return p;
}

// aerodynamic torque on the shaft: grows with the square of the speed, always against the rotation (negative when driven)
Real rotor_torque(const Real omega_mr)
{
    return -5e-4 * omega_mr * std::abs(omega_mr);
}

// advances the drivetrain over `duration`; the load torque is a function of the rotor speed at the start of the RK4 step
DrivetrainState run(const DrivetrainParams& p, DrivetrainInputs in, DrivetrainState x, const Real duration, const Real dt = 5e-4)
{
    for (Real t = 0; t < duration - dt / 2; t += dt) {
        // the load is a pure function of the stage state
        const auto f = [&](const Real, const DrivetrainState& s) {
            DrivetrainInputs stage = in;
            stage.torque_mainrotor_mr = rotor_torque(s[drv::omega_mr]);
            return drivetrain_derivative(p, stage, s);
        };
        x = num::rk4_step(f, t, x, dt);
        drivetrain_post_step(p, in, x);
    }
    return x;
}

// omega' = -c omega^2 / J for the aerodynamic torque of rotor_torque() without any other torque on the shaft
Real coast_down_speed(const DrivetrainParams& p, const Real omega0, const Real t)
{
    return omega0 / (1 + 5e-4 * std::abs(omega0) * t / p.J_mrtrpr_mr);
}

DrivetrainState state(const Real omega_mo, const Real Omega_mo, const Real omega_mr, const Real Omega_mr, const Real integral = 0)
{
    return DrivetrainState{omega_mo, Omega_mo, omega_mr, Omega_mr, integral};
}

}  // namespace

TEST_CASE("the drivetrain parameters are built from the compiled helicopter parameters")
{
    const auto si = hoverhere::params::compile_to_si(hoverhere::params::make_default_params());
    const DrivetrainParams p = make_drivetrain_params(si.helicopter);
    CHECK(is_valid(p));
    CHECK(p.n_mo2mr == si.helicopter.transmission.n_mo2mr);
    CHECK(p.J_momg_mo == si.helicopter.transmission.J_momg_mo);
    CHECK(p.mainrotor_coulomb_friction == doctest::Approx(0.1 * si.helicopter.mass_total));
    CHECK_FALSE(is_valid(DrivetrainParams{}));
}

TEST_CASE("the freewheel locks and drives the rotor when the motor side is faster")
{
    const DrivetrainParams p = electric_helicopter();
    DrivetrainInputs in;
    in.omega_mo_target = motor_speed_target(p, kRotorSpeed);

    // start from standstill, the governor accelerates the motor and through the locked freewheel the rotor
    DrivetrainState x = state(0, 0, 0, 0);
    Real max_freewheel_torque = 0;
    bool open_while_driving = false;
    const Real dt = 5e-4;
    for (Real t = 0; t < 6.0; t += dt) {
        in.torque_mainrotor_mr = rotor_torque(x[drv::omega_mr]);
        const DrivetrainEvaluation e = evaluate_drivetrain(p, in, x);
        max_freewheel_torque = std::max(max_freewheel_torque, e.T_freewheel_mr);
        if (t > 0.5 && e.freewheeling) {
            open_while_driving = true;
        }
        const auto f = [&](const Real, const DrivetrainState& s) { return drivetrain_derivative(p, in, s); };
        x = num::rk4_step(f, t, x, dt);
        drivetrain_post_step(p, in, x);
    }

    CHECK(x[drv::omega_mr] == doctest::Approx(kRotorSpeed).epsilon(0.01));  // the governor reaches its target
    CHECK(x[drv::omega_mo] / p.n_mo2mr == doctest::Approx(x[drv::omega_mr]).epsilon(1e-3));  // locked: both sides run synchronously
    CHECK_FALSE(open_while_driving);
    CHECK(max_freewheel_torque > 1.0);                                   // torque was transmitted
    CHECK(x[drv::Omega_mo] / p.n_mo2mr - x[drv::Omega_mr] >= 0);           // wound up in the driving direction
}

TEST_CASE("the freewheel opens when the rotor runs faster than the motor side")
{
    DrivetrainParams p = electric_helicopter();
    p.mainrotor_coulomb_friction = 0;  // so that the free running rotor has no loss at all
    DrivetrainInputs in;
    in.motor_enabled = false;

    // spinning rotor, motor standing: autorotation / motor off
    const DrivetrainState x0 = state(0, 0, kRotorSpeed, 50.0);
    const DrivetrainEvaluation e0 = evaluate_drivetrain(p, in, x0);
    CHECK(e0.freewheeling);
    CHECK(e0.T_freewheel_mr == 0);
    CHECK(e0.derivative[drv::omega_mr] == 0);  // nothing drives or brakes the rotor
    CHECK(e0.derivative[drv::omega_mo] == doctest::Approx(0).scale(1.0));

    const DrivetrainState x1 = run(p, in, x0, 2.0);
    // the rotor only feels its own aerodynamic load: it follows the analytic coast down, the motor does not brake it
    CHECK(x1[drv::omega_mr] == doctest::Approx(coast_down_speed(p, kRotorSpeed, 2.0)).epsilon(1e-4));
    CHECK(std::abs(x1[drv::omega_mo]) < 1.0);       // and it does not drag the motor along
    CHECK(is_freewheeling(p, x1));
    // the open freewheel keeps the motor angle at the coupling position (no unbounded windup)
    CHECK(x1[drv::Omega_mo] / p.n_mo2mr - x1[drv::Omega_mr] <= 1e-9);
}

TEST_CASE("an open freewheel locks again when the motor catches up")
{
    DrivetrainParams p = electric_helicopter();
    DrivetrainInputs in;
    in.omega_mo_target = motor_speed_target(p, 100.0);

    // motor standing, rotor at 100 rad/s coasting, governor on: the motor spins up through the rotor speed and locks
    DrivetrainState x = state(0, 0, 100.0, 30.0);
    bool was_open = is_freewheeling(p, x);
    bool locked_later = false;
    const Real dt = 5e-4;
    for (Real t = 0; t < 3.0; t += dt) {
        in.torque_mainrotor_mr = rotor_torque(x[drv::omega_mr]);
        const auto f = [&](const Real, const DrivetrainState& s) { return drivetrain_derivative(p, in, s); };
        x = num::rk4_step(f, t, x, dt);
        drivetrain_post_step(p, in, x);
        if (t > 0.1 && !is_freewheeling(p, x)) {
            locked_later = true;
        }
    }
    CHECK(was_open);
    CHECK(locked_later);
    CHECK(x[drv::omega_mo] / p.n_mo2mr == doctest::Approx(x[drv::omega_mr]).epsilon(1e-3));
}

TEST_CASE("the freewheel only ever transmits torque in the driving direction")
{
    const DrivetrainParams p = electric_helicopter();
    DrivetrainInputs in;
    // wound up but the motor side is much slower: the damper alone would pull the rotor back, the pawl cannot
    const DrivetrainEvaluation e = evaluate_drivetrain(p, in, state(10, 6 * 0.001, 100.0, 0.0005));
    CHECK(e.T_freewheel_mr >= 0);
    CHECK(e.freewheeling);
}

TEST_CASE("the reversed rotor mirrors the freewheel behavior")
{
    DrivetrainParams p = electric_helicopter(true);
    p.mainrotor_coulomb_friction = 0;
    DrivetrainInputs in;
    in.motor_enabled = false;

    // rotor turns backwards, standing motor: open
    const DrivetrainState coasting = state(0, 0, -kRotorSpeed, -50.0);
    CHECK(is_freewheeling(p, coasting));
    const DrivetrainState x1 = run(p, in, coasting, 2.0);
    CHECK(x1[drv::omega_mr] == doctest::Approx(coast_down_speed(p, -kRotorSpeed, 2.0)).epsilon(1e-4));
    CHECK(std::abs(x1[drv::omega_mo]) < 1.0);

    // the motor drives backwards and takes the rotor along
    DrivetrainInputs drive;
    drive.omega_mo_target = motor_speed_target(p, kRotorSpeed);
    CHECK(drive.omega_mo_target < 0);
    const DrivetrainState x2 = run(p, drive, state(0, 0, 0, 0), 6.0);
    CHECK(x2[drv::omega_mr] == doctest::Approx(-kRotorSpeed).epsilon(0.01));
    CHECK(x2[drv::omega_mo] / p.n_mo2mr == doctest::Approx(x2[drv::omega_mr]).epsilon(1e-3));
}

TEST_CASE("post step resets the motor angle of an open freewheel and the integrator of a stopped motor")
{
    const DrivetrainParams p = electric_helicopter();
    DrivetrainInputs in;
    DrivetrainState x = state(5, 0.2, 100, 0.5, 3.0);  // Omega_mg = 0.033 < Omega_mr = 0.5: open
    drivetrain_post_step(p, in, x);
    CHECK(x[drv::Omega_mo] == doctest::Approx(0.5 * p.n_mo2mr));
    CHECK(x[drv::governor_int] == 3.0);

    in.motor_enabled = false;
    drivetrain_post_step(p, in, x);
    CHECK(x[drv::governor_int] == 0);

    // a locked freewheel is left alone
    DrivetrainState locked = state(700, 5.0, 100, 0.5, 1.0);
    in.motor_enabled = true;
    drivetrain_post_step(p, in, locked);
    CHECK(locked[drv::Omega_mo] == 5.0);
}

TEST_CASE("governor saturates, freezes the integrator and can be bypassed")
{
    const DrivetrainParams p = electric_helicopter();

    const GovernorOutput within = governor_pi(p, 700, 650, 1.0);  // Kp 50 + Ki 1 = 5.5 V
    CHECK(within.V_s_raw == doctest::Approx(0.1 * 50 + 0.5 * 1.0));
    CHECK(within.V_s_out == within.V_s_raw);
    CHECK_FALSE(within.saturated);
    CHECK(within.integral_rate == 50);

    const GovernorOutput high = governor_pi(p, 700, 0, 0.0);  // 70 V requested
    CHECK(high.saturated);
    CHECK(high.V_s_out == p.saturation_max);
    CHECK(high.integral_rate == 0);  // anti windup

    const GovernorOutput low = governor_pi(p, 0, 700, 0.0);  // negative request, the controller does not brake
    CHECK(low.saturated);
    CHECK(low.V_s_out == p.saturation_min);

    // the reversed rotor mirrors the limits
    const DrivetrainParams reversed = electric_helicopter(true);
    const GovernorOutput mirror = governor_pi(reversed, -700, 0, 0.0);
    CHECK(mirror.V_s_out == -reversed.saturation_max);

    DrivetrainInputs in;
    in.omega_mo_target = 700;
    in.voltage_override = 12.0;
    const DrivetrainEvaluation e = evaluate_drivetrain(p, in, state(100, 0, 20, 0));
    CHECK(e.V_s_out == 12.0);
    CHECK_FALSE(e.saturated);
    CHECK(e.derivative[drv::governor_int] == 0);
}

TEST_CASE("the motor setpoint is signed, geared and ramps up softly")
{
    const DrivetrainParams p = electric_helicopter();
    CHECK(motor_speed_target(p, 100) == doctest::Approx(600));
    CHECK(motor_speed_target(electric_helicopter(true), 100) == doctest::Approx(-600));

    Real target = 0;
    const Real dt = 0.01;
    const Real factor = 0.5;  // reaches the setpoint after 1 / factor = 2 s
    int steps = 0;
    while (target < 600 && steps < 1000) {
        target = soft_start_step(target, 600, dt, factor);
        ++steps;
    }
    CHECK(steps == doctest::Approx(200).epsilon(0.02));
    CHECK(soft_start_step(600, 600, dt, factor) == 600);
    CHECK(soft_start_step(-300, -600, dt, factor) == doctest::Approx(-300 - 600 * dt * factor));
    CHECK(soft_start_step(300, 0, dt, factor) == 0);
}

TEST_CASE("tail rotor and propeller loads act on the main rotor shaft through the gear ratio and the efficiency")
{
    DrivetrainParams p = electric_helicopter();
    p.mainrotor_coulomb_friction = 0;
    DrivetrainInputs in;
    in.motor_enabled = false;
    in.torque_mainrotor_mr = 1.0;
    in.torque_tailrotor_tr = 0.9;
    in.torque_propeller_pr = 0.4;
    const DrivetrainEvaluation e = evaluate_drivetrain(p, in, state(0, 0, 50, 10));  // open freewheel, standing motor
    CHECK(e.T_load_mr == doctest::Approx(1.0 + 0.9 / 4.5 / 0.8 + 0.4 / 1.0 / 0.8));
    CHECK(e.derivative[drv::omega_mr] == doctest::Approx(e.T_load_mr / p.J_mrtrpr_mr));
}
