#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstring>
#include <filesystem>
#include <limits>
#include <optional>
#include <thread>
#include <vector>

#include <doctest/doctest.h>

#include "hoverhere/io/param_loader.hpp"
#include "hoverhere/simulation/helicopter_sim.hpp"

namespace fs = std::filesystem;
namespace io = hoverhere::io;
namespace hp = hoverhere::params;
using namespace hoverhere::simulation;
using hoverhere::Real;

namespace {

fs::path logo_resources()
{
    return fs::path(HOVERHERE_UNITY_ASSETS_DIR) / "Helicopters_Available" / "Logo600SE_V3" / "Resources";
}

std::optional<SimSetup> logo_setup(const bool with_mechanics = true)
{
    const fs::path file = logo_resources() / "Logo600SE_V3_parameter_file.xml";
    if (!fs::exists(file)) {
        return std::nullopt;
    }
    SimSetup setup;
    if (!io::load_parameters_file(file, hp::ParamDocument::Heli, setup.params).ok()) {
        return std::nullopt;
    }
    if (with_mechanics) {
        auto main = hp::mechanics::make_default_mainrotor_mechanics();
        if (io::load_mainrotor_mechanics_file(logo_resources() / "Logo600SE_V3_mainrotor_mechanics.xml", main).ok()) {
            setup.mainrotor_mechanics = main;
        }
        auto tail = hp::mechanics::make_default_tailrotor_mechanics();
        if (io::load_tailrotor_mechanics_file(logo_resources() / "Logo600SE_V3_tailrotor_mechanics.xml", tail).ok()) {
            setup.tailrotor_mechanics = tail;
        }
    }
    return setup;
}

#define REQUIRE_LOGO(var, ...)                                       \
    const auto var##_opt = logo_setup(__VA_ARGS__);                  \
    if (!var##_opt) {                                                \
        WARN_MESSAGE(false, "Unity assets not found, test skipped"); \
        return;                                                      \
    }                                                                \
    const SimSetup& var = *var##_opt

// flat ground y = 0 over a large square
struct Ground {
    std::vector<double> vertices;
    std::vector<std::uint32_t> indices;
    explicit Ground(const double half = 200)
        : vertices{-half, 0, -half, half, 0, -half, half, 0, half, -half, 0, half}, indices{0, 3, 1, 1, 3, 2}
    {
    }
};

// lowest contact point of the body frame
double lowest_contact_point(const SimSetup& setup)
{
    const auto si = hp::compile_to_si(setup.params);
    double lowest = 0;
    const auto& c = si.helicopter.collision;
    for (const auto* list : {&c.positions_usual, &c.positions_left, &c.positions_right, &c.positions_steering_center,
                             &c.positions_steering_left, &c.positions_steering_right}) {
        for (std::int32_t i = 0; i < list->count; ++i) {
            lowest = std::min(lowest, list->items[i].a[1]);
        }
    }
    return lowest;
}

hh_reset_params on_ground(const SimSetup& setup, const bool spooled_up = true)
{
    hh_reset_params r = default_reset_params();
    r.rotor_spooled_up = spooled_up ? 1 : 0;
    r.position_O[1] = -lowest_contact_point(setup) + 0.005;
    return r;
}

hh_sim_options options(const int rotor_model)
{
    hh_sim_options o = default_sim_options();
    o.rotor_model = rotor_model;
    return o;
}

bool finite_outputs(const hh_sim_outputs& o)
{
    const double* p = &o.position_O[0];
    for (int i = 0; i < 3; ++i) {
        if (!std::isfinite(p[i]) || !std::isfinite(o.velocity_O[i]) || !std::isfinite(o.omega_L[i])) {
            return false;
        }
    }
    return std::isfinite(o.mainrotor_speed) && std::isfinite(o.mainrotor.thrust);
}

void run(HelicopterSim& sim, const hh_sim_inputs& in, const double seconds)
{
    const int steps = static_cast<int>(std::lround(seconds / sim.dt()));
    for (int i = 0; i < steps; ++i) {
        sim.step_fixed(in);
    }
}

}  // namespace

