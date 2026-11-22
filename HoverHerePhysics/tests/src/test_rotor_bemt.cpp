#include <algorithm>
#include <cmath>
#include <fstream>
#include <numbers>
#include <sstream>
#include <string>
#include <vector>

#include <doctest/doctest.h>

#include "hoverhere/aero/rotor_bemt.hpp"
#include "hoverhere/core/frames.hpp"
#include "hoverhere/io/param_loader.hpp"
#include "hoverhere/numerics/rk4.hpp"
#include "hoverhere/params/params.hpp"

using hoverhere::Real;
using namespace hoverhere::aero;
using hoverhere::math::Mat3;
using hoverhere::math::Quat;
using hoverhere::math::Vec3;
namespace frames = hoverhere::frames;
namespace num = hoverhere::numerics;
namespace qvm = boost::qvm;

namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;
constexpr Real kDeg = kPi / 180;

// Mikado Logo 600 SE as in the Unity test export: R = 0.75 m, chord 55 mm, 2 blades, 4.5 kg, 1500 rpm
BemtConfig logo600()
{
    BemtConfig c{};
    c.rotor.R = 0.75;
    c.rotor.c = 0.055;
    c.rotor.b = 2;
    c.rotor.posLH = Vec3{{0, 0, 0}};
    c.rotor.oriLH = Vec3{{0, 0, 0}};
    return c;
}

constexpr Real kRotorSpeed = 1500 * kPi / 30;  // [rad/s]

BemtInputs hover_inputs(const Real collective, const Real vertical_velocity = 0)
{
    BemtInputs in{};
    in.body.position_O = Vec3{{0, 0, 0}};
    in.body.A_OL = Mat3{{{1, 0, 0}, {0, 1, 0}, {0, 0, 1}}};
    in.body.velocity_O = Vec3{{0, vertical_velocity, 0}};
    in.body.velocity_L = in.body.velocity_O;
    in.body.omega_L = Vec3{{0, 0, 0}};
    in.disc = make_rotor_disc_state(frames::quat_identity(), 0, 0);
    in.omega_shaft = kRotorSpeed;
    in.blade_pitch.fill(collective);
    in.rho_air = 1.27;
    in.mass_total = 4.5;
    in.ground.hub_distance_to_ground = 10;
    return in;
}

// evaluate and advance the slow states a few times with fixed inputs
BemtOutputs settle(const BemtConfig& cfg, const BemtGrid& grid, BemtFilterState& state, const BemtInputs& in, const int passes = 4, const Real dt = 0.01)
{
    RotorRng rng;
    BemtOutputs out{};
    for (int i = 0; i < passes; ++i) {
        out = bemt_evaluate(cfg, grid, state, in);
        bemt_post_step(cfg, state, in, out, dt * (i + 1), dt, rng);
    }
    return out;
}

struct ReferenceRow {
    Real collective_deg, vz, vi_mean, thrust, torque, vz_scaled;
};

std::vector<ReferenceRow> read_reference()
{
    std::vector<ReferenceRow> rows;
    std::ifstream in(std::string(HOVERHERE_TEST_DATA_DIR) + "/reference/bemt_logo600_quasi_static.csv");
    std::string line;
    std::getline(in, line);  // header
    while (std::getline(in, line)) {
        std::replace(line.begin(), line.end(), ',', ' ');
        std::istringstream s(line);
        ReferenceRow r{};
        if (s >> r.collective_deg >> r.vz >> r.vi_mean >> r.thrust >> r.torque >> r.vz_scaled) {
            rows.push_back(r);
        }
    }
    return rows;
}

bool same(const BemtFilterState& a, const BemtFilterState& b)
{
    return a.Vi_raw_previous == b.Vi_raw_previous && a.Vi_lag == b.Vi_lag && a.lag_initialized == b.lag_initialized &&
           a.v_johnson_lagged == b.v_johnson_lagged && a.vrs_strength == b.vrs_strength && a.wake_velocity == b.wake_velocity &&
           a.turbulence.force_L.a[0] == b.turbulence.force_L.a[0] && a.turbulence.force_L.a[1] == b.turbulence.force_L.a[1] &&
           a.turbulence.force_L.a[2] == b.turbulence.force_L.a[2] && a.turbulence.torque_L.a[0] == b.turbulence.torque_L.a[0] &&
           a.turbulence.torque_L.a[2] == b.turbulence.torque_L.a[2] && a.turbulence.torque_velocity.a[0] == b.turbulence.torque_velocity.a[0] &&
           a.turbulence.elapsed_old == b.turbulence.elapsed_old;
}

}  // namespace

// ---------------------------------------------------------------------------------------------------------------------
// port fidelity: the C++ rotor reproduces the sweep that the Unity code exported (bemt_rotor_test.csv of the Logo 600)
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the quasi static BEMT rotor reproduces the sweep exported by the Unity code")
{
    const auto reference = read_reference();
    REQUIRE(reference.size() == 182);

    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    const BemtGrid grid = make_bemt_grid(cfg);

    int compared = 0;
    Real worst_thrust = 0, worst_torque = 0, worst_vi = 0;
    for (const Real collective_deg : {-6.0, -4.0, -2.0, 0.0, 2.0, 4.0, 6.0}) {
        // continuation sweep like the Unity export: from hover downwards, then a reset and from hover upwards, four warm
        // started passes per speed
        BemtFilterState state;
        const Real step = 0.05;
        const int negative_steps = static_cast<int>(std::lround(15.0 / step));
        for (int sweep_step = 0; sweep_step <= 500; ++sweep_step) {
            if (sweep_step == negative_steps + 1) {
                state = BemtFilterState{};
            }
            const Real vz = sweep_step <= negative_steps ? -sweep_step * step : (sweep_step - negative_steps) * step;
            const BemtOutputs out = settle(cfg, grid, state, hover_inputs(collective_deg * kDeg, vz));
            for (const ReferenceRow& row : reference) {
                if (row.collective_deg == collective_deg && std::abs(row.vz - vz) < 1e-6) {
                    ++compared;
                    worst_thrust = std::max(worst_thrust, std::abs(out.force_O.a[1] - row.thrust));
                    worst_torque = std::max(worst_torque, std::abs(out.torque_D.a[1] - row.torque));
                    worst_vi = std::max(worst_vi, std::abs(out.Vi_mean - row.vi_mean));
                    CHECK(std::abs(out.Vz_scaled - row.vz_scaled) < 2e-4);
                }
            }
        }
    }
    CHECK(compared == 182);
    INFO("worst deviation: thrust " << worst_thrust << " N, torque " << worst_torque << " Nm, Vi " << worst_vi << " m/s");
    CHECK(worst_thrust < 2e-3);  // the Unity code computes in single precision and writes four digits
    CHECK(worst_torque < 1e-3);
    CHECK(worst_vi < 1e-3);
}

TEST_CASE("the Johnson vortex ring inflow reproduces the table exported by the Unity code")
{
    std::ifstream file(std::string(HOVERHERE_TEST_DATA_DIR) + "/reference/johnson_vrs_table.csv");
    REQUIRE(file.good());
    std::string line;
    std::getline(file, line);
    const JohnsonVrsParams params;
    int rows = 0;
    Real worst = 0;
    while (std::getline(file, line)) {
        std::replace(line.begin(), line.end(), ',', ' ');
        std::istringstream s(line);
        Real vz, vx, baseline, final_value, strength;
        if (!(s >> vz >> vx >> baseline >> final_value >> strength)) {
            continue;
        }
        ++rows;
        Real computed_strength = 0;
        const Real computed_final = johnson_table3(vz, vx, params, computed_strength);
        const Real computed_baseline = johnson_baseline(vz, vx, params);
        worst = std::max({worst, std::abs(computed_baseline - baseline), std::abs(computed_final - final_value), std::abs(computed_strength - strength)});
    }
    CHECK(rows == 287);
    CHECK(worst < 2e-4);  // four printed digits
}

