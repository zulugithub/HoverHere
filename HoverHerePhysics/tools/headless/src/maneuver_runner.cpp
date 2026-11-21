#include "maneuver_runner.hpp"

#include <algorithm>
#include <cmath>
#include <cstdio>
#include <fstream>
#include <iostream>
#include <limits>
#include <sstream>

#include "hoverhere/core/frames.hpp"

namespace hoverhere::headless {

namespace fs = std::filesystem;
using simulation::HelicopterSim;

namespace {

constexpr double kRadToDeg = 180.0 / 3.14159265358979323846;

double ramp(const double t, const double t0, const double t1, const double a, const double b)
{
    if (t <= t0) {
        return a;
    }
    if (t >= t1) {
        return b;
    }
    return a + (b - a) * (t - t0) / (t1 - t0);
}

// Pulse: 0 outside [t0, t1], `value` inside, with ramps of `edge` seconds.
double pulse(const double t, const double t0, const double t1, const double value, const double edge = 0.05)
{
    return ramp(t, t0, t0 + edge, 0, value) - ramp(t, t1, t1 + edge, 0, value);
}

}  // namespace

std::vector<Maneuver> builtin_maneuvers()
{
    std::vector<Maneuver> list;

    list.push_back({"hover", "hover at the trim collective, sticks centered", 10, 50, false,
                    [](const double, const double trim) { Sticks s; s.collective = trim; return s; }});

    list.push_back({"collective_step", "step response: collective +0.10 at t = 2 s for 3 s, then back", 9, 50, false,
                    [](const double t, const double trim) {
                        Sticks s;
                        s.collective = trim + pulse(t, 2, 5, 0.10);
                        return s;
                    }});

    // The flybarless controller commands rotation RATES: a held stick gives a constant rate, so the step is kept short (the attitude stays
    // in the validity range of the model); the doublets bring the attitude back.
    list.push_back({"roll_step", "step response: roll stick +0.25 (right) at t = 2 s for 0.5 s (rate command: the helicopter keeps rolling)", 5, 50, false,
                    [](const double t, const double trim) {
                        Sticks s;
                        s.collective = trim;
                        s.roll = pulse(t, 2, 2.5, 0.25);
                        return s;
                    }});

    list.push_back({"pitch_step", "step response: pitch stick +0.25 (nose up) at t = 2 s for 0.5 s", 5, 50, false,
                    [](const double t, const double trim) {
                        Sticks s;
                        s.collective = trim;
                        s.pitch = pulse(t, 2, 2.5, 0.25);
                        return s;
                    }});

    list.push_back({"yaw_step", "step response: yaw stick +0.30 at t = 2 s for 1.5 s", 7, 50, false,
                    [](const double t, const double trim) {
                        Sticks s;
                        s.collective = trim;
                        s.yaw = pulse(t, 2, 3.5, 0.30);
                        return s;
                    }});

    list.push_back({"roll_doublet", "roll stick +0.25 for 0.6 s, then -0.25 for 0.6 s (attitude comes back)", 8, 50, false,
                    [](const double t, const double trim) {
                        Sticks s;
                        s.collective = trim;
                        s.roll = pulse(t, 2, 2.6, 0.25) - pulse(t, 2.6, 3.2, 0.25);
                        return s;
                    }});

    list.push_back({"pitch_doublet", "pitch stick +0.25 for 0.6 s, then -0.25 for 0.6 s (attitude comes back)", 8, 50, false,
                    [](const double t, const double trim) {
                        Sticks s;
                        s.collective = trim;
                        s.pitch = pulse(t, 2, 2.6, 0.25) - pulse(t, 2.6, 3.2, 0.25);
                        return s;
                    }});

    list.push_back({"vrs_descent",
                    "descent into the vortex ring state and recovery: hover 2 s, collective reduced slowly by 0.45 over 5 s (the sink rate passes the "
                    "vortex ring band), recovery at t = 11 s with more collective and a short nose down pulse to gain forward speed",
                    18, 120, false,
                    [](const double t, const double trim) {
                        Sticks s;
                        s.collective = trim + ramp(t, 2, 7, 0, -0.45) + ramp(t, 11, 12, 0, 0.55);
                        s.pitch = pulse(t, 11, 12.2, -0.12, 0.2);
                        return s;
                    }});

    list.push_back({"takeoff_land", "lift off from the ground, hover at about 1.5 m, come down and touch down gently", 16, 0, true,
                    [](const double t, const double trim) {
                        Sticks s;
                        // minimum pitch on the ground, lift-off at t = 1 s, hover, controlled descent from t = 6 s, touchdown, collective down
                        s.collective = trim + ramp(t, 0.5, 2.0, -0.4, 0.10) - ramp(t, 3.5, 4.5, 0, 0.10) - ramp(t, 6.0, 7.0, 0, 0.06) +
                                       ramp(t, 9.5, 10.0, 0, 0.06) - ramp(t, 11.5, 12.5, 0, 0.5);
                        return s;
                    }});

    return list;
}

std::optional<Maneuver> find_maneuver(const std::string& name)
{
    for (Maneuver& m : builtin_maneuvers()) {
        if (m.name == name) {
            return m;
        }
    }
    return std::nullopt;
}

bool load_stick_profile(const fs::path& file, const bool add_trim, Maneuver& out, std::string& error)
{
    std::ifstream in(file);
    if (!in) {
        error = "cannot open " + file.string();
        return false;
    }
    struct Row {
        double t, collective, yaw, pitch, roll, propeller, booster, brake;
    };
    std::vector<Row> rows;
    std::string line;
    std::vector<std::string> columns;
    while (std::getline(in, line)) {
        if (!line.empty() && line.back() == '\r') {
            line.pop_back();
        }
        if (line.empty() || line[0] == '#') {
            continue;
        }
        std::vector<std::string> cells;
        std::stringstream ss(line);
        std::string cell;
        while (std::getline(ss, cell, ',')) {
            cells.push_back(cell);
        }
        if (columns.empty()) {
            columns = cells;
            if (columns.empty() || columns[0] != "t") {
                error = "the first column of the profile has to be 't'";
                return false;
            }
            continue;
        }
        Row row{};
        for (std::size_t i = 0; i < cells.size() && i < columns.size(); ++i) {
            char* end = nullptr;
            const double v = std::strtod(cells[i].c_str(), &end);
            if (end == cells[i].c_str()) {
                error = "cannot read '" + cells[i] + "' in " + file.string();
                return false;
            }
            if (columns[i] == "t") row.t = v;
            else if (columns[i] == "collective") row.collective = v;
            else if (columns[i] == "yaw") row.yaw = v;
            else if (columns[i] == "pitch") row.pitch = v;
            else if (columns[i] == "roll") row.roll = v;
            else if (columns[i] == "propeller") row.propeller = v;
            else if (columns[i] == "booster") row.booster = v;
            else if (columns[i] == "brake") row.brake = v;
        }
        if (!rows.empty() && row.t < rows.back().t) {
            error = "the time column of the profile has to be non decreasing";
            return false;
        }
        rows.push_back(row);
    }
    if (rows.empty()) {
        error = "the profile has no data rows";
        return false;
    }
    out = Maneuver{};
    out.name = file.stem().string();
    out.description = "stick profile " + file.string();
    out.duration = rows.back().t;
    out.start_height = 50;
    out.sticks = [rows, add_trim](const double t, const double trim) {
        Sticks s;
        std::size_t hi = 0;
        while (hi < rows.size() && rows[hi].t <= t) {
            ++hi;
        }
        Row r{};
        if (hi == 0) {
            r = rows.front();
        } else if (hi == rows.size()) {
            r = rows.back();
        } else {
            const Row& a = rows[hi - 1];
            const Row& b = rows[hi];
            const double w = b.t > a.t ? (t - a.t) / (b.t - a.t) : 1.0;
            r = Row{t, a.collective + (b.collective - a.collective) * w, a.yaw + (b.yaw - a.yaw) * w, a.pitch + (b.pitch - a.pitch) * w,
                    a.roll + (b.roll - a.roll) * w, a.propeller + (b.propeller - a.propeller) * w, a.booster + (b.booster - a.booster) * w,
                    a.brake + (b.brake - a.brake) * w};
        }
        s.collective = r.collective + (add_trim ? trim : 0.0);
        s.yaw = r.yaw;
        s.pitch = r.pitch;
        s.roll = r.roll;
        s.propeller = r.propeller;
        s.booster = r.booster;
        s.wheel_brake = r.brake;
        return s;
    };
    return true;
}

bool load_obj(const fs::path& file, std::vector<double>& vertices, std::vector<std::uint32_t>& indices, std::string& error)
{
    std::ifstream in(file);
    if (!in) {
        error = "cannot open " + file.string();
        return false;
    }
    vertices.clear();
    indices.clear();
    std::string line;
    while (std::getline(in, line)) {
        if (line.size() > 2 && line[0] == 'v' && line[1] == ' ') {
            double x = 0, y = 0, z = 0;
            std::istringstream numbers(line.substr(2));
            if (!(numbers >> x >> y >> z)) {
                error = "bad vertex line: " + line;
                return false;
            }
            vertices.insert(vertices.end(), {x, y, z});
        } else if (line.size() > 2 && line[0] == 'f' && line[1] == ' ') {
            std::vector<std::uint32_t> face;
            std::stringstream ss(line.substr(2));
            std::string token;
            while (ss >> token) {
                const long v = std::strtol(token.c_str(), nullptr, 10);  // "v", "v/t", "v/t/n" and "v//n" all start with the vertex number
                const long count = static_cast<long>(vertices.size() / 3);
                const long index = v > 0 ? v - 1 : count + v;
                if (index < 0 || index >= count) {
                    error = "face index out of range: " + line;
                    return false;
                }
                face.push_back(static_cast<std::uint32_t>(index));
            }
            for (std::size_t k = 1; k + 1 < face.size(); ++k) {
                indices.insert(indices.end(), {face[0], face[k], face[k + 1]});
            }
        }
    }
    if (vertices.empty() || indices.empty()) {
        error = file.string() + " has no triangles";
        return false;
    }
    return true;
}

namespace {

struct Euler {
    double roll, pitch, yaw;  // [deg] aviation order: yaw about the vertical axis first, then pitch (nose up positive), then roll (right wing down positive)
};

Euler euler_from_quaternion(const double (&q)[4])
{
    using frames::Mat3;
    const Mat3 A = frames::A_RL(math::Quat{{q[0], q[1], q[2], q[3]}});
    const double fx = A.a[0][0];
    const double fy = A.a[1][0];
    const double fz = A.a[2][0];
    const double ry = A.a[1][2];
    const double uy = A.a[1][1];
    Euler e{};
    e.pitch = std::asin(std::clamp(fy, -1.0, 1.0)) * kRadToDeg;
    e.yaw = std::atan2(-fz, fx) * kRadToDeg;
    e.roll = std::atan2(-ry, uy) * kRadToDeg;
    return e;
}

struct Column {
    std::string name;
    std::function<double(const hh_sim_outputs&, const Sticks&, const Euler&)> value;
};

std::vector<Column> state_columns()
{
    std::vector<Column> c;
    const auto add = [&c](std::string name, std::function<double(const hh_sim_outputs&, const Sticks&, const Euler&)> f) {
        c.push_back({std::move(name), std::move(f)});
    };
    add("time", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.time; });
    add("stick_collective", [](const hh_sim_outputs&, const Sticks& s, const Euler&) { return s.collective; });
    add("stick_yaw", [](const hh_sim_outputs&, const Sticks& s, const Euler&) { return s.yaw; });
    add("stick_pitch", [](const hh_sim_outputs&, const Sticks& s, const Euler&) { return s.pitch; });
    add("stick_roll", [](const hh_sim_outputs&, const Sticks& s, const Euler&) { return s.roll; });
    add("stick_propeller", [](const hh_sim_outputs&, const Sticks& s, const Euler&) { return s.propeller; });
    add("stick_booster", [](const hh_sim_outputs&, const Sticks& s, const Euler&) { return s.booster; });
    add("pos_x", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.position_O[0]; });
    add("pos_y", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.position_O[1]; });
    add("pos_z", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.position_O[2]; });
    add("q_w", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.orientation[0]; });
    add("q_x", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.orientation[1]; });
    add("q_y", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.orientation[2]; });
    add("q_z", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.orientation[3]; });
    add("roll_deg", [](const hh_sim_outputs&, const Sticks&, const Euler& e) { return e.roll; });
    add("pitch_deg", [](const hh_sim_outputs&, const Sticks&, const Euler& e) { return e.pitch; });
    add("yaw_deg", [](const hh_sim_outputs&, const Sticks&, const Euler& e) { return e.yaw; });
    add("vel_x", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.velocity_O[0]; });
    add("vel_y", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.velocity_O[1]; });
    add("vel_z", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.velocity_O[2]; });
    add("vel_body_x", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.velocity_L[0]; });
    add("vel_body_y", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.velocity_L[1]; });
    add("vel_body_z", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.velocity_L[2]; });
    add("omega_x", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.omega_L[0]; });
    add("omega_y", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.omega_L[1]; });
    add("omega_z", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.omega_L[2]; });
    add("accel_x", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.accel_L[0]; });
    add("accel_y", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.accel_L[1]; });
    add("accel_z", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.accel_L[2]; });
    add("motor_speed", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.motor_speed; });
    add("mainrotor_speed", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor_speed; });
    add("tailrotor_speed", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.tailrotor_speed; });
    add("propeller_speed", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.propeller_speed; });
    add("governor_integral", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.governor_integral; });
    add("governor_target", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.governor_target; });
    add("motor_voltage", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.motor_voltage_applied; });
    add("motor_current", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.audio.motor_current; });
    add("motor_power_electric", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.motor_power_electric; });
    add("motor_power_mechanical", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.motor_power_mechanical; });
    add("freewheeling", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return (o.flags & HH_FLAG_FREEWHEELING) != 0 ? 1.0 : 0.0; });
    add("mr_thrust", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.thrust; });
    add("mr_torque", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.torque; });
    add("mr_induced_velocity", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.induced_velocity; });
    add("mr_force_x", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.force_O[0]; });
    add("mr_force_y", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.force_O[1]; });
    add("mr_force_z", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.force_O[2]; });
    add("mr_flapping_a_s", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.flapping_a_s; });
    add("mr_flapping_b_s", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.flapping_b_s; });
    add("mr_ground_effect", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.ground_effect_factor; });
    add("mr_vortex_ring", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.vortex_ring_strength; });
    add("mr_turbulence", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.mainrotor.turbulence_strength; });
    add("tr_thrust", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.tailrotor.thrust; });
    add("tr_torque", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.tailrotor.torque; });
    add("tr_induced_velocity", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.tailrotor.induced_velocity; });
    add("pr_thrust", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.propeller.thrust; });
    add("booster_thrust", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.booster_thrust; });
    add("disc_q_w", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.rotor_disc_orientation[0]; });
    add("disc_q_x", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.rotor_disc_orientation[1]; });
    add("disc_q_y", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.rotor_disc_orientation[2]; });
    add("disc_q_z", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.rotor_disc_orientation[3]; });
    add("disc_rate_x", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.rotor_disc_rate_x; });
    add("disc_rate_z", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.rotor_disc_rate_z; });
    add("servo_cmd_1", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.servo_command[0]; });
    add("servo_cmd_2", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.servo_command[1]; });
    add("servo_cmd_3", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.servo_command[2]; });
    add("servo_cmd_tail", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.servo_command[3]; });
    add("servo_1", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.servo_actual[0]; });
    add("servo_2", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.servo_actual[1]; });
    add("servo_3", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.servo_actual[2]; });
    add("servo_tail", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.servo_actual[3]; });
    add("swash_roll", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.swashplate_roll; });
    add("swash_pitch", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.swashplate_pitch; });
    add("swash_collective", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.swashplate_collective; });
    add("tail_pitch", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.tailrotor_pitch; });
    for (int i = 0; i < HH_BLADE_STATIONS; ++i) {
        add("blade_pitch_" + std::to_string(i), [i](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.blade_pitch[i]; });
    }
    add("audio_mr_noise", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.audio.mainrotor_noise; });
    add("audio_mr_stall", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.audio.mainrotor_stall; });
    add("audio_tr_pitch_volume", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.audio.tailrotor_pitch_volume; });
    add("audio_vortex_ring", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.audio.vortex_ring; });
    add("audio_turbulence", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.audio.turbulence; });
    add("audio_motor_load", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.audio.motor_load; });
    add("contacts_touching", [](const hh_sim_outputs& o, const Sticks&, const Euler&) {
        int n = 0;
        for (int i = 0; i < o.contact_count; ++i) {
            n += o.contacts[i].in_contact != 0 ? 1 : 0;
        }
        return static_cast<double>(n);
    });
    add("contact_force_total", [](const hh_sim_outputs& o, const Sticks&, const Euler&) {
        double sum = 0;
        for (int i = 0; i < o.contact_count; ++i) {
            sum += o.contacts[i].normal_force;
        }
        return sum;
    });
    add("contact_friction_total", [](const hh_sim_outputs& o, const Sticks&, const Euler&) {
        double sum = 0;
        for (int i = 0; i < o.contact_count; ++i) {
            sum += o.contacts[i].tangential_force;
        }
        return sum;
    });
    add("max_penetration", [](const hh_sim_outputs& o, const Sticks&, const Euler&) {
        double m = 0;
        for (int i = 0; i < o.contact_count; ++i) {
            m = std::max(m, o.contacts[i].penetration);
        }
        return m;
    });
    add("gear_force_left", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.gear_force_y[0]; });
    add("gear_force_right", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.gear_force_y[1]; });
    add("gear_force_center", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.gear_force_y[2]; });
    add("hub_distance_main", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.hub_distance_to_ground[0]; });
    add("hub_distance_tail", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return o.hub_distance_to_ground[1]; });
    add("crashed", [](const hh_sim_outputs& o, const Sticks&, const Euler&) { return (o.flags & HH_FLAG_CRASHED) != 0 ? 1.0 : 0.0; });
    return c;
}

