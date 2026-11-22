#include <algorithm>
#include <cmath>
#include <filesystem>
#include <numbers>
#include <optional>

#include <doctest/doctest.h>

#include "hoverhere/aero/rotor_bemt.hpp"
#include "hoverhere/aero/rotor_classic.hpp"
#include "hoverhere/control/flight_controller.hpp"
#include "hoverhere/core/frames.hpp"
#include "hoverhere/io/param_loader.hpp"
#include "hoverhere/params/params.hpp"

namespace fs = std::filesystem;
namespace io = hoverhere::io;
namespace frames = hoverhere::frames;
namespace kin = hoverhere::kinematics;
namespace qvm = boost::qvm;
using hoverhere::Real;
using hoverhere::math::Mat3;
using hoverhere::math::Vec3;
using namespace hoverhere::control;
using namespace hoverhere::aero;

namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;
constexpr Real kDeg = kPi / 180;

const hoverhere::params::ModelParamsSI& default_params()
{
    static const hoverhere::params::ModelParamsSI p = hoverhere::params::compile_to_si(hoverhere::params::make_default_params());
    return p;
}

fs::path logo_resources()
{
    return fs::path(HOVERHERE_UNITY_ASSETS_DIR) / "Helicopters_Available" / "Logo600SE_V3" / "Resources";
}

const std::optional<hoverhere::params::mechanics::MainrotorMechanics>& main_mechanics()
{
    static const std::optional<hoverhere::params::mechanics::MainrotorMechanics> m = []() -> std::optional<hoverhere::params::mechanics::MainrotorMechanics> {
        const fs::path file = logo_resources() / "Logo600SE_V3_mainrotor_mechanics.xml";
        if (!fs::exists(file)) {
            return std::nullopt;
        }
        auto target = hoverhere::params::mechanics::make_default_mainrotor_mechanics();
        return io::load_mainrotor_mechanics_file(file, target).ok() ? std::optional{target} : std::nullopt;
    }();
    return m;
}

const std::optional<hoverhere::params::mechanics::TailrotorMechanics>& tail_mechanics()
{
    static const std::optional<hoverhere::params::mechanics::TailrotorMechanics> m = []() -> std::optional<hoverhere::params::mechanics::TailrotorMechanics> {
        const fs::path file = logo_resources() / "Logo600SE_V3_tailrotor_mechanics.xml";
        if (!fs::exists(file)) {
            return std::nullopt;
        }
        auto target = hoverhere::params::mechanics::make_default_tailrotor_mechanics();
        return io::load_tailrotor_mechanics_file(file, target).ok() ? std::optional{target} : std::nullopt;
    }();
    return m;
}

#define REQUIRE_MECHANICS()                                          \
    if (!main_mechanics() || !tail_mechanics()) {                    \
        WARN_MESSAGE(false, "Unity assets not found, test skipped"); \
        return;                                                      \
    }

// simple controller with round numbers: the same gains in every bank
FblConfig simple_config()
{
    FblConfig c{};
    c.fbl_K_a = {2, 2, 2};
    c.fbl_K_p = {0.1, 0.1, 0.1};
    c.fbl_K_I = {0, 0, 0};
    c.fbl_K_d = {0, 0, 0};
    c.gyro_K_a = {4, 4, 4};
    c.gyro_K_p = {0.2, 0.2, 0.2};
    c.gyro_K_I = {0, 0, 0};
    c.gyro_K_d = {0, 0, 0};
    c.target_rpm = {150, 170, 190};
    c.speed_below_which_integrators_decay = 0.1 * 150;
    return c;
}

ControllerInputs flying(const Real roll = 0, const Real yaw = 0, const Real pitch = 0, const Real collective = 0)
{
    ControllerInputs in{};
    in.omega_mr = 150;
    in.rc.roll = roll;
    in.rc.yaw = yaw;
    in.rc.pitch = pitch;
    in.rc.collective = collective;
    return in;
}

FblController controller(const FblConfig& config)
{
    return FblController(config, std::nullopt, kin::make_default_ccpm_mixer());
}

}  // namespace

