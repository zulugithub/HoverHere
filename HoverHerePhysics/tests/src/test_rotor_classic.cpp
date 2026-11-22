#include <algorithm>
#include <cmath>
#include <cstring>
#include <numbers>

#include <doctest/doctest.h>

#include "hoverhere/aero/rotor_classic.hpp"
#include "hoverhere/core/frames.hpp"
#include "hoverhere/dynamics/rigid_body.hpp"
#include "hoverhere/numerics/rk4.hpp"
#include "hoverhere/params/params.hpp"

using hoverhere::Real;
using namespace hoverhere::aero;
using hoverhere::math::Mat3;
using hoverhere::math::Vec3;
namespace frames = hoverhere::frames;
namespace kin = hoverhere::kinematics;
namespace num = hoverhere::numerics;
namespace qvm = boost::qvm;

namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;
constexpr Real kDeg = kPi / 180;

const hoverhere::params::ModelParamsSI& default_params()
{
    static const hoverhere::params::ModelParamsSI p = hoverhere::params::compile_to_si(hoverhere::params::make_default_params());
    return p;
}

ClassicRotorConfig main_rotor_config()
{
    return make_classic_rotor_config(default_params(), RotorKind::Mainrotor);
}

// helicopter at rest in the air, upright
ClassicRotorInputs hover_inputs(const ClassicRotorConfig& cfg, const Real collective)
{
    ClassicRotorInputs in{};
    in.body.position_O = Vec3{{0, 5, 0}};
    in.body.A_OL = Mat3{{{1, 0, 0}, {0, 1, 0}, {0, 0, 1}}};
    in.omega_shaft = cfg.omega_reference;
    in.control.Theta_col = collective;
    in.rho_air = 1.27;
    in.ground.hub_distance_to_ground = 50;
    return in;
}

Real hover_collective_for_weight(const ClassicRotorConfig& cfg)
{
    // bisection on the collective pitch: thrust equals the weight
    const Real weight = cfg.mass_total * cfg.gravity;
    Real lo = 0, hi = 15 * kDeg;
    const ClassicRotorFilterState state{};
    for (int i = 0; i < 60; ++i) {
        const Real mid = 0.5 * (lo + hi);
        const Real thrust = classic_rotor_evaluate(cfg, state, hover_inputs(cfg, mid)).thrust;
        (thrust < weight ? lo : hi) = mid;
    }
    return 0.5 * (lo + hi);
}

}  // namespace

// ---------------------------------------------------------------------------------------------------------------------
// shared building blocks
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("limit_symmetric saturates smoothly and keeps the slope one inside")
{
    CHECK(limit_symmetric(3, 100, 10) == 3);
    CHECK(limit_symmetric(-85, 100, 10) == -85);
    CHECK(limit_symmetric(500, 100, 10) == 100);
    CHECK(limit_symmetric(-500, 100, 10) == -100);
    // continuous with slope 1 -> 0 over the transition
    Real previous = limit_symmetric(88, 100, 10);
    for (Real x = 88.5; x <= 112; x += 0.5) {
        const Real y = limit_symmetric(x, 100, 10);
        CHECK(y >= previous);
        CHECK(y - previous <= 0.5 + 1e-12);
        previous = y;
    }
    CHECK(limit_symmetric(110, 100, 10) == doctest::Approx(100));
    CHECK(limit_symmetric(90, 100, 10) == doctest::Approx(90));  // start of the transition: still linear
    CHECK(limit_symmetric(-100, 100, 10) == doctest::Approx(-limit_symmetric(100, 100, 10)));
}

TEST_CASE("the hub stiffness is linear for small and progressive for large flapping angles")
{
    const Real k = 120;
    const Real start = 3 * kDeg;
    CHECK(nonlinear_hub_stiffness(k, start, 0) == k);
    CHECK(nonlinear_hub_stiffness(k, start, start) == k);
    CHECK(nonlinear_hub_stiffness(k, start, -start) == k);
    CHECK(nonlinear_hub_stiffness(k, start, start + 0.1) == doctest::Approx(k + (k * 0.1) * (k * 0.1)));
    CHECK(nonlinear_hub_stiffness(k, start, -start - 0.1) == doctest::Approx(k + (k * 0.1) * (k * 0.1)));
}

