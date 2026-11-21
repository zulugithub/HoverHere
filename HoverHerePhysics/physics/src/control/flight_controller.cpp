#include "hoverhere/control/flight_controller.hpp"

#include <algorithm>
#include <cmath>
#include <numbers>

#include "hoverhere/core/frames.hpp"
#include "hoverhere/numerics/smooth_step.hpp"

namespace hoverhere::control {

using numerics::sign0;

namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;

Real limit_unit(const Real x, bool& saturated) noexcept
{
    if (x > 1) {
        saturated = true;
        return 1;
    }
    if (x < -1) {
        saturated = true;
        return -1;
    }
    return x;
}

}  // namespace

// ----------------------------------------------------------------------------------------------------------------
// configuration
// ----------------------------------------------------------------------------------------------------------------
FblConfig make_fbl_config(const params::si::ModelParams& p) noexcept
{
    const params::si::HelicopterParams& h = p.helicopter;
    FblConfig c{};
    c.rotor_systems_configuration = h.rotor_systems_configuration;
    c.fbl_K_a = h.flybarless.K_a;
    c.fbl_K_I = h.flybarless.K_I;
    c.fbl_K_p = h.flybarless.K_p;
    c.fbl_K_d = h.flybarless.K_d;
    c.gyro_K_a = h.gyro.K_a;
    c.gyro_K_I = h.gyro.K_I;
    c.gyro_K_p = h.gyro.K_p;
    c.gyro_K_d = h.gyro.K_d;
    c.direct_feedthrough_flybarless = h.flybarless.direct_feedthrough_flybarless;
    c.direct_feedthrough_gyroscope = h.gyro.direct_feedthrough_gyroscope;
    c.target_rpm = h.governor.target_rpm;
    c.initial_roll_integral = h.flybarless.initial_roll_angle;
    c.initial_pitch_integral = h.flybarless.initial_pitch_angle;
    c.collective_stroke_offset = h.mainrotor.K_col != 0 ? c.collective_stroke * h.mainrotor.Theta_col_0 / h.mainrotor.K_col : Real{0};
    c.speed_below_which_integrators_decay = Real{0.7} * h.governor.target_rpm[0];
    return c;
}

kinematics::CcpmMixer make_fallback_ccpm_mixer(const params::si::HelicopterParams& h, const FblConfig& fbl) noexcept
{
    kinematics::CcpmMixer mixer = kinematics::make_default_ccpm_mixer();
    mixer.beta_0 = 0;
    mixer.c_collective = fbl.collective_stroke != 0 ? h.mainrotor.K_col / fbl.collective_stroke : Real{0};
    mixer.c_cyclic = fbl.cyclic_throw_roll != 0 ? Real{0.5} * (h.flapping.A_lon + h.flapping.B_lat) / fbl.cyclic_throw_roll : Real{0};
    return mixer;
}

// ----------------------------------------------------------------------------------------------------------------
// FBL controller
// ----------------------------------------------------------------------------------------------------------------
FblController::FblController(const FblConfig& config, const std::optional<params::mechanics::MainrotorMechanics>& mechanics, const kinematics::CcpmMixer& fallback_mixer)
    : cfg_(config), mechanics_(mechanics), mixer_(fallback_mixer)
{
    reset();
}

void FblController::reset()
{
    integral_ = {cfg_.initial_roll_integral, 0, cfg_.initial_pitch_integral};
    error_previous_ = {0, 0, 0};
    has_previous_ = false;
}