// ---------------------------------------------------------------------------------------------------------------------
// the controller laws
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the proportional part commands the rate error with the gains of its own axis")
{
    const FblController fbl = controller(simple_config());

    // roll: K_p (K_a stick - rate) = 0.1 (2 * 0.5 - 0.4)
    ControllerInputs in = flying(0.5);
    in.imu.gyro_B = Vec3{{0.4, 0, 0}};
    CHECK(fbl.evaluate(in, 0.001).normalized.lateral_mr == doctest::Approx(0.06));
    // the rate feeds back negatively: a fast roll against the stick reverses the command
    in.imu.gyro_B = Vec3{{1.5, 0, 0}};
    CHECK(fbl.evaluate(in, 0.001).normalized.lateral_mr == doctest::Approx(-0.05));

    // pitch (about z, flybarless gains) and yaw (about y, the tail gyro with its own gains)
    ControllerInputs pitch = flying(0, 0, -0.25);
    pitch.imu.gyro_B = Vec3{{0, 0, 0.2}};
    CHECK(fbl.evaluate(pitch, 0.001).normalized.longitudinal_mr == doctest::Approx(0.1 * (2 * -0.25 - 0.2)));
    ControllerInputs yaw = flying(0, 0.5, 0);
    yaw.imu.gyro_B = Vec3{{0, 1.0, 0}};
    CHECK(fbl.evaluate(yaw, 0.001).normalized.collective_tr == doctest::Approx(0.2 * (4 * 0.5 - 1.0)));

    // the axes do not couple
    CHECK(fbl.evaluate(yaw, 0.001).normalized.lateral_mr == 0);
    CHECK(fbl.evaluate(yaw, 0.001).normalized.longitudinal_mr == 0);

    // the collective and the propeller go straight through
    ControllerInputs col = flying();
    col.rc.collective = 0.7;
    col.rc.propeller = -0.3;
    col.rc.booster = 0.9;
    const ControllerOutputs out = fbl.evaluate(col, 0.001);
    CHECK(out.normalized.collective_mr == 0.7);
    CHECK(out.normalized.collective_pr == -0.3);
    CHECK(out.booster == 0.9);
}

TEST_CASE("the flight bank selects the gains and the rotor speed setpoint")
{
    FblConfig c = simple_config();
    c.fbl_K_a = {1, 2, 3};
    const FblController fbl = controller(c);
    for (int bank = 0; bank < 3; ++bank) {
        ControllerInputs in = flying(0.5);
        in.rc.flight_bank = bank;
        const ControllerOutputs out = fbl.evaluate(in, 0.001);
        CHECK(out.normalized.lateral_mr == doctest::Approx(0.1 * (bank + 1) * 0.5));
        CHECK(out.governor.rotor_speed_target == doctest::Approx(150 + 20 * bank));
    }
    ControllerInputs odd = flying(0.5);
    odd.rc.flight_bank = 7;  // out of range: the last bank
    CHECK(fbl.evaluate(odd, 0.001).governor.rotor_speed_target == doctest::Approx(190));
    odd.rc.motor_enabled = false;
    CHECK_FALSE(fbl.evaluate(odd, 0.001).governor.motor_enabled);
}

TEST_CASE("evaluate is pure: nothing is stored until post_step")
{
    FblConfig c = simple_config();
    c.fbl_K_I = {1, 1, 1};
    c.fbl_K_d = {1, 1, 1};
    c.gyro_K_I = {1, 1, 1};
    FblController fbl = controller(c);
    ControllerInputs in = flying(0.3, 0.1, -0.2);
    in.imu.gyro_B = Vec3{{0.1, 0.2, 0.3}};

    const ControllerOutputs a = fbl.evaluate(in, 0.002);
    const ControllerOutputs b = fbl.evaluate(in, 0.002);
    CHECK(a.normalized.lateral_mr == b.normalized.lateral_mr);
    CHECK(a.normalized.collective_tr == b.normalized.collective_tr);
    CHECK(a.sigma == b.sigma);
    for (int i = 0; i < 100; ++i) {  // an RK4 stage may evaluate as often as it likes
        (void)fbl.evaluate(in, 0.002);
    }
    CHECK(fbl.integrals() == std::array<Real, 3>{0, 0, 0});
    CHECK(fbl.evaluate(in, 0.002).normalized.lateral_mr == a.normalized.lateral_mr);

    fbl.post_step(in, 0.002);
    CHECK(fbl.integrals() != std::array<Real, 3>{0, 0, 0});
}

