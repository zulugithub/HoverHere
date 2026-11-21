#include <algorithm>
#include <cmath>
#include <filesystem>
#include <numbers>
#include <optional>

#include <doctest/doctest.h>

#include "hoverhere/core/frames.hpp"
#include "hoverhere/io/param_loader.hpp"
#include "hoverhere/kinematics/kinematics.hpp"

namespace fs = std::filesystem;
namespace io = hoverhere::io;
namespace frames = hoverhere::frames;
namespace qvm = boost::qvm;
using namespace hoverhere::kinematics;
using hoverhere::Real;

namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;
constexpr Real kDeg = kPi / 180;

fs::path logo_resources()
{
    return fs::path(HOVERHERE_UNITY_ASSETS_DIR) / "Helicopters_Available" / "Logo600SE_V3" / "Resources";
}

// The rotor mechanics of the Logo 600 as exported from Blender; empty when the Unity assets are not checked out.
const std::optional<MainrotorMechanics>& main_mechanics()
{
    static const std::optional<MainrotorMechanics> m = [] () -> std::optional<MainrotorMechanics> {
        const fs::path file = logo_resources() / "Logo600SE_V3_mainrotor_mechanics.xml";
        if (!fs::exists(file)) {
            return std::nullopt;
        }
        MainrotorMechanics target = hoverhere::params::mechanics::make_default_mainrotor_mechanics();
        return io::load_mainrotor_mechanics_file(file, target).ok() ? std::optional{target} : std::nullopt;
    }();
    return m;
}

const std::optional<TailrotorMechanics>& tail_mechanics()
{
    static const std::optional<TailrotorMechanics> m = [] () -> std::optional<TailrotorMechanics> {
        const fs::path file = logo_resources() / "Logo600SE_V3_tailrotor_mechanics.xml";
        if (!fs::exists(file)) {
            return std::nullopt;
        }
        TailrotorMechanics target = hoverhere::params::mechanics::make_default_tailrotor_mechanics();
        return io::load_tailrotor_mechanics_file(file, target).ok() ? std::optional{target} : std::nullopt;
    }();
    return m;
}

#define REQUIRE_MAIN_MECHANICS()                                                    \
    if (!main_mechanics()) {                                                        \
        WARN_MESSAGE(false, "Unity assets not found, test skipped");                \
        return;                                                                     \
    }                                                                               \
    const MainrotorMechanics& m = *main_mechanics()

#define REQUIRE_TAIL_MECHANICS()                                                    \
    if (!tail_mechanics()) {                                                        \
        WARN_MESSAGE(false, "Unity assets not found, test skipped");                \
        return;                                                                     \
    }                                                                               \
    const TailrotorMechanics& m = *tail_mechanics()

#define CHECK_NEAR(a, b, tol) CHECK(std::abs((a) - (b)) < (tol))

Real length(const Vec3& v)
{
    return qvm::mag(v);
}

Real max_abs_difference(const Vec3& a, const Vec3& b)
{
    return std::max({std::abs(a.a[0] - b.a[0]), std::abs(a.a[1] - b.a[1]), std::abs(a.a[2] - b.a[2])});
}

const SwashplateCommand kCommands[] = {
    {0, 0, 0},
    {0, 0, 0.003},
    {0, 0, -0.004},
    {8 * kDeg, 0, 0},
    {0, -10 * kDeg, 0},
    {-6 * kDeg, 7 * kDeg, 0.002},
    {12 * kDeg, 12 * kDeg, -0.003},
    {-14 * kDeg, -9 * kDeg, 0.0035},
};

}  // namespace

// ---------------------------------------------------------------------------------------------------------------------
// servo dynamics
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the PT1 servo is discretized exactly and independent of the step size")
{
    const Real T = 0.08;
    Real coarse = 0.2;
    coarse = pt1_step(coarse, 1.0, 0.1, T);
    Real fine = 0.2;
    for (int i = 0; i < 1000; ++i) {
        fine = pt1_step(fine, 1.0, 1e-4, T);
    }
    CHECK(coarse == doctest::Approx(fine).epsilon(1e-12));
    CHECK(coarse == doctest::Approx(1.0 - 0.8 * std::exp(-0.1 / T)).epsilon(1e-12));

    CHECK(pt1_step(0.3, 0.9, 0.01, 0.0) == 0.9);   // no lag: pass through
    CHECK(pt1_step(0.3, 0.9, 0.01, -1.0) == 0.9);
    CHECK(pt1_step(0.3, 0.9, 0.0, T) == 0.3);       // no time passes
}