// ---------------------------------------------------------------------------------------------------------------------
// grid and airfoil
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the polar grid covers the rotor disc between the root cutout and the tip")
{
    const BemtConfig cfg = logo600();
    const BemtGrid grid = make_bemt_grid(cfg);
    static_assert(kBemtElementCount == 40);
    CHECK(bemt_index(0, 0) == 0);
    CHECK(bemt_index(1, 0) == kBemtAzimuthCount);
    CHECK(bemt_index(3, 9) == 39);

    CHECK(grid.sector_angle == doctest::Approx(36 * kDeg));
    CHECK(grid.radius[bemt_index(0, 0)] > 0.2 * cfg.rotor.R);
    CHECK(grid.radius[bemt_index(3, 0)] < cfg.rotor.R);
    Real area = 0;
    for (std::size_t i = 0; i < kBemtElementCount; ++i) {
        area += grid.radius[i] * (2 * grid.segment_length) * grid.sector_angle;
        CHECK(grid.psi[i] == doctest::Approx(grid.sector_angle * static_cast<Real>(i % kBemtAzimuthCount)));
    }
    CHECK(area == doctest::Approx(kPi * cfg.rotor.R * cfg.rotor.R * (1 - 0.2 * 0.2)).epsilon(1e-12));  // the midpoint rule is exact here
}

TEST_CASE("the continuous airfoil curves are smooth, odd in lift and positive in drag")
{
    const AirfoilCurves airfoil;
    Real cl = 0, cd = 0;
    airfoil_coefficients(airfoil, 0, cl, cd);
    CHECK(cl == 0);
    CHECK(cd == doctest::Approx(airfoil.cd_offset));

    Real previous_cl = 0, previous_cd = 0;
    airfoil_coefficients(airfoil, -kPi, previous_cl, previous_cd);
    Real max_cl_jump = 0, max_cd_jump = 0;
    for (Real alpha = -kPi + 0.001; alpha <= kPi; alpha += 0.001) {
        airfoil_coefficients(airfoil, alpha, cl, cd);
        max_cl_jump = std::max(max_cl_jump, std::abs(cl - previous_cl));
        max_cd_jump = std::max(max_cd_jump, std::abs(cd - previous_cd));
        previous_cl = cl;
        previous_cd = cd;
        CHECK(cd > 0);
        Real cl_mirror = 0, cd_mirror = 0;
        airfoil_coefficients(airfoil, -alpha, cl_mirror, cd_mirror);
        CHECK(cl_mirror == doctest::Approx(-cl).epsilon(1e-12).scale(1e-12));
        CHECK(cd_mirror == doctest::Approx(cd).epsilon(1e-12));
    }
    CHECK(max_cl_jump < 0.02);  // continuous: the Newton iteration needs that
    CHECK(max_cd_jump < 0.02);

    airfoil_coefficients(airfoil, 5 * kDeg, cl, cd);
    CHECK(cl == doctest::Approx(airfoil.cl_slope * 5 * kDeg));  // linear range
}

// ---------------------------------------------------------------------------------------------------------------------
// hover and forward flight
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the BEMT rotor in hover has plausible thrust, torque, induced velocity and power")
{
    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    const BemtGrid grid = make_bemt_grid(cfg);
    BemtFilterState state;
    const BemtOutputs out = settle(cfg, grid, state, hover_inputs(4 * kDeg));

    CHECK(out.thrust > 25);
    CHECK(out.thrust < 60);  // about the weight of a 4.5 kg helicopter
    CHECK(out.force_O.a[1] == doctest::Approx(out.thrust).epsilon(1e-6));  // along the shaft
    CHECK(std::abs(out.force_O.a[0]) < 1e-3);
    CHECK(std::abs(out.force_O.a[2]) < 1e-3);
    CHECK(out.shaft_torque < 0);                // opposes the rotation about +y
    CHECK(std::abs(out.torque_D.a[0]) < 1e-3);  // symmetric: no tilting moment
    CHECK(std::abs(out.torque_D.a[2]) < 1e-3);

    // momentum theory of the whole disc: v = sqrt(T / (2 rho A)), the blade elements are not far from it
    const Real v_momentum = std::sqrt(out.thrust / (2 * 1.27 * kPi * 0.75 * 0.75));
    CHECK(out.Vi_mean == doctest::Approx(v_momentum).epsilon(0.25));
    CHECK(out.Vi_mean > 0);
    for (std::size_t i = 0; i < kBemtElementCount; ++i) {
        CHECK(out.Vi[i] > 0);
        CHECK(out.dT[i] > 0);
        CHECK(out.residual[i] < 0.002);
        CHECK(out.V2[i] < 0);  // a normally turning rotor moves along the blade-local -z
    }
    CHECK(out.max_residual < 0.002);
    CHECK(out.nonconverged_elements == 0);

    // the rotor power is the induced plus the profile power
    const Real power = -out.shaft_torque * kRotorSpeed;
    CHECK(power > out.thrust * out.Vi_mean);
    CHECK(power < 4 * out.thrust * out.Vi_mean);

    // thrust grows with the collective, and is negative for negative collective
    Real previous = -1e9;
    for (Real coll = -6; coll <= 8; coll += 2) {
        BemtFilterState s;
        const BemtOutputs o = settle(cfg, grid, s, hover_inputs(coll * kDeg));
        CHECK(o.force_O.a[1] > previous);
        previous = o.force_O.a[1];
    }
}

TEST_CASE("in forward flight the advancing blade carries more load and the disc is pushed to roll")
{
    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    const BemtGrid grid = make_bemt_grid(cfg);

    const auto flight = [&](const Real speed) {
        BemtFilterState state;
        BemtInputs in = hover_inputs(4 * kDeg);
        in.body.velocity_O = Vec3{{speed, 0, 0}};  // forward along +x
        in.body.velocity_L = in.body.velocity_O;
        return settle(cfg, grid, state, in);
    };
    const BemtOutputs hover = flight(0);
    CHECK(std::abs(hover.torque_D.a[0]) < 1e-3);

    Real previous_moment = std::abs(hover.torque_D.a[0]);
    for (const Real speed : {5.0, 10.0, 15.0}) {
        const BemtOutputs out = flight(speed);
        CHECK(out.horizontal_speed == doctest::Approx(speed));
        // virtual blade 7 (252 deg) advances (moves with the flight direction), blade 2 (72 deg) retreats
        const std::size_t advancing = bemt_index(2, 7);
        const std::size_t retreating = bemt_index(2, 2);
        CHECK(std::abs(out.V2[advancing]) > std::abs(out.V2[retreating]));
        CHECK(out.dT[advancing] > 1.2 * out.dT[retreating]);
        // the thrust of the advancing side (at +z) makes a moment about -x
        CHECK(out.torque_D.a[0] < 0);
        CHECK(std::abs(out.torque_D.a[0]) > previous_moment);
        previous_moment = std::abs(out.torque_D.a[0]);
        // the axial momentum balance of every element knows no translational lift: the mean induced velocity stays close to the hover value
        CHECK(out.Vi_mean == doctest::Approx(hover.Vi_mean).epsilon(0.05));
    }
}