FblController::Evaluation FblController::compute(const ControllerInputs& in, const Real dt) const noexcept
{
    const RcInputs& rc = in.rc;
    const std::size_t bank = static_cast<std::size_t>(std::clamp(rc.flight_bank, 0, 2));
    const bool tandem = cfg_.rotor_systems_configuration == 1;

    // channels: 0 roll (flybarless), 1 yaw (tail gyro), 2 pitch (flybarless)
    const std::array<Real, 3> stick{rc.roll, rc.yaw, rc.pitch};
    const std::array<Real, 3> rate{in.imu.gyro_B.a[0], in.imu.gyro_B.a[1], in.imu.gyro_B.a[2]};
    const std::array<Real, 3> K_a{cfg_.fbl_K_a[bank], cfg_.gyro_K_a[bank], cfg_.fbl_K_a[bank]};
    const std::array<Real, 3> K_p{cfg_.fbl_K_p[bank], cfg_.gyro_K_p[bank], cfg_.fbl_K_p[bank]};
    const std::array<Real, 3> K_I{cfg_.fbl_K_I[bank], cfg_.gyro_K_I[bank], cfg_.fbl_K_I[bank]};
    const std::array<Real, 3> K_d{cfg_.fbl_K_d[bank], cfg_.gyro_K_d[bank], cfg_.fbl_K_d[bank]};

    Evaluation e{};
    std::array<Real, 3> pid{};
    for (std::size_t ch = 0; ch < 3; ++ch) {
        e.error[ch] = K_a[ch] * stick[ch] - rate[ch];
        const Real derivative = has_previous_ && dt > 0 ? (e.error[ch] - error_previous_[ch]) / dt : Real{0};
        pid[ch] = K_p[ch] * e.error[ch] + K_I[ch] * integral_[ch];
        if (!tandem) {
            pid[ch] += K_d[ch] * Real{0.001} * derivative;  // the tandem controller has no differential part
        }
    }

    NormalizedCommands& c = e.commands;
    if (!tandem) {
        // single main rotor: roll -> lateral cyclic, pitch -> longitudinal cyclic, yaw -> tail collective
        Real lateral = cfg_.direct_feedthrough_flybarless ? rc.roll : pid[0];
        Real longitudinal = cfg_.direct_feedthrough_flybarless ? rc.pitch : pid[2];
        Real tail = cfg_.direct_feedthrough_gyroscope ? rc.yaw : pid[1];
        c.lateral_mr = limit_unit(lateral, e.saturated[0]);
        c.collective_tr = limit_unit(tail, e.saturated[1]);
        c.longitudinal_mr = limit_unit(longitudinal, e.saturated[2]);
        c.collective_mr = rc.collective;
        c.collective_pr = rc.propeller;
    }
    else {
        // tandem rotor: the controller outputs are mixed over the two rotors
        const Real x = limit_unit(pid[0], e.saturated[0]);
        const Real y = limit_unit(pid[1], e.saturated[1]);
        const Real z = limit_unit(pid[2], e.saturated[2]);
        c.lateral_mr = x * Real{0.6} - y * Real{0.4};
        c.lateral_tr = x * Real{0.6} + y * Real{0.4};
        c.collective_mr = +z * Real{0.2} + rc.collective * Real{0.8};
        c.collective_tr = -z * Real{0.2} + rc.collective * Real{0.8};
        if (cfg_.direct_feedthrough_flybarless) {
            c.lateral_mr = rc.roll * Real{0.5} - y * Real{0.5};
            c.lateral_tr = rc.roll * Real{0.5} + y * Real{0.5};
            c.collective_mr = +rc.pitch * Real{0.25} + rc.collective * Real{0.75};
            c.collective_tr = -rc.pitch * Real{0.25} + rc.collective * Real{0.75};
        }
        if (cfg_.direct_feedthrough_gyroscope) {
            c.lateral_mr = x * Real{0.5} - rc.yaw * Real{0.5};
            c.lateral_tr = x * Real{0.5} + rc.yaw * Real{0.5};
        }
        if (cfg_.direct_feedthrough_flybarless && cfg_.direct_feedthrough_gyroscope) {
            c.lateral_mr = rc.roll * Real{0.5} - rc.yaw * Real{0.5};
            c.lateral_tr = rc.roll * Real{0.5} + rc.yaw * Real{0.5};
        }
        bool unused = false;
        for (Real* v : {&c.lateral_mr, &c.lateral_tr, &c.collective_mr, &c.collective_tr}) {
            *v = limit_unit(*v, unused);
        }
        c.collective_pr = rc.propeller;
    }
    return e;
}