// ---------------------------------------------------------------------------------------------------------------------
// main rotor: steps 1 - 3
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("step 1 produces servo angles that satisfy the rod and arm length constraints")
{
    REQUIRE_MAIN_MECHANICS();
    for (const SwashplateCommand& command : kCommands) {
        const Step1Result r = step1_swashplate_to_servos(m, command);
        CHECK(r.converged);
        CHECK(r.residual < 1e-9);
        for (std::size_t i = 0; i < kServoCount; ++i) {
            CHECK(std::abs(length(r.r_BiO_O[i] - m.limb_stationary[i].r_AiO_O) - m.limb_stationary[i].l_BiAi) < 1e-9);
            CHECK(std::abs(length(r.r_CiO_O[i] - r.r_BiO_O[i]) - m.limb_stationary[i].l_CiBi) < 1e-9);
            CHECK(std::abs(r.sigma[i]) < 90 * kDeg);
        }
    }
}

TEST_CASE("step 2 inverts step 1: servo angles give back the commanded swashplate")
{
    REQUIRE_MAIN_MECHANICS();
    for (const SwashplateCommand& command : kCommands) {
        const Step1Result s1 = step1_swashplate_to_servos(m, command);
        const SwashplatePose pose = step2_servos_to_swashplate(m, s1.sigma);
        INFO("roll " << command.roll / kDeg << " pitch " << command.pitch / kDeg << " collective " << command.collective);
        CHECK(pose.converged);
        CHECK(pose.residual < 5e-7);  // limited by the Blender export: the C_i lie up to 1.2e-7 m outside the swashplate plane
        const SwashplateCommand back = swashplate_command_from_pose(m, pose);
        // the exported servo frames are orthonormal to about 1e-5 only
        CHECK_NEAR(back.roll, command.roll, 2e-4);
        CHECK_NEAR(back.pitch, command.pitch, 2e-4);
        CHECK_NEAR(back.collective, command.collective, 2e-5);
        // and the pose is a proper swashplate frame with the planar joint constraint n_x of A_OLK = 0
        CHECK(std::abs(length(pose.normal_O) - 1) < 1e-12);
        CHECK(std::abs(pose.A_OLK.a[2][0]) < 1e-12);
        CHECK(std::abs(pose.r_KO_O.a[0]) == 0.0);
        CHECK(std::abs(pose.r_KO_O.a[2]) == 0.0);
    }
}

TEST_CASE("step 1 -> step 2 is the identity on the servo angles as well")
{
    REQUIRE_MAIN_MECHANICS();
    for (const SwashplateCommand& command : kCommands) {
        const Step1Result s1 = step1_swashplate_to_servos(m, command);
        const SwashplatePose pose = step2_servos_to_swashplate(m, s1.sigma);
        // re-run step 1 for the reconstructed swashplate: same servo angles (to 0.06 deg: the exported arm of servo 2 is 0.6 deg out of its plane)
        const Step1Result again = step1_swashplate_to_servos(m, swashplate_command_from_pose(m, pose));
        for (std::size_t i = 0; i < kServoCount; ++i) {
            CHECK_NEAR(again.sigma[i], s1.sigma[i], 1e-3);
        }
    }
}

TEST_CASE("the neutral swashplate is level and centred, with equal blade pitch at every azimuth")
{
    REQUIRE_MAIN_MECHANICS();
    const Step1Result s1 = step1_swashplate_to_servos(m, SwashplateCommand{});
    const SwashplatePose pose = step2_servos_to_swashplate(m, s1.sigma);
    CHECK(max_abs_difference(pose.normal_O, Vec3{{0, 1, 0}}) < 1e-4);
    CHECK_NEAR(pose.r_KO_O.a[1], m.r_KO_O.a[1], 1e-4);

    const auto beta = step3_virtual_blade_angles(m, pose);
    const auto [lo, hi] = std::minmax_element(beta.begin(), beta.end());
    CHECK(*hi - *lo < 0.05 * kDeg);
}

