#include <algorithm>
#include <cmath>
#include <filesystem>
#include <fstream>
#include <map>
#include <set>
#include <sstream>
#include <string>
#include <vector>

#include <doctest/doctest.h>

#include "hoverhere/io/param_loader.hpp"

namespace fs = std::filesystem;
namespace io = hoverhere::io;
using namespace hoverhere::params;
using hoverhere::Real;

namespace {

std::string read_file(const fs::path& file)
{
    std::ifstream in(file, std::ios::binary);
    std::ostringstream buffer;
    buffer << in.rdbuf();
    return buffer.str();
}

io::LoadReport load_heli(const std::string_view xml, ModelParams& p, const io::LoadOptions& options = {})
{
    return io::load_parameters_from_text(xml, "test.xml", ParamDocument::Heli, p, options);
}

bool has_diagnostic(const io::LoadReport& report, const io::Severity severity, const std::string_view fragment, const std::string_view path = {})
{
    return std::any_of(report.diagnostics.begin(), report.diagnostics.end(), [&](const io::Diagnostic& d) {
        return d.severity == severity && d.message.find(fragment) != std::string::npos && (path.empty() || d.element_path == path);
    });
}

constexpr const char* kHeliHead = "<?xml version=\"1.0\"?>\n<stru_transmitter_and_helicopter>\n";
constexpr const char* kHeliTail = "</stru_transmitter_and_helicopter>\n";

std::string heli_xml(const std::string& body)
{
    return std::string(kHeliHead) + body + kHeliTail;
}

}  // namespace