TEST_CASE("wind and flight speed are equivalent: the air velocity relative to the rotor counts")
{
    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    const BemtGrid grid = make_bemt_grid(cfg);

    BemtFilterState moving_state, wind_state;
    BemtInputs moving = hover_inputs(4 * kDeg);
    moving.body.velocity_O = Vec3{{-7, 0, 0}};
    moving.body.velocity_L = moving.body.velocity_O;
    BemtInputs windy = hover_inputs(4 * kDeg);
    windy.wind_O = Vec3{{7, 0, 0}};  // helicopter at rest in a wind of +7 m/s: the air moves past it like it moves through still air at -7 m/s

    const BemtOutputs a = settle(cfg, grid, moving_state, moving);
    const BemtOutputs b = settle(cfg, grid, wind_state, windy);
    CHECK(a.horizontal_speed == doctest::Approx(7));
    CHECK(b.horizontal_speed == doctest::Approx(7));
    CHECK(a.force_O.a[1] == doctest::Approx(b.force_O.a[1]).epsilon(1e-9));
    CHECK(a.torque_D.a[0] == doctest::Approx(b.torque_D.a[0]).epsilon(1e-9));
}

// ---------------------------------------------------------------------------------------------------------------------
// vortex ring state and the sign convention
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the Johnson blend gives a continuous induced velocity through the descent and the vortex ring region")
{
    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    const BemtGrid grid = make_bemt_grid(cfg);
    const Real collective = 4 * kDeg;

    struct Sweep {
        Real max_vi_step = 0, max_thrust_step = 0, max_strength = 0, final_weight = 0;
        bool weight_monotonic = true;
    };
    // descent from 0 to -10 m/s (Vz / vh from 0 to about -3) with continuation, like the Unity export
    const auto sweep = [&](const Real step) {
        BemtFilterState state;
        const BemtOutputs hover = settle(cfg, grid, state, hover_inputs(collective, 0));
        REQUIRE(hover.strength_vrs == 0);
        REQUIRE(hover.descent_blend_weight == 0);
        Sweep r;
        Real previous_vi = hover.Vi_mean, previous_thrust = hover.thrust, previous_weight = 0;
        const int count = static_cast<int>(std::lround(10.0 / step));
        for (int i = 1; i <= count; ++i) {
            const BemtOutputs out = settle(cfg, grid, state, hover_inputs(collective, -i * step));
            r.max_vi_step = std::max(r.max_vi_step, std::abs(out.Vi_mean - previous_vi));
            r.max_thrust_step = std::max(r.max_thrust_step, std::abs(out.thrust - previous_thrust));
            r.max_strength = std::max(r.max_strength, out.strength_vrs);
            r.weight_monotonic = r.weight_monotonic && out.descent_blend_weight >= previous_weight - 1e-12;
            previous_vi = out.Vi_mean;
            previous_thrust = out.thrust;
            previous_weight = out.descent_blend_weight;
        }
        r.final_weight = previous_weight;
        return r;
    };
    const Sweep coarse = sweep(0.05);
    const Sweep fine = sweep(0.0125);

    // The thrust drops steeply where the baseline blends over (the Unity export has the same slope: 4.5 N between two samples
    // of 5 cm/s), but it is continuous: four times closer samples give about four times smaller steps. A jump would not scale.
    CHECK(coarse.max_thrust_step / fine.max_thrust_step > 2.5);
    CHECK(coarse.max_vi_step / fine.max_vi_step > 2.5);
    CHECK(fine.max_thrust_step < 1.5);
    CHECK(fine.max_vi_step < 0.1);
    CHECK(coarse.max_strength > 0.95);  // the vortex ring region is crossed
    CHECK(coarse.weight_monotonic);
    CHECK(fine.weight_monotonic);
    CHECK(coarse.final_weight == 1);  // deep in the descent the Johnson inflow is blended in completely
}

TEST_CASE("the vortex ring region reduces the thrust below the blade element solution")
{
    BemtConfig with = logo600();
    with.quasi_static = true;
    BemtConfig without = with;
    without.vortex_ring_state = false;
    const BemtGrid grid = make_bemt_grid(with);

    // at Vz / vh = -1 the inflow is much larger than the blade element solution (the rotor sits in its own wake)
    const BemtInputs in = hover_inputs(4 * kDeg, -3.1);
    BemtFilterState a, b;
    const BemtOutputs vrs = settle(with, grid, a, in);
    const BemtOutputs pure = settle(without, grid, b, in);
    CHECK(vrs.Vz_scaled == doctest::Approx(-1.0).epsilon(0.1));
    CHECK(vrs.strength_vrs > 0.9);
    CHECK(vrs.Vi_mean > pure.Vi_mean);  // more inflow ...
    CHECK(vrs.thrust < pure.thrust);    // ... less thrust
    CHECK(vrs.descent_blend_weight > 0.4);

    // the flag removes the blend completely
    for (std::size_t i = 0; i < kBemtElementCount; ++i) {
        CHECK(pure.vrs_weight[i] == 0);
        CHECK(pure.Vi_target[i] == pure.Vi_raw[i]);
    }
}

TEST_CASE("an inverted rotor in a climb behaves like the normal rotor in a descent: the sign conventions are point symmetric")
{
    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    const BemtGrid grid = make_bemt_grid(cfg);

    for (const Real descent : {1.0, 3.1, 5.0}) {
        BemtFilterState normal_state, inverted_state;
        const BemtOutputs normal = settle(cfg, grid, normal_state, hover_inputs(4 * kDeg, -descent));
        const BemtOutputs inverted = settle(cfg, grid, inverted_state, hover_inputs(-4 * kDeg, +descent));
        INFO("descent " << descent << " m/s");
        CHECK(inverted.Vz_scaled == doctest::Approx(normal.Vz_scaled).epsilon(1e-9));  // both are descents in terms of the thrust direction
        CHECK(inverted.strength_vrs == doctest::Approx(normal.strength_vrs).epsilon(1e-9));
        CHECK(inverted.v_johnson_target == doctest::Approx(-normal.v_johnson_target).epsilon(1e-9));
        CHECK(std::abs(inverted.Vi_mean + normal.Vi_mean) < 0.05 * std::abs(normal.Vi_mean) + 0.02);
        CHECK(std::abs(inverted.force_O.a[1] + normal.force_O.a[1]) < 0.02 * std::abs(normal.force_O.a[1]) + 0.5);
    }
}

TEST_CASE("the descent coherence gate only passes a descent that is coherent over the whole disc")
{
    const BemtConfig cfg = logo600();  // flight mode: the gate is active
    const BemtGrid grid = make_bemt_grid(cfg);

    BemtFilterState hover_state;
    CHECK(bemt_evaluate(cfg, grid, hover_state, hover_inputs(4 * kDeg, 0)).use_vrs == 0);

    BemtFilterState descent_state;
    const BemtOutputs descent = bemt_evaluate(cfg, grid, descent_state, hover_inputs(4 * kDeg, -3));
    CHECK(descent.use_vrs == 1);
    CHECK(descent.descent_blend_weight > 0.4);
    BemtConfig steady = cfg;
    steady.quasi_static = true;  // the steady state ignores the gate
    BemtFilterState steady_state;
    CHECK(bemt_evaluate(steady, grid, steady_state, hover_inputs(4 * kDeg, 0)).descent_blend_weight == 0);
    BemtFilterState steady_state_descent;
    CHECK(bemt_evaluate(steady, grid, steady_state_descent, hover_inputs(4 * kDeg, -3)).descent_blend_weight > descent.descent_blend_weight - 1e-12);

    // a rotor that pitches (half the disc moves up, half down) is not in a coherent descent
    BemtInputs pitching = hover_inputs(4 * kDeg, 0);
    pitching.body.omega_L = Vec3{{0, 0, 2.0}};
    pitching.disc = make_rotor_disc_state(frames::quat_identity(), 0, 0);
    BemtFilterState pitching_state;
    CHECK(bemt_evaluate(cfg, grid, pitching_state, pitching).use_vrs < 0.5);
}

