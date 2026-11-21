#pragma once

#include <string_view>

namespace hoverhere {

struct Version {
    int major;
    int minor;
    int patch;
};

Version version() noexcept;
std::string_view version_string() noexcept;

}  // namespace hoverhere