TEST_CASE("the integrators advance once per post step and stop at the output limit")
{
    FblConfig c = simple_config();
    c.fbl_K_I = {1, 1, 1};
    c.gyro_K_I = {1, 1, 1};
    FblController fbl = controller(c);
    const ControllerInputs in = flying(0.5, 0.25, -0.5);  // errors: roll 1.0, yaw 1.0, pitch -1.0 (rate zero)
    const Real dt = 0.01;
    for (int i = 0; i < 10; ++i) {
        fbl.post_step(in, dt);
    }
    const auto integrals = fbl.integrals();
    CHECK(integrals[0] == doctest::Approx(1.0 * 10 * dt));   // roll
    CHECK(integrals[1] == doctest::Approx(1.0 * 10 * dt));   // yaw
    CHECK(integrals[2] == doctest::Approx(-1.0 * 10 * dt));  // pitch
    // the integral part enters the command
    CHECK(fbl.evaluate(in, dt).normalized.lateral_mr == doctest::Approx(0.1 * 1.0 + 1.0 * 0.1));

    // anti windup: where the output is limited the integrator stands still
    FblConfig strong = simple_config();
    strong.fbl_K_p = {10, 10, 10};  // 10 * 1.0 saturates at once
    strong.fbl_K_I = {1, 1, 1};
    FblController limited = controller(strong);
    for (int i = 0; i < 50; ++i) {
        limited.post_step(in, dt);
    }
    CHECK(limited.integrals()[0] == 0);
    CHECK(limited.integrals()[2] == 0);
    CHECK(limited.evaluate(in, dt).normalized.lateral_mr == 1);
    CHECK(limited.evaluate(in, dt).normalized.longitudinal_mr == -1);
    ControllerInputs quiet = flying(0.01, 0, 0);  // the error becomes small: the integrator runs again
    limited.post_step(quiet, dt);
    CHECK(limited.integrals()[0] > 0);
}

TEST_CASE("the differential part reacts to a change of the error but not on the first sample")
{
    FblConfig c = simple_config();
    c.fbl_K_p = {0, 0, 0};
    c.fbl_K_d = {1, 1, 1};  // times 0.001 in the output
    c.d_filter_time_constant = 0;  // the raw difference quotient; the filter has its own test
    FblController fbl = controller(c);
    const Real dt = 0.01;

    CHECK(fbl.evaluate(flying(0.5), dt).normalized.lateral_mr == 0);  // no previous error: no derivative kick
    fbl.post_step(flying(0.5), dt);                                   // error 1.0 remembered
    // the error grows by 0.4 within 10 ms: derivative 40 / s, output 0.001 * 1 * 40
    CHECK(fbl.evaluate(flying(0.7), dt).normalized.lateral_mr == doctest::Approx(0.04));
    CHECK(fbl.evaluate(flying(0.3), dt).normalized.lateral_mr == doctest::Approx(-0.04));
    CHECK(fbl.evaluate(flying(0.5), dt).normalized.lateral_mr == 0);  // unchanged
}

TEST_CASE("the differential part is low passed with the time constant of the filter")
{
    FblConfig c = simple_config();
    c.fbl_K_p = {0, 0, 0};
    c.fbl_K_d = {1, 1, 1};
    c.d_filter_time_constant = 0.01;
    FblController fbl = controller(c);
    const Real dt = 0.01;
    const Real step = 1 - std::exp(-1.0);  // dt equals the time constant: 63 % of a step per sample

    fbl.post_step(flying(0.5), dt);  // error 1.0 remembered
    // the error jumps by 0.4: the raw derivative is 40 / s, the filtered one reaches 63 % of it at the first sample
    CHECK(fbl.evaluate(flying(0.7), dt).normalized.lateral_mr == doctest::Approx(0.04 * step));
    fbl.post_step(flying(0.7), dt);
    // the error stays: the raw derivative is 0, the filtered one decays with the same time constant
    CHECK(fbl.evaluate(flying(0.7), dt).normalized.lateral_mr == doctest::Approx(0.04 * step * std::exp(-1.0)));
}

TEST_CASE("direct feedthrough passes the sticks and the outputs are limited to one")
{
    FblConfig c = simple_config();
    c.direct_feedthrough_flybarless = true;
    c.direct_feedthrough_gyroscope = true;
    const FblController direct = controller(c);
    ControllerInputs in = flying(0.4, -0.6, 0.8);
    in.imu.gyro_B = Vec3{{3, 3, 3}};  // would command something very different
    const ControllerOutputs out = direct.evaluate(in, 0.001);
    CHECK(out.normalized.lateral_mr == 0.4);
    CHECK(out.normalized.collective_tr == -0.6);
    CHECK(out.normalized.longitudinal_mr == 0.8);

    FblConfig strong = simple_config();
    strong.fbl_K_p = {100, 100, 100};
    const FblController saturated = controller(strong);
    const ControllerOutputs big = saturated.evaluate(flying(1.0, 1.0, -1.0), 0.001);
    CHECK(big.normalized.lateral_mr == 1);
    CHECK(big.normalized.longitudinal_mr == -1);
    CHECK(big.normalized.collective_tr <= 1);
}