void write_number(std::ostream& os, const double v)
{
    char buffer[40];
    std::snprintf(buffer, sizeof(buffer), "%.9g", v);
    os << buffer;
}

}  // namespace

double find_hover_collective(const simulation::SimSetup& setup, const hh_sim_options& options, const int flight_bank)
{
    HelicopterSim sim;
    if (!sim.configure(setup, options)) {
        return 0;
    }
    std::vector<double> vertices = {-1000, 0, -1000, 1000, 0, -1000, 1000, 0, 1000, -1000, 0, 1000};
    std::vector<std::uint32_t> indices = {0, 3, 1, 1, 3, 2};
    sim.set_ground_mesh(std::span<const double>(vertices), std::span<const std::uint32_t>(indices));
    // The collective that leaves the helicopter at rest in the air: bisection on the sink rate it settles at (the rotor thrust grows with
    // the sink rate, so a hover needs a slightly higher collective than "thrust = weight at the moment of the measurement").
    const auto sink_rate = [&](const double collective) {
        hh_reset_params reset = simulation::default_reset_params();
        reset.position_O[1] = 60;
        sim.reset(reset);
        hh_sim_inputs in = simulation::default_sim_inputs();
        in.collective = collective;
        in.flight_bank = flight_bank;
        const int steps = static_cast<int>(std::lround(2.0 / sim.dt()));
        double sum = 0;
        int samples = 0;
        hh_sim_outputs out;
        for (int i = 0; i < steps; ++i) {
            sim.step_fixed(in);
            if (i >= steps * 3 / 4 && i % 10 == 0) {
                sim.fill_outputs(out);
                sum += out.velocity_O[1];
                ++samples;
            }
        }
        return samples > 0 ? sum / samples : 0.0;
    };

    double lo = -1.0;
    double hi = 1.0;
    if (sink_rate(hi) < 0) {
        return hi;  // cannot hover at all
    }
    if (sink_rate(lo) > 0) {
        return lo;
    }
    for (int i = 0; i < 14; ++i) {
        const double mid = 0.5 * (lo + hi);
        if (sink_rate(mid) > 0) {
            hi = mid;
        } else {
            lo = mid;
        }
    }
    return 0.5 * (lo + hi);
}

