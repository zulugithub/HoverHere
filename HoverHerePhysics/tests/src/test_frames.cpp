#include <cmath>
#include <numbers>

#include <doctest/doctest.h>

#include "hoverhere/core/frames.hpp"

using hoverhere::Real;
using hoverhere::math::Mat3;
using hoverhere::math::Quat;
using hoverhere::math::Vec3;
namespace frames = hoverhere::frames;
namespace qvm = boost::qvm;

namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;

Real max_abs_difference(const Mat3& a, const Mat3& b)
{
    Real worst = 0;
    for (int r = 0; r < 3; ++r) {
        for (int c = 0; c < 3; ++c) {
            worst = std::max(worst, std::abs(a.a[r][c] - b.a[r][c]));
        }
    }
    return worst;
}

Real max_abs_difference(const Vec3& a, const Vec3& b)
{
    return std::max({std::abs(a.a[0] - b.a[0]), std::abs(a.a[1] - b.a[1]), std::abs(a.a[2] - b.a[2])});
}

bool is_proper_rotation(const Mat3& m, const Real tolerance = 1e-12)
{
    const Mat3 identity{{{1, 0, 0}, {0, 1, 0}, {0, 0, 1}}};
    return max_abs_difference(m * frames::transposed(m), identity) < tolerance && std::abs(qvm::determinant(m) - 1) < tolerance;
}

const Vec3 x_axis{{1, 0, 0}};
const Vec3 y_axis{{0, 1, 0}};
const Vec3 z_axis{{0, 0, 1}};

}  // namespace

TEST_CASE("elementary rotations follow the right-hand rule")
{
    CHECK(max_abs_difference(frames::rot_z(kPi / 2) * x_axis, y_axis) < 1e-15);
    CHECK(max_abs_difference(frames::rot_x(kPi / 2) * y_axis, z_axis) < 1e-15);
    CHECK(max_abs_difference(frames::rot_y(kPi / 2) * z_axis, x_axis) < 1e-15);
    CHECK(max_abs_difference(qvm::cross(x_axis, y_axis), z_axis) == 0.0);  // right handed system
    CHECK(is_proper_rotation(frames::rot_x(0.3)));
    CHECK(is_proper_rotation(frames::rot_y(-1.1)));
    CHECK(is_proper_rotation(frames::rot_z(2.5)));
}

TEST_CASE("S123 / B321 and B123 compose the elementary rotations in the documented order")
{
    const Real phi = 0.3;
    const Real theta = -0.7;
    const Real psi = 1.9;
    CHECK(max_abs_difference(frames::A_RL_S123(phi, theta, psi), frames::rot_z(psi) * frames::rot_y(theta) * frames::rot_x(phi)) < 1e-15);
    CHECK(max_abs_difference(frames::A_RL_B123(phi, theta, psi), frames::rot_x(phi) * frames::rot_y(theta) * frames::rot_z(psi)) < 1e-15);
    CHECK(max_abs_difference(frames::A_RL_S123(phi, theta, psi), frames::A_RL_B123(phi, theta, psi)) > 0.1);
    CHECK(is_proper_rotation(frames::A_RL_S123(phi, theta, psi)));

    // vector form used for the rotor axes: the local y-axis of a rotor with oriLH = (90 deg, 0, 0) points along +z
    const Vec3 shaft = frames::A_RL_S123(Vec3{{kPi / 2, 0, 0}}) * y_axis;
    CHECK(max_abs_difference(shaft, z_axis) < 1e-15);
}

TEST_CASE("Euler angles are recovered from the rotation matrix")
{
    for (const Real phi : {-2.5, -0.4, 0.0, 0.9, 3.0}) {
        for (const Real theta : {-1.4, -0.3, 0.0, 0.6, 1.4}) {
            for (const Real psi : {-3.0, -1.0, 0.0, 0.2, 2.8}) {
                const Vec3 s = frames::euler_S123_from_A(frames::A_RL_S123(phi, theta, psi));
                CHECK(max_abs_difference(s, Vec3{{phi, theta, psi}}) < 1e-9);
                const Vec3 b = frames::euler_B123_from_A(frames::A_RL_B123(phi, theta, psi));
                CHECK(max_abs_difference(b, Vec3{{phi, theta, psi}}) < 1e-9);
            }
        }
    }
}

TEST_CASE("Euler angle extraction survives the gimbal lock")
{
    for (const Real theta : {kPi / 2, -kPi / 2}) {
        const Mat3 a = frames::A_RL_S123(0.4, theta, 0.9);
        const Vec3 e = frames::euler_S123_from_A(a);
        CHECK(max_abs_difference(frames::A_RL_S123(e), a) < 1e-9);
        const Mat3 b = frames::A_RL_B123(0.4, theta, 0.9);
        const Vec3 f = frames::euler_B123_from_A(b);
        CHECK(max_abs_difference(frames::A_RL_B123(f), b) < 1e-9);
    }
}

