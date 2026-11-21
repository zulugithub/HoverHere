#include <algorithm>
#include <cmath>
#include <cstring>

#include "hoverhere/params/params.hpp"

namespace hoverhere::params {
namespace {

const std::byte* field(const void* root, const ParamDescriptor& d) noexcept
{
    return static_cast<const std::byte*>(root) + d.offset;
}

std::byte* field(void* root, const ParamDescriptor& d) noexcept
{
    return static_cast<std::byte*>(root) + d.offset;
}

double load_real(const std::byte* p) noexcept
{
    double v;
    std::memcpy(&v, p, sizeof v);
    return v;
}

void store_real(std::byte* p, const double v) noexcept
{
    std::memcpy(p, &v, sizeof v);
}

std::int32_t load_int(const std::byte* p) noexcept
{
    std::int32_t v;
    std::memcpy(&v, p, sizeof v);
    return v;
}

}  // namespace

const ParamDescriptor* find_descriptor(const std::span<const ParamDescriptor> table, const std::string_view path) noexcept
{
    const auto it = std::find_if(table.begin(), table.end(), [path](const ParamDescriptor& d) { return d.path == path; });
    return it == table.end() ? nullptr : &*it;
}

std::size_t value_count(const void* const root, const ParamDescriptor& d) noexcept
{
    if (d.type != ParamType::PointList) {
        return component_capacity(d);
    }
    const std::int32_t count = std::clamp(load_int(field(root, d)), std::int32_t{0}, static_cast<std::int32_t>(d.capacity));
    return 3 * static_cast<std::size_t>(count);
}

double read_component(const void* const root, const ParamDescriptor& d, const std::size_t index) noexcept
{
    const std::byte* p = field(root, d);
    switch (d.type) {
    case ParamType::Real:
        return load_real(p);
    case ParamType::Int:
    case ParamType::Enum:
        return static_cast<double>(load_int(p));
    case ParamType::Bool:
        return std::to_integer<unsigned>(*p) != 0 ? 1.0 : 0.0;
    case ParamType::Vec3:
    case ParamType::Bank:
    case ParamType::Mat3:
        return load_real(p + sizeof(double) * index);
    case ParamType::PointList:
        return load_real(p + 2 * sizeof(std::int32_t) + sizeof(double) * index);
    }
    return 0.0;
}

void write_component(void* const root, const ParamDescriptor& d, const std::size_t index, const double value) noexcept
{
    std::byte* p = field(root, d);
    switch (d.type) {
    case ParamType::Real:
        store_real(p, value);
        break;
    case ParamType::Int:
    case ParamType::Enum: {
        const std::int32_t v = static_cast<std::int32_t>(std::llround(value));
        std::memcpy(p, &v, sizeof v);
        break;
    }
    case ParamType::Bool:
        *p = static_cast<std::byte>(value != 0.0 ? 1 : 0);
        break;
    case ParamType::Vec3:
    case ParamType::Bank:
    case ParamType::Mat3:
        store_real(p + sizeof(double) * index, value);
        break;
    case ParamType::PointList:
        store_real(p + 2 * sizeof(std::int32_t) + sizeof(double) * index, value);
        break;
    }
}

std::size_t set_list_count(void* const root, const ParamDescriptor& d, const std::size_t count) noexcept
{
    const std::int32_t stored = static_cast<std::int32_t>(std::min<std::size_t>(count, d.capacity));
    std::memcpy(field(root, d), &stored, sizeof stored);
    return static_cast<std::size_t>(stored);
}

}  // namespace hoverhere::params