RunSummary run_maneuver(const Maneuver& maneuver, const RunOptions& options)
{
    RunSummary summary;
    const auto fail = [&summary](const std::string& message) {
        summary.error = message;
        return summary;
    };

    if (!maneuver.sticks) {
        return fail("the maneuver has no stick profile");
    }
    summary.trim_collective = options.trim_collective ? *options.trim_collective : find_hover_collective(options.setup, options.sim_options, options.flight_bank);
    if (options.log != nullptr) {
        *options.log << "hover trim collective: " << summary.trim_collective << "\n";
    }

    HelicopterSim sim;
    std::string error;
    if (!sim.configure(options.setup, options.sim_options, &error)) {
        return fail("model error: " + error);
    }
    std::vector<double> flat_vertices = {-1000, 0, -1000, 1000, 0, -1000, 1000, 0, 1000, -1000, 0, 1000};
    std::vector<std::uint32_t> flat_indices = {0, 3, 1, 1, 3, 2};
    const bool custom_ground = !options.ground_vertices.empty() && !options.ground_indices.empty();
    const bool mesh_ok = custom_ground ? sim.set_ground_mesh(std::span<const double>(options.ground_vertices),
                                                             std::span<const std::uint32_t>(options.ground_indices))
                                       : sim.set_ground_mesh(std::span<const double>(flat_vertices), std::span<const std::uint32_t>(flat_indices));
    if (!mesh_ok) {
        return fail("the ground mesh is not valid");
    }

    hh_reset_params reset = simulation::default_reset_params();
    reset.position_O[1] = maneuver.start_height;
    if (maneuver.starts_on_ground) {
        const auto si = params::compile_to_si(options.setup.params);
        double lowest = 0;
        const auto& c = si.helicopter.collision;
        for (const auto* list : {&c.positions_usual, &c.positions_left, &c.positions_right, &c.positions_steering_center, &c.positions_steering_left,
                                 &c.positions_steering_right}) {
            for (std::int32_t i = 0; i < list->count; ++i) {
                lowest = std::min(lowest, list->items[i].a[1]);
            }
        }
        reset.position_O[1] = -lowest + 0.005;
    }
    sim.reset(reset);

    const double duration = options.duration > 0 ? options.duration : maneuver.duration;
    const double dt = sim.dt();
    const std::uint64_t total_steps = static_cast<std::uint64_t>(std::llround(duration / dt));
    const std::uint64_t log_every = std::max<std::uint64_t>(1, static_cast<std::uint64_t>(std::llround(1.0 / (options.log_rate * dt))));
    const std::uint64_t grid_every = options.grid_rate > 0 ? std::max<std::uint64_t>(1, static_cast<std::uint64_t>(std::llround(1.0 / (options.grid_rate * dt)))) : 0;

    std::ofstream states;
    std::ofstream grid;
    const std::vector<Column> columns = state_columns();
    if (!options.output_directory.empty()) {
        std::error_code ec;
        fs::create_directories(options.output_directory, ec);
        summary.states_csv = options.output_directory / (maneuver.name + "_states.csv");
        states.open(summary.states_csv);
        if (!states) {
            return fail("cannot write " + summary.states_csv.string());
        }
        for (std::size_t i = 0; i < columns.size(); ++i) {
            states << (i != 0 ? "," : "") << columns[i].name;
        }
        states << "\n";
        hh_bemt_telemetry probe{};
        probe.struct_size = sizeof(probe);
        if (grid_every != 0 && sim.fill_bemt_telemetry(probe)) {
            summary.grid_csv = options.output_directory / (maneuver.name + "_bemt_grid.csv");
            grid.open(summary.grid_csv);
            grid << "time,r_index,psi_index,radius,psi,beta,Vc,V2,Vi_raw,Vi_target,Vi,alpha,cl,cd,dL,dD,dT,dQ,vrs_weight,residual,use_vrs,strength_vrs,"
                    "Vz_scaled,Vx_scaled,v_johnson_lagged,hover_induced_velocity\n";
        }
    }

    summary.min_vertical_velocity = std::numeric_limits<double>::max();
    summary.max_vertical_velocity = std::numeric_limits<double>::lowest();
    hh_sim_outputs out;
    hh_bemt_telemetry tele{};
    tele.struct_size = sizeof(tele);
    Sticks sticks;

    const auto record = [&](const bool log_states, const bool log_grid) {
        sim.fill_outputs(out);
        summary.max_height = std::max(summary.max_height, out.position_O[1]);
        summary.min_vertical_velocity = std::min(summary.min_vertical_velocity, out.velocity_O[1]);
        summary.max_vertical_velocity = std::max(summary.max_vertical_velocity, out.velocity_O[1]);
        summary.max_vortex_ring_strength = std::max(summary.max_vortex_ring_strength, out.mainrotor.vortex_ring_strength);
        if (log_states && states.is_open()) {
            const Euler e = euler_from_quaternion(out.orientation);
            for (std::size_t i = 0; i < columns.size(); ++i) {
                if (i != 0) {
                    states << ',';
                }
                write_number(states, columns[i].value(out, sticks, e));
            }
            states << '\n';
        }
        if (log_grid && grid.is_open() && sim.fill_bemt_telemetry(tele)) {
            for (std::size_t k = 0; k < HH_BEMT_ELEMENTS; ++k) {
                write_number(grid, tele.time);
                grid << ',' << (k / HH_BEMT_AZIMUTH) << ',' << (k % HH_BEMT_AZIMUTH);
                for (const double v : {tele.radius[k], tele.psi[k], tele.beta[k], tele.Vc[k], tele.V2[k], tele.Vi_raw[k], tele.Vi_target[k], tele.Vi[k],
                                       tele.alpha[k], tele.cl[k], tele.cd[k], tele.dL[k], tele.dD[k], tele.dT[k], tele.dQ[k], tele.vrs_weight[k],
                                       tele.residual[k], tele.use_vrs, tele.strength_vrs, tele.Vz_scaled, tele.Vx_scaled, tele.v_johnson_lagged,
                                       tele.hover_induced_velocity}) {
                    grid << ',';
                    write_number(grid, v);
                }
                grid << '\n';
            }
        }
    };

    for (std::uint64_t step = 0; step < total_steps; ++step) {
        const double t = static_cast<double>(step) * dt;
        sticks = maneuver.sticks(t, summary.trim_collective);
        hh_sim_inputs in = simulation::default_sim_inputs();
        in.collective = std::clamp(sticks.collective, -1.0, 1.0);
        in.yaw = std::clamp(sticks.yaw, -1.0, 1.0);
        in.pitch = std::clamp(sticks.pitch, -1.0, 1.0);
        in.roll = std::clamp(sticks.roll, -1.0, 1.0);
        in.propeller = std::clamp(sticks.propeller, -1.0, 1.0);
        in.booster = std::clamp(sticks.booster, -1.0, 1.0);
        in.motor_enabled = sticks.motor_enabled ? 1 : 0;
        in.wheel_brake = std::clamp(sticks.wheel_brake, 0.0, 1.0);
        in.flight_bank = options.flight_bank;
        if (step % log_every == 0) {
            record(true, grid_every != 0 && step % grid_every == 0);
        } else if (grid_every != 0 && step % grid_every == 0) {
            record(false, true);
        }
        sim.step_fixed(in);
        summary.steps = step + 1;
    }
    record(true, grid_every != 0);

    sim.fill_outputs(out);
    summary.duration = out.time;
    summary.final_height = out.position_O[1];
    summary.crashed = (out.flags & HH_FLAG_CRASHED) != 0;
    summary.diverged = (out.flags & HH_FLAG_DIVERGED) != 0;
    summary.ok = !summary.diverged;
    if (summary.diverged) {
        summary.error = "the simulation diverged (non finite state)";
    }
    return summary;
}

}  // namespace hoverhere::headless
