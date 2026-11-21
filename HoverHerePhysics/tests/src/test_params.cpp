#include <cmath>
#include <ostream>
#include <set>
#include <string>
#include <vector>

#include <doctest/doctest.h>

#include "hoverhere/core/frames.hpp"
#include "hoverhere/params/params.hpp"

using namespace hoverhere::params;
using hoverhere::Real;

namespace {

const ParamDescriptor& descriptor(const std::string_view path)
{
    const ParamDescriptor* d = find_descriptor(model_param_descriptors(), path);
    REQUIRE_MESSAGE(d != nullptr, "no descriptor for " << std::string(path));
    return *d;
}

constexpr Real kPi = hoverhere::frames::kPi;
constexpr Real kDeg = hoverhere::frames::kDegToRad;
constexpr Real kRpm = hoverhere::frames::kRpmToRadPerSec;

}  // namespace

TEST_CASE("generated descriptor tables are complete and consistent")
{
    const auto table = model_param_descriptors();
    CHECK(table.size() == 219);
    CHECK(mechanics::mainrotor_mechanics_descriptors().size() == 2 + 3 * 6 + 2 * 5 + 2 * 4);
    CHECK(mechanics::tailrotor_mechanics_descriptors().size() == 1 + 13 + 2 * 11);

    std::set<std::string_view> paths;
    for (const ParamDescriptor& d : table) {
        CHECK_MESSAGE(paths.insert(d.path).second, "duplicate path " << std::string(d.path));
        CHECK_MESSAGE(d.offset < sizeof(ModelParams), std::string(d.path));
        CHECK_MESSAGE(d.min <= d.max, std::string(d.path));
        CHECK_MESSAGE(!d.doc.empty(), std::string(d.path));
        CHECK_MESSAGE(d.unit.empty() == (d.type == ParamType::Bool || d.type == ParamType::Enum), std::string(d.path));
    }
    CHECK(find_descriptor(table, "does/not/exist") == nullptr);
    CHECK(descriptor("helicopter/mass_total").unit == "kg");
    // the C++ name is spelled correctly, the XML element keeps the historic spelling of the parameter files
    CHECK(descriptor("helicopter/gyro/direct_feedthrough_gyroscope").xml_path == "helicopter/gyro/direct_feadtrough_gyroscope");
}

TEST_CASE("every parameter has its own storage: write_component / read_component round trip")
{
    ModelParams p = make_default_params();
    const auto table = model_param_descriptors();

    // give every component of every parameter a unique value; an overlap of two parameters would overwrite one of them
    double next = 1.0;
    std::vector<double> written;
    for (const ParamDescriptor& d : table) {
        if (d.type == ParamType::PointList) {
            set_list_count(&p, d, d.capacity);
        }
        const std::size_t n = value_count(&p, d);
        for (std::size_t i = 0; i < n; ++i) {
            double v = next;
            next += 1.0;
            if (d.type == ParamType::Bool) {
                v = 1.0;
            } else if (d.type == ParamType::Int || d.type == ParamType::Enum) {
                v = static_cast<double>(static_cast<int>(next) % 7);
            }
            write_component(&p, d, i, v);
            written.push_back(v);
        }
    }
    std::size_t k = 0;
    for (const ParamDescriptor& d : table) {
        const std::size_t n = value_count(&p, d);
        for (std::size_t i = 0; i < n; ++i, ++k) {
            CHECK_MESSAGE(read_component(&p, d, i) == written[k], std::string(d.path) << "[" << i << "]");
        }
    }
}

TEST_CASE("descriptor offsets address the typed struct members")
{
    ModelParams p = make_default_params();

    write_component(&p, descriptor("helicopter/mass_total"), 0, 4.25);
    CHECK(p.helicopter.mass_total == 4.25);

    write_component(&p, descriptor("helicopter/accumulator/cell_count"), 0, 12);
    CHECK(p.helicopter.accumulator.cell_count == 12);

    write_component(&p, descriptor("helicopter/tailrotor/rotor_exists"), 0, 0);
    CHECK_FALSE(p.helicopter.tailrotor.rotor_exists);
    write_component(&p, descriptor("helicopter/propeller/rotor_exists"), 0, 1);
    CHECK(p.helicopter.propeller.rotor_exists);

    write_component(&p, descriptor("helicopter/collision/positions_left_type"), 0, 1);
    CHECK(p.helicopter.collision.positions_left_type == 1);

    write_component(&p, descriptor("helicopter/Jxx_Jyy_Jzz"), 2, 0.5);
    CHECK(p.helicopter.Jxx_Jyy_Jzz.a[2] == 0.5);

    write_component(&p, descriptor("helicopter/governor/target_rpm"), 1, 1999);
    CHECK(p.helicopter.governor.target_rpm[1] == 1999);

    const ParamDescriptor& list = descriptor("helicopter/collision/positions_right");
    CHECK(set_list_count(&p, list, 100) == list.capacity);  // clamped to the capacity
    set_list_count(&p, list, 2);
    write_component(&p, list, 3, -0.25);
    CHECK(p.helicopter.collision.positions_right.count == 2);
    CHECK(p.helicopter.collision.positions_right.items[1].a[0] == -0.25);
    CHECK(value_count(&p, list) == 6);

    // the same parameter reached through the SI struct has the same offset
    ModelParamsSI si{};
    write_component(&si, descriptor("helicopter/mainrotor/R"), 0, 0.5);
    CHECK(si.helicopter.mainrotor.R == 0.5);
}

