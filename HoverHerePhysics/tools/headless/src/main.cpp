#include <algorithm>
#include <array>
#include <cmath>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <numbers>
#include <optional>
#include <string>
#include <string_view>
#include <vector>

#include "hoverhere/aero/rotor_bemt.hpp"
#include "hoverhere/core/math.hpp"
#include "hoverhere/io/param_loader.hpp"
#include "hoverhere/params/params.hpp"
#include "hoverhere/version.hpp"
#include "maneuver_runner.hpp"

#ifdef HOVERHERE_WITH_C_API
#include "c_api_check.h"
#include "hoverhere/sim_api.h"
#endif

namespace {

int failures = 0;

void expect(const bool condition, const char* const description)
{
    std::printf("[%s] %s\n", condition ? " ok " : "FAIL", description);
    if (!condition) {
        ++failures;
    }
}

void check_version()
{
    const hoverhere::Version version = hoverhere::version();
    const std::string expected =
        std::to_string(version.major) + '.' + std::to_string(version.minor) + '.' + std::to_string(version.patch);
    expect(hoverhere::version_string() == expected, "version_string() matches version()");
}

void check_math()
{
    namespace qvm = boost::qvm;
    using hoverhere::Real;
    using hoverhere::math::Mat3;
    using hoverhere::math::Quat;
    using hoverhere::math::Vec3;

    constexpr Real tolerance = 1e-12;
    const Vec3 x_axis = {{1, 0, 0}};
    const Vec3 y_axis = {{0, 1, 0}};
    const Vec3 z_axis = {{0, 0, 1}};

    const Quat q = qvm::rot_quat(z_axis, std::numbers::pi_v<Real> / 2);
    const Mat3 r = qvm::convert_to<Mat3>(q);

    expect(qvm::mag(q * x_axis - y_axis) < tolerance, "Boost.QVM: quat * vec rotates x onto y about +z");
    expect(qvm::mag(r * x_axis - y_axis) < tolerance, "Boost.QVM: mat * vec agrees with quat * vec");
    expect(std::abs(qvm::mag(q) - 1) < tolerance, "Boost.QVM: rot_quat yields a unit quaternion");
    expect(std::abs(qvm::determinant(r) - 1) < tolerance, "Boost.QVM: rotation matrix is proper (det == +1)");
    expect(qvm::mag(qvm::cross(x_axis, y_axis) - z_axis) < tolerance, "Boost.QVM: cross(x, y) == z (right-handed)");
}

#ifdef HOVERHERE_WITH_C_API
void check_c_api()
{
    expect(hoverhere_c_api_check() == 0, "C API: lifecycle, argument checks, stepping, ground mesh, layout table and physics thread (C translation unit)");

    hh_version_info info{};
    info.struct_size = static_cast<std::uint32_t>(sizeof(info));
    const hoverhere::Version version = hoverhere::version();
    const bool matches = hh_get_version(&info) == HH_OK && info.major == static_cast<std::uint32_t>(version.major) &&
                         info.minor == static_cast<std::uint32_t>(version.minor) &&
                         info.patch == static_cast<std::uint32_t>(version.patch);
    expect(matches, "C API: shared library version matches the physics library");
}
#endif

const char* const kUsage =
    "usage:\n"
    "  hoverhere_headless_test --smoke\n"
    "  hoverhere_headless_test list\n"
    "  hoverhere_headless_test run <maneuver> --heli <parameter_file.xml> [options]\n"
    "  hoverhere_headless_test run --profile <sticks.csv> --heli <parameter_file.xml> [options]\n"
    "  hoverhere_headless_test airfoil --heli <parameter_file.xml> --out <polar.csv>\n"
    "\n"
    "options of run:\n"
    "  --heli FILE            helicopter parameter file (required)\n"
    "  --scenery FILE         scenery parameter file (gravity, air density, wind)\n"
    "  --main-mech FILE       main rotor mechanics file (default: <name>_mainrotor_mechanics.xml next to the parameter file)\n"
    "  --tail-mech FILE       tail rotor mechanics file (default: <name>_tailrotor_mechanics.xml next to the parameter file)\n"
    "  --no-mechanics         ignore the mechanics files (ideal CCPM mixer, analytical tail pitch)\n"
    "  --rotor bemt|classic   main rotor model (default bemt)\n"
    "  --duration SECONDS     override the duration of the maneuver\n"
    "  --rate HZ              rate of the state CSV (default 100)\n"
    "  --grid-rate HZ         rate of the BEMT grid CSV, 0 = off (default 20)\n"
    "  --out DIR              output directory of the CSV files (default: ./hoverhere_out)\n"
    "  --trim VALUE           hover collective stick value (default: found by a bisection)\n"
    "  --profile FILE         stick profile CSV: t,collective,yaw,pitch,roll[,propeller,booster,brake], linear interpolation\n"
    "  --add-trim             add the hover trim to the collective column of the profile\n"
    "  --ground-obj FILE      ground mesh (Wavefront OBJ of a skymap collision_landing_object.obj, mapped like Unity: x -> -x, z -> -z)\n"
    "  --ground-obj-raw FILE  same, coordinates taken as stored\n"
    "  --bank N               flight bank 0..2 (controller gains and rotor speed setpoint, default 0)\n"
    "  --set PATH=VALUE       set a parameter of the helicopter file after loading (user units), e.g. helicopter/flybarless/K_d=0 or\n"
    "                         helicopter/flybarless/K_p[1]=0.2 (component of a 3 value parameter), tuning/turbulence/frequency=2; repeatable\n"
    "  --dt SECONDS           physics step (default 0.001)\n"
    "  --seed N               random seed of the turbulence\n"
    "  --no-vrs --no-ground-effect --no-turbulence --no-dynamic-inflow --quasi-static   switch off parts of the BEMT model\n";

[[nodiscard]] bool load_report_ok(const hoverhere::io::LoadReport& report)
{
    if (!report.diagnostics.empty()) {
        std::cerr << report.format() << "\n";
    }
    return report.ok();
}

int run_command(const std::vector<std::string>& args)
{
    namespace fs = std::filesystem;
    namespace hh = hoverhere::headless;
    namespace io = hoverhere::io;

    hh::RunOptions options;
    options.log = &std::cout;
    options.output_directory = "hoverhere_out";
    std::optional<std::string> maneuver_name;
    std::optional<fs::path> heli;
    std::optional<fs::path> scenery;
    std::optional<fs::path> main_mech;
    std::optional<fs::path> tail_mech;
    std::optional<fs::path> profile;
    std::optional<fs::path> ground_obj;
    bool ground_raw = false;
    bool no_mechanics = false;
    bool add_trim = false;
    std::vector<std::string> assignments;

    for (std::size_t i = 0; i < args.size(); ++i) {
        const std::string& a = args[i];
        const auto value = [&]() -> const std::string& {
            if (i + 1 >= args.size()) {
                std::cerr << "missing value for " << a << "\n";
                std::exit(2);
            }
            return args[++i];
        };
        if (a == "--heli") {
            heli = value();
        } else if (a == "--scenery") {
            scenery = value();
        } else if (a == "--main-mech") {
            main_mech = value();
        } else if (a == "--tail-mech") {
            tail_mech = value();
        } else if (a == "--no-mechanics") {
            no_mechanics = true;
        } else if (a == "--rotor") {
            const std::string v = value();
            if (v == "bemt") {
                options.sim_options.rotor_model = HH_ROTOR_BEMT;
            } else if (v == "classic") {
                options.sim_options.rotor_model = HH_ROTOR_CLASSIC;
            } else {
                std::cerr << "unknown rotor model " << v << "\n";
                return 2;
            }
        } else if (a == "--duration") {
            options.duration = std::atof(value().c_str());
        } else if (a == "--rate") {
            options.log_rate = std::atof(value().c_str());
        } else if (a == "--grid-rate") {
            options.grid_rate = std::atof(value().c_str());
        } else if (a == "--out") {
            options.output_directory = value();
        } else if (a == "--trim") {
            options.trim_collective = std::atof(value().c_str());
        } else if (a == "--profile") {
            profile = value();
        } else if (a == "--add-trim") {
            add_trim = true;
        } else if (a == "--ground-obj") {
            ground_obj = value();
        } else if (a == "--ground-obj-raw") {
            ground_obj = value();
            ground_raw = true;
        } else if (a == "--bank") {
            options.flight_bank = std::clamp(std::atoi(value().c_str()), 0, 2);
        } else if (a == "--set") {
            assignments.push_back(value());
        } else if (a == "--dt") {
            options.sim_options.dt = std::atof(value().c_str());
        } else if (a == "--seed") {
            options.sim_options.seed = std::strtoull(value().c_str(), nullptr, 10);
        } else if (a == "--no-vrs") {
            options.sim_options.bemt_flags |= HH_BEMT_NO_VORTEX_RING;
        } else if (a == "--no-ground-effect") {
            options.sim_options.bemt_flags |= HH_BEMT_NO_GROUND_EFFECT;
        } else if (a == "--no-turbulence") {
            options.sim_options.bemt_flags |= HH_BEMT_NO_TURBULENCE;
        } else if (a == "--no-dynamic-inflow") {
            options.sim_options.bemt_flags |= HH_BEMT_NO_DYNAMIC_INFLOW;
        } else if (a == "--quasi-static") {
            options.sim_options.bemt_flags |= HH_BEMT_QUASI_STATIC;
        } else if (!a.empty() && a[0] != '-' && !maneuver_name) {
            maneuver_name = a;
        } else {
            std::cerr << "unknown argument " << a << "\n" << kUsage;
            return 2;
        }
    }
    if (!heli) {
        std::cerr << "--heli is required\n" << kUsage;
        return 2;
    }
    if (!maneuver_name && !profile) {
        std::cerr << "give a maneuver name (see 'list') or --profile\n" << kUsage;
        return 2;
    }

    // parameters
    if (!load_report_ok(io::load_parameters_file(*heli, hoverhere::params::ParamDocument::Heli, options.setup.params))) {
        return 3;
    }
    for (const std::string& assignment : assignments) {
        const std::size_t equals = assignment.find('=');
        if (equals == std::string::npos) {
            std::cerr << "--set expects PATH=VALUE: " << assignment << "\n";
            return 2;
        }
        std::string path = assignment.substr(0, equals);
        std::size_t component = 0;
        const std::size_t bracket = path.find('[');
        if (bracket != std::string::npos) {
            component = static_cast<std::size_t>(std::atoi(path.c_str() + bracket + 1));
            path = path.substr(0, bracket);
        }
        const auto* descriptor = hoverhere::params::find_descriptor(hoverhere::params::model_param_descriptors(), path);
        if (descriptor == nullptr) {
            std::cerr << "unknown parameter " << path << "\n";
            return 2;
        }
        hoverhere::params::write_component(&options.setup.params, *descriptor, component, std::atof(assignment.c_str() + equals + 1));
        std::cout << "set " << path << "[" << component << "] = " << assignment.substr(equals + 1) << "\n";
    }
    if (scenery && !load_report_ok(io::load_parameters_file(*scenery, hoverhere::params::ParamDocument::Scenery, options.setup.params))) {
        return 3;
    }
    if (!no_mechanics) {
        const std::string stem = heli->stem().string();
        const std::size_t pos = stem.find("_parameter_file");
        const std::string base = pos == std::string::npos ? stem : stem.substr(0, pos);
        if (!main_mech && fs::exists(heli->parent_path() / (base + "_mainrotor_mechanics.xml"))) {
            main_mech = heli->parent_path() / (base + "_mainrotor_mechanics.xml");
        }
        if (!tail_mech && fs::exists(heli->parent_path() / (base + "_tailrotor_mechanics.xml"))) {
            tail_mech = heli->parent_path() / (base + "_tailrotor_mechanics.xml");
        }
        if (main_mech) {
            auto m = hoverhere::params::mechanics::make_default_mainrotor_mechanics();
            if (!load_report_ok(io::load_mainrotor_mechanics_file(*main_mech, m))) {
                return 3;
            }
            options.setup.mainrotor_mechanics = m;
            std::cout << "main rotor mechanics: " << main_mech->string() << "\n";
        }
        if (tail_mech) {
            auto m = hoverhere::params::mechanics::make_default_tailrotor_mechanics();
            if (!load_report_ok(io::load_tailrotor_mechanics_file(*tail_mech, m))) {
                return 3;
            }
            options.setup.tailrotor_mechanics = m;
            std::cout << "tail rotor mechanics: " << tail_mech->string() << "\n";
        }
    }

    // ground
    if (ground_obj) {
        std::string error;
        if (!hh::load_obj(*ground_obj, options.ground_vertices, options.ground_indices, error)) {
            std::cerr << error << "\n";
            return 3;
        }
        if (!ground_raw) {
            for (std::size_t i = 0; i + 2 < options.ground_vertices.size(); i += 3) {
                options.ground_vertices[i] = -options.ground_vertices[i];          // the Unity project mirrors the mesh over x ...
                options.ground_vertices[i + 2] = -options.ground_vertices[i + 2];  // ... and converts z to the right handed physics frame
            }
        }
        std::cout << "ground mesh: " << ground_obj->string() << " (" << options.ground_indices.size() / 3 << " triangles)\n";
    }

    // maneuver
    hh::Maneuver maneuver;
    if (profile) {
        std::string error;
        if (!hh::load_stick_profile(*profile, add_trim, maneuver, error)) {
            std::cerr << error << "\n";
            return 3;
        }
    } else {
        const auto found = hh::find_maneuver(*maneuver_name);
        if (!found) {
            std::cerr << "unknown maneuver '" << *maneuver_name << "', see 'list'\n";
            return 2;
        }
        maneuver = *found;
    }
    std::cout << "maneuver " << maneuver.name << ": " << maneuver.description << "\n";

    const hh::RunSummary summary = hh::run_maneuver(maneuver, options);
    if (!summary.error.empty()) {
        std::cerr << "error: " << summary.error << "\n";
    }
    std::printf("simulated %.3f s in %llu steps\n", summary.duration, static_cast<unsigned long long>(summary.steps));
    std::printf("height: max %.2f m, final %.2f m   vertical velocity: %.2f ... %.2f m/s   max vortex ring strength: %.2f%s\n", summary.max_height,
                summary.final_height, summary.min_vertical_velocity, summary.max_vertical_velocity, summary.max_vortex_ring_strength,
                summary.crashed ? "   CRASHED" : "");
    if (!summary.states_csv.empty()) {
        std::printf("states:    %s\n", summary.states_csv.string().c_str());
    }
    if (!summary.grid_csv.empty()) {
        std::printf("BEMT grid: %s\n", summary.grid_csv.string().c_str());
    }
    return summary.ok ? 0 : 1;
}

// Evaluates the airfoil polar of the BEMT configuration of a loaded helicopter over -180 ... +180 deg with the real
// airfoil_coefficients(): the single source of the curves (tools/headless/plot_airfoil.py only plots this CSV).
// Only the main rotor runs the BEMT; the tail rotor uses the classic model without airfoil curves, so its columns
// come from the same BEMT configuration and are identical to the main rotor columns.
int airfoil_command(const std::vector<std::string>& args)
{
    namespace fs = std::filesystem;
    std::optional<fs::path> heli;
    std::optional<fs::path> out;
    for (std::size_t i = 0; i < args.size(); ++i) {
        if ((args[i] == "--heli" || args[i] == "--out") && i + 1 < args.size()) {
            (args[i] == "--heli" ? heli : out) = args[i + 1];
            ++i;
        } else {
            std::cerr << "unknown argument " << args[i] << "\n" << kUsage;
            return 2;
        }
    }
    if (!heli || !out) {
        std::cerr << "--heli and --out are required\n" << kUsage;
        return 2;
    }
    hoverhere::params::ModelParams params = hoverhere::params::make_default_params();
    if (!load_report_ok(hoverhere::io::load_parameters_file(*heli, hoverhere::params::ParamDocument::Heli, params))) {
        return 3;
    }
    const hoverhere::params::ModelParamsSI si = hoverhere::params::compile_to_si(params);
    const hoverhere::aero::BemtConfig main_config = hoverhere::aero::make_bemt_config(si);
    const hoverhere::aero::BemtConfig tail_config = hoverhere::aero::make_bemt_config(si);  // same airfoil, see above

    if (out->has_parent_path()) {
        std::error_code ec;
        fs::create_directories(out->parent_path(), ec);
    }
    std::ofstream file(*out);
    if (!file) {
        std::cerr << "cannot write " << out->string() << "\n";
        return 3;
    }
    file << "alpha_deg,cl_mr,cd_mr,cl_tr,cd_tr\n";
    constexpr int steps_per_degree = 2;
    for (int k = -180 * steps_per_degree; k <= 180 * steps_per_degree; ++k) {
        const hoverhere::Real alpha_deg = static_cast<hoverhere::Real>(k) / steps_per_degree;
        const hoverhere::Real alpha = alpha_deg * std::numbers::pi_v<hoverhere::Real> / 180;
        hoverhere::Real cl_mr = 0, cd_mr = 0, cl_tr = 0, cd_tr = 0;
        hoverhere::aero::airfoil_coefficients(main_config.airfoil, alpha, cl_mr, cd_mr);
        hoverhere::aero::airfoil_coefficients(tail_config.airfoil, alpha, cl_tr, cd_tr);
        std::array<char, 160> row{};
        std::snprintf(row.data(), row.size(), "%.1f,%.9g,%.9g,%.9g,%.9g\n", alpha_deg, cl_mr, cd_mr, cl_tr, cd_tr);
        file << row.data();
    }
    file.close();
    std::printf("airfoil polar: %s\n", out->string().c_str());
    return 0;
}

int list_command()
{
    for (const hoverhere::headless::Maneuver& m : hoverhere::headless::builtin_maneuvers()) {
        std::printf("  %-16s %5.1f s   %s\n", m.name.c_str(), m.duration, m.description.c_str());
    }
    return 0;
}

}  // namespace

int main(const int argc, char** const argv)
{
    const std::vector<std::string> args(argv + 1, argv + argc);
    if (!args.empty() && args[0] == "list") {
        return list_command();
    }
    if (!args.empty() && args[0] == "airfoil") {
        return airfoil_command(std::vector<std::string>(args.begin() + 1, args.end()));
    }
    if (!args.empty() && args[0] == "run") {
        return run_command(std::vector<std::string>(args.begin() + 1, args.end()));
    }
    if (!args.empty() && args[0] != "--smoke") {
        std::fprintf(stderr, "%s", kUsage);
        return 2;
    }

    const std::string_view version = hoverhere::version_string();
    std::printf("hoverhere_headless_test, physics library %.*s\n", static_cast<int>(version.size()), version.data());

    check_version();
    check_math();
#ifdef HOVERHERE_WITH_C_API
    check_c_api();
#endif

    std::printf("%s\n", failures == 0 ? "smoke test passed" : "smoke test FAILED");
    return failures == 0 ? 0 : 1;
}