TEST_CASE("ground effect raises the thrust close to the ground and only when the wake blows towards it")
{
    const Vec3 down{{0, -1, 0}};
    GroundEffectInput ground;
    ground.surface_normal_O = Vec3{{0, 1, 0}};
    const Real R = 0.75;

    ground.hub_distance_to_ground = 100;
    CHECK(ground_effect_factor(R, ground, down, 0, 3) == doctest::Approx(1).epsilon(1e-4));
    ground.hub_distance_to_ground = 0.25;
    const Real near_hover = ground_effect_factor(R, ground, down, 0, 3);
    CHECK(near_hover > 1.05);
    CHECK(near_hover < 1.6);
    ground.hub_distance_to_ground = 0.1;
    CHECK(ground_effect_factor(R, ground, down, 0, 3) > near_hover);               // closer, stronger
    CHECK(ground_effect_factor(R, ground, down, 10, 3) < near_hover);              // forward speed removes the cushion
    CHECK(ground_effect_factor(R, ground, Vec3{{0, 1, 0}}, 0, 3) == 1);            // wake blows away from the ground
    CHECK(ground_effect_factor(R, ground, down, 0, 0) == 1);                       // no induced velocity
    ground.surface_normal_O = Vec3{{0, -1, 0}};                                    // ground above the rotor
    CHECK(ground_effect_factor(R, ground, down, 0, 3) == 1);
}

TEST_CASE("the random numbers of the turbulence are reproducible and uniform in [-1, 1)")
{
    RotorRng a(42), b(42), c(43);
    Real sum = 0;
    bool differs = false;
    for (int i = 0; i < 20000; ++i) {
        const Real x = a.symmetric();
        CHECK(x >= -1);
        CHECK(x < 1);
        CHECK(x == b.symmetric());
        differs = differs || (x != c.symmetric());
        sum += x;
    }
    CHECK(differs);
    CHECK(std::abs(sum / 20000) < 0.02);
}

TEST_CASE("the turbulence strength follows the helicopter motion and changes the random values once per period")
{
    TurbulenceConfig c;
    c.frequency = 5;  // period 0.2 s
    TurbulenceState s;
    RotorRng rng(7);

    // calm: hovering without rotation has no turbulence of its own
    CHECK(turbulence_strength(c, Vec3{{0, 0, 0}}, 0, 0) == 0);
    // fast rotation, slow translation: full strength, fast translation reduces it
    CHECK(turbulence_strength(c, Vec3{{10, 0, 0}}, 0, 0) == doctest::Approx(1));
    CHECK(turbulence_strength(c, Vec3{{10, 0, 0}}, 20, 0) == doctest::Approx(0.25));
    // rotation about the shaft counts only a fifth
    CHECK(turbulence_strength(c, Vec3{{0, 4, 0}}, 0, 0) < turbulence_strength(c, Vec3{{4, 0, 0}}, 0, 0));
    CHECK(turbulence_strength(c, Vec3{{0, 0, 0}}, 0, 0.6) == doctest::Approx(0.6));  // vortex ring state adds
    CHECK(turbulence_strength(c, Vec3{{10, 0, 0}}, 0, 0.6) == 1);                      // limited to one

    // a new draw happens when the time wraps around the period
    Real time = 0;
    Vec3 previous_force = s.force_L;
    int draws = 0;
    for (int i = 0; i < 950; ++i) {  // 0.95 s in 1 ms steps: four periods are completed
        time += 0.001;
        turbulence_post_step(c, s, 1.0, time, 0.001, rng);
        if (qvm::mag(s.force_L - previous_force) > 0) {
            ++draws;
            previous_force = s.force_L;
        }
    }
    CHECK(draws == 4);
    CHECK(std::abs(s.force_L.a[1]) <= c.force_strength.a[1]);
    CHECK(std::abs(s.torque_target_L.a[0]) <= c.torque_strength.a[0]);
}

// ---------------------------------------------------------------------------------------------------------------------
// classic rotor: thrust, torque
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the classic rotor carries the weight at a plausible collective and follows momentum theory")
{
    const ClassicRotorConfig cfg = main_rotor_config();
    const Real collective = hover_collective_for_weight(cfg);
    INFO("hover collective " << collective / kDeg << " deg for " << cfg.mass_total << " kg");
    CHECK(collective > 1 * kDeg);
    CHECK(collective < 12 * kDeg);

    const ClassicRotorFilterState state{};
    const ClassicRotorOutputs out = classic_rotor_evaluate(cfg, state, hover_inputs(cfg, collective));
    CHECK(out.thrust == doctest::Approx(cfg.mass_total * cfg.gravity).epsilon(1e-3));
    // the induced velocity of the hover satisfies v_i = sqrt(T / (2 rho A)) (before the tuning of the inflow)
    const Real rho = 1.27;
    const Real area = std::numbers::pi_v<Real> * cfg.rotor.R * cfg.rotor.R;
    const Real v_momentum = std::sqrt(out.thrust / (2 * rho * area));
    CHECK(out.induced_velocity > 0);  // downwash
    CHECK(out.induced_velocity == doctest::Approx(v_momentum).epsilon(0.05));
    // the thrust acts along the shaft: force on the fuselage is up
    CHECK(out.force_O.a[1] > 0);
    CHECK(std::abs(out.force_O.a[0]) < 1e-3 * out.force_O.a[1]);
    CHECK(std::abs(out.force_O.a[2]) < 1e-3 * out.force_O.a[1]);
}

