#pragma once

#include <algorithm>
#include <cmath>

#include "hoverhere/core/math.hpp"

// Coordinate transformations and frame helpers (documentation chapter 2). Everything is right handed; the Unity
// left-handed convention (z flipped) only exists in the Unity bridge.
//
//   r^R = A^RL r^L        A^RL maps coordinates of the local frame L to the reference frame R (an active rotation of
//                         the vector when R and L are read as "before" and "after"), A^LR = (A^RL)^T
//   S123 / B321           A^RL = Rz(psi) Ry(theta) Rx(phi)        (rotor orientation oriLH, reference_to_masscentrum)
//   B123 / S321           A^RL = Rx(phi) Ry(theta) Rz(psi)
//   quaternion            q rotates L to R, A^RL(q) = qvm::convert_to<Mat3>(q), rate  q' = 1/2 q (x) (0, omega^L)
namespace hoverhere::frames {

using math::Mat3;
using math::Quat;
using math::Vec3;

inline constexpr Real kPi = 3.14159265358979323846;
inline constexpr Real kDegToRad = kPi / 180;
inline constexpr Real kRadToDeg = 180 / kPi;
inline constexpr Real kRpmToRadPerSec = kPi / 30;

// ---------------------------------------------------------------------------------------------------------------
// elementary rotations (right-hand rule, active)
// ---------------------------------------------------------------------------------------------------------------
[[nodiscard]] inline Mat3 rot_x(const Real angle) noexcept
{
    const Real c = std::cos(angle);
    const Real s = std::sin(angle);
    return Mat3{{{1, 0, 0}, {0, c, -s}, {0, s, c}}};
}

[[nodiscard]] inline Mat3 rot_y(const Real angle) noexcept
{
    const Real c = std::cos(angle);
    const Real s = std::sin(angle);
    return Mat3{{{c, 0, s}, {0, 1, 0}, {-s, 0, c}}};
}

[[nodiscard]] inline Mat3 rot_z(const Real angle) noexcept
{
    const Real c = std::cos(angle);
    const Real s = std::sin(angle);
    return Mat3{{{c, -s, 0}, {s, c, 0}, {0, 0, 1}}};
}

// Rz(psi) Ry(theta) Rx(phi), angles in rad. Same as A_RL_S123 / A_RL_B321 of the C# code.
[[nodiscard]] inline Mat3 A_RL_S123(const Real phi, const Real theta, const Real psi) noexcept
{
    return rot_z(psi) * rot_y(theta) * rot_x(phi);
}

[[nodiscard]] inline Mat3 A_RL_S123(const Vec3& phi_theta_psi) noexcept
{
    return A_RL_S123(phi_theta_psi.a[0], phi_theta_psi.a[1], phi_theta_psi.a[2]);
}

// Rx(phi) Ry(theta) Rz(psi) (intrinsic x - y' - z'')
[[nodiscard]] inline Mat3 A_RL_B123(const Real phi, const Real theta, const Real psi) noexcept
{
    return rot_x(phi) * rot_y(theta) * rot_z(psi);
}

[[nodiscard]] inline Mat3 A_RL_B123(const Vec3& phi_theta_psi) noexcept
{
    return A_RL_B123(phi_theta_psi.a[0], phi_theta_psi.a[1], phi_theta_psi.a[2]);
}

// ---------------------------------------------------------------------------------------------------------------
// Euler angles from a rotation matrix (the inverse of the two functions above)
// ---------------------------------------------------------------------------------------------------------------
// Returns (phi, theta, psi) with theta in [-pi/2, pi/2]; at the gimbal lock (|theta| = pi/2) psi is set to 0.
[[nodiscard]] inline Vec3 euler_S123_from_A(const Mat3& A) noexcept
{
    const Real theta = -std::asin(std::clamp(A.a[2][0], Real{-1}, Real{1}));
    if (std::abs(A.a[2][0]) > 1 - Real{1e-12}) {
        return Vec3{{std::atan2(-A.a[1][2], A.a[1][1]), theta, 0}};
    }
    return Vec3{{std::atan2(A.a[2][1], A.a[2][2]), theta, std::atan2(A.a[1][0], A.a[0][0])}};
}

[[nodiscard]] inline Vec3 euler_B123_from_A(const Mat3& A) noexcept
{
    const Real theta = std::asin(std::clamp(A.a[0][2], Real{-1}, Real{1}));
    if (std::abs(A.a[0][2]) > 1 - Real{1e-12}) {
        return Vec3{{std::atan2(A.a[2][1], A.a[1][1]), theta, 0}};
    }
    return Vec3{{std::atan2(-A.a[1][2], A.a[2][2]), theta, std::atan2(-A.a[0][1], A.a[0][0])}};
}

// ---------------------------------------------------------------------------------------------------------------
// frames from axes
// ---------------------------------------------------------------------------------------------------------------
// skew(v) w = v x w
[[nodiscard]] inline Mat3 skew(const Vec3& v) noexcept
{
    return Mat3{{{0, -v.a[2], v.a[1]}, {v.a[2], 0, -v.a[0]}, {-v.a[1], v.a[0], 0}}};
}

[[nodiscard]] inline Mat3 transposed(const Mat3& m) noexcept
{
    return Mat3{{{m.a[0][0], m.a[1][0], m.a[2][0]}, {m.a[0][1], m.a[1][1], m.a[2][1]}, {m.a[0][2], m.a[1][2], m.a[2][2]}}};
}

// A^LR = (A^RL)^T of a rotation matrix.
[[nodiscard]] inline Mat3 A_LR(const Mat3& A_RL_matrix) noexcept
{
    return transposed(A_RL_matrix);
}

// A^RL = (e_x, e_y, e_z): the columns are the axes of L written in R.
[[nodiscard]] inline Mat3 A_RL_from_axes(const Vec3& e_x, const Vec3& e_y, const Vec3& e_z) noexcept
{
    return Mat3{{{e_x.a[0], e_y.a[0], e_z.a[0]}, {e_x.a[1], e_y.a[1], e_z.a[1]}, {e_x.a[2], e_y.a[2], e_z.a[2]}}};
}

// Frame with e_y along `forward` and e_z as far "up" as possible: e_x = normalize(up x forward), e_z = e_x x e_y
// (equations 3.3 / 3.4). `forward` and `upward` must not be parallel.
[[nodiscard]] inline Mat3 A_RL_from_forward_upward(const Vec3& forward, const Vec3& upward) noexcept
{
    namespace qvm = boost::qvm;
    const Vec3 e_y = qvm::normalized(forward);
    const Vec3 e_x = qvm::normalized(qvm::cross(upward, forward));
    const Vec3 e_z = qvm::cross(e_x, e_y);
    return A_RL_from_axes(e_x, e_y, e_z);
}

// ---------------------------------------------------------------------------------------------------------------
// quaternions
// ---------------------------------------------------------------------------------------------------------------
[[nodiscard]] inline Quat quat_identity() noexcept
{
    return Quat{{1, 0, 0, 0}};
}

[[nodiscard]] inline Quat quat_from_A(const Mat3& A) noexcept
{
    return boost::qvm::normalized(boost::qvm::convert_to<Quat>(A));
}

[[nodiscard]] inline Mat3 A_RL(const Quat& q) noexcept
{
    return boost::qvm::convert_to<Mat3>(q);
}

[[nodiscard]] inline Quat quat_normalized(const Quat& q) noexcept
{
    return boost::qvm::normalized(q);
}

// Rotation q(omega, dt) = exp(1/2 omega dt) of the angular velocity omega applied for dt (series for tiny angles).
[[nodiscard]] inline Quat quat_exp(const Vec3& omega, const Real dt) noexcept
{
    const Real w = boost::qvm::mag(omega);
    const Real half_angle = Real{0.5} * w * dt;
    const Real sinc = half_angle < Real{1e-8} ? Real{0.5} * dt * (1 - half_angle * half_angle / 6)
                                              : std::sin(half_angle) / w;
    return Quat{{std::cos(half_angle), sinc * omega.a[0], sinc * omega.a[1], sinc * omega.a[2]}};
}

// q' = 1/2 q (x) (0, omega^L) for an angular velocity given in the local (body) frame.
[[nodiscard]] inline Quat quat_derivative(const Quat& q, const Vec3& omega_L) noexcept
{
    const Quat w{{0, omega_L.a[0], omega_L.a[1], omega_L.a[2]}};
    const Quat qw = q * w;
    return Quat{{Real{0.5} * qw.a[0], Real{0.5} * qw.a[1], Real{0.5} * qw.a[2], Real{0.5} * qw.a[3]}};
}

// Advances q by omega^L over dt exactly (constant body rate) and keeps it a unit quaternion.
[[nodiscard]] inline Quat quat_integrate(const Quat& q, const Vec3& omega_L, const Real dt) noexcept
{
    return boost::qvm::normalized(q * quat_exp(omega_L, dt));
}

// ---------------------------------------------------------------------------------------------------------------
// kinematics of a point (equations 2.15 - 2.17)
// ---------------------------------------------------------------------------------------------------------------
// Velocity in R of a point fixed in L at r^L: v_P^R = v_O^R + A^RL (omega^L x r^L).
[[nodiscard]] inline Vec3 point_velocity_R(const Vec3& v_O_R, const Mat3& A, const Vec3& omega_L, const Vec3& r_L) noexcept
{
    return v_O_R + A * boost::qvm::cross(omega_L, r_L);
}

}  // namespace hoverhere::frames