TEST_CASE("the tandem controller mixes the three controller outputs over both rotors")
{
    FblConfig c = simple_config();
    c.rotor_systems_configuration = 1;
    c.fbl_K_a = {1, 1, 1};
    c.fbl_K_p = {1, 1, 1};
    c.gyro_K_a = {1, 1, 1};
    c.gyro_K_p = {1, 1, 1};
    const FblController fbl = controller(c);

    // roll x = 0.5, yaw y = 0.25, pitch z = 0.5, collective 0.5 (all rates zero)
    ControllerInputs in = flying(0.5, 0.25, 0.5, 0.5);
    ControllerOutputs out = fbl.evaluate(in, 0.001);
    CHECK(out.normalized.lateral_mr == doctest::Approx(0.5 * 0.6 - 0.25 * 0.4));
    CHECK(out.normalized.lateral_tr == doctest::Approx(0.5 * 0.6 + 0.25 * 0.4));
    CHECK(out.normalized.collective_mr == doctest::Approx(+0.5 * 0.2 + 0.5 * 0.8));
    CHECK(out.normalized.collective_tr == doctest::Approx(-0.5 * 0.2 + 0.5 * 0.8));
    CHECK(out.normalized.longitudinal_mr == 0);
    CHECK(out.normalized.longitudinal_tr == 0);
    for (const Real servo : out.sigma) {
        CHECK(servo == 0);  // no servo kinematics for the tandem
    }

    c.direct_feedthrough_flybarless = true;
    out = controller(c).evaluate(in, 0.001);
    CHECK(out.normalized.lateral_mr == doctest::Approx(0.5 * 0.5 - 0.25 * 0.5));
    CHECK(out.normalized.collective_mr == doctest::Approx(0.5 * 0.25 + 0.5 * 0.75));
    c.direct_feedthrough_gyroscope = true;
    out = controller(c).evaluate(in, 0.001);
    CHECK(out.normalized.lateral_mr == doctest::Approx(0.5 * 0.5 - 0.25 * 0.5));
    CHECK(out.normalized.lateral_tr == doctest::Approx(0.5 * 0.5 + 0.25 * 0.5));
}

TEST_CASE("the integrators decay at low rotor speed and are cleared when the rotor stands still")
{
    FblConfig c = simple_config();
    c.initial_roll_integral = 0.2;
    c.initial_pitch_integral = -0.2;
    FblController fbl = controller(c);
    CHECK(fbl.integrals() == std::array<Real, 3>{0.2, 0, -0.2});  // trim after reset

    ControllerInputs nominal = flying();
    for (int i = 0; i < 100; ++i) {
        fbl.post_step(nominal, 0.001);  // at nominal speed with zero error nothing changes
    }
    CHECK(fbl.integrals()[0] == doctest::Approx(0.2));

    ControllerInputs spinning_up = flying();
    spinning_up.omega_mr = 0.05 * 150;  // below 10 % of the setpoint (flybarless.integrator_decay_rpm_ratio)
    for (int i = 0; i < 10; ++i) {
        fbl.post_step(spinning_up, 0.001);
    }
    CHECK(fbl.integrals()[0] == doctest::Approx(0.2 - 10 * 0.01));  // 0.01 per millisecond step towards zero
    CHECK(fbl.integrals()[2] == doctest::Approx(-0.2 + 10 * 0.01));

    // it never overshoots zero
    for (int i = 0; i < 1000; ++i) {
        fbl.post_step(spinning_up, 0.001);
    }
    CHECK(fbl.integrals()[0] == 0);
    CHECK(fbl.integrals()[2] == 0);

    // on the ground with a stopped rotor everything is cleared
    fbl.reset();
    ControllerInputs stopped = flying();
    stopped.omega_mr = 20 * frames::kRpmToRadPerSec;
    fbl.post_step(stopped, 0.001);
    CHECK(fbl.integrals() == std::array<Real, 3>{0, 0, 0});
    fbl.reset();
    CHECK(fbl.integrals() == std::array<Real, 3>{0.2, 0, -0.2});
}

