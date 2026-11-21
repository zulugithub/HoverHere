#include <cmath>
#include <cstdio>
#include <cstring>

#include "hoverhere/core/frames.hpp"
#include "hoverhere/params/params.hpp"

namespace hoverhere::params {

std::vector<Violation> validate(const void* const root, const std::span<const ParamDescriptor> table)
{
    std::vector<Violation> violations;
    for (const ParamDescriptor& d : table) {
        if (d.calculated) {
            continue;
        }
        const std::size_t count = value_count(root, d);
        for (std::size_t i = 0; i < count; ++i) {
            const double v = read_component(root, d, i);
            if (!(v >= d.min && v <= d.max)) {
                violations.push_back({&d, i, v});
            }
        }
    }
    return violations;
}

std::vector<Violation> validate(const ModelParams& params)
{
    return validate(&params, model_param_descriptors());
}

std::string describe(const Violation& violation)
{
    const ParamDescriptor& d = *violation.descriptor;
    char numbers[96];
    std::snprintf(numbers, sizeof numbers, ": value %.9g outside [%.9g, %.9g] ", violation.value, d.min, d.max);
    std::string text(d.path);
    if (component_capacity(d) > 1) {
        text += "[" + std::to_string(violation.component) + "]";
    }
    text += numbers;
    text += d.unit;
    return text;
}

void update_calculated(ModelParams& p) noexcept
{
    using frames::kDegToRad;
    using frames::kRpmToRadPerSec;
    user::HelicopterParams& h = p.helicopter;

    // the rotor local y-axis is the shaft (booster: thrust) axis
    const math::Vec3 y_axis{{0, 1, 0}};
    h.mainrotor.dirLH = frames::A_RL_S123(h.mainrotor.oriLH * kDegToRad) * y_axis;
    h.tailrotor.dirLH = frames::A_RL_S123(h.tailrotor.oriLH * kDegToRad) * y_axis;
    h.propeller.dirLH = frames::A_RL_S123(h.propeller.oriLH * kDegToRad) * y_axis;
    h.booster.dirLH = frames::A_RL_S123(h.booster.oriLH * kDegToRad) * y_axis;

    // ke = kt = k = 1 / kv with kv in (rad/s)/V
    const Real motor_constant = 1 / (h.brushless.ns * kRpmToRadPerSec);
    h.brushless.Ke = motor_constant;
    h.brushless.Kt = motor_constant;
    h.brushless.k = motor_constant;

    h.accumulator.voltage = h.accumulator.voltage_per_cell * static_cast<Real>(h.accumulator.cell_count);
    h.governor.saturation_max = h.accumulator.voltage;

    // reduction i = n_B / n_A for an input gear with n_A teeth and an output gear with n_B teeth
    const auto ratio = [](const bool invert, const std::int32_t driven, const std::int32_t driving) {
        return -(invert ? Real{-1} : Real{1}) * static_cast<Real>(driven) / static_cast<Real>(driving);
    };
    user::Transmission& t = h.transmission;
    t.n_mo2mr = ratio(t.invert_motor2maingear_transmission, t.i_gear_maingear, t.i_gear_motor);
    t.n_mr2tr = ratio(t.invert_mainrotor2tailrotor_transmission, t.i_gear_tailrotor, t.i_gear_mainrotor);
    t.n_mr2pr = ratio(t.invert_mainrotor2propeller_transmission, t.i_gear_propeller, t.i_gear_mainrotor);
    t.n_mo2tr = t.n_mr2tr * t.n_mo2mr;
    t.n_mo2pr = t.n_mr2pr * t.n_mo2mr;

    // J = J1 + J2 / n^2 = J1 + J2 * (omega_driven / omega_driving)^2; the C# code left J_mrtrpr_mr untouched when only
    // the propeller exists
    t.J_momg_mo = h.brushless.J + t.J_mg / (t.n_mo2mr * t.n_mo2mr);
    t.J_mrtrpr_mr = h.mainrotor.J;
    if (h.tailrotor.rotor_exists) {
        t.J_mrtrpr_mr += h.tailrotor.J / (t.n_mr2tr * t.n_mr2tr);
    }
    if (h.propeller.rotor_exists) {
        t.J_mrtrpr_mr += h.propeller.J / (t.n_mr2pr * t.n_mr2pr);
    }
}

ModelParamsSI to_si(const ModelParams& params) noexcept
{
    ModelParamsSI out{};
    std::memcpy(static_cast<void*>(&out), static_cast<const void*>(&params), sizeof out);
    for (const ParamDescriptor& d : model_param_descriptors()) {
        if ((d.si_factor == 1.0 && d.si_offset == 0.0) || d.type == ParamType::Int || d.type == ParamType::Enum ||
            d.type == ParamType::Bool) {
            continue;
        }
        const std::size_t count = value_count(&params, d);
        for (std::size_t i = 0; i < count; ++i) {
            write_component(&out, d, i, d.si_factor * read_component(&params, d, i) + d.si_offset);
        }
    }
    return out;
}

ModelParamsSI compile_to_si(ModelParams params) noexcept
{
    update_calculated(params);
    return to_si(params);
}

}  // namespace hoverhere::params
