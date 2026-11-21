#pragma once

#include <cstddef>
#include <span>
#include <string>
#include <string_view>
#include <vector>

#include "hoverhere/params/mechanics_params.hpp"
#include "hoverhere/params/model_params.hpp"
#include "hoverhere/params/param_types.hpp"

// Generic access to the generated parameter structs through their descriptor tables, validation against the
// schema bounds and the compilation of the user-unit parameters to SI.
namespace hoverhere::params {

[[nodiscard]] const ParamDescriptor* find_descriptor(std::span<const ParamDescriptor> table, std::string_view path) noexcept;

// Number of double components currently stored (a list reports 3 * count).
[[nodiscard]] std::size_t value_count(const void* root, const ParamDescriptor& d) noexcept;
[[nodiscard]] double read_component(const void* root, const ParamDescriptor& d, std::size_t index) noexcept;
// Writes one component; integers are rounded, bool is "value != 0". For lists call set_list_count first.
void write_component(void* root, const ParamDescriptor& d, std::size_t index, double value) noexcept;
// Sets the number of entries of a Vec3List (clamped to its capacity) and returns the stored count.
std::size_t set_list_count(void* root, const ParamDescriptor& d, std::size_t count) noexcept;

struct Violation {
    const ParamDescriptor* descriptor;
    std::size_t component;
    double value;
};

// Every component of every non-calculated parameter that is outside [min, max] (NaN counts as outside).
[[nodiscard]] std::vector<Violation> validate(const void* root, std::span<const ParamDescriptor> table);
[[nodiscard]] std::vector<Violation> validate(const ModelParams& params);
[[nodiscard]] std::string describe(const Violation& violation);

// Fills the calculated parameters (gear ratios, inertias, motor constants, rotor axes) from the input parameters,
// in user units (port of Parameter.Update_Calculated_Parameter()).
void update_calculated(ModelParams& params) noexcept;

// Unit conversion of every parameter to SI (see schema/units.yaml); calculated parameters must be up to date.
[[nodiscard]] ModelParamsSI to_si(const ModelParams& params) noexcept;

// update_calculated() followed by to_si(): the parameter set the physics works with.
[[nodiscard]] ModelParamsSI compile_to_si(ModelParams params) noexcept;

}  // namespace hoverhere::params