ControllerOutputs FblController::evaluate(const ControllerInputs& in, const Real dt) const
{
    const Evaluation e = compute(in, dt);
    const NormalizedCommands& c = e.commands;

    ControllerOutputs out{};
    out.normalized = c;
    out.governor.rotor_speed_target = cfg_.target_rpm[static_cast<std::size_t>(std::clamp(in.rc.flight_bank, 0, 2))];
    out.governor.motor_enabled = in.rc.motor_enabled;
    out.booster = in.rc.booster;

    if (cfg_.rotor_systems_configuration == 0) {
        kinematics::SwashplateCommand swashplate;
        swashplate.roll = cfg_.cyclic_throw_roll * c.lateral_mr;
        swashplate.pitch = cfg_.cyclic_throw_pitch * c.longitudinal_mr;
        swashplate.collective = cfg_.collective_stroke * c.collective_mr + cfg_.collective_stroke_offset;
        const std::array<Real, kinematics::kServoCount> servos =
            mechanics_ ? kinematics::step1_swashplate_to_servos(*mechanics_, swashplate).sigma : kinematics::ccpm_servos(mixer_, swashplate);
        std::copy(servos.begin(), servos.end(), out.sigma.begin());
        out.sigma[3] = cfg_.tail_throw * c.collective_tr;
    }
    return out;
}

void FblController::post_step(const ControllerInputs& in, const Real dt)
{
    const Evaluation e = compute(in, dt);
    for (std::size_t ch = 0; ch < 3; ++ch) {
        if (!e.saturated[ch]) {  // anti windup: no integration while the output is limited
            integral_[ch] += e.error[ch] * dt;
        }
    }
    error_previous_ = e.error;
    has_previous_ = true;

    // At low rotor speed (on the ground) the integrators decay, and they are cleared when the rotor stands still.
    const Real speed = std::abs(in.omega_mr);
    if (speed < cfg_.speed_below_which_integrators_decay) {
        const Real scale = dt / kControllerReferenceStep;
        const std::array<Real, 3> decay{Real{0.01} * scale, Real{0.01} * Real{0.01} * scale, Real{0.01} * scale};
        for (std::size_t ch = 0; ch < 3; ++ch) {
            integral_[ch] = sign0(integral_[ch]) * std::max(Real{0}, std::abs(integral_[ch]) - decay[ch]);
        }
    }
    if (speed < 60 * frames::kRpmToRadPerSec) {
        integral_ = {0, 0, 0};
    }
}

// ----------------------------------------------------------------------------------------------------------------
// servos
// ----------------------------------------------------------------------------------------------------------------
void ServoBank::post_step(const std::array<Real, 4>& command, const Real dt) noexcept
{
    for (std::size_t i = 0; i < 4; ++i) {
        sigma_[i] = kinematics::pt1_step(sigma_[i], command[i], dt, tau_[i]);
    }
}

std::array<Real, 4> servo_time_constants(const params::si::ServoDamping& damping) noexcept
{
    constexpr Real min_time_constant = 0.005;  // [s] below: instantaneous
    const Real swashplate = (damping.servo_col_mr_time_constant + damping.servo_lat_mr_time_constant + damping.servo_lon_mr_time_constant) / 3;
    const auto usable = [](const Real tau) { return tau > min_time_constant ? tau : Real{0}; };
    return {usable(swashplate), usable(swashplate), usable(swashplate), usable(damping.servo_col_tr_time_constant)};
}

std::array<Real, kinematics::kVirtualBladeCount> blade_pitch_from_servos(const std::optional<params::mechanics::MainrotorMechanics>& mechanics,
                                                                           const kinematics::CcpmMixer& mixer, const std::array<Real, kinematics::kServoCount>& sigma) noexcept
{
    if (mechanics) {
        return kinematics::step3_virtual_blade_angles(*mechanics, kinematics::step2_servos_to_swashplate(*mechanics, sigma));
    }
    const kinematics::SwashplateCommand command = kinematics::ccpm_swashplate(mixer, sigma);
    std::array<Real, kinematics::kVirtualBladeCount> beta{};
    for (std::size_t c = 0; c < beta.size(); ++c) {
        beta[c] = kinematics::ccpm_pitch_at_azimuth(mixer, command, 2 * kPi * static_cast<Real>(c) / static_cast<Real>(beta.size()));
    }
    return beta;
}

}  // namespace hoverhere::control