TEST_CASE("the simulation configures, rejects bad options and works without mechanics")
{
    REQUIRE_LOGO(setup);
    HelicopterSim sim;
    CHECK_FALSE(sim.configured());
    std::string error;
    hh_sim_options bad = default_sim_options();
    bad.dt = 0;
    CHECK_FALSE(sim.configure(setup, bad, &error));
    CHECK_FALSE(error.empty());
    CHECK_FALSE(sim.configured());

    CHECK(sim.configure(setup, options(HH_ROTOR_BEMT), &error));
    CHECK(sim.configured());
    CHECK(sim.dt() == doctest::Approx(0.001));

    SimSetup no_mechanics = setup;
    no_mechanics.mainrotor_mechanics.reset();
    no_mechanics.tailrotor_mechanics.reset();
    REQUIRE(sim.configure(no_mechanics, options(HH_ROTOR_BEMT), &error));
    sim.reset(on_ground(setup));
    hh_sim_outputs out;
    sim.fill_outputs(out);
    CHECK((out.flags & HH_FLAG_MECHANICS_MAINROTOR) == 0);
    CHECK((out.flags & HH_FLAG_MAINROTOR_BEMT) != 0);
}

TEST_CASE("spooled up helicopter stands on the ground, the governor holds the rotor speed, nothing diverges")
{
    REQUIRE_LOGO(setup);
    for (const int model : {HH_ROTOR_CLASSIC, HH_ROTOR_BEMT}) {
        INFO("rotor model " << model);
        HelicopterSim sim;
        REQUIRE(sim.configure(setup, options(model)));
        const Ground ground;
        REQUIRE(sim.set_ground_mesh(std::span<const double>(ground.vertices), std::span<const std::uint32_t>(ground.indices)));
        sim.reset(on_ground(setup));

        hh_sim_inputs in = default_sim_inputs();
        in.collective = 0;  // flat pitch: the helicopter stays down
        hh_sim_outputs out;
        sim.fill_outputs(out);
        const double target = out.governor_target;
        CHECK(std::abs(out.mainrotor_speed) == doctest::Approx(std::abs(target) / sim.model_params().helicopter.transmission.n_mo2mr).epsilon(0.02));

        run(sim, in, model == HH_ROTOR_BEMT ? 0.6 : 1.5);
        sim.fill_outputs(out);
        CHECK(finite_outputs(out));
        CHECK((out.flags & HH_FLAG_DIVERGED) == 0);
        CHECK((out.flags & HH_FLAG_CRASHED) == 0);
        CHECK((out.flags & HH_FLAG_ANY_CONTACT) != 0);
        CHECK(out.position_O[1] > 0.0);
        CHECK(out.position_O[1] < 1.0);
        CHECK(std::abs(out.velocity_O[1]) < 0.3);
        const double omega_nominal = std::abs(target) / sim.model_params().helicopter.transmission.n_mo2mr;
        CHECK(std::abs(out.mainrotor_speed) == doctest::Approx(omega_nominal).epsilon(0.08));
        CHECK(out.step_count == static_cast<std::uint64_t>(std::lround((model == HH_ROTOR_BEMT ? 0.6 : 1.5) / sim.dt())));

        // contact results: per point forces add up, the gear carries about the weight
        double normal_sum_y = 0;
        int touching = 0;
        for (int i = 0; i < out.contact_count; ++i) {
            const hh_contact& c = out.contacts[i];
            if (c.in_contact != 0) {
                ++touching;
                normal_sum_y += c.normal_force_O[1] + c.friction_force_O[1];
                CHECK(c.penetration >= 0);
                CHECK(c.normal_force >= 0);
            }
        }
        CHECK(touching > 0);
        const double weight = sim.model_params().helicopter.mass_total * sim.model_params().scenery.gravity;
        // static balance: the gear carries the weight minus what the rotors lift
        const double lift = out.mainrotor.force_O[1] + out.tailrotor.force_O[1];
        CHECK(normal_sum_y == doctest::Approx(weight - lift).epsilon(0.15));
    }
}

TEST_CASE("full collective lifts the helicopter off the ground")
{
    REQUIRE_LOGO(setup);
    HelicopterSim sim;
    REQUIRE(sim.configure(setup, options(HH_ROTOR_CLASSIC)));
    const Ground ground;
    REQUIRE(sim.set_ground_mesh(std::span<const double>(ground.vertices), std::span<const std::uint32_t>(ground.indices)));
    sim.reset(on_ground(setup));
    hh_sim_inputs in = default_sim_inputs();
    in.collective = 1.0;
    hh_sim_outputs out;
    double max_height = 0;
    for (int i = 0; i < 4000; ++i) {
        sim.step_fixed(in);
        if (i % 100 == 0) {
            sim.fill_outputs(out);
            max_height = std::max(max_height, out.position_O[1]);
        }
    }
    sim.fill_outputs(out);
    CHECK(finite_outputs(out));
    CHECK(max_height > 1.0);
    CHECK(out.mainrotor.thrust > 0);
    CHECK(out.mainrotor.induced_velocity > 0);
    CHECK(out.audio.mainrotor_speed == doctest::Approx(out.mainrotor_speed));
}