// ---------------------------------------------------------------------------------------------------------------------
// vortex ring development: slow, unconditionally stable lags (no limit cycle at the edge of the vortex ring window)
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the Johnson time lag is a first order lag: monotone, without overshoot and independent of the step size")
{
    const Real target = 5;
    const Real tau = 10 * 2 * kPi / kRotorSpeed;  // 10 revolutions, 0.4 s at 1500 rpm
    CHECK(tau == doctest::Approx(0.4).epsilon(0.01));

    Real at_tau[2]{};
    int index = 0;
    for (const Real dt : {0.001, 0.01}) {
        Real v = 0, previous = 0, peak = 0;
        bool monotone = true;
        const int steps = static_cast<int>(std::lround(3.0 / dt));
        for (int i = 1; i <= steps; ++i) {
            v = johnson_time_lag(target, v, kRotorSpeed, 10, dt);
            monotone = monotone && v >= previous;
            previous = v;
            peak = std::max(peak, v);
            if (i == static_cast<int>(std::lround(tau / dt))) {
                at_tau[index] = v;
            }
        }
        INFO("dt = " << dt);
        CHECK(monotone);
        CHECK(peak <= target);  // the old integrator + lag overshot and rang at about sqrt(1 / (tau dt))
        CHECK(v == doctest::Approx(target).epsilon(0.01));
        CHECK(at_tau[index] == doctest::Approx(target * (1 - std::exp(-1.0))).epsilon(0.03));
        ++index;
    }
    CHECK(std::abs(at_tau[0] - at_tau[1]) < 0.05);

    // a (nearly) stopped rotor has no lag, and the generic step handles tau = 0
    CHECK(johnson_time_lag(target, 0, 0.05, 10, 0.01) == target);
    CHECK(first_order_lag_step(1, 3, 0.01, 0) == 3);
}

TEST_CASE("the vortex ring strength builds up and decays with the PT1 lag instead of switching")
{
    const BemtConfig cfg = logo600();  // flight mode with the coherence gate, vrs.tau_strength = 0.4 s
    const BemtGrid grid = make_bemt_grid(cfg);
    const Real dt = 0.005;
    RotorRng rng;

    BemtFilterState state;
    settle(cfg, grid, state, hover_inputs(4 * kDeg, 0));
    REQUIRE(state.vrs_strength == 0);

    // sink rate jumps into the vortex ring window: the instantaneous strength is 1 at once, the reported one rises like 1 - exp(-t / tau)
    const BemtInputs in = hover_inputs(4 * kDeg, -3.1);
    Real previous = 0, at_tau = 0;
    bool monotone = true;
    for (int i = 1; i <= static_cast<int>(std::lround(3.0 / dt)); ++i) {
        const BemtOutputs out = bemt_evaluate(cfg, grid, state, in);
        REQUIRE(out.strength_vrs * out.use_vrs > 0.9);
        CHECK(out.vrs_strength == state.vrs_strength);  // evaluate reports the lagged state, it does not write it
        bemt_post_step(cfg, state, in, out, i * dt, dt, rng);
        monotone = monotone && state.vrs_strength >= previous - 1e-12;
        previous = state.vrs_strength;
        if (i == static_cast<int>(std::lround(cfg.vrs.tau_strength / dt))) {
            at_tau = state.vrs_strength;
        }
    }
    CHECK(monotone);
    CHECK(at_tau > 0.5);
    CHECK(at_tau < 0.75);
    CHECK(previous > 0.95);

    // quasi static physics and tau = 0 report the instantaneous strength
    BemtConfig steady = cfg;
    steady.quasi_static = true;
    BemtFilterState steady_state;
    const BemtOutputs steady_out = settle(steady, grid, steady_state, in);
    CHECK(steady_out.vrs_strength == doctest::Approx(steady_out.strength_vrs * steady_out.use_vrs));
    BemtConfig instant = cfg;
    instant.vrs.tau_strength = 0;
    BemtFilterState instant_state;
    settle(instant, grid, instant_state, hover_inputs(4 * kDeg, 0));
    const BemtOutputs instant_out = bemt_evaluate(instant, grid, instant_state, in);
    CHECK(instant_out.vrs_strength == doctest::Approx(instant_out.strength_vrs * instant_out.use_vrs));
}

TEST_CASE("a sink rate that chatters in and out of the vortex ring window at 5 Hz does not make the strength or the inflow ring")
{
    const BemtConfig cfg = logo600();
    const BemtGrid grid = make_bemt_grid(cfg);
    const Real dt = 0.002;

    struct Ripple {
        Real strength_min = 1, strength_max = 0, instant_min = 1, instant_max = 0;
        Real johnson_min = 1e9, johnson_max = -1e9, johnson_target_min = 1e9, johnson_target_max = -1e9;
        Real Vi_min = 1e9, Vi_max = -1e9;
    };
    // worst case input: the sink rate flips between "in the window" (-3.1 m/s, Vz / vh = -1) and "out" (-0.1 m/s) every 0.09 s,
    // about 5.5 Hz like the limit cycle of the headless run (vrs_descent, t = 4.3 .. 6.7 s)
    const auto run = [&](const BemtConfig& c) {
        RotorRng rng;
        BemtFilterState state;
        settle(c, grid, state, hover_inputs(4 * kDeg, 0));
        Ripple r;
        const int steps = static_cast<int>(std::lround(4.0 / dt));
        for (int i = 0; i < steps; ++i) {
            const Real t = i * dt;
            const bool inside = static_cast<int>(t / 0.09) % 2 == 0;
            const BemtInputs in = hover_inputs(4 * kDeg, inside ? -3.1 : -0.1);
            const BemtOutputs out = bemt_evaluate(c, grid, state, in);
            bemt_post_step(c, state, in, out, t + dt, dt, rng);
            if (t > 2) {  // after the build-up
                const Real instant = out.strength_vrs * out.use_vrs;
                r.strength_min = std::min(r.strength_min, out.vrs_strength);
                r.strength_max = std::max(r.strength_max, out.vrs_strength);
                r.instant_min = std::min(r.instant_min, instant);
                r.instant_max = std::max(r.instant_max, instant);
                r.johnson_min = std::min(r.johnson_min, out.v_johnson_lagged);
                r.johnson_max = std::max(r.johnson_max, out.v_johnson_lagged);
                r.johnson_target_min = std::min(r.johnson_target_min, out.v_johnson_target);
                r.johnson_target_max = std::max(r.johnson_target_max, out.v_johnson_target);
                r.Vi_min = std::min(r.Vi_min, out.Vi_mean);
                r.Vi_max = std::max(r.Vi_max, out.Vi_mean);
            }
        }
        return r;
    };
    const Ripple with = run(cfg);
    BemtConfig pure_cfg = cfg;
    pure_cfg.vortex_ring_state = false;
    const Ripple pure = run(pure_cfg);

    // the input really switches ...
    REQUIRE(with.instant_max - with.instant_min > 0.9);
    REQUIRE(with.johnson_target_max - with.johnson_target_min > 1.5);
    // ... but the reported strength and the Johnson inflow only ripple slightly around their mean (a 0.4 s lag against a 0.09 s half period)
    CHECK(with.strength_max - with.strength_min < 0.25);
    CHECK(with.johnson_max - with.johnson_min < 0.3 * (with.johnson_target_max - with.johnson_target_min));
    // The fast Pitt-Peters inflow is not smoothed: it follows the physical swing of the flow like the pure blade element rotor, the
    // vortex ring state adds no ripple of its own on top of it.
    CHECK(with.Vi_max - with.Vi_min < 1.1 * (pure.Vi_max - pure.Vi_min) + 0.1);
}