TEST_CASE("step 3 keeps the pitch link constraints for every shaft angle")
{
    REQUIRE_MAIN_MECHANICS();
    const SwashplatePose pose = step2_servos_to_swashplate(m, step1_swashplate_to_servos(m, SwashplateCommand{5 * kDeg, -8 * kDeg, 0.002}).sigma);
    for (int k = 0; k < 24; ++k) {
        const Real Omega = 2 * kPi * k / 24;
        const RotatingBlades blades = step3_swashplate_to_blades(m, pose, Omega);
        for (std::size_t i = 0; i < kRotatingLimbCount; ++i) {
            const BladeLinkage& b = blades.blade[i];
            CHECK(b.residual < 1e-9);
            CHECK(std::abs(length(b.r_EiO_O) - m.limb_rotating[i].l_EiO) < 1e-9);
            CHECK(std::abs(qvm::dot(b.r_EiO_O, b.n_blade_O)) < 1e-9);  // E moves on the circle normal to the blade axis
            CHECK(std::abs(length(b.r_DiO_O - pose.r_KO_O) - m.limb_rotating[i].l_DiJ) < 1e-7);
        }
    }
}

TEST_CASE("collective raises the pitch of all blades equally, cyclic modulates it once per revolution")
{
    REQUIRE_MAIN_MECHANICS();
    const auto pitch = [&](const SwashplateCommand& command) {
        return step3_virtual_blade_angles(m, step2_servos_to_swashplate(m, step1_swashplate_to_servos(m, command).sigma));
    };

    const auto low = pitch({0, 0, -0.003});
    const auto mid = pitch({0, 0, 0});
    const auto high = pitch({0, 0, 0.003});
    for (std::size_t c = 0; c < kVirtualBladeCount; ++c) {
        CHECK(low[c] < mid[c]);
        CHECK(mid[c] < high[c]);
    }
    const auto [lo, hi] = std::minmax_element(high.begin(), high.end());
    CHECK(*hi - *lo < 0.1 * kDeg);  // pure collective is the same at every azimuth

    // 10 stations: the mean over the revolution is the collective part, a cyclic input does not change it much
    const auto cyclic = pitch({6 * kDeg, 0, 0});
    Real mean = 0, amplitude_cos = 0, amplitude_sin = 0;
    for (std::size_t c = 0; c < kVirtualBladeCount; ++c) {
        const Real psi = 2 * kPi * static_cast<Real>(c) / kVirtualBladeCount;
        mean += cyclic[c] / kVirtualBladeCount;
        amplitude_cos += 2 * cyclic[c] * std::cos(psi) / kVirtualBladeCount;
        amplitude_sin += 2 * cyclic[c] * std::sin(psi) / kVirtualBladeCount;
    }
    const Real first_harmonic = std::hypot(amplitude_cos, amplitude_sin);
    CHECK(first_harmonic > 2 * kDeg);
    CHECK(std::abs(mean - mid[0]) < 0.1 * first_harmonic);
}

TEST_CASE("the physical blades and the azimuth stations agree")
{
    REQUIRE_MAIN_MECHANICS();
    const SwashplatePose pose = step2_servos_to_swashplate(m, step1_swashplate_to_servos(m, SwashplateCommand{4 * kDeg, 6 * kDeg, 0.001}).sigma);
    for (const Real Omega : {0.0, 0.7, 2.0, 4.1}) {
        const RotatingBlades blades = step3_swashplate_to_blades(m, pose, Omega);
        CHECK_NEAR(blades.blade[0].beta, step3_pitch_at_azimuth(m, pose, Omega), 1e-9);
        // the second blade of the two blade head sits 180 degrees further around
        CHECK_NEAR(blades.blade[1].beta, step3_pitch_at_azimuth(m, pose, Omega + kPi), 2e-3);
    }
}

TEST_CASE("the roundtrip commanded swashplate -> servos -> swashplate -> blades reproduces the blade pitch of the commanded swashplate")
{
    REQUIRE_MAIN_MECHANICS();
    // the commanded pose reconstructed by step 2 from the step 1 servo angles is the same pose, so step 3 must give
    // the same blade pitch as for a pose built directly from the command
    for (const SwashplateCommand& command : kCommands) {
        const Step1Result s1 = step1_swashplate_to_servos(m, command);
        SwashplatePose direct{};
        direct.normal_O = frames::rot_z(command.pitch) * (frames::rot_x(command.roll) * Vec3{{0, 1, 0}});
        direct.r_KO_O = Vec3{{0, m.r_KO_O.a[1] + command.collective, 0}};
        const SwashplatePose via_servos = step2_servos_to_swashplate(m, s1.sigma);
        for (const Real psi : {0.0, 1.0, 2.5, 4.0, 5.5}) {
            CHECK_NEAR(step3_pitch_at_azimuth(m, via_servos, psi), step3_pitch_at_azimuth(m, direct, psi), 5e-4);
        }
    }
}