TEST_CASE("step_fixed is deterministic and reset reproduces a run exactly")
{
    REQUIRE_LOGO(setup);
    const auto run_once = [&](HelicopterSim& sim) {
        sim.reset(on_ground(setup));
        hh_sim_inputs in = default_sim_inputs();
        in.collective = 0.3;
        in.roll = 0.1;
        in.pitch = -0.05;
        run(sim, in, 0.3);
        hh_sim_outputs out;
        sim.fill_outputs(out);
        return out;
    };
    HelicopterSim a;
    HelicopterSim b;
    const Ground ground;
    for (HelicopterSim* sim : {&a, &b}) {
        REQUIRE(sim->configure(setup, options(HH_ROTOR_BEMT)));
        REQUIRE(sim->set_ground_mesh(std::span<const double>(ground.vertices), std::span<const std::uint32_t>(ground.indices)));
    }
    const hh_sim_outputs oa = run_once(a);
    const hh_sim_outputs ob = run_once(b);
    const hh_sim_outputs oa2 = run_once(a);  // same simulator again after a reset
    CHECK(std::memcmp(oa.position_O, ob.position_O, sizeof(oa.position_O)) == 0);
    CHECK(std::memcmp(oa.orientation, ob.orientation, sizeof(oa.orientation)) == 0);
    CHECK(oa.mainrotor_speed == ob.mainrotor_speed);
    CHECK(oa.mainrotor.thrust == ob.mainrotor.thrust);
    CHECK(std::memcmp(oa.position_O, oa2.position_O, sizeof(oa.position_O)) == 0);
    CHECK(oa.mainrotor_speed == oa2.mainrotor_speed);
    CHECK(oa.mainrotor.thrust == oa2.mainrotor.thrust);
}

TEST_CASE("advance turns frame times into whole fixed steps, with the frame cut and the substep clamp")
{
    REQUIRE_LOGO(setup);
    hh_sim_options o = options(HH_ROTOR_CLASSIC);
    o.max_frame_dt = 0.05;
    o.max_substeps = 30;
    HelicopterSim sim;
    REQUIRE(sim.configure(setup, o));
    sim.reset(on_ground(setup));
    const hh_sim_inputs in = default_sim_inputs();
    CHECK(sim.advance(0.0105, in) == 10);   // 0.5 ms are kept
    CHECK(sim.advance(0.0005, in) == 1);    // ... and complete the next step
    CHECK(sim.advance(0.0, in) == 0);
    CHECK(sim.advance(-1.0, in) == 0);
    CHECK(sim.advance(1.0, in) == 30);      // cut to 50 ms = 50 steps, then limited to 30
    CHECK(sim.step_count() == 41);
}

TEST_CASE("the physics thread runs on its own and the game thread reads without locks")
{
    REQUIRE_LOGO(setup);
    HelicopterSim sim;
    REQUIRE(sim.configure(setup, options(HH_ROTOR_CLASSIC)));
    const Ground ground;
    REQUIRE(sim.set_ground_mesh(std::span<const double>(ground.vertices), std::span<const std::uint32_t>(ground.indices)));
    sim.reset(on_ground(setup));
    hh_sim_outputs out;
    CHECK_FALSE(sim.read_outputs(out));

    hh_sim_inputs in = default_sim_inputs();
    in.collective = -1;
    sim.post_inputs(in);
    REQUIRE(sim.start_thread());
    CHECK(sim.thread_running());
    CHECK_FALSE(sim.start_thread());
    sim.step_fixed(in);  // ignored while the thread owns the state

    bool got = false;
    double last_time = -1;
    int changes = 0;
    const auto deadline = std::chrono::steady_clock::now() + std::chrono::milliseconds(3000);
    while (std::chrono::steady_clock::now() < deadline && changes < 5) {
        if (sim.read_outputs(out)) {
            got = true;
            if (out.time != last_time) {
                ++changes;
                last_time = out.time;
            }
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(5));
    }
    sim.stop_thread();
    CHECK_FALSE(sim.thread_running());
    CHECK(got);
    CHECK(changes >= 5);
    CHECK(out.time > 0.0);
    CHECK(out.time < 3.0);
    CHECK(out.step_count > 0);
    CHECK(finite_outputs(out));

    // after the thread stopped the synchronous mode works again
    const auto before = sim.step_count();
    sim.step_fixed(in);
    CHECK(sim.step_count() == before + 1);
}