// ---------------------------------------------------------------------------------------------------------------------
// dynamic inflow (Pitt-Peters apparent mass)
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the Pitt-Peters time constant uses the through flow of the disc and the fumble factor")
{
    const Real R = 0.75;
    const Real k = 8 / (3 * kPi);
    const Real tau = pitt_peters_time_constant(1.0, R, 3, 3, 1);  // U_local = sqrt(Vh^2 + (Vi + Vc)^2), no blade speed
    CHECK(tau == doctest::Approx(k * R / 5.0));
    CHECK(pitt_peters_time_constant(5.0, R, 3, 3, 1) == doctest::Approx(5 * tau));
    CHECK(pitt_peters_time_constant(0.0, R, 3, 3, 1) == 0);
    CHECK(pitt_peters_time_constant(1.0, R, 0, 0, 0) == doctest::Approx(k * R / 0.5));  // the through flow has a floor of 0.5 m/s
    CHECK(pitt_peters_time_constant(1.0, R, 0, -3, 3) == doctest::Approx(k * R / 0.5));  // a reversing column passes through the floor
    CHECK(pitt_peters_time_constant(1.0, R, 0, 3, 0) > pitt_peters_time_constant(1.0, R, 0, 6, 0));  // slower flow, slower inflow
    // hover of the Logo 600: Vi is about 3 m/s, so tau is a fraction of a second, not the 10 ms of the blade speed
    CHECK(pitt_peters_time_constant(1.0, R, 0, 3, 0) > 0.1);
    CHECK(pitt_peters_time_constant(1.0, R, 0, 3, 0) < 0.3);
}

TEST_CASE("the implicit Euler step of the inflow lag is unconditionally stable and first order accurate")
{
    for (const Real ratio : {0.001, 0.1, 1.0, 10.0, 1000.0}) {  // monotone approach for any step size
        const Real next = pitt_peters_step(2.0, 5.0, ratio * 0.01, 0.01);
        CHECK(next > 2.0);
        CHECK(next <= 5.0);
    }
    CHECK(pitt_peters_step(2.0, 5.0, 1e9, 0.01) == doctest::Approx(5.0).epsilon(1e-6));
    CHECK(pitt_peters_step(5.0, 5.0, 0.01, 0.01) == 5.0);  // fixed point
    CHECK(pitt_peters_step(2.0, 5.0, 0.01, 0.0) == 5.0);   // no inertia: follows instantly

    const Real tau = 0.05;
    Real v = 0;
    const Real dt = 1e-4;
    for (int i = 0; i < 500; ++i) {  // t = tau
        v = pitt_peters_step(v, 1.0, dt, tau);
    }
    CHECK(v == doctest::Approx(1 - std::exp(-1.0)).epsilon(5e-3));
}

TEST_CASE("the induced velocity follows its target with the Pitt-Peters lag and the thrust overshoots after a collective step")
{
    BemtConfig cfg = logo600();
    cfg.turbulence = false;
    cfg.ground_effect = false;
    const BemtGrid grid = make_bemt_grid(cfg);
    RotorRng rng;
    const Real dt = 0.001;

    BemtFilterState state;
    Real time = 0;
    BemtInputs in = hover_inputs(2 * kDeg);
    for (int i = 0; i < 3000; ++i) {  // settle at 2 degrees (tau is a few 100 ms)
        const BemtOutputs out = bemt_evaluate(cfg, grid, state, in);
        bemt_post_step(cfg, state, in, out, time += dt, dt, rng);
    }
    const BemtOutputs settled_low = bemt_evaluate(cfg, grid, state, in);
    for (std::size_t i = 0; i < kBemtElementCount; ++i) {
        CHECK(settled_low.Vi[i] == doctest::Approx(settled_low.Vi_target[i]).epsilon(1e-3));
    }

    // step to 6 degrees: the target jumps, the induced velocity in the flow does not
    in.blade_pitch.fill(6 * kDeg);
    const BemtOutputs after_step = bemt_evaluate(cfg, grid, state, in);
    const std::size_t probe = bemt_index(2, 3);
    CHECK(after_step.Vi_target[probe] > settled_low.Vi[probe] + 0.5);
    CHECK(after_step.Vi[probe] == doctest::Approx(settled_low.Vi[probe]).epsilon(1e-3));
    const Real thrust_just_after = after_step.thrust;

    // the lag closes the gap with the local time constant (a few 100 ms, set by the through flow of the disc)
    const Real tau = pitt_peters_time_constant(cfg.wake_inertia_fumble_factor, cfg.rotor.R, after_step.horizontal_speed, state.Vi_lag[probe], after_step.Vc[probe]);
    const Real gap_start = after_step.Vi_target[probe] - state.Vi_lag[probe];
    BemtOutputs out = after_step;
    const int steps = static_cast<int>(std::lround(tau / dt));
    for (int i = 0; i < steps; ++i) {
        out = bemt_evaluate(cfg, grid, state, in);
        bemt_post_step(cfg, state, in, out, time += dt, dt, rng);
    }
    const Real closed = (state.Vi_lag[probe] - settled_low.Vi[probe]) / gap_start;
    CHECK(closed > 0.5);
    CHECK(closed < 0.85);

    for (int i = 0; i < 3000; ++i) {
        out = bemt_evaluate(cfg, grid, state, in);
        bemt_post_step(cfg, state, in, out, time += dt, dt, rng);
    }
    const BemtOutputs steady = bemt_evaluate(cfg, grid, state, in);
    CHECK(steady.Vi[probe] == doctest::Approx(steady.Vi_target[probe]).epsilon(1e-3));
    CHECK(thrust_just_after > steady.thrust);  // inflow lag: the thrust overshoots before the wake adjusts
    CHECK(steady.thrust > settled_low.thrust);
}