TEST_CASE("the default parameters are inside the schema bounds")
{
    const ModelParams p = make_default_params();
    const auto violations = validate(p);
    for (const Violation& v : violations) {
        MESSAGE(describe(v));
    }
    CHECK(violations.empty());

    CHECK(p.helicopter.mass_total == 3.6);
    CHECK(p.helicopter.mainrotor.R == 0.775);
    CHECK(p.helicopter.tailrotor.R == 0.138);
    CHECK(p.helicopter.propeller.rotor_exists == false);
    CHECK(p.helicopter.governor.target_rpm[2] == 1550);
    CHECK(p.scenery.gravity == 9.81);
    CHECK(p.tuning.rotor_tuning.mainrotor_tuning.inflow_factor == 1.2);
    CHECK(p.tuning.rotor_tuning.tailrotor_tuning.inflow_factor == 1.0);
    CHECK(p.helicopter.vertical_fin.horizontal0_or_vertical1);
    CHECK(p.helicopter.collision.positions_left_rised_offset.a[2] == 0.095);
}

TEST_CASE("validate reports values outside the bounds and NaN, but not calculated values")
{
    ModelParams p = make_default_params();
    p.helicopter.mass_total = 250;  // max 100
    p.helicopter.mainrotor.b = 1;   // min 2
    p.scenery.weather.wind_direction = std::nan("");
    p.helicopter.accumulator.voltage = 1e9;  // calculated

    const auto violations = validate(p);
    std::set<std::string> paths;
    for (const Violation& v : violations) {
        paths.insert(std::string(v.descriptor->path));
    }
    CHECK(paths == std::set<std::string>{"helicopter/mass_total", "helicopter/mainrotor/b", "scenery/weather/wind_direction"});
    for (const Violation& v : violations) {
        if (v.descriptor->path == "helicopter/mass_total") {
            CHECK(describe(v).find("helicopter/mass_total: value 250 outside [0.1, 100] kg") == 0);
        }
    }
}

TEST_CASE("update_calculated reproduces the formulas of Update_Calculated_Parameter")
{
    ModelParams p = make_default_params();
    update_calculated(p);
    const auto& h = p.helicopter;

    CHECK(h.brushless.Ke == doctest::Approx(1.0 / (560 * kRpm)));
    CHECK(h.brushless.Kt == h.brushless.Ke);
    CHECK(h.brushless.k == h.brushless.Ke);
    CHECK(h.accumulator.voltage == doctest::Approx(6 * 3.7));
    CHECK(h.governor.saturation_max == h.accumulator.voltage);

    CHECK(h.transmission.n_mo2mr == doctest::Approx(106.0 / 15.0));  // inverted: -(-1) * 106 / 15
    CHECK(h.transmission.n_mr2tr == doctest::Approx(-9.0 / 42.0));
    CHECK(h.transmission.n_mr2pr == doctest::Approx(-9.0 / 42.0));
    CHECK(h.transmission.n_mo2tr == doctest::Approx(h.transmission.n_mr2tr * h.transmission.n_mo2mr));
    CHECK(h.transmission.J_momg_mo == doctest::Approx(8e-5 + 0.0002 / ((106.0 / 15.0) * (106.0 / 15.0))));
    CHECK(h.transmission.J_mrtrpr_mr == doctest::Approx(0.06 + 0.000115 / ((9.0 / 42.0) * (9.0 / 42.0))));

    // tailrotor oriLH = (90, 0, 0) deg: the shaft points along +z; mainrotor oriLH = 0: along +y
    CHECK(std::abs(h.tailrotor.dirLH.a[0]) < 1e-12);
    CHECK(std::abs(h.tailrotor.dirLH.a[1]) < 1e-12);
    CHECK(h.tailrotor.dirLH.a[2] == doctest::Approx(1.0));
    CHECK(h.mainrotor.dirLH.a[1] == doctest::Approx(1.0));
    // propeller oriLH = (0, 0, 270) deg: rotating y by 270 deg about z gives +x
    CHECK(h.propeller.dirLH.a[0] == doctest::Approx(1.0));

    // with a propeller but without a tailrotor the C# code left J_mrtrpr_mr unset; here every existing rotor contributes
    ModelParams q = make_default_params();
    q.helicopter.tailrotor.rotor_exists = false;
    q.helicopter.propeller.rotor_exists = true;
    update_calculated(q);
    CHECK(q.helicopter.transmission.J_mrtrpr_mr == doctest::Approx(0.06 + 0.0003 / ((9.0 / 42.0) * (9.0 / 42.0))));
    q.helicopter.propeller.rotor_exists = false;
    update_calculated(q);
    CHECK(q.helicopter.transmission.J_mrtrpr_mr == doctest::Approx(0.06));
}