TEST_CASE("thrust grows with the collective pitch and with the rotor speed")
{
    const ClassicRotorConfig cfg = main_rotor_config();
    const ClassicRotorFilterState state{};
    Real previous = classic_rotor_evaluate(cfg, state, hover_inputs(cfg, 0)).thrust;
    for (Real collective = 1 * kDeg; collective <= 10 * kDeg; collective += 1 * kDeg) {
        const Real thrust = classic_rotor_evaluate(cfg, state, hover_inputs(cfg, collective)).thrust;
        CHECK(thrust > previous);
        previous = thrust;
    }
    ClassicRotorInputs in = hover_inputs(cfg, 5 * kDeg);
    const Real nominal = classic_rotor_evaluate(cfg, state, in).thrust;
    in.omega_shaft *= 0.8;
    CHECK(classic_rotor_evaluate(cfg, state, in).thrust < nominal);
    // negative collective gives negative thrust with the downwash on the other side
    const ClassicRotorOutputs inverted = classic_rotor_evaluate(cfg, state, hover_inputs(cfg, -5 * kDeg));
    CHECK(inverted.thrust < 0);
    CHECK(inverted.induced_velocity < 0);
}

TEST_CASE("the rotor torque opposes the rotation and is the power balance divided by the rotor speed")
{
    const ClassicRotorConfig cfg = main_rotor_config();
    const ClassicRotorFilterState state{};
    const ClassicRotorInputs in = hover_inputs(cfg, hover_collective_for_weight(cfg));
    const ClassicRotorOutputs out = classic_rotor_evaluate(cfg, state, in);
    CHECK(out.torque < 0);  // positive rotation, negative aerodynamic torque
    CHECK(-out.torque * in.omega_shaft == doctest::Approx(out.P_profile + out.P_induced + 0.01 * (out.P_parasite + out.P_climb)).epsilon(1e-9));
    CHECK(out.P_profile > 0);
    CHECK(out.P_induced > 0);
    // the fuselage feels the same torque about the shaft (no hub offset, no flapping in the hover)
    CHECK(out.torque_L.a[1] == doctest::Approx(out.torque).epsilon(1e-9));

    // reversed rotation: the torque changes its sign, the thrust does not
    ClassicRotorInputs reversed = in;
    reversed.omega_shaft = -in.omega_shaft;
    const ClassicRotorOutputs back = classic_rotor_evaluate(cfg, state, reversed);
    CHECK(back.torque > 0);
    CHECK(back.thrust == doctest::Approx(out.thrust).epsilon(1e-6));

    // while freewheeling no shaft torque reaches the fuselage, the rotor still feels it
    ClassicRotorInputs coasting = in;
    coasting.freewheeling = true;
    const ClassicRotorOutputs free = classic_rotor_evaluate(cfg, state, coasting);
    CHECK(free.torque == doctest::Approx(out.torque).epsilon(1e-12));
    CHECK(free.torque_L.a[1] == doctest::Approx(0).scale(1.0));
}

TEST_CASE("a rotor that stands still produces nothing")
{
    const ClassicRotorConfig cfg = main_rotor_config();
    const ClassicRotorFilterState state{};
    ClassicRotorInputs in = hover_inputs(cfg, 8 * kDeg);
    in.omega_shaft = 0.05;
    in.flapping = {0.1, -0.1};
    const ClassicRotorOutputs out = classic_rotor_evaluate(cfg, state, in);
    CHECK(out.thrust == 0);
    CHECK(out.torque == 0);
    CHECK(qvm::mag(out.force_O) == 0);
    CHECK(qvm::mag(out.torque_L) == 0);
    CHECK(out.dflapping[flap::a_s] == 0);
    CHECK(out.dflapping[flap::b_s] == 0);
}