TEST_CASE("the integrators stay active at 50 % of the nominal speed (autorotation) and decay at 2 % (on the ground)")
{
    const auto& p = default_params();
    FblConfig c = make_fbl_config(p);
    const Real nominal = c.target_rpm[0];
    c.initial_roll_integral = 0.2;
    c.initial_pitch_integral = -0.2;
    FblController fbl = controller(c);

    ControllerInputs autorotation = flying();
    autorotation.omega_mr = 0.5 * nominal;
    for (int i = 0; i < 100; ++i) {
        fbl.post_step(autorotation, 0.001);
    }
    CHECK(fbl.integrals()[0] == doctest::Approx(0.2));  // untouched, the controller keeps its full authority
    CHECK(fbl.integrals()[2] == doctest::Approx(-0.2));

    ControllerInputs ground = flying();
    ground.omega_mr = 0.02 * nominal;  // on the ground (below 60 rpm the integrators are cleared completely)
    fbl.post_step(ground, 0.001);
    CHECK(fbl.integrals()[0] == 0);
    CHECK(fbl.integrals()[2] == 0);

    // between the clearing limit and the decay ratio the integrators decay: raise the ratio to include 50 %
    c.speed_below_which_integrators_decay = 0.7 * nominal;
    FblController decaying = controller(c);
    decaying.post_step(autorotation, 0.001);
    CHECK(std::abs(decaying.integrals()[0]) < 0.2);
}

TEST_CASE("the configuration is built from the helicopter parameters")
{
    const auto& p = default_params();
    const FblConfig c = make_fbl_config(p);
    CHECK(c.fbl_K_p[0] == p.helicopter.flybarless.K_p[0]);
    CHECK(c.gyro_K_a[2] == p.helicopter.gyro.K_a[2]);
    CHECK(c.target_rpm[1] == p.helicopter.governor.target_rpm[1]);
    CHECK(c.speed_below_which_integrators_decay ==
          doctest::Approx(p.helicopter.flybarless.integrator_decay_rpm_ratio * p.helicopter.governor.target_rpm[0]));
    CHECK(p.helicopter.flybarless.integrator_decay_rpm_ratio == doctest::Approx(0.10));
    CHECK(c.collective_stroke_offset == doctest::Approx(c.collective_stroke * p.helicopter.mainrotor.Theta_col_0 / p.helicopter.mainrotor.K_col));
    CHECK(c.cyclic_throw_roll == doctest::Approx(15 * kDeg));
    CHECK(c.tail_throw == doctest::Approx(30 * kDeg));
}

// ---------------------------------------------------------------------------------------------------------------------
// controller -> servo angles -> kinematics
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the servo commands drive the swashplate mechanism to the commanded tilt and stroke")
{
    REQUIRE_MECHANICS();
    FblConfig c = simple_config();
    c.direct_feedthrough_flybarless = true;
    c.direct_feedthrough_gyroscope = true;
    c.collective_stroke_offset = 0.001;
    const FblController fbl(c, main_mechanics(), kin::make_default_ccpm_mixer());

    const ControllerOutputs out = fbl.evaluate(flying(0.6, 0.25, -0.4, 0.5), 0.001);
    const kin::SwashplatePose pose = kin::step2_servos_to_swashplate(*main_mechanics(), {out.sigma[0], out.sigma[1], out.sigma[2]});
    const kin::SwashplateCommand command = kin::swashplate_command_from_pose(*main_mechanics(), pose);
    CHECK(command.roll == doctest::Approx(c.cyclic_throw_roll * 0.6).epsilon(0.02));
    CHECK(command.pitch == doctest::Approx(c.cyclic_throw_pitch * -0.4).epsilon(0.02));
    CHECK(command.collective == doctest::Approx(c.collective_stroke * 0.5 + 0.001).epsilon(0.02));
    CHECK(out.sigma[3] == doctest::Approx(c.tail_throw * 0.25));
    CHECK(out.normalized.lateral_mr == 0.6);

    // without any command the servos sit at their neutral position
    const ControllerOutputs neutral = controller(c).evaluate(flying(), 0.001);
    CHECK(neutral.sigma[3] == 0);
    for (std::size_t i = 0; i < 3; ++i) {
        CHECK(std::abs(neutral.sigma[i]) < 10 * kDeg);
    }
}