TEST_CASE("BEMT telemetry exposes the 4 x 10 grid, the classic rotor has none")
{
    REQUIRE_LOGO(setup);
    HelicopterSim sim;
    REQUIRE(sim.configure(setup, options(HH_ROTOR_BEMT)));
    sim.reset(on_ground(setup));
    hh_sim_inputs in = default_sim_inputs();
    in.collective = -0.5;
    run(sim, in, 0.05);
    hh_bemt_telemetry tele{};
    tele.struct_size = sizeof(tele);
    CHECK(sim.fill_bemt_telemetry(tele));
    CHECK(tele.valid == 1);
    CHECK(tele.radius[0] > 0);
    CHECK(tele.radius[HH_BEMT_ELEMENTS - 1] > tele.radius[0]);
    CHECK(std::isfinite(tele.thrust));
    const std::string csv = sim.bemt_elements_csv();
    CHECK(std::count(csv.begin(), csv.end(), '\n') == 1 + HH_BEMT_ELEMENTS);

    REQUIRE(sim.configure(setup, options(HH_ROTOR_CLASSIC)));
    sim.reset(on_ground(setup));
    CHECK_FALSE(sim.fill_bemt_telemetry(tele));
    CHECK(tele.valid == 0);
    CHECK(sim.bemt_elements_csv().empty());
}

TEST_CASE("servo, swashplate and mechanism outputs follow the sticks")
{
    REQUIRE_LOGO(setup);
    HelicopterSim sim;
    REQUIRE(sim.configure(setup, options(HH_ROTOR_BEMT)));
    sim.reset(on_ground(setup));
    hh_sim_inputs in = default_sim_inputs();
    in.collective = 0.0;
    run(sim, in, 0.3);
    hh_sim_outputs neutral;
    sim.fill_outputs(neutral);
    CHECK((neutral.flags & HH_FLAG_MECHANICS_MAINROTOR) != 0);
    CHECK((neutral.flags & HH_FLAG_MECHANICS_TAILROTOR) != 0);
    CHECK(neutral.mainrotor_mechanics.valid == 1);
    CHECK(neutral.tailrotor_mechanics.valid == 1);

    in.pitch = 0.5;  // forward cyclic
    in.collective = 0.5;
    run(sim, in, 0.4);
    hh_sim_outputs pushed;
    sim.fill_outputs(pushed);
    CHECK(pushed.swashplate_collective != doctest::Approx(neutral.swashplate_collective));
    CHECK(pushed.swashplate_pitch != doctest::Approx(neutral.swashplate_pitch));
    bool servo_moved = false;
    for (int i = 0; i < HH_SERVOS; ++i) {
        servo_moved = servo_moved || std::abs(pushed.servo_actual[i] - neutral.servo_actual[i]) > 1e-3;
    }
    CHECK(servo_moved);
    // the PT1 servo follows its command
    for (int i = 0; i < 3; ++i) {
        CHECK(std::abs(pushed.servo_actual[i] - pushed.servo_command[i]) < 0.1);
    }
    // the ten blade pitch values show the cyclic: not all equal
    const auto [lo, hi] = std::minmax_element(std::begin(pushed.blade_pitch), std::end(pushed.blade_pitch));
    CHECK(*hi - *lo > 0.02);
}

TEST_CASE("non finite parameters or a diverging state stop the simulation instead of spreading NaN")
{
    REQUIRE_LOGO(setup);
    HelicopterSim sim;
    REQUIRE(sim.configure(setup, options(HH_ROTOR_CLASSIC)));
    hh_reset_params r = on_ground(setup);
    r.velocity_O[0] = std::numeric_limits<double>::infinity();
    sim.reset(r);
    const hh_sim_inputs in = default_sim_inputs();
    run(sim, in, 0.01);
    hh_sim_outputs out;
    sim.fill_outputs(out);
    CHECK((out.flags & HH_FLAG_DIVERGED) != 0);
    // a reset recovers
    sim.reset(on_ground(setup));
    run(sim, in, 0.01);
    sim.fill_outputs(out);
    CHECK((out.flags & HH_FLAG_DIVERGED) == 0);
}