TEST_CASE("braking a fast tilt rate to zero does not make the induced velocity jump")
{
    // tick-tock: the disc tilts so fast that one half of it climbs (Vc > 0) and the other descends; at the reversal point the rate ends
    BemtConfig cfg = logo600();
    cfg.turbulence = false;
    cfg.ground_effect = false;
    const BemtGrid grid = make_bemt_grid(cfg);
    RotorRng rng;
    const Real dt = 0.001;
    const Real collective = 5 * kDeg;

    for (const Real rate : {4.0, 8.0, 12.0}) {
        BemtFilterState state;
        BemtInputs in = hover_inputs(collective);
        Real time = 0;
        for (int i = 0; i < 2000; ++i) {  // hover, then
            const BemtOutputs out = bemt_evaluate(cfg, grid, state, in);
            bemt_post_step(cfg, state, in, out, time += dt, dt, rng);
        }
        in.disc = make_rotor_disc_state(frames::quat_identity(), rate, 0);
        for (int i = 0; i < 1000; ++i) {  // tilt about the x-axis of the disc
            const BemtOutputs out = bemt_evaluate(cfg, grid, state, in);
            bemt_post_step(cfg, state, in, out, time += dt, dt, rng);
        }
        BemtOutputs out = bemt_evaluate(cfg, grid, state, in);
        Real Vc_max = -1e9, Vc_min = 1e9;
        for (std::size_t i = 0; i < kBemtElementCount; ++i) {
            Vc_max = std::max(Vc_max, out.Vc[i]);
            Vc_min = std::min(Vc_min, out.Vc[i]);
        }
        REQUIRE(Vc_max > 0.5 * rate * cfg.rotor.R);  // really a climbing and a descending half
        REQUIRE(Vc_min < -0.5 * rate * cfg.rotor.R);

        // the rate ends at once: no element of the lagged inflow may jump from one step to the next
        in.disc = make_rotor_disc_state(frames::quat_identity(), 0, 0);
        std::array<Real, kBemtElementCount> previous = out.Vi;
        Real max_jump = 0;
        for (int i = 0; i < 1500; ++i) {
            bemt_post_step(cfg, state, in, out, time += dt, dt, rng);
            out = bemt_evaluate(cfg, grid, state, in);
            for (std::size_t e = 0; e < kBemtElementCount; ++e) {
                max_jump = std::max(max_jump, std::abs(out.Vi[e] - previous[e]));
            }
            previous = out.Vi;
        }
        INFO("rate = " << rate << " rad/s, max step of Vi " << max_jump << " m/s");
        CHECK(max_jump < 0.1);  // [m/s per ms]; with the blade speed in tau (10 ms) the step is about 0.5
    }
}