// ---------------------------------------------------------------------------------------------------------------------
// flapping
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("cyclic input drives the flapping angles to the commanded value and tilts the thrust vector the same way")
{
    ClassicRotorConfig cfg = main_rotor_config();
    cfg.vortex_ring_state = false;
    cfg.turbulence = false;
    cfg.ground_effect = false;
    cfg.hub.A_b_s = 0;  // no cross coupling for this check
    cfg.hub.B_a_s = 0;
    cfg.hub.A_a_r = 0;
    cfg.hub.B_a_r = 0;
    const ClassicRotorFilterState state{};

    ClassicRotorInputs in = hover_inputs(cfg, hover_collective_for_weight(cfg));
    in.control.Theta_cyc_b_s = 4 * kDeg;   // lateral: about the rotor x-axis, thrust tilts towards +z
    in.control.Theta_cyc_a_s = -3 * kDeg;  // longitudinal: about the rotor z-axis, positive a_s tilts the thrust towards -x
    const ClassicRotorOutputs first = classic_rotor_evaluate(cfg, state, in);
    CHECK(first.flapping_tau > 0);
    CHECK(first.dflapping[flap::b_s] > 0);
    CHECK(first.dflapping[flap::a_s] < 0);
    CHECK(first.dflapping[flap::b_s] == doctest::Approx(in.control.Theta_cyc_b_s / first.flapping_tau).epsilon(1e-9));

    // integrate the flapping state with RK4: it settles at the commanded angles
    const auto f = [&](const Real, const FlappingState& s) {
        ClassicRotorInputs stage = in;
        stage.flapping = s;
        return classic_rotor_evaluate(cfg, state, stage).dflapping;
    };
    FlappingState x{0, 0};
    const Real dt = 0.001;
    for (int i = 0; i < 4000; ++i) {
        x = num::rk4_step(f, i * dt, x, dt);
    }
    CHECK(x[flap::b_s] == doctest::Approx(in.control.Theta_cyc_b_s).epsilon(1e-3));
    CHECK(x[flap::a_s] == doctest::Approx(in.control.Theta_cyc_a_s).epsilon(1e-3));

    in.flapping = x;
    const ClassicRotorOutputs tilted = classic_rotor_evaluate(cfg, state, in);
    CHECK(tilted.force_O.a[2] > 0);                       // towards +z
    CHECK(tilted.force_O.a[0] > 0);                       // a_s < 0 tilts towards +x
    CHECK(tilted.force_O.a[2] / tilted.force_O.a[1] == doctest::Approx(std::tan(x[flap::b_s])).epsilon(0.02));
    // the hub offset and the hub stiffness create moments that roll the fuselage the same way
    CHECK(tilted.torque_L.a[0] > 0);
}

TEST_CASE("a body rotation drags the flapping angles along and forward flight tips the disc up")
{
    ClassicRotorConfig cfg = main_rotor_config();
    const ClassicRotorFilterState state{};
    ClassicRotorInputs in = hover_inputs(cfg, 4 * kDeg);

    in.body.omega_L = Vec3{{0.5, 0, 0}};  // rolling to the right
    const ClassicRotorOutputs rolling = classic_rotor_evaluate(cfg, state, in);
    CHECK(rolling.dflapping[flap::b_s] < 0);  // the disc lags behind: relative to the fuselage it tilts the other way

    in.body.omega_L = Vec3{{0, 0, 0}};
    in.body.velocity_L = Vec3{{15, 0, 0}};  // flying forward along +x
    in.body.velocity_O = in.body.velocity_L;
    const ClassicRotorOutputs forward = classic_rotor_evaluate(cfg, state, in);
    // the term A_a_r * u_a drives a_s
    ClassicRotorInputs hover = hover_inputs(cfg, 4 * kDeg);
    const Real difference = forward.dflapping[flap::a_s] - classic_rotor_evaluate(cfg, state, hover).dflapping[flap::a_s];
    const Real reduce = 1;  // at nominal speed the flapping is not reduced
    CHECK(difference == doctest::Approx(cfg.hub.A_a_r * 15 * reduce).epsilon(1e-9));
    CHECK(forward.horizontal_speed == doctest::Approx(15));
    // the forward flight air speed at the hub is 15 m/s along the body x-axis (the rotor frame of the default helicopter is the body frame)
    CHECK(forward.hub_speed == doctest::Approx(15));
}

