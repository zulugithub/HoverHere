#pragma once

#include <array>
#include <cstddef>
#include <cstdint>
#include <limits>
#include <span>
#include <string_view>

#include "hoverhere/core/math.hpp"
#include "hoverhere/params/param_documents.hpp"

namespace hoverhere::params {

// One value per flight bank; flight bank 0..2 selects the entry.
using Bank3 = std::array<Real, 3>;

// Fixed capacity list of points. `reserved` keeps the items 8-byte aligned and the layout identical in C and C#.
template <std::size_t Capacity>
struct Vec3List {
    static constexpr std::size_t kCapacity = Capacity;
    std::int32_t count;
    std::int32_t reserved;
    math::Vec3 items[Capacity];
};

enum class ParamType : std::uint8_t { Real, Int, Bool, Enum, Vec3, Bank, Mat3, PointList };

inline constexpr double kUnbounded = std::numeric_limits<double>::infinity();

// Describes one leaf of a generated parameter struct. All tables are produced by schema/tools/generate_params.py.
struct ParamDescriptor {
    std::string_view path;      // names as in the C++ struct, e.g. "helicopter/mainrotor/R" or "limb_stationary[1]/l_BiAi"
    std::string_view xml_path;  // element path inside the XML document, e.g. "limb_stationary/stru_limb_stationary[1]/parameter/l_BiAi"
    std::string_view unit;      // unit of the stored value (user unit), see schema/units.yaml
    std::string_view doc;
    ParamType type;
    ParamDocument document;
    bool calculated;            // derived value: never read from files
    bool required;              // missing element in the file is an error
    bool cpp_only;              // parameter of the C++ port without counterpart in the C# classes of the Unity application
    std::uint32_t offset;       // byte offset inside the root struct; user and SI struct share the layout
    std::uint32_t capacity;     // list capacity (Vec3List), otherwise 1
    double min;                 // bound for every component (+-kUnbounded when not bounded)
    double max;
    double si_factor;           // si = si_factor * value + si_offset
    double si_offset;
    const std::string_view* options;  // enum option names
    std::uint32_t option_count;
};

// Number of double components of one value (a list reports its capacity).
[[nodiscard]] constexpr std::size_t component_capacity(const ParamDescriptor& d) noexcept
{
    switch (d.type) {
    case ParamType::Vec3:
    case ParamType::Bank:
        return 3;
    case ParamType::Mat3:
        return 9;
    case ParamType::PointList:
        return 3 * static_cast<std::size_t>(d.capacity);
    default:
        return 1;
    }
}

}  // namespace hoverhere::params