TEST_CASE("the inflow lag can be switched off and a zero fumble factor removes the inertia")
{
    BemtConfig cfg = logo600();
    cfg.turbulence = false;
    const BemtGrid grid = make_bemt_grid(cfg);
    RotorRng rng;

    // dynamic_inflow = false: the induced velocity is the target at once
    BemtConfig instant = cfg;
    instant.dynamic_inflow = false;
    BemtFilterState state;
    BemtInputs in = hover_inputs(2 * kDeg);
    settle(instant, grid, state, in, 5, 0.001);
    in.blade_pitch.fill(6 * kDeg);
    const BemtOutputs out = bemt_evaluate(instant, grid, state, in);
    for (std::size_t i = 0; i < kBemtElementCount; ++i) {
        CHECK(out.Vi[i] == out.Vi_target[i]);
    }

    // fumble factor 0: after one step the lagged value is the target
    BemtConfig none = cfg;
    none.wake_inertia_fumble_factor = 0;
    BemtFilterState state2;
    settle(none, grid, state2, hover_inputs(2 * kDeg), 5, 0.001);
    const BemtOutputs first = bemt_evaluate(none, grid, state2, in);
    bemt_post_step(none, state2, in, first, 0.01, 0.001, rng);
    for (std::size_t i = 0; i < kBemtElementCount; ++i) {
        CHECK(state2.Vi_lag[i] == first.Vi_target[i]);
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// time structure
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("RK4 stages evaluate the BEMT rotor without touching its filter state")
{
    const BemtConfig cfg = logo600();  // everything on: dynamic inflow, vortex ring, turbulence
    const BemtGrid grid = make_bemt_grid(cfg);
    BemtFilterState state;
    RotorRng rng(3);
    BemtInputs in = hover_inputs(3 * kDeg, -2.5);
    in.body.omega_L = Vec3{{2, 0, 1}};  // excites the turbulence
    Real time = 0;
    for (int i = 0; i < 700; ++i) {  // build up a non trivial state
        const BemtOutputs out = bemt_evaluate(cfg, grid, state, in);
        bemt_post_step(cfg, state, in, out, time += 0.001, 0.001, rng);
    }
    REQUIRE(state.lag_initialized);
    REQUIRE(state.v_johnson_lagged != 0);
    REQUIRE(qvm::mag(state.turbulence.force_L) > 0);

    const BemtFilterState before = state;
    const auto f = [&](const Real, const RotorDiscStateVector& d) {
        BemtInputs stage = in;
        stage.disc = d;
        return rotor_disc_derivative(cfg, d, in.omega_shaft, bemt_evaluate(cfg, grid, state, stage));
    };
    RotorDiscStateVector x = make_rotor_disc_state(frames::quat_from_A(frames::rot_x(0.05)), 0.2, -0.1);
    for (int i = 0; i < 25; ++i) {  // 25 full RK4 steps = 100 stage evaluations
        x = num::rk4_step(f, i * 0.001, x, 0.001);
        normalize_disc_orientation(x);
    }
    CHECK(same(before, state));

    // the evaluation is a pure function: independent of the order of the calls
    BemtInputs other = in;
    other.disc = x;
    const BemtOutputs a = bemt_evaluate(cfg, grid, state, in);
    const BemtOutputs unrelated = bemt_evaluate(cfg, grid, state, other);
    const BemtOutputs b = bemt_evaluate(cfg, grid, state, in);
    CHECK(a.force_O.a[1] == b.force_O.a[1]);
    CHECK(a.torque_D.a[0] == b.torque_D.a[0]);
    CHECK(a.Vi == b.Vi);
    CHECK(a.Vi_raw == b.Vi_raw);
    CHECK(unrelated.torque_hub_on_disc_D.a[0] != a.torque_hub_on_disc_D.a[0]);  // the disc state does matter
}

TEST_CASE("only the post step advances the slow states")
{
    const BemtConfig cfg = logo600();
    const BemtGrid grid = make_bemt_grid(cfg);
    BemtFilterState state;
    RotorRng rng;
    const BemtInputs in = hover_inputs(4 * kDeg, -3);
    const BemtOutputs out = bemt_evaluate(cfg, grid, state, in);
    CHECK_FALSE(state.lag_initialized);
    CHECK(state.Vi_raw_previous[0] == 0);

    bemt_post_step(cfg, state, in, out, 0.001, 0.001, rng);
    CHECK(state.lag_initialized);
    CHECK(state.Vi_raw_previous == out.Vi_raw);  // warm start for the Newton solver
    CHECK(state.Vi_lag == out.Vi_target);        // cold start of the lag
    CHECK(state.v_johnson_lagged == out.v_johnson_target);
    CHECK(state.wake_velocity == out.Vi_mean);

    // the warm start does not change the solution
    const BemtOutputs again = bemt_evaluate(cfg, grid, state, in);
    for (std::size_t i = 0; i < kBemtElementCount; ++i) {
        CHECK(again.Vi_raw[i] == doctest::Approx(out.Vi_raw[i]).epsilon(1e-3).scale(1e-3));
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// ground effect, turbulence, hub
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the BEMT ground effect raises the force near the ground and can be switched off")
{
    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    cfg.turbulence = false;
    const BemtGrid grid = make_bemt_grid(cfg);
    BemtInputs in = hover_inputs(4 * kDeg);

    cfg.ground_effect = false;
    BemtFilterState off_state;
    const BemtOutputs off = settle(cfg, grid, off_state, in);
    CHECK(off.ground_effect_factor == 1);

    cfg.ground_effect = true;
    in.ground.hub_distance_to_ground = 0.3;
    BemtFilterState on_state;
    const BemtOutputs on = settle(cfg, grid, on_state, in);
    CHECK(on.ground_effect_factor > 1.05);
    CHECK(on.force_O.a[1] == doctest::Approx(off.force_O.a[1] * on.ground_effect_factor).epsilon(1e-6));
    CHECK(on.torque_D.a[1] == doctest::Approx(off.torque_D.a[1]).epsilon(1e-6));  // the torque is not changed

    in.ground.surface_normal_O = Vec3{{0, -1, 0}};  // a ceiling does not act like ground
    BemtFilterState ceiling_state;
    CHECK(settle(cfg, grid, ceiling_state, in).ground_effect_factor == 1);
}

TEST_CASE("the BEMT turbulence acts on the fuselage only when the flag is set")
{
    BemtConfig cfg = logo600();
    cfg.ground_effect = false;
    const BemtGrid grid = make_bemt_grid(cfg);
    const RotorGeometry geometry = make_rotor_geometry(cfg.rotor);
    BemtFilterState state;
    RotorRng rng(11);
    BemtInputs in = hover_inputs(4 * kDeg);
    in.body.omega_L = Vec3{{6, 0, 0}};  // fast rotation: full strength

    Real time = 0;
    for (int i = 0; i < 700; ++i) {
        const BemtOutputs out = bemt_evaluate(cfg, grid, state, in);
        bemt_post_step(cfg, state, in, out, time += 0.001, 0.001, rng);
    }
    const BemtOutputs out = bemt_evaluate(cfg, grid, state, in);
    CHECK(out.turbulence_strength == doctest::Approx(1));
    const BemtFuselageLoads shaken = bemt_loads_on_fuselage(cfg, geometry, in.body, state, out, false);
    BemtConfig calm = cfg;
    calm.turbulence = false;
    const BemtFuselageLoads quiet = bemt_loads_on_fuselage(calm, geometry, in.body, state, out, false);
    CHECK(qvm::mag(shaken.force_O - quiet.force_O) > 0.1);
    CHECK(qvm::mag(shaken.torque_L - quiet.torque_L) > 0.1);
    CHECK(qvm::mag(shaken.force_O - quiet.force_O) == doctest::Approx(qvm::mag(state.turbulence.force_L)).epsilon(1e-12));
}

TEST_CASE("the hub spring and damper pull the fuselage and the disc towards each other")
{
    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    const BemtGrid grid = make_bemt_grid(cfg);
    BemtFilterState state;

    // disc tilted by 5 degrees about the rotor x-axis: the hub spring torque on the fuselage turns it the same way, the disc back
    BemtInputs in = hover_inputs(4 * kDeg);
    const Real tilt = 5 * kDeg;
    in.disc = make_rotor_disc_state(frames::quat_from_A(frames::rot_x(tilt)), 0, 0);
    const BemtOutputs out = settle(cfg, grid, state, in);
    CHECK(out.torque_hub_on_body_L.a[0] == doctest::Approx(cfg.hub_stiffness * std::sin(tilt)).epsilon(1e-9));
    CHECK(out.torque_hub_on_disc_D.a[0] == doctest::Approx(-cfg.hub_stiffness * std::sin(tilt)).epsilon(1e-9));
    CHECK(std::abs(out.torque_hub_on_body_L.a[1]) < 1e-12);  // no torque about the shaft
    CHECK(std::abs(out.torque_hub_on_body_L.a[2]) < 1e-9);

    // the disc accelerates back towards the hub
    const RotorDiscStateVector derivative = rotor_disc_derivative(cfg, in.disc, in.omega_shaft, out);
    CHECK(derivative[disc::wx] < 0);

    // tilted about the z-axis: the normal moves towards -x for a positive angle, the spring torque is about +z
    BemtInputs about_z = hover_inputs(4 * kDeg);
    about_z.disc = make_rotor_disc_state(frames::quat_from_A(frames::rot_z(tilt)), 0, 0);
    BemtFilterState state_z;
    const BemtOutputs out_z = settle(cfg, grid, state_z, about_z);
    CHECK(out_z.torque_hub_on_body_L.a[2] == doctest::Approx(cfg.hub_stiffness * std::sin(tilt)).epsilon(1e-9));

    // damping: the fuselage rolls with 1 rad/s while the disc keeps its attitude: the damper decelerates the fuselage and drags the disc
    BemtInputs rolling = hover_inputs(4 * kDeg);
    rolling.body.omega_L = Vec3{{1, 0, 0}};
    BemtFilterState state_r;
    const BemtOutputs out_r = settle(cfg, grid, state_r, rolling);
    CHECK(out_r.torque_hub_on_body_L.a[0] == doctest::Approx(-cfg.hub_damping * 1.0).epsilon(1e-9));
    CHECK(out_r.torque_hub_on_disc_D.a[0] == doctest::Approx(+cfg.hub_damping * 1.0).epsilon(1e-9));
}

TEST_CASE("the disc turns about its shaft with the rotor speed and keeps the quaternion rate consistent")
{
    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    const BemtGrid grid = make_bemt_grid(cfg);
    BemtFilterState state;
    const BemtInputs in = hover_inputs(4 * kDeg);
    const BemtOutputs out = settle(cfg, grid, state, in);
    const RotorDiscStateVector derivative = rotor_disc_derivative(cfg, in.disc, in.omega_shaft, out);
    CHECK(derivative[disc::q0] == 0);
    CHECK(derivative[disc::q2] == doctest::Approx(in.omega_shaft / 2));  // q' = 1/2 q (x) (0, 0, omega, 0)
    // in the hover without tilt nothing accelerates the disc about its x- and z-axis
    CHECK(std::abs(derivative[disc::wx]) < 0.05);
    CHECK(std::abs(derivative[disc::wz]) < 0.05);
}

// ---------------------------------------------------------------------------------------------------------------------
// loads on the fuselage
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the loads on the fuselage contain the thrust at the hub, the shaft torque and the hub torque")
{
    BemtConfig cfg = logo600();
    cfg.turbulence = false;
    RotorGeometry geometry{};
    geometry.position_L = Vec3{{0, 0.3, 0}};
    geometry.A_LR = Mat3{{{1, 0, 0}, {0, 1, 0}, {0, 0, 1}}};
    geometry.axis_L = Vec3{{0, 1, 0}};
    BodyKinematics body{};
    body.A_OL = Mat3{{{1, 0, 0}, {0, 1, 0}, {0, 0, 1}}};

    BemtOutputs out{};
    out.force_O = Vec3{{0, 40, 5}};
    out.shaft_torque = -2;
    out.torque_hub_on_body_L = Vec3{{0.5, 0, -0.25}};
    const BemtFilterState state{};

    const BemtFuselageLoads loads = bemt_loads_on_fuselage(cfg, geometry, body, state, out, false);
    CHECK(loads.force_O.a[1] == 40);
    CHECK(loads.force_O.a[2] == 5);
    // r x F = (0, 0.3, 0) x (0, 40, 5) = (1.5, 0, 0)
    CHECK(loads.torque_L.a[0] == doctest::Approx(0.5 + 1.5));
    CHECK(loads.torque_L.a[1] == doctest::Approx(-2));  // the shaft torque of the aerodynamic load acts on the fuselage with the same sign
    CHECK(loads.torque_L.a[2] == doctest::Approx(-0.25));

    const BemtFuselageLoads freewheeling = bemt_loads_on_fuselage(cfg, geometry, body, state, out, true);
    CHECK(freewheeling.torque_L.a[1] == doctest::Approx(0).scale(1.0));  // no torque through the open freewheel
    CHECK(freewheeling.torque_L.a[0] == loads.torque_L.a[0]);

    // the force is a world vector, the hub offset torque uses it in the body frame: a yawed fuselage rotates it back
    body.A_OL = frames::rot_y(kPi / 2);  // body x -> world -z
    const BemtFuselageLoads yawed = bemt_loads_on_fuselage(cfg, geometry, body, state, out, true);
    const Vec3 force_L = frames::transposed(body.A_OL) * out.force_O;
    const Vec3 expected = qvm::cross(geometry.position_L, force_L) + out.torque_hub_on_body_L;
    CHECK(qvm::mag(yawed.torque_L - expected) < 1e-12);
}

TEST_CASE("the BEMT configuration comes from the compiled parameters and the fumble factor is a schema parameter")
{
    const auto p = hoverhere::params::compile_to_si(hoverhere::params::make_default_params());
    const BemtConfig cfg = make_bemt_config(p);
    CHECK(cfg.rotor.R == p.helicopter.mainrotor.R);
    CHECK(cfg.rotor.b == p.helicopter.mainrotor.b);
    CHECK(cfg.disc_inertia_tilt == p.helicopter.flapping.I_flapping);
    CHECK(cfg.disc_inertia_spin == p.helicopter.mainrotor.J);
    CHECK(cfg.wake_inertia_fumble_factor == 1.0);  // default of the schema
    CHECK(cfg.vrs.tau_strength == 0.4);            // vrs_time_constant, default of the schema
    CHECK(cfg.vortex_ring_state);
    CHECK(cfg.dynamic_inflow);
    CHECK_FALSE(cfg.quasi_static);

    // the parameter is read from the parameter files; Unity ignores the element
    const std::string xml = R"(<?xml version="1.0"?>
<stru_transmitter_and_helicopter>
  <tuning>
    <rotor_tuning>
      <mainrotor_tuning> <wake_inertia_fumble_factor> <val>25</val> </wake_inertia_fumble_factor> </mainrotor_tuning>
    </rotor_tuning>
  </tuning>
</stru_transmitter_and_helicopter>)";
    hoverhere::params::ModelParams user = hoverhere::params::make_default_params();
    const auto report = hoverhere::io::load_parameters_from_text(xml, "fumble.xml", hoverhere::params::ParamDocument::Heli, user);
    INFO(report.format());
    CHECK(report.ok());
    CHECK(report.diagnostics.empty());
    CHECK(make_bemt_config(hoverhere::params::compile_to_si(user)).wake_inertia_fumble_factor == 25.0);
    CHECK(user.tuning.rotor_tuning.tailrotor_tuning.wake_inertia_fumble_factor == 1.0);
}

TEST_CASE("every grid point can be logged as a CSV row")
{
    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    const BemtGrid grid = make_bemt_grid(cfg);
    BemtFilterState state;
    const BemtOutputs out = settle(cfg, grid, state, hover_inputs(4 * kDeg));
    const std::string csv = bemt_elements_csv(grid, out);

    std::istringstream stream(csv);
    std::string line;
    std::getline(stream, line);
    CHECK(line.rfind("r_index,psi_index,r,psi,beta,Vc,V2,Vi_raw,Vi_target,Vi,alpha,cl,cd,dL,dD,dT,dQ,vrs_weight,residual,iterations", 0) == 0);
    int rows = 0;
    while (std::getline(stream, line)) {
        ++rows;
        std::replace(line.begin(), line.end(), ',', ' ');
        std::istringstream fields(line);
        std::size_t r = 0, c = 0;
        Real radius = 0, psi = 0, beta = 0, vc = 0, v2 = 0, vi_raw = 0, vi_target = 0, vi = 0;
        REQUIRE((fields >> r >> c >> radius >> psi >> beta >> vc >> v2 >> vi_raw >> vi_target >> vi));
        const std::size_t i = bemt_index(r, c);
        CHECK(radius == doctest::Approx(grid.radius[i]).epsilon(1e-8));
        CHECK(psi == doctest::Approx(grid.psi[i]).epsilon(1e-8).scale(1e-9));
        CHECK(vi == doctest::Approx(out.Vi[i]).epsilon(1e-8));
        CHECK(v2 == doctest::Approx(out.V2[i]).epsilon(1e-8));
    }
    CHECK(rows == static_cast<int>(kBemtElementCount));
}

TEST_CASE("a rotor that turns the other way (inverted rotation direction) is the mirror image: same thrust, torque opposing the rotation")
{
    BemtConfig cfg = logo600();
    cfg.quasi_static = true;
    const BemtGrid grid = make_bemt_grid(cfg);
    BemtFilterState normal_state;
    BemtFilterState inverted_state;
    BemtInputs normal = hover_inputs(5 * kDeg);
    BemtInputs inverted = hover_inputs(5 * kDeg);
    inverted.omega_shaft = -kRotorSpeed;
    const BemtOutputs a = bemt_evaluate(cfg, grid, normal_state, normal);
    const BemtOutputs b = bemt_evaluate(cfg, grid, inverted_state, inverted);
    CHECK(a.thrust > 10);
    CHECK(b.thrust == doctest::Approx(a.thrust).epsilon(1e-6));
    CHECK(b.force_O.a[1] == doctest::Approx(a.force_O.a[1]).epsilon(1e-6));
    CHECK(a.shaft_torque < 0);                    // resists the positive rotation
    CHECK(b.shaft_torque > 0);                    // resists the negative rotation
    CHECK(b.shaft_torque == doctest::Approx(-a.shaft_torque).epsilon(1e-6));
    CHECK(b.Vi_mean == doctest::Approx(a.Vi_mean).epsilon(1e-6));
}

TEST_CASE("the element solution is continuous in the axial velocity through the zero crossing of Vc")
{
    // local climb <-> local descent (and the sign change of the induced velocity of an element that sees an upwash): the Newton solver with
    // its warm start must stay on one solution branch; sweep Vc slowly up and down and look for jumps of the raw solution
    BemtConfig cfg = logo600();
    cfg.turbulence = false;
    cfg.ground_effect = false;
    const BemtGrid grid = make_bemt_grid(cfg);
    RotorRng rng;
    const std::size_t probe = bemt_index(3, 0);
    for (const Real collective : {2 * kDeg, 5 * kDeg, 9 * kDeg}) {
        for (const int direction : {+1, -1}) {
            BemtFilterState state;
            Real time = 0;
            Real previous = 0;
            bool first = true;
            Real max_jump = 0;
            for (int k = 0; k <= 400; ++k) {
                const Real vz = direction > 0 ? -5 + 0.05 * k : 15 - 0.05 * k;  // Vc = vz, the zero crossing is inside
                const BemtInputs in = hover_inputs(collective, vz);
                const BemtOutputs out = bemt_evaluate(cfg, grid, state, in);
                bemt_post_step(cfg, state, in, out, time += 0.001, 0.001, rng);
                if (!first) {
                    max_jump = std::max(max_jump, std::abs(out.Vi_raw[probe] - previous));
                }
                first = false;
                previous = out.Vi_raw[probe];
            }
            INFO("collective " << collective / kDeg << " deg, direction " << direction);
            CHECK(max_jump < 0.3);  // [m/s per 0.05 m/s of Vc]
        }
    }
}