TEST_CASE("to_si converts angles, speeds, charge and temperature")
{
    ModelParams p = make_default_params();
    const ModelParamsSI si = compile_to_si(p);

    CHECK(si.helicopter.mainrotor.K_col == doctest::Approx(12 * kDeg));
    CHECK(si.helicopter.mainrotor.Theta_col_0 == 0.0);
    CHECK(si.helicopter.tailrotor.Theta_col_0 == doctest::Approx(-7.5 * kDeg));
    CHECK(si.helicopter.tailrotor.oriLH.a[0] == doctest::Approx(kPi / 2));
    CHECK(si.helicopter.governor.target_rpm[0] == doctest::Approx(1160 * kRpm));
    CHECK(si.helicopter.flybarless.K_a[1] == doctest::Approx(270 * kDeg));
    CHECK(si.helicopter.flybarless.K_I[0] == 2.5);                              // 1/s stays
    CHECK(si.helicopter.accumulator.capacity == doctest::Approx(5000 * 3.6));  // mAh -> A s
    CHECK(si.helicopter.brushless.ns == doctest::Approx(560 * kRpm));
    CHECK(si.helicopter.sound_volume == doctest::Approx(1.0));  // percent -> ratio
    CHECK(si.scenery.weather.temperature == doctest::Approx(293.15));
    CHECK(si.scenery.gravity == 9.81);

    // values without a conversion, integers and booleans are copied
    CHECK(si.helicopter.mass_total == 3.6);
    CHECK(si.helicopter.accumulator.cell_count == 6);
    CHECK(si.helicopter.transmission.invert_motor2maingear_transmission);
    CHECK(si.helicopter.collision.damping_factor == 100.0);

    // calculated parameters are part of the SI set
    CHECK(si.helicopter.brushless.Ke == doctest::Approx(1.0 / (560 * kRpm)));
    CHECK(si.helicopter.transmission.n_mo2mr == doctest::Approx(106.0 / 15.0));

    // lists are converted entry by entry (m stays m)
    p.helicopter.collision.positions_left.count = 2;
    p.helicopter.collision.positions_left.items[1] = {{0.5, -0.3, 0.1}};
    const ModelParamsSI si2 = to_si(p);
    CHECK(si2.helicopter.collision.positions_left.count == 2);
    CHECK(si2.helicopter.collision.positions_left.items[1].a[1] == -0.3);
}

TEST_CASE("to_si applies the conversion of every descriptor")
{
    ModelParams tens = make_default_params();
    const auto table = model_param_descriptors();
    const auto is_real_valued = [](const ParamDescriptor& d) {
        return d.type == ParamType::Real || d.type == ParamType::Vec3 || d.type == ParamType::Bank;
    };
    for (const ParamDescriptor& d : table) {
        if (is_real_valued(d)) {
            for (std::size_t i = 0; i < value_count(&tens, d); ++i) {
                write_component(&tens, d, i, 10.0);
            }
        }
    }
    const ModelParamsSI si = to_si(tens);
    for (const ParamDescriptor& d : table) {
        if (is_real_valued(d)) {
            for (std::size_t i = 0; i < value_count(&si, d); ++i) {
                CHECK_MESSAGE(read_component(&si, d, i) == doctest::Approx(d.si_factor * 10.0 + d.si_offset), std::string(d.path));
            }
        }
    }
}