TEST_CASE("the flapping stays fully effective at 50 % of the nominal speed and fades out only near standstill")
{
    ClassicRotorConfig cfg = main_rotor_config();
    const ClassicRotorFilterState state{};
    ClassicRotorInputs in = hover_inputs(cfg, 4 * kDeg);
    in.control.Theta_cyc_b_s = 0.1;
    CHECK(cfg.hub.low_rpm_flapping_fade_upper == doctest::Approx(0.10));
    CHECK(cfg.hub.low_rpm_flapping_fade_lower == doctest::Approx(0.03));
    in.omega_shaft = cfg.omega_reference;
    const ClassicRotorOutputs full = classic_rotor_evaluate(cfg, state, in);
    in.omega_shaft = 0.5 * cfg.omega_reference;  // autorotation: the commanded tilt is reached as at nominal speed
    const ClassicRotorOutputs autorotation = classic_rotor_evaluate(cfg, state, in);
    CHECK(autorotation.dflapping[flap::b_s] * autorotation.flapping_tau == doctest::Approx(full.dflapping[flap::b_s] * full.flapping_tau).epsilon(1e-9));
    FlappingState angles{0.05, -0.04};
    limit_flapping_at_low_rpm(angles, 0.5 * cfg.omega_reference, cfg.omega_reference, cfg.hub.low_rpm_flapping_fade_upper,
                              cfg.hub.low_rpm_flapping_fade_lower, 0.001);
    CHECK(angles[0] == 0.05);
    CHECK(angles[1] == -0.04);
    in.omega_shaft = 0.02 * cfg.omega_reference;  // on the ground: switched off
    CHECK(classic_rotor_evaluate(cfg, state, in).dflapping[flap::b_s] == 0);
    // a raised upper ratio brings the old behavior (fade below 70 %) back
    cfg.hub.low_rpm_flapping_fade_upper = 0.7;
    in.omega_shaft = 0.5 * cfg.omega_reference;
    const ClassicRotorOutputs faded = classic_rotor_evaluate(cfg, state, in);
    CHECK(std::abs(faded.dflapping[flap::b_s]) * faded.flapping_tau < std::abs(full.dflapping[flap::b_s]) * full.flapping_tau);
}

TEST_CASE("the flapping fades out at low rotor speed")
{
    ClassicRotorConfig cfg = main_rotor_config();
    const ClassicRotorFilterState state{};
    ClassicRotorInputs in = hover_inputs(cfg, 4 * kDeg);
    in.control.Theta_cyc_b_s = 0.1;
    in.omega_shaft = cfg.omega_reference;
    const Real full = classic_rotor_evaluate(cfg, state, in).dflapping[flap::b_s] * classic_rotor_evaluate(cfg, state, in).flapping_tau;
    in.omega_shaft = 0.06 * cfg.omega_reference;
    const ClassicRotorOutputs partial = classic_rotor_evaluate(cfg, state, in);
    // the commanded value is the same but its derivative is reduced between the lower and upper fade ratio (3 % and 10 %) of the nominal speed
    CHECK(std::abs(partial.dflapping[flap::b_s]) * partial.flapping_tau < full);
    in.omega_shaft = 0.02 * cfg.omega_reference;
    const ClassicRotorOutputs slow = classic_rotor_evaluate(cfg, state, in);
    CHECK(std::abs(slow.dflapping[flap::b_s]) <= std::abs(partial.dflapping[flap::b_s]) + 1e-12);

    const Real up = cfg.hub.low_rpm_flapping_fade_upper;
    const Real lo = cfg.hub.low_rpm_flapping_fade_lower;
    FlappingState angles{0.05, -0.04};
    limit_flapping_at_low_rpm(angles, cfg.omega_reference, cfg.omega_reference, up, lo, 0.001);
    CHECK(angles[0] == 0.05);  // nominal speed: untouched
    limit_flapping_at_low_rpm(angles, 0.06 * cfg.omega_reference, cfg.omega_reference, up, lo, 0.001);
    CHECK(std::abs(angles[0]) < 0.05);  // decays
    CHECK(angles[0] > 0);               // towards zero from the same side
    CHECK(angles[1] < 0);
    limit_flapping_at_low_rpm(angles, 30 * frames::kRpmToRadPerSec, cfg.omega_reference, up, lo, 0.001);
    CHECK(angles[0] == 0);
    CHECK(angles[1] == 0);
}