TEST_CASE("without linkage geometry the ideal CCPM mixer reproduces the classic rotor control of Unity")
{
    // Unity classic path: Theta_col = K_col * deflection + Theta_col_0, Theta_cyc_a_s = A_lon * deflection, Theta_cyc_b_s = B_lat * deflection
    auto p = default_params();
    p.helicopter.mainrotor.Theta_col_0 = 3 * kDeg;
    p.helicopter.mainrotor.K_col = 12 * kDeg;
    p.helicopter.flapping.A_lon = 10 * kDeg;
    p.helicopter.flapping.B_lat = 10 * kDeg;
    FblConfig c = make_fbl_config(p);
    c.direct_feedthrough_flybarless = true;
    c.direct_feedthrough_gyroscope = true;
    const kin::CcpmMixer mixer = make_fallback_ccpm_mixer(p.helicopter, c);
    const FblController fbl(c, std::nullopt, mixer);

    for (const Real collective : {-1.0, -0.3, 0.0, 0.6, 1.0}) {
        for (const Real lateral : {-0.8, 0.0, 0.5}) {
            for (const Real longitudinal : {-0.5, 0.0, 1.0}) {
                const ControllerOutputs out = fbl.evaluate(flying(lateral, 0, longitudinal, collective), 0.001);
                const RotorControl control = classic_control_from_ccpm_servos(mixer, {out.sigma[0], out.sigma[1], out.sigma[2]});
                CHECK(control.Theta_col == doctest::Approx(12 * kDeg * collective + 3 * kDeg).epsilon(1e-9));
                CHECK(control.Theta_cyc_b_s == doctest::Approx(10 * kDeg * lateral).epsilon(1e-9).scale(1e-3));
                CHECK(control.Theta_cyc_a_s == doctest::Approx(10 * kDeg * longitudinal).epsilon(1e-9).scale(1e-3));
            }
        }
    }
}

TEST_CASE("the servos lag with an exact PT1 and the time constants come from the parameter file")
{
    ServoBank servos({0.1, 0.1, 0.1, 0.05});
    servos.reset({0, 0, 0, 0});
    const std::array<Real, 4> command{1.0, -1.0, 0.5, 2.0};
    for (int i = 0; i < 100; ++i) {  // 0.1 s = one time constant of the swashplate servos, two of the tail servo
        servos.post_step(command, 0.001);
    }
    CHECK(servos.sigma()[0] == doctest::Approx(1 - std::exp(-1.0)).epsilon(1e-12));
    CHECK(servos.sigma()[1] == doctest::Approx(-(1 - std::exp(-1.0))).epsilon(1e-12));
    CHECK(servos.sigma()[3] == doctest::Approx(2 * (1 - std::exp(-2.0))).epsilon(1e-12));

    // independent of the step size
    ServoBank coarse({0.1, 0.1, 0.1, 0.05});
    coarse.post_step(command, 0.1);
    CHECK(coarse.sigma()[0] == doctest::Approx(servos.sigma()[0]).epsilon(1e-12));

    hoverhere::params::si::ServoDamping damping{};
    damping.servo_col_mr_time_constant = 0.04;
    damping.servo_lat_mr_time_constant = 0.1;
    damping.servo_lon_mr_time_constant = 0.1;
    damping.servo_col_tr_time_constant = 0.025;
    const auto tau = servo_time_constants(damping);
    CHECK(tau[0] == doctest::Approx(0.08));
    CHECK(tau[2] == tau[0]);
    CHECK(tau[3] == doctest::Approx(0.025));
    damping.servo_col_tr_time_constant = 0.001;  // below 5 ms: instantaneous
    CHECK(servo_time_constants(damping)[3] == 0);
    ServoBank instant({0, 0, 0, 0});
    instant.post_step(command, 0.001);
    CHECK(instant.sigma() == command);
}

