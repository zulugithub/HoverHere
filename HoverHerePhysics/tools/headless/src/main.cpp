#include <cmath>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <numbers>
#include <string>
#include <string_view>

#include "hoverhere/core/math.hpp"
#include "hoverhere/version.hpp"

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
    expect(hoverhere_c_api_check() == 0, "C API: ABI version, argument checks and handle lifecycle (C translation unit)");

    hh_version_info info{};
    info.struct_size = static_cast<std::uint32_t>(sizeof(info));
    const hoverhere::Version version = hoverhere::version();
    const bool matches = hh_get_version(&info) == HH_OK && info.major == static_cast<std::uint32_t>(version.major) &&
                         info.minor == static_cast<std::uint32_t>(version.minor) &&
                         info.patch == static_cast<std::uint32_t>(version.patch);
    expect(matches, "C API: shared library version matches the physics library");
}
#endif

}  // namespace

int main(const int argc, char** const argv)
{
    if (argc > 1 && std::strcmp(argv[1], "--smoke") != 0) {
        std::fprintf(stderr, "usage: hoverhere_headless_test [--smoke]\n");
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
