#pragma once

#include <filesystem>
#include <functional>
#include <iosfwd>
#include <optional>
#include <string>
#include <vector>

#include "hoverhere/simulation/helicopter_sim.hpp"

// Headless maneuver runner: flies a stick profile through the complete simulation (synchronous, deterministic fixed steps) and writes
// every state to CSV files that can be loaded straight into pandas / matplotlib (see tools/headless/plot_example.py).
namespace hoverhere::headless {

struct Sticks {
    double collective = 0;   // [-1, 1]
    double yaw = 0;
    double pitch = 0;
    double roll = 0;
    double propeller = 0;
    double booster = 0;
    bool motor_enabled = true;
    double wheel_brake = 0;
};

// A stick profile: a function of time and the hover trim collective (the stick value that holds the weight of the helicopter).
struct Maneuver {
    std::string name;
    std::string description;
    double duration = 10;           // [s]
    double start_height = 50;       // [m] of the centre of mass above the ground plane (the ground only matters for takeoff and landing)
    bool starts_on_ground = false;  // place the helicopter on its gear instead
    std::function<Sticks(double time, double trim_collective)> sticks;
};

[[nodiscard]] std::vector<Maneuver> builtin_maneuvers();
[[nodiscard]] std::optional<Maneuver> find_maneuver(const std::string& name);

// Stick profile from a CSV file with the header "t,collective,yaw,pitch,roll[,propeller,booster,brake]" and linear interpolation between
// the rows (a row with the same t as the one before gives a step). Values are absolute; with add_trim the trim collective is added to the
// collective column. Returns false with a message on malformed input.
bool load_stick_profile(const std::filesystem::path& file, bool add_trim, Maneuver& out, std::string& error);

struct RunOptions {
    simulation::SimSetup setup;
    hh_sim_options sim_options = simulation::default_sim_options();
    std::vector<double> ground_vertices;       // ground mesh (right handed world frame); empty: a flat plane at y = 0
    std::vector<std::uint32_t> ground_indices;
    double duration = 0;                       // [s], 0: the duration of the maneuver
    double log_rate = 100;                     // [Hz] of the state CSV
    double grid_rate = 20;                     // [Hz] of the BEMT grid CSV (0: off)
    std::filesystem::path output_directory;    // empty: no files, only the summary
    int flight_bank = 0;                       // 0..2: selects the controller gains and the rotor speed
    std::optional<double> trim_collective;     // skip the trim search
    std::ostream* log = nullptr;
};

struct RunSummary {
    bool ok = false;
    std::string error;
    double trim_collective = 0;
    double duration = 0;
    std::uint64_t steps = 0;
    double max_height = 0;
    double final_height = 0;
    double min_vertical_velocity = 0;
    double max_vertical_velocity = 0;
    double max_vortex_ring_strength = 0;
    bool crashed = false;
    bool diverged = false;
    std::filesystem::path states_csv;
    std::filesystem::path grid_csv;
};

// Collective stick value for which the vertical rotor force holds the weight in a hover at the setpoint rotor speed.
[[nodiscard]] double find_hover_collective(const simulation::SimSetup& setup, const hh_sim_options& options, int flight_bank = 0);

[[nodiscard]] RunSummary run_maneuver(const Maneuver& maneuver, const RunOptions& options);

// Wavefront OBJ (v and f lines, polygons are triangulated as fans). The coordinates are taken as stored. Returns false with a message.
bool load_obj(const std::filesystem::path& file, std::vector<double>& vertices, std::vector<std::uint32_t>& indices, std::string& error);

}  // namespace hoverhere::headless