// ---------------------------------------------------------------------------------------------------------------------
// vortex ring state, ground effect, turbulence
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the vortex ring state develops in a settling descent and disappears again in level flight")
{
    ClassicRotorConfig cfg = main_rotor_config();
    ClassicRotorFilterState state;
    RotorRng rng;
    const Real collective = hover_collective_for_weight(cfg);

    ClassicRotorInputs in = hover_inputs(cfg, collective);
    // descending at the centre of the settling area of the speed diagram: 60 degrees descent angle at the vertical speed of the parameters
    const Real centre_horizontal = std::abs(cfg.vortex_ring.v_vertical / std::tan(60 * kDeg));
    in.body.velocity_L = Vec3{{centre_horizontal, -std::abs(cfg.vortex_ring.v_vertical), 0}};
    in.body.velocity_O = in.body.velocity_L;
    Real time = 0;
    Real previous = 0;
    bool monotonic = true;
    for (int i = 0; i < 4000; ++i) {
        const ClassicRotorOutputs out = classic_rotor_evaluate(cfg, state, in);
        time += 0.001;
        classic_rotor_post_step(cfg, state, in, out, time, 0.001, rng);
        monotonic = monotonic && state.vrs_strength >= previous - 1e-15;
        previous = state.vrs_strength;
    }
    CHECK(monotonic);
    CHECK(state.vrs_strength > 0.9);

    // the fully developed vortex ring reduces the force on the fuselage by the tuning factor
    ClassicRotorFilterState none;
    ClassicRotorFilterState developed;
    developed.vrs_strength = state.vrs_strength;  // the vortex ring alone, without the random turbulence it also excites
    const ClassicRotorOutputs without = classic_rotor_evaluate(cfg, none, in);
    const ClassicRotorOutputs with = classic_rotor_evaluate(cfg, developed, in);
    CHECK(with.force_O.a[1] < without.force_O.a[1]);
    CHECK(with.force_O.a[1] / without.force_O.a[1] == doctest::Approx(1 - state.vrs_strength * cfg.vortex_ring.force_reduction_factor).epsilon(1e-9));
    CHECK(with.torque_L.a[1] / without.torque_L.a[1] == doctest::Approx(1 - state.vrs_strength * cfg.vortex_ring.torque_reduction_factor).epsilon(1e-9));

    // in level flight at speed the strength decays
    in.body.velocity_L = Vec3{{20, 0, 0}};
    in.body.velocity_O = in.body.velocity_L;
    for (int i = 0; i < 4000; ++i) {
        const ClassicRotorOutputs out = classic_rotor_evaluate(cfg, state, in);
        time += 0.001;
        classic_rotor_post_step(cfg, state, in, out, time, 0.001, rng);
    }
    CHECK(state.vrs_strength == 0);

    // the flag switches it off
    cfg.vortex_ring_state = false;
    state.vrs_strength = 0.7;
    in.body.velocity_L = Vec3{{centre_horizontal, -std::abs(cfg.vortex_ring.v_vertical), 0}};
    in.body.velocity_O = in.body.velocity_L;
    const ClassicRotorOutputs off = classic_rotor_evaluate(cfg, state, in);
    classic_rotor_post_step(cfg, state, in, off, 0.001, 0.001, rng);
    CHECK(state.vrs_strength == 0);
}

TEST_CASE("the classic ground effect increases the force of the main rotor but not of a rotor without the effect")
{
    ClassicRotorConfig cfg = main_rotor_config();
    cfg.turbulence = false;
    const ClassicRotorFilterState state{};
    ClassicRotorInputs in = hover_inputs(cfg, hover_collective_for_weight(cfg));
    in.ground.hub_distance_to_ground = 0.2;

    cfg.ground_effect = false;
    const ClassicRotorOutputs baseline = classic_rotor_evaluate(cfg, state, in);
    CHECK(baseline.ground_effect_factor == 1);

    cfg.ground_effect = true;
    const ClassicRotorOutputs near = classic_rotor_evaluate(cfg, state, in);
    CHECK(near.ground_effect_factor > 1.05);
    CHECK(near.force_O.a[1] == doctest::Approx(baseline.force_O.a[1] * near.ground_effect_factor).epsilon(1e-9));
    CHECK(near.thrust == baseline.thrust);                                   // the thrust value itself is not changed
    CHECK(near.torque_L.a[1] == doctest::Approx(baseline.torque_L.a[1]));   // and neither is the torque

    in.ground.hub_distance_to_ground = 50;
    CHECK(classic_rotor_evaluate(cfg, state, in).ground_effect_factor == doctest::Approx(1).epsilon(1e-3));
}