TEST_CASE("a parameter file overlays the values that are present and keeps all others")
{
    ModelParams p = make_default_params();
    const ModelParams before = p;
    const auto report = load_heli(heli_xml(R"(
  <helicopter>
    <mass_total>  <val>0.988</val>  </mass_total>
    <Jxx_Jyy_Jzz> <vect3> <y>0.5</y> </vect3> </Jxx_Jyy_Jzz>
    <governor>
      <target_rpm><vect3>  <x>1800</x><y>2000</y><z>2400</z>  </vect3></target_rpm>
      <Kp>  <val>+0.01</val>  </Kp>
    </governor>
    <accumulator> <cell_count> <val>12</val> </cell_count> </accumulator>
    <gyro> <direct_feadtrough_gyroscope> <val>true</val> </direct_feadtrough_gyroscope> </gyro>
    <collision> <positions_left_type> <val>1</val> </positions_left_type> </collision>
  </helicopter>
)"), p);

    INFO(report.format());
    CHECK(report.ok());
    CHECK(report.diagnostics.empty());
    CHECK(report.values_loaded == 7);
    CHECK(p.helicopter.mass_total == 0.988);
    CHECK(p.helicopter.Jxx_Jyy_Jzz.a[0] == before.helicopter.Jxx_Jyy_Jzz.a[0]);  // only y was given
    CHECK(p.helicopter.Jxx_Jyy_Jzz.a[1] == 0.5);
    CHECK(p.helicopter.Jxx_Jyy_Jzz.a[2] == before.helicopter.Jxx_Jyy_Jzz.a[2]);
    CHECK(p.helicopter.governor.target_rpm[0] == 1800);
    CHECK(p.helicopter.governor.target_rpm[2] == 2400);
    CHECK(p.helicopter.governor.Kp == 0.01);
    CHECK(p.helicopter.governor.Ki == before.helicopter.governor.Ki);
    CHECK(p.helicopter.accumulator.cell_count == 12);
    CHECK(p.helicopter.gyro.direct_feedthrough_gyroscope);
    CHECK(p.helicopter.collision.positions_left_type == 1);
    CHECK(p.helicopter.mainrotor.R == before.helicopter.mainrotor.R);
}

TEST_CASE("vector lists are replaced as a whole and an empty list keeps the default")
{
    ModelParams p = make_default_params();
    p.helicopter.collision.positions_left.count = 1;
    p.helicopter.collision.positions_left.items[0] = {{9, 9, 9}};
    p.helicopter.collision.positions_right.count = 1;
    p.helicopter.collision.positions_right.items[0] = {{7, 7, 7}};

    const auto report = load_heli(heli_xml(R"(
  <helicopter><collision>
    <positions_left><vect3>
      <Vector3><x>0.25</x><y>-0.31</y><z>-0.14</z></Vector3>
      <Vector3><x>-0.1</x><y>-0.31</y><z>-0.14</z></Vector3>
    </vect3></positions_left>
    <positions_right><vect3></vect3></positions_right>
  </collision></helicopter>
)"), p);
    INFO(report.format());
    CHECK(report.ok());
    CHECK(p.helicopter.collision.positions_left.count == 2);
    CHECK(p.helicopter.collision.positions_left.items[0].a[0] == 0.25);
    CHECK(p.helicopter.collision.positions_left.items[1].a[2] == -0.14);
    CHECK(p.helicopter.collision.positions_right.count == 1);
    CHECK(p.helicopter.collision.positions_right.items[0].a[0] == 7);
}

TEST_CASE("of several elements with the same name only the first is used, the others are reported")
{
    ModelParams p = make_default_params();
    const auto report = load_heli(heli_xml(R"(
  <helicopter>
    <mass_total><val>1</val></mass_total>
    <mass_total><val>2</val></mass_total>
    <propeller><b><val>5</val></b><R><val>0.4</val></R></propeller>
    <propeller><R><val>0.3</val></R></propeller>
  </helicopter>
)"), p);
    CHECK(report.ok());
    // like the XmlSerializer of the Unity application, which reads every member once
    CHECK(p.helicopter.mass_total == 1);
    CHECK(p.helicopter.propeller.b == 5);
    CHECK(p.helicopter.propeller.R == 0.4);
    CHECK(has_diagnostic(report, io::Severity::Warning, "more than once", "helicopter/mass_total"));
    CHECK(has_diagnostic(report, io::Severity::Warning, "more than once", "helicopter/propeller"));
}

TEST_CASE("metadata of saved parameter files, calculated values and Unity-only elements are ignored silently")
{
    ModelParams p = make_default_params();
    const auto report = load_heli(heli_xml(R"(
  <transmitter><countdown_minutes><val>8</val></countdown_minutes></transmitter>
  <helicopter>
    <name>Bel206</name>
    <mass_total>
      <hint>Total mass of heli</hint><comment/><unit>kg</unit><calculated>false</calculated>
      <show_in_sim>false</show_in_sim><save_under_player_prefs>false</save_under_player_prefs>
      <val>2.5</val><min>0.1</min><max>100</max>
    </mass_total>
    <brushless><Ke><val>5</val></Ke></brushless>
    <rotor_systems_configuration><val>1</val><str>Single main rotor</str><str>Tandem rotor</str></rotor_systems_configuration>
    <visual_effects>
      <mainrotor_running_deformation><val>1</val></mainrotor_running_deformation>
      <mainrotor_running_deformation><val>2</val></mainrotor_running_deformation>
      <landing_gear_main_radius><val>0.03</val></landing_gear_main_radius>
    </visual_effects>
  </helicopter>
)"), p);
    INFO(report.format());
    CHECK(report.diagnostics.empty());
    CHECK(p.helicopter.mass_total == 2.5);
    CHECK(p.helicopter.brushless.Ke == 0.0);
    CHECK(p.helicopter.rotor_systems_configuration == 1);
    CHECK(p.helicopter.visual_effects.landing_gear_main_radius == 0.03);
}

TEST_CASE("misplaced and unknown elements are reported as warnings with their line")
{
    ModelParams p = make_default_params();
    const std::string xml = heli_xml(R"(
  <helicopter>
    <fuselage>
      <S_fxyz>
        <vect3> <x>0.05</x><y>0.1</y><z>0.1</z> </vect3>
        <downwash_factor_mainrotor> <val>0.8</val> </downwash_factor_mainrotor>
      </S_fxyz>
    </fuselage>
    <mass_total><val>1.5</val></mass_total>
  </helicopter>
  <flapping><e><val>0.05</val></e></flapping>
  <tuning><rotor_tuning><propeller><inflow_factor><val>1</val></inflow_factor></propeller></rotor_tuning></tuning>
)");
    const auto report = load_heli(xml, p);
    INFO(report.format());
    CHECK(report.ok());
    CHECK(has_diagnostic(report, io::Severity::Warning, "unknown element <downwash_factor_mainrotor>", "helicopter/fuselage/S_fxyz/downwash_factor_mainrotor"));
    CHECK(has_diagnostic(report, io::Severity::Warning, "unknown element <flapping>", "flapping"));
    CHECK(has_diagnostic(report, io::Severity::Warning, "unknown element <propeller>", "tuning/rotor_tuning/propeller"));
    // the misplaced values have no effect, like in the Unity application
    CHECK(p.helicopter.fuselage.downwash_factor_mainrotor == 0.5);
    CHECK(p.helicopter.flapping.e == 0.07);
    CHECK(p.helicopter.mass_total == 1.5);
    const auto it = std::find_if(report.diagnostics.begin(), report.diagnostics.end(), [](const io::Diagnostic& d) { return d.element_path == "flapping"; });
    REQUIRE(it != report.diagnostics.end());
    CHECK(it->line == 13);

    io::LoadOptions strict;
    strict.unknown_elements_are_errors = true;
    ModelParams q = make_default_params();
    CHECK_FALSE(load_heli(xml, q, strict).ok());
    CHECK(q.helicopter.mass_total == make_default_params().helicopter.mass_total);  // not committed
}

TEST_CASE("values outside the schema bounds are loaded and reported")
{
    ModelParams p = make_default_params();
    const auto report = load_heli(heli_xml("<helicopter><mass_total><val>500</val></mass_total></helicopter>"), p);
    CHECK(report.ok());
    CHECK(p.helicopter.mass_total == 500);
    CHECK(has_diagnostic(report, io::Severity::Warning, "outside [0.1, 100] kg", "helicopter/mass_total"));

    io::LoadOptions quiet;
    quiet.report_range_violations = false;
    ModelParams q = make_default_params();
    CHECK(load_heli(heli_xml("<helicopter><mass_total><val>500</val></mass_total></helicopter>"), q, quiet).diagnostics.empty());
}

TEST_CASE("malformed values are errors and leave the target untouched")
{
    const ModelParams defaults = make_default_params();
    const char* const bad[] = {
        "<mass_total><val>abc</val></mass_total>",
        "<mass_total><val>1.5kg</val></mass_total>",
        "<mass_total><val>nan</val></mass_total>",
        "<accumulator><cell_count><val>6.5</val></cell_count></accumulator>",
        "<gyro><direct_feadtrough_gyroscope><val>True</val></direct_feadtrough_gyroscope></gyro>",
        "<Jxx_Jyy_Jzz><vect3><x>1</x><y>two</y></vect3></Jxx_Jyy_Jzz>",
    };
    for (const char* const body : bad) {
        ModelParams p = defaults;
        p.helicopter.mass_total = 3.3;
        // a valid value in front of the bad one must not be committed either
        const auto report = load_heli(heli_xml(std::string("<helicopter><governor><Kp><val>0.5</val></Kp></governor>") + body + "</helicopter>"), p);
        INFO(body);
        CHECK_FALSE(report.ok());
        CHECK(p.helicopter.mass_total == 3.3);
        CHECK(p.helicopter.governor.Kp == defaults.helicopter.governor.Kp);
    }
}

TEST_CASE("lists longer than their capacity are an error")
{
    std::string list;
    for (int i = 0; i < 9; ++i) {
        list += "<Vector3><x>" + std::to_string(i) + "</x><y>0</y><z>0</z></Vector3>";
    }
    ModelParams p = make_default_params();
    const auto report = load_heli(heli_xml("<helicopter><collision><positions_usual><vect3>" + list + "</vect3></positions_usual></collision></helicopter>"), p);
    CHECK_FALSE(report.ok());
    CHECK(has_diagnostic(report, io::Severity::Error, "capacity is 8"));
    CHECK(p.helicopter.collision.positions_usual.count == 0);
}

TEST_CASE("wrong root elements and malformed XML are errors")
{
    ModelParams p = make_default_params();
    CHECK_FALSE(io::load_parameters_from_text("<stru_scenery/>", "a.xml", ParamDocument::Heli, p).ok());
    CHECK_FALSE(io::load_parameters_from_text("<stru_transmitter_and_helicopter><helicopter>", "b.xml", ParamDocument::Heli, p).ok());
    CHECK_FALSE(io::load_parameters_from_text("", "c.xml", ParamDocument::Heli, p).ok());
    CHECK_FALSE(io::load_parameters_file("this/file/does/not/exist.xml", ParamDocument::Heli, p).ok());
    CHECK_FALSE(io::load_parameters_from_text(heli_xml(""), "d.xml", ParamDocument::MainrotorMechanics, p).ok());

    const auto syntax = io::load_parameters_from_text("<a>\n<b>\n</a>", "e.xml", ParamDocument::Heli, p);
    REQUIRE_FALSE(syntax.diagnostics.empty());
    CHECK(syntax.diagnostics[0].line == 3);
}

TEST_CASE("scenery files are loaded the same way")
{
    ModelParams p = make_default_params();
    const auto report = io::load_parameters_from_text(R"(<stru_scenery>
  <gravity><val>1.62</val></gravity>
  <camera_height><val>1.65</val></camera_height>
  <initial_position><position><vect3><x>5</x><y>1</y><z>-11</z></vect3></position></initial_position>
  <weather><rho_air><val>1.17</val></rho_air><wind_speed><val>3</val></wind_speed></weather>
  <animals><number_of_bird_flocks><val>1</val></number_of_bird_flocks></animals>
</stru_scenery>)", "scenery.xml", ParamDocument::Scenery, p);
    INFO(report.format());
    CHECK(report.diagnostics.empty());
    CHECK(p.scenery.gravity == 1.62);
    CHECK(p.scenery.weather.rho_air == 1.17);
    CHECK(p.scenery.weather.wind_speed == 3.0);
}

TEST_CASE("detect_document recognizes the files by their root element")
{
    CHECK(io::detect_document("<stru_transmitter_and_helicopter/>") == ParamDocument::Heli);
    CHECK(io::detect_document("<stru_scenery/>") == ParamDocument::Scenery);
    CHECK(io::detect_document("<stru_mainrotor_mechanics/>") == ParamDocument::MainrotorMechanics);
    CHECK(io::detect_document("<stru_tailrotor_mechanics/>") == ParamDocument::TailrotorMechanics);
    CHECK_FALSE(io::detect_document("<stru_selection_content/>").has_value());
    CHECK_FALSE(io::detect_document("not xml").has_value());
}

// ---------------------------------------------------------------------------------------------------------------------
// the shipped files, compared with what the C# loader produces (XmlMerging + XmlSerializer)
// ---------------------------------------------------------------------------------------------------------------------
namespace {

struct GoldenRow {
    std::string path;
    std::vector<double> values;
};

struct Golden {
    std::string kind;
    std::vector<GoldenRow> input;
    std::vector<GoldenRow> calculated;
};

Golden read_golden(const fs::path& file)
{
    Golden golden;
    std::ifstream in(file);
    std::string line;
    std::vector<GoldenRow>* section = nullptr;
    while (std::getline(in, line)) {
        if (!line.empty() && line.back() == '\r') {
            line.pop_back();
        }
        if (line.rfind("# kind: ", 0) == 0) {
            golden.kind = line.substr(8);
        } else if (line == "@input") {
            section = &golden.input;
        } else if (line == "@calculated") {
            section = &golden.calculated;
        } else if (!line.empty() && line[0] != '#' && section != nullptr) {
            std::istringstream fields(line);
            GoldenRow row;
            std::getline(fields, row.path, '\t');
            std::string token;
            while (std::getline(fields, token, '\t')) {
                row.values.push_back(std::stod(token));
            }
            section->push_back(std::move(row));
        }
    }
    return golden;
}

bool close(const double a, const double b, const double relative, const double absolute)
{
    return std::abs(a - b) <= relative * std::max(std::abs(a), std::abs(b)) + absolute;
}

std::vector<fs::path> find_files(const fs::path& root, const std::string_view contains)
{
    std::vector<fs::path> files;
    if (fs::exists(root)) {
        for (const auto& entry : fs::recursive_directory_iterator(root)) {
            const std::string name = entry.path().filename().string();
            if (entry.is_regular_file() && name.find(contains) != std::string::npos && entry.path().extension() == ".xml") {
                files.push_back(entry.path());
            }
        }
    }
    std::sort(files.begin(), files.end());
    return files;
}

bool is_ignored(const ParamDocument document, const std::string& path)
{
    for (const std::string_view pattern : document_info(document).ignored_elements) {
        if (path == pattern || (path.size() > pattern.size() && path.compare(0, pattern.size(), pattern) == 0 && path[pattern.size()] == '/')) {
            return true;
        }
    }
    return false;
}

struct DataSet {
    fs::path assets = HOVERHERE_UNITY_ASSETS_DIR;
    fs::path golden = fs::path(HOVERHERE_TEST_DATA_DIR) / "golden";

    std::vector<std::pair<fs::path, ParamDocument>> parameter_files() const
    {
        std::vector<std::pair<fs::path, ParamDocument>> files;
        for (const fs::path& f : find_files(assets / "Helicopters_Available", "_parameter_file")) {
            files.emplace_back(f, ParamDocument::Heli);
        }
        for (const fs::path& f : find_files(assets / "StreamingAssets" / "Skymaps", "_parameter_file")) {
            files.emplace_back(f, ParamDocument::Scenery);
        }
        return files;
    }
};

}  // namespace

TEST_CASE("the shipped helicopter and scenery files give the same values as the C# loader")
{
    const DataSet data;
    const auto files = data.parameter_files();
    if (files.empty()) {
        WARN_MESSAGE(false, "Unity assets not found at " << data.assets.string() << ", test skipped");
        return;
    }
    CHECK(files.size() == 31);

    int compared_values = 0;
    int unity_only_values = 0;
    for (const auto& [file, document] : files) {
        const std::string name = file.stem().string();
        SUBCASE(name.c_str())
        {
            const fs::path golden_file = data.golden / (name + ".golden.txt");
            REQUIRE_MESSAGE(fs::exists(golden_file), "missing golden file " << golden_file.string());
            const Golden golden = read_golden(golden_file);

            ModelParams p = make_default_params();
            const io::LoadReport report = io::load_parameters_file(file, document, p);
            INFO(report.format());
            CHECK(report.ok());
            // no value of a shipped file may be outside the schema bounds
            for (const io::Diagnostic& d : report.diagnostics) {
                CHECK_MESSAGE(d.message.find("outside [") == std::string::npos, d.message << " [" << d.element_path << "]");
            }
            update_calculated(p);

            std::map<std::string, const ParamDescriptor*> by_xml_path;
            for (const ParamDescriptor& d : model_param_descriptors()) {
                if (d.document == document) {
                    by_xml_path[std::string(d.xml_path)] = &d;
                }
            }

            std::set<std::string> in_golden;
            for (const GoldenRow& row : golden.input) {
                in_golden.insert(row.path);
                const auto it = by_xml_path.find(row.path);
                if (it == by_xml_path.end()) {
                    CHECK_MESSAGE(is_ignored(document, row.path), "C# parameter " << row.path << " is neither in the schema nor ignored");
                    ++unity_only_values;
                    continue;
                }
                const ParamDescriptor& d = *it->second;
                if (d.calculated) {
                    continue;
                }
                REQUIRE_MESSAGE(value_count(&p, d) == row.values.size(), row.path);
                for (std::size_t i = 0; i < row.values.size(); ++i) {
                    CHECK_MESSAGE(close(read_component(&p, d, i), row.values[i], 1e-6, 1e-12), row.path << "[" << i << "]: " << read_component(&p, d, i) << " vs C# " << row.values[i]);
                    ++compared_values;
                }
            }
            // every parameter of the schema exists in the C# classes
            for (const auto& [xml_path, d] : by_xml_path) {
                CHECK_MESSAGE(in_golden.count(xml_path) == 1, "schema parameter " << xml_path << " does not exist in the C# classes");
            }

            for (const GoldenRow& row : golden.calculated) {
                const auto it = by_xml_path.find(row.path);
                REQUIRE_MESSAGE(it != by_xml_path.end(), row.path);
                const ParamDescriptor& d = *it->second;
                CHECK_MESSAGE(d.calculated, row.path);
                const bool propeller_without_tailrotor = row.path == "helicopter/transmission/J_mrtrpr_mr" && !p.helicopter.tailrotor.rotor_exists && p.helicopter.propeller.rotor_exists;
                if (propeller_without_tailrotor) {
                    continue;  // the C# code does not calculate this case
                }
                REQUIRE_MESSAGE(value_count(&p, d) == row.values.size(), row.path);
                for (std::size_t i = 0; i < row.values.size(); ++i) {
                    // the C# code calculates in single precision
                    CHECK_MESSAGE(close(read_component(&p, d, i), row.values[i], 2e-6, 1e-6), row.path << "[" << i << "]: " << read_component(&p, d, i) << " vs C# " << row.values[i]);
                }
            }
        }
    }
    (void)compared_values;
    (void)unity_only_values;
}

TEST_CASE("known problems of the shipped parameter files are reported")
{
    const DataSet data;
    const fs::path bell = data.assets / "Helicopters_Available" / "Bell206" / "Resources" / "Bell206_parameter_file.xml";
    const fs::path logo12s = data.assets / "Helicopters_Available" / "Logo600SE_V3" / "Resources" / "Logo600SE_V3_parameter_file_12S_Setup.xml";
    if (!fs::exists(bell) || !fs::exists(logo12s)) {
        WARN_MESSAGE(false, "Unity assets not found, test skipped");
        return;
    }
    ModelParams p = make_default_params();
    const auto report = io::load_parameters_file(bell, ParamDocument::Heli, p);
    CHECK(report.ok());
    // downwash factors written inside <S_fxyz>, and <propeller> instead of <propeller_tuning>
    CHECK(has_diagnostic(report, io::Severity::Warning, "unknown element <downwash_factor_mainrotor>", "helicopter/fuselage/S_fxyz/downwash_factor_mainrotor"));
    CHECK(has_diagnostic(report, io::Severity::Warning, "unknown element <propeller>", "tuning/rotor_tuning/propeller"));
    CHECK(p.helicopter.fuselage.downwash_factor_mainrotor == 0.5);

    ModelParams q = make_default_params();
    const auto report12 = io::load_parameters_file(logo12s, ParamDocument::Heli, q);
    CHECK(report12.ok());
    // <flapping> sits outside <helicopter> and therefore never took effect
    CHECK(has_diagnostic(report12, io::Severity::Warning, "unknown element <flapping>", "flapping"));
    CHECK(q.helicopter.flapping.hub_stiffness_mr == make_default_params().helicopter.flapping.hub_stiffness_mr);
}

// ---------------------------------------------------------------------------------------------------------------------
// rotor mechanics geometry
// ---------------------------------------------------------------------------------------------------------------------
namespace {

using hoverhere::math::Mat3;
using hoverhere::math::Vec3;

Real norm(const Vec3& v)
{
    return std::sqrt(v.a[0] * v.a[0] + v.a[1] * v.a[1] + v.a[2] * v.a[2]);
}

bool orthonormal(const Mat3& m, const Real tolerance)
{
    for (int i = 0; i < 3; ++i) {
        for (int j = 0; j < 3; ++j) {
            Real row_dot = 0;
            Real col_dot = 0;
            for (int k = 0; k < 3; ++k) {
                row_dot += m.a[i][k] * m.a[j][k];
                col_dot += m.a[k][i] * m.a[k][j];
            }
            const Real expected = i == j ? 1.0 : 0.0;
            if (std::abs(row_dot - expected) > tolerance || std::abs(col_dot - expected) > tolerance) {
                return false;
            }
        }
    }
    const Real det = m.a[0][0] * (m.a[1][1] * m.a[2][2] - m.a[1][2] * m.a[2][1]) - m.a[0][1] * (m.a[1][0] * m.a[2][2] - m.a[1][2] * m.a[2][0]) +
                     m.a[0][2] * (m.a[1][0] * m.a[2][1] - m.a[1][1] * m.a[2][0]);
    return std::abs(det - 1) < tolerance;
}

fs::path logo_resources()
{
    return fs::path(HOVERHERE_UNITY_ASSETS_DIR) / "Helicopters_Available" / "Logo600SE_V3" / "Resources";
}

}  // namespace

TEST_CASE("the Blender main rotor mechanics file is loaded completely")
{
    const fs::path file = logo_resources() / "Logo600SE_V3_mainrotor_mechanics.xml";
    if (!fs::exists(file)) {
        WARN_MESSAGE(false, "Unity assets not found, test skipped");
        return;
    }
    mechanics::MainrotorMechanics m = mechanics::make_default_mainrotor_mechanics();
    const auto report = io::load_mainrotor_mechanics_file(file, m);
    INFO(report.format());
    REQUIRE(report.ok());
    CHECK(report.diagnostics.empty());
    CHECK(report.values_loaded == 38);

    CHECK(m.blades_count == 2);
    CHECK(m.r_KO_O.a[1] == doctest::Approx(-0.08499600738286972));
    CHECK(m.limb_stationary[1].l_BiAi == doctest::Approx(0.016999763836902736));
    CHECK(m.limb_stationary[2].r_CiK_LK.a[2] == doctest::Approx(-0.031072216108441353));
    CHECK(m.limb_rotating[1].l_EiDi == doctest::Approx(0.08527769609000496));
    CHECK(m.driver_rotating[0].l_GiFi == doctest::Approx(0.02550490652675202));

    // the nine floats are row-major: the third value is element (0,2), the fourth element (1,0)
    CHECK(m.limb_stationary[0].A_OLAi.a[0][2] == doctest::Approx(0.9999998807907104));
    CHECK(m.limb_stationary[0].A_OLAi.a[1][0] == doctest::Approx(0.9999952912330627));
    CHECK(m.limb_stationary[1].A_OLAi.a[0][1] == doctest::Approx(0.5000317692756653));

    for (const auto& limb : m.limb_stationary) {
        CHECK(orthonormal(limb.A_OLAi, 1e-5));
        CHECK(norm(limb.r_BiAi_LAi) == doctest::Approx(limb.l_BiAi).epsilon(1e-6));  // the arm length is the length of its vector
    }
    for (const auto& limb : m.limb_rotating) {
        CHECK(norm(limb.r_DiJ_LJ) == doctest::Approx(limb.l_DiJ).epsilon(1e-6));
        CHECK(norm(limb.r_EiO_O) == doctest::Approx(limb.l_EiO).epsilon(1e-3));
    }
}

TEST_CASE("the Blender tail rotor mechanics file is loaded completely")
{
    const fs::path file = logo_resources() / "Logo600SE_V3_tailrotor_mechanics.xml";
    if (!fs::exists(file)) {
        WARN_MESSAGE(false, "Unity assets not found, test skipped");
        return;
    }
    mechanics::TailrotorMechanics m = mechanics::make_default_tailrotor_mechanics();
    const auto report = io::load_tailrotor_mechanics_file(file, m);
    INFO(report.format());
    REQUIRE(report.ok());
    CHECK(report.diagnostics.empty());
    CHECK(report.values_loaded == 36);

    CHECK(m.blades_count == 2);
    CHECK(m.limb_stationary.l_CB == doctest::Approx(0.7905608224504999));
    CHECK(m.limb_stationary.r_AO_O.a[0] == doctest::Approx(0.8040772080421448));
    CHECK(m.limb_rotating[0].l_GiJ == doctest::Approx(0.01981693924591821));
    CHECK(m.limb_rotating[1].r_IiO_Bi.a[2] == doctest::Approx(-0.008441506884992123));
    CHECK(norm(m.limb_stationary.r_BA_LA) == doctest::Approx(m.limb_stationary.l_BA).epsilon(1e-6));
    for (const Mat3* a : {&m.limb_stationary.A_OLA, &m.limb_stationary.A_OLD, &m.limb_stationary.A_OLK}) {
        CHECK(orthonormal(*a, 1e-5));
    }
    for (const auto& limb : m.limb_rotating) {
        for (const Mat3* a : {&limb.A_OLJ, &limb.A_OLGi, &limb.A_OLBi}) {
            CHECK(orthonormal(*a, 1e-5));
        }
    }
}

TEST_CASE("mechanics files without all elements are rejected")
{
    const fs::path file = logo_resources() / "Logo600SE_V3_mainrotor_mechanics.xml";
    if (!fs::exists(file)) {
        WARN_MESSAGE(false, "Unity assets not found, test skipped");
        return;
    }
    std::string xml = read_file(file);
    const std::size_t pos = xml.find("<l_CiBi>");
    REQUIRE(pos != std::string::npos);
    xml.erase(pos, xml.find("</l_CiBi>", pos) + 9 - pos);  // first stationary limb has no pushrod length

    mechanics::MainrotorMechanics m = mechanics::make_default_mainrotor_mechanics();
    m.blades_count = 4;
    const auto report = io::load_mainrotor_mechanics_from_text(xml, "broken.xml", m);
    CHECK_FALSE(report.ok());
    CHECK(has_diagnostic(report, io::Severity::Error, "required element is missing", "limb_stationary/stru_limb_stationary[0]/parameter/l_CiBi"));
    CHECK(m.blades_count == 4);  // not committed

    // a matrix needs nine entries
    std::string matrix = read_file(file);
    const std::size_t cell = matrix.find("<float>");
    matrix.erase(cell, matrix.find("</float>", cell) + 8 - cell);
    CHECK_FALSE(io::load_mainrotor_mechanics_from_text(matrix, "broken2.xml", m).ok());
}