TEST_CASE("the blade pitch of the detailed mechanism and of the CCPM fallback follow the same servo angles")
{
    REQUIRE_MECHANICS();
    const kin::CcpmMixer mixer = kin::calibrate_ccpm_mixer(*main_mechanics());
    kin::SwashplateCommand command;
    command.collective = 0.002;
    command.roll = 4 * kDeg;
    command.pitch = -3 * kDeg;
    const std::array<Real, 3> sigma = kin::step1_swashplate_to_servos(*main_mechanics(), command).sigma;
    const auto detailed = blade_pitch_from_servos(main_mechanics(), mixer, sigma);
    const auto linear = blade_pitch_from_servos(std::nullopt, mixer, sigma);
    const auto pose = kin::step2_servos_to_swashplate(*main_mechanics(), sigma);
    CHECK(detailed == kin::step3_virtual_blade_angles(*main_mechanics(), pose));
    for (std::size_t c = 0; c < detailed.size(); ++c) {
        CHECK(std::abs(detailed[c] - linear[c]) < 0.5 * kDeg);
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// interface test: IFlightController -> servos -> kinematics -> rotor
// ---------------------------------------------------------------------------------------------------------------------
namespace {

// the servos follow the controller output for `seconds` at a fixed rc input: returns the settled servo angles
std::array<Real, 4> settle_servos(IFlightController& fc, ServoBank& servos, const ControllerInputs& in, const Real seconds = 1.0)
{
    const Real dt = 0.001;
    for (int i = 0; i < static_cast<int>(seconds / dt); ++i) {
        const ControllerOutputs out = fc.evaluate(in, dt);
        fc.post_step(in, dt);
        servos.post_step(out.sigma, dt);
    }
    return servos.sigma();
}

BemtInputs bemt_inputs(const BemtConfig& cfg, const std::array<Real, kBemtAzimuthCount>& blade_pitch, const Real omega)
{
    BemtInputs b{};
    b.body.A_OL = Mat3{{{1, 0, 0}, {0, 1, 0}, {0, 0, 1}}};
    b.disc = make_rotor_disc_state(frames::quat_identity(), 0, 0);
    b.omega_shaft = omega;
    b.blade_pitch = blade_pitch;
    b.rho_air = 1.27;
    b.mass_total = default_params().helicopter.mass_total;
    b.ground.hub_distance_to_ground = 50;
    (void)cfg;
    return b;
}

BemtOutputs run_bemt(const BemtConfig& cfg, const BemtInputs& in)
{
    const BemtGrid grid = make_bemt_grid(cfg);
    BemtFilterState state;
    RotorRng rng;
    BemtOutputs out{};
    for (int i = 0; i < 4; ++i) {
        out = bemt_evaluate(cfg, grid, state, in);
        bemt_post_step(cfg, state, in, out, 0.01 * (i + 1), 0.01, rng);
    }
    return out;
}

}  // namespace

TEST_CASE("sticks reach the BEMT rotor through the servos and the mechanism with the right sense")
{
    REQUIRE_MECHANICS();
    const auto& p = default_params();
    FblConfig fbl_config = make_fbl_config(p);
    fbl_config.direct_feedthrough_flybarless = true;  // the output is proportional to the stick, no rate feedback winds up
    BemtConfig bemt = make_bemt_config(p);
    bemt.quasi_static = true;
    const Real omega = fbl_config.target_rpm[0];

    // collective: more stick, more thrust
    Real previous_thrust = -1e9;
    for (const Real collective : {-0.5, 0.0, 0.25, 0.5, 0.75}) {
        FblController fbl(fbl_config, main_mechanics(), kin::make_default_ccpm_mixer());
        ServoBank servos(servo_time_constants(p.helicopter.servo_damping));
        const ControllerInputs in = [&] {
            ControllerInputs x = flying(0, 0, 0, collective);
            x.omega_mr = omega;
            return x;
        }();
        const auto sigma = settle_servos(fbl, servos, in);
        const auto beta = blade_pitch_from_servos(main_mechanics(), kin::make_default_ccpm_mixer(), {sigma[0], sigma[1], sigma[2]});
        const BemtOutputs out = run_bemt(bemt, bemt_inputs(bemt, beta, omega));
        INFO("collective stick " << collective << ": mean pitch " << beta[0] / kDeg << " deg, thrust " << out.thrust << " N");
        CHECK(out.force_O.a[1] > previous_thrust);
        previous_thrust = out.force_O.a[1];
        if (collective > 0.2) {
            CHECK(beta[0] > 0);
            CHECK(out.force_O.a[1] > 0);
        }
    }

    // Cyclic: the pitch links of the Logo head trail the blades by 90 degrees (articulated rotor, the flapping lag of the blades is
    // meant to supply the other 90). The BEMT disc is rigid, so the aerodynamic moment of a lateral (roll) swashplate tilt is about
    // the lateral axis z, that of a longitudinal (pitch) tilt about the longitudinal axis x; gyroscopic precession and the hub spring
    // turn these moments into a disc tilt. Roll right: blade pitch is largest at 180 deg azimuth (-x), the extra thrust there
    // gives a moment about -z; pitch forward: largest at 90 deg (-z), moment about +x.
    const auto moment = [&](const Real roll, const Real pitch) {
        FblController fbl(fbl_config, main_mechanics(), kin::make_default_ccpm_mixer());
        ServoBank servos(servo_time_constants(p.helicopter.servo_damping));
        ControllerInputs in = flying(roll, 0, pitch, 0.4);
        in.omega_mr = omega;
        const auto sigma = settle_servos(fbl, servos, in);
        const auto beta = blade_pitch_from_servos(main_mechanics(), kin::make_default_ccpm_mixer(), {sigma[0], sigma[1], sigma[2]});
        return run_bemt(bemt, bemt_inputs(bemt, beta, omega)).torque_D;
    };
    const Vec3 neutral = moment(0, 0);
    const Vec3 roll_right = moment(0.3, 0) - neutral;
    CHECK(roll_right.a[2] < -3);
    CHECK(std::abs(roll_right.a[0]) < 0.05 * std::abs(roll_right.a[2]));  // a pure moment about the lateral axis
    CHECK((moment(-0.3, 0) - neutral).a[2] > 3);
    CHECK(moment(0.3, 0).a[2] - neutral.a[2] == doctest::Approx(-(moment(-0.3, 0).a[2] - neutral.a[2])).epsilon(0.1));  // symmetric
    const Vec3 pitch_forward = moment(0, 0.3) - neutral;
    CHECK(pitch_forward.a[0] > 3);
    CHECK(std::abs(pitch_forward.a[2]) < 0.05 * std::abs(pitch_forward.a[0]));
    CHECK((moment(0, -0.3) - neutral).a[0] < -3);
    // both cyclic axes have the same authority
    CHECK(std::abs(roll_right.a[2]) == doctest::Approx(std::abs(pitch_forward.a[0])).epsilon(0.05));
}

TEST_CASE("the yaw stick turns the tail rotor mechanism and the classic tail rotor thrust the right way")
{
    REQUIRE_MECHANICS();
    const auto& p = default_params();
    FblConfig fbl_config = make_fbl_config(p);
    fbl_config.direct_feedthrough_gyroscope = true;  // the output is the stick: the gyro would fight the input of a helicopter that does not turn
    ClassicRotorConfig tail = make_classic_rotor_config(p, RotorKind::Tailrotor);
    const Real omega_tail = fbl_config.target_rpm[0] / p.helicopter.transmission.n_mr2tr;

    const auto loads = [&](const Real yaw_stick) {
        FblController fbl(fbl_config, main_mechanics(), kin::make_default_ccpm_mixer());
        ServoBank servos(servo_time_constants(p.helicopter.servo_damping));
        ControllerInputs in = flying(0, yaw_stick, 0, 0);
        in.omega_mr = fbl_config.target_rpm[0];
        const auto sigma = settle_servos(fbl, servos, in);
        const kin::TailrotorPose pose = kin::tailrotor_forward_kinematics(*tail_mechanics(), sigma[3], 0);
        ClassicRotorInputs rotor{};
        rotor.body.A_OL = Mat3{{{1, 0, 0}, {0, 1, 0}, {0, 0, 1}}};
        rotor.omega_shaft = omega_tail;
        rotor.control = classic_control_from_tailrotor(pose, p.helicopter.tailrotor.Theta_col_0);
        rotor.rho_air = 1.27;
        rotor.ground.hub_distance_to_ground = 50;
        return std::pair{rotor.control, classic_rotor_evaluate(tail, ClassicRotorFilterState{}, rotor)};
    };
    const auto [control_zero, zero] = loads(0);
    const auto [control_plus, plus] = loads(0.4);
    const auto [control_minus, minus] = loads(-0.4);

    // the pitch follows the stick like Theta_col = K_col * deflection + Theta_col_0 of the classic parameters (full stick = 30 deg servo)
    CHECK(control_plus.Theta_col > control_zero.Theta_col);
    CHECK(control_minus.Theta_col < control_zero.Theta_col);
    CHECK(control_zero.Theta_col == doctest::Approx(p.helicopter.tailrotor.Theta_col_0).epsilon(1e-2).scale(0.01));
    // the same order of magnitude as the K_col of the parameters (the tail of the Logo mechanism has 0.8 deg per degree of servo)
    CHECK(std::abs(control_plus.Theta_col - control_zero.Theta_col) == doctest::Approx(0.4 * p.helicopter.tailrotor.K_col).epsilon(0.25));

    // the thrust along the body +z pushes the tail to the right and yaws the nose to the left: +y
    CHECK(plus.force_O.a[2] > zero.force_O.a[2]);
    CHECK(minus.force_O.a[2] < zero.force_O.a[2]);
    CHECK(plus.torque_L.a[1] > zero.torque_L.a[1]);  // the moment of the tail thrust about the yaw axis
}