TEST_CASE("turbulence acts on the fuselage only when the flag is set")
{
    ClassicRotorConfig cfg = main_rotor_config();
    cfg.vortex_ring_state = false;
    cfg.ground_effect = false;
    cfg.turbulence_config.torque_smooth_time = 0.3;  // quick filter: the effect shows within the 0.7 s of this test
    ClassicRotorFilterState state;
    RotorRng rng(5);
    ClassicRotorInputs in = hover_inputs(cfg, hover_collective_for_weight(cfg));
    in.body.omega_L = Vec3{{6, 0, 0}};  // fast rotation: full turbulence strength

    const ClassicRotorOutputs quiet = classic_rotor_evaluate(cfg, state, in);
    Real time = 0;
    for (int i = 0; i < 700; ++i) {
        const ClassicRotorOutputs out = classic_rotor_evaluate(cfg, state, in);
        time += 0.001;
        classic_rotor_post_step(cfg, state, in, out, time, 0.001, rng);
    }
    const ClassicRotorOutputs shaken = classic_rotor_evaluate(cfg, state, in);
    CHECK(shaken.turbulence_strength == doctest::Approx(1));
    CHECK(qvm::mag(shaken.force_O - quiet.force_O) > 0.1);
    CHECK(qvm::mag(shaken.torque_L - quiet.torque_L) > 0.1);

    cfg.turbulence = false;
    const ClassicRotorOutputs off = classic_rotor_evaluate(cfg, state, in);
    CHECK(qvm::mag(off.force_O - quiet.force_O) < 1e-9);
}

TEST_CASE("the default turbulence torque has the strength the parameters were tuned for (rms 5 % of the amplitude, as in the Unity code)")
{
    const TurbulenceConfig c{};  // defaults: 5 Hz draws, amplitude 50 Nm about x, smoothing time of the legacy behavior
    TurbulenceState s;
    RotorRng rng(11);
    const Real dt = 0.001;
    Real sum_square = 0;
    int samples = 0;
    for (int i = 0; i < 120000; ++i) {
        const Real time = (i + 1) * dt;
        turbulence_post_step(c, s, 1, time, dt, rng);
        if (time > 30) {
            sum_square += s.torque_L.a[0] * s.torque_L.a[0];
            ++samples;
        }
    }
    const Real rms = std::sqrt(sum_square / samples) / c.torque_strength.a[0];
    CHECK(rms > 0.03);
    CHECK(rms < 0.08);
}

// ---------------------------------------------------------------------------------------------------------------------
// time structure
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("evaluating the classic rotor in RK4 stages leaves the filter state untouched")
{
    const ClassicRotorConfig cfg = main_rotor_config();
    ClassicRotorFilterState state;
    RotorRng rng(9);
    ClassicRotorInputs in = hover_inputs(cfg, hover_collective_for_weight(cfg));
    in.body.omega_L = Vec3{{3, 0.5, 2}};
    in.body.velocity_L = Vec3{{0, -2.5, 0}};
    in.body.velocity_O = in.body.velocity_L;
    Real time = 0;
    for (int i = 0; i < 500; ++i) {  // non trivial filter state
        const ClassicRotorOutputs out = classic_rotor_evaluate(cfg, state, in);
        time += 0.001;
        classic_rotor_post_step(cfg, state, in, out, time, 0.001, rng);
    }
    REQUIRE(state.vrs_strength > 0);

    const ClassicRotorFilterState before = state;
    const auto f = [&](const Real, const FlappingState& s) {
        ClassicRotorInputs stage = in;
        stage.flapping = s;
        return classic_rotor_evaluate(cfg, state, stage).dflapping;
    };
    FlappingState x{0.02, -0.01};
    for (int i = 0; i < 10; ++i) {
        x = num::rk4_step(f, 0, x, 0.001);
    }
    CHECK(std::memcmp(&before, &state, sizeof(ClassicRotorFilterState)) == 0);

    // and the evaluation is a pure function
    const ClassicRotorOutputs a = classic_rotor_evaluate(cfg, state, in);
    const ClassicRotorOutputs b = classic_rotor_evaluate(cfg, state, in);
    CHECK(a.thrust == b.thrust);
    CHECK(a.force_O.a[1] == b.force_O.a[1]);
    CHECK(a.dflapping == b.dflapping);
}