TEST_CASE("step 4 places the rotation driver joints at the documented distances")
{
    REQUIRE_MAIN_MECHANICS();
    const SwashplatePose pose = step2_servos_to_swashplate(m, step1_swashplate_to_servos(m, SwashplateCommand{5 * kDeg, -3 * kDeg, 0.001}).sigma);
    for (const Real Omega : {0.0, 1.1, 3.0, 5.0}) {
        const RotatingBlades blades = step3_swashplate_to_blades(m, pose, Omega);
        const auto drivers = step4_driver_visuals(m, pose, blades, Omega);
        for (std::size_t i = 0; i < kRotatingLimbCount; ++i) {
            CHECK(drivers[i].reachable);
            CHECK(std::abs(length(drivers[i].r_GiO_O - drivers[i].r_FiO_O) - m.driver_rotating[i].l_GiFi) < 1e-9);
            CHECK(std::abs(length(drivers[i].r_HiO_O - drivers[i].r_GiO_O) - m.driver_rotating[i].l_HiGi) < 1e-7);
            CHECK(std::abs(length(drivers[i].r_HiO_O - pose.r_KO_O) - m.driver_rotating[i].l_HiJ) < 1e-9);
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// ideal linear CCPM mixer
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the CCPM mixer is exactly invertible")
{
    const CcpmMixer mixer = make_default_ccpm_mixer();
    for (const SwashplateCommand& command : kCommands) {
        const SwashplateCommand back = ccpm_swashplate(mixer, ccpm_servos(mixer, command));
        CHECK(back.roll == doctest::Approx(command.roll).scale(1.0).epsilon(1e-12));
        CHECK(back.pitch == doctest::Approx(command.pitch).scale(1.0).epsilon(1e-12));
        CHECK(back.collective == doctest::Approx(command.collective).scale(1.0).epsilon(1e-12));
    }
    // collective moves all servos together, a cyclic input sums to zero over the three servos
    const auto collective = ccpm_servos(mixer, SwashplateCommand{0, 0, 0.002});
    CHECK(collective[0] == doctest::Approx(collective[1]).epsilon(1e-12));
    CHECK(collective[1] == doctest::Approx(collective[2]).epsilon(1e-12));
    const auto tilt = ccpm_servos(mixer, SwashplateCommand{0.1, -0.05, 0});
    CHECK(tilt[0] + tilt[1] + tilt[2] == doctest::Approx(0).scale(1.0).epsilon(1e-12));
}

TEST_CASE("the calibrated CCPM mixer follows the detailed mechanism for moderate inputs")
{
    REQUIRE_MAIN_MECHANICS();
    const CcpmMixer mixer = calibrate_ccpm_mixer(m);
    const CcpmMixer analytic = make_default_ccpm_mixer();

    // the analytic estimate from the Logo geometry (arm 17 mm, circles 36 / 24 mm, pitch arm 31 mm) is close to the fit
    CHECK(mixer.k_collective == doctest::Approx(analytic.k_collective).epsilon(0.1));
    CHECK(mixer.k_tilt == doctest::Approx(analytic.k_tilt).epsilon(0.1));
    CHECK(mixer.c_collective == doctest::Approx(analytic.c_collective).epsilon(0.1));
    CHECK(mixer.c_cyclic == doctest::Approx(analytic.c_cyclic).epsilon(0.15));
    CHECK(std::abs(std::remainder(mixer.phi - analytic.phi, 2 * kPi)) < 10 * kDeg);

    for (const SwashplateCommand& command : {SwashplateCommand{5 * kDeg, -4 * kDeg, 0.0015}, SwashplateCommand{-3 * kDeg, 6 * kDeg, -0.002}}) {
        const Step1Result s1 = step1_swashplate_to_servos(m, command);
        const auto linear = ccpm_servos(mixer, command);
        for (std::size_t i = 0; i < kServoCount; ++i) {
            CHECK(std::abs(linear[i] - s1.sigma[i]) < 1.0 * kDeg);
        }
        const SwashplatePose pose = step2_servos_to_swashplate(m, s1.sigma);
        for (const Real psi : {0.0, 1.3, 2.9, 4.4}) {
            CHECK(std::abs(ccpm_pitch_at_azimuth(mixer, command, psi) - step3_pitch_at_azimuth(m, pose, psi)) < 0.5 * kDeg);
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// tail rotor
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("tail rotor kinematics keep the lever and rod lengths")
{
    REQUIRE_TAIL_MECHANICS();
    for (const Real sigma_deg : {-30.0, -15.0, 0.0, 12.0, 30.0}) {
        const TailrotorPose pose = tailrotor_forward_kinematics(m, sigma_deg * kDeg, 0.7);
        INFO("servo " << sigma_deg << " deg");
        const auto& s = m.limb_stationary;
        CHECK(pose.reachable);
        CHECK(std::abs(length(pose.r_BO_O - s.r_AO_O) - s.l_BA) < 1e-6);
        CHECK(std::abs(length(pose.r_CO_O - pose.r_BO_O) - s.l_CB) < 1e-9);
        CHECK(std::abs(length(pose.r_CO_O - s.r_DO_O) - s.l_CD) < 1e-9);
        CHECK(std::abs(length(pose.r_EO_O - s.r_DO_O) - s.l_ED) < 1e-6);
        CHECK(pose.r_KO_O.a[0] == 0.0);
        CHECK(pose.r_KO_O.a[2] == 0.0);
    }
}

TEST_CASE("tail rotor blade pitch changes monotonically with the servo angle and is the same for all blades")
{
    REQUIRE_TAIL_MECHANICS();
    Real previous = 0;
    bool first = true;
    Real direction = 0;
    for (Real sigma_deg = -30; sigma_deg <= 30.01; sigma_deg += 5) {
        const TailrotorPose pose = tailrotor_forward_kinematics(m, sigma_deg * kDeg, 0);
        CHECK(pose.blade[0].beta == pose.blade[1].beta);
        if (!first) {
            const Real delta = pose.blade[0].beta - previous;
            CHECK(std::abs(delta) > 1e-4);  // it does change
            if (direction == 0) {
                direction = delta > 0 ? 1.0 : -1.0;
            }
            CHECK(delta * direction > 0);
        }
        previous = pose.blade[0].beta;
        first = false;
    }
    const TailrotorPose lo = tailrotor_forward_kinematics(m, -30 * kDeg, 0);
    const TailrotorPose hi = tailrotor_forward_kinematics(m, +30 * kDeg, 0);
    CHECK(std::abs(hi.blade[0].beta - lo.blade[0].beta) > 5 * kDeg);   // a few tens of degrees of travel
    CHECK(std::abs(hi.blade[0].beta - lo.blade[0].beta) < 90 * kDeg);
}

TEST_CASE("the shaft angle rotates the rotating tail rotor parts about the shaft axis")
{
    REQUIRE_TAIL_MECHANICS();
    const Real sigma = 10 * kDeg;
    const TailrotorPose base = tailrotor_forward_kinematics(m, sigma, 0);
    for (const Real Omega : {0.4, 1.9, 3.5}) {
        const TailrotorPose turned = tailrotor_forward_kinematics(m, sigma, Omega);
        CHECK(turned.blade[0].beta == base.blade[0].beta);  // the blade pitch does not depend on the shaft angle
        CHECK(turned.stroke == base.stroke);
        const auto A = frames::rot_y(Omega);
        for (std::size_t i = 0; i < kRotatingLimbCount; ++i) {
            CHECK(max_abs_difference(turned.blade[i].r_HiO_O, A * base.blade[i].r_HiO_O) < 1e-12);
            CHECK(max_abs_difference(turned.blade[i].n_blade_O, A * base.blade[i].n_blade_O) < 1e-12);
            CHECK(max_abs_difference(turned.blade[i].r_GiO_O, A * base.blade[i].r_GiO_O + Vec3{{0, 0, 0}}) < 1e-12 + 1e-12 * std::abs(turned.stroke));
        }
        CHECK(max_abs_difference(turned.r_BO_O, base.r_BO_O) == 0.0);  // the stationary side does not move
    }
}