TEST_CASE("A_RL(q) is the active rotation of the quaternion")
{
    const Vec3 axis = qvm::normalized(Vec3{{1, -2, 0.5}});
    const Real angle = 1.234;
    const Quat q = qvm::rot_quat(axis, angle);
    const Mat3 a = frames::A_RL(q);
    CHECK(is_proper_rotation(a));
    for (const Vec3& v : {x_axis, y_axis, z_axis, Vec3{{0.3, -1.2, 2.2}}}) {
        CHECK(max_abs_difference(a * v, q * v) < 1e-14);
    }
    // rotation about the axis itself leaves it unchanged, Rodrigues for another vector
    CHECK(max_abs_difference(a * axis, axis) < 1e-14);
    const Vec3 v{{0.3, -1.2, 2.2}};
    const Vec3 rodrigues = v * std::cos(angle) + qvm::cross(axis, v) * std::sin(angle) + axis * (qvm::dot(axis, v) * (1 - std::cos(angle)));
    CHECK(max_abs_difference(a * v, rodrigues) < 1e-14);

    CHECK(max_abs_difference(frames::quat_identity() * v, v) == 0.0);
}

TEST_CASE("quaternion and matrix conversions are consistent")
{
    const Mat3 a = frames::A_RL_S123(0.5, -0.8, 2.2);
    const Quat q = frames::quat_from_A(a);
    CHECK(std::abs(qvm::mag(q) - 1) < 1e-14);
    CHECK(max_abs_difference(frames::A_RL(q), a) < 1e-13);
}

TEST_CASE("the quaternion rate matches A' = A skew(omega) for a body-fixed angular velocity")
{
    const Quat q = frames::quat_from_A(frames::A_RL_S123(0.2, 0.5, -1.0));
    const Vec3 omega{{0.7, -1.3, 2.1}};
    const Real dt = 1e-6;

    const Mat3 a0 = frames::A_RL(q);
    const Mat3 a1 = frames::A_RL(frames::quat_integrate(q, omega, dt));
    Mat3 numeric = a1;
    for (int r = 0; r < 3; ++r) {
        for (int c = 0; c < 3; ++c) {
            numeric.a[r][c] = (a1.a[r][c] - a0.a[r][c]) / dt;
        }
    }
    CHECK(max_abs_difference(numeric, a0 * frames::skew(omega)) < 1e-5);

    // and the differential form q' = 1/2 q (x) (0, omega)
    const Quat dq = frames::quat_derivative(q, omega);
    const Quat q1 = frames::quat_integrate(q, omega, dt);
    CHECK(std::abs((q1.a[0] - q.a[0]) / dt - dq.a[0]) < 1e-5);
    CHECK(std::abs((q1.a[1] - q.a[1]) / dt - dq.a[1]) < 1e-5);
    CHECK(std::abs((q1.a[2] - q.a[2]) / dt - dq.a[2]) < 1e-5);
    CHECK(std::abs((q1.a[3] - q.a[3]) / dt - dq.a[3]) < 1e-5);
}

TEST_CASE("exact quaternion integration of a constant body rate")
{
    const Vec3 omega{{0.0, 0.0, 2.0 * kPi}};  // one revolution per second about the local z-axis
    Quat q = frames::quat_identity();
    for (int i = 0; i < 100000; ++i) {
        q = frames::quat_integrate(q, omega, 1e-5);  // 1 s in total
    }
    CHECK(std::abs(qvm::mag(q) - 1) < 1e-14);
    CHECK(max_abs_difference(frames::A_RL(q), Mat3{{{1, 0, 0}, {0, 1, 0}, {0, 0, 1}}}) < 1e-9);

    // a quarter of the revolution rotates x onto y
    q = frames::quat_integrate(frames::quat_identity(), omega, 0.25);
    CHECK(max_abs_difference(q * x_axis, y_axis) < 1e-14);

    // tiny angles use the series expansion, no division by zero for omega = 0
    const Quat unchanged = frames::quat_integrate(frames::quat_identity(), Vec3{{0, 0, 0}}, 0.01);
    CHECK(max_abs_difference(unchanged * x_axis, x_axis) == 0.0);
}

TEST_CASE("frame from forward and upward direction")
{
    const Vec3 forward{{0.3, 1.0, -0.2}};
    const Vec3 upward{{0.0, 0.2, 1.0}};
    const Mat3 a = frames::A_RL_from_forward_upward(forward, upward);
    CHECK(is_proper_rotation(a, 1e-12));
    CHECK(max_abs_difference(a * y_axis, qvm::normalized(forward)) < 1e-14);
    CHECK(std::abs(qvm::dot(a * x_axis, upward)) < 1e-14);  // e_x = up x forward is perpendicular to up

    const Mat3 b = frames::A_RL_from_axes(x_axis, y_axis, z_axis);
    CHECK(max_abs_difference(b, Mat3{{{1, 0, 0}, {0, 1, 0}, {0, 0, 1}}}) == 0.0);
}

TEST_CASE("skew matrix is the cross product and point velocities add the rotational part")
{
    const Vec3 a{{0.3, -1.2, 2.2}};
    const Vec3 b{{-0.7, 0.1, 0.4}};
    CHECK(max_abs_difference(frames::skew(a) * b, qvm::cross(a, b)) < 1e-15);

    // body rotating about z with 2 rad/s, point at x = 1 m in the body frame, body yawed by 90 degrees
    const Mat3 attitude = frames::rot_z(kPi / 2);
    const Vec3 v = frames::point_velocity_R(Vec3{{1, 0, 0}}, attitude, Vec3{{0, 0, 2}}, Vec3{{1, 0, 0}});
    // omega x r = (0, 2, 0) in the body frame, rotated by 90 degrees about z: (-2, 0, 0), plus the body velocity (1, 0, 0)
    CHECK(max_abs_difference(v, Vec3{{-1, 0, 0}}) < 1e-14);
}