// ---------------------------------------------------------------------------------------------------------------------
// control input
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the effective pitch angles come from the ideal CCPM mixer and from the tail kinematics")
{
    const kin::CcpmMixer mixer = kin::make_default_ccpm_mixer();
    kin::SwashplateCommand command;
    command.collective = 0.003;
    command.roll = 5 * kDeg;
    command.pitch = -4 * kDeg;
    const RotorControl c = classic_control_from_ccpm(mixer, command);
    CHECK(c.Theta_col == doctest::Approx(mixer.beta_0 + mixer.c_collective * 0.003));
    CHECK(c.Theta_cyc_b_s == doctest::Approx(mixer.c_cyclic * 5 * kDeg));
    CHECK(c.Theta_cyc_a_s == doctest::Approx(mixer.c_cyclic * -4 * kDeg));
    // from servo angles: the same through the exact inverse of the mixer
    const RotorControl d = classic_control_from_ccpm_servos(mixer, kin::ccpm_servos(mixer, command));
    CHECK(d.Theta_col == doctest::Approx(c.Theta_col).epsilon(1e-12));
    CHECK(d.Theta_cyc_a_s == doctest::Approx(c.Theta_cyc_a_s).epsilon(1e-12));
    CHECK(d.Theta_cyc_b_s == doctest::Approx(c.Theta_cyc_b_s).epsilon(1e-12));

    kin::TailrotorPose pose{};
    pose.blade[0].beta = 0.2;
    CHECK(classic_control_from_tailrotor(pose, -0.1).Theta_col == doctest::Approx(-0.3));  // Theta_col_0 - beta
    CHECK(classic_control_from_tailrotor(pose, -0.1).Theta_cyc_a_s == 0);
    CHECK(classic_control_from_tailrotor(pose, -0.1).Theta_cyc_b_s == 0);
}

TEST_CASE("the configuration of the rotors follows the Unity ODE")
{
    const auto& p = default_params();
    const ClassicRotorConfig main = make_classic_rotor_config(p, RotorKind::Mainrotor);
    CHECK(main.flapping);
    CHECK(main.vortex_ring_state);
    CHECK(main.turbulence);
    CHECK(main.ground_effect);
    CHECK(main.mass_total == doctest::Approx(p.helicopter.mass_total));
    const ClassicRotorConfig tail = make_classic_rotor_config(p, RotorKind::Tailrotor);
    CHECK_FALSE(tail.flapping);
    CHECK_FALSE(tail.vortex_ring_state);
    CHECK_FALSE(tail.turbulence);
    CHECK_FALSE(tail.ground_effect);
    const ClassicRotorConfig propeller = make_classic_rotor_config(p, RotorKind::Propeller);
    CHECK_FALSE(propeller.flapping);

    auto tandem = p;
    tandem.helicopter.rotor_systems_configuration = 1;
    const ClassicRotorConfig second = make_classic_rotor_config(tandem, RotorKind::Tailrotor);
    CHECK(second.flapping);
    CHECK(second.ground_effect);
    CHECK(second.mass_total == doctest::Approx(p.helicopter.mass_total / 2));
    CHECK(make_classic_rotor_config(tandem, RotorKind::Mainrotor).mass_total == doctest::Approx(p.helicopter.mass_total / 2));
}

// ---------------------------------------------------------------------------------------------------------------------
// kinematics helpers
// ---------------------------------------------------------------------------------------------------------------------
TEST_CASE("the body kinematics give the velocities in both frames")
{
    using namespace hoverhere::dynamics;
    const Mat3 attitude = frames::A_RL_S123(0.3, -0.5, 1.1);
    const RigidBodyState state = make_rigid_body_state(Vec3{{1, 2, 3}}, frames::quat_from_A(attitude), Vec3{{4, -5, 6}}, Vec3{{0.1, 0.2, 0.3}});
    const BodyKinematics k = body_kinematics(state);
    CHECK(qvm::mag(k.position_O - Vec3{{1, 2, 3}}) == 0);
    CHECK(qvm::mag(k.velocity_O - Vec3{{4, -5, 6}}) == 0);
    CHECK(qvm::mag(k.velocity_L - frames::transposed(attitude) * Vec3{{4, -5, 6}}) < 1e-12);
    CHECK(qvm::mag(k.A_OL * k.velocity_L - k.velocity_O) < 1e-12);
    CHECK(qvm::mag(k.omega_L - Vec3{{0.1, 0.2, 0.3}}) == 0);
}

TEST_CASE("the wind is a world frame vector from direction, speed and the vertical component")
{
    hoverhere::params::si::Weather weather{};
    weather.wind_direction = 0;
    weather.wind_speed = 5;
    weather.wind_speed_vertical = 0.5;
    const Vec3 along_z = wind_velocity_O(weather);
    CHECK(along_z.a[0] == doctest::Approx(0).scale(1.0));
    CHECK(along_z.a[1] == 0.5);
    CHECK(along_z.a[2] == doctest::Approx(5));

    weather.wind_direction = kPi / 2;
    const Vec3 turned = wind_velocity_O(weather);
    CHECK(turned.a[0] == doctest::Approx(5));
    CHECK(turned.a[2] == doctest::Approx(0).scale(1.0));
    CHECK(std::hypot(turned.a[0], turned.a[2]) == doctest::Approx(5));
}
