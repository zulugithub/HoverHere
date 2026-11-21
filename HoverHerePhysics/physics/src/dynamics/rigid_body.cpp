#include "hoverhere/dynamics/rigid_body.hpp"

#include <cmath>

namespace hoverhere::dynamics {

namespace qvm = boost::qvm;
using math::Mat3;
using math::Quat;
using math::Vec3;

namespace {

Real determinant(const Mat3& m) noexcept
{
    return m.a[0][0] * (m.a[1][1] * m.a[2][2] - m.a[1][2] * m.a[2][1]) - m.a[0][1] * (m.a[1][0] * m.a[2][2] - m.a[1][2] * m.a[2][0]) +
           m.a[0][2] * (m.a[1][0] * m.a[2][1] - m.a[1][1] * m.a[2][0]);
}

// inverse by cofactors; the caller guarantees det != 0
Mat3 inverse_with_determinant(const Mat3& m, const Real det) noexcept
{
    const Real inv = 1 / det;
    return Mat3{{{(m.a[1][1] * m.a[2][2] - m.a[1][2] * m.a[2][1]) * inv, (m.a[0][2] * m.a[2][1] - m.a[0][1] * m.a[2][2]) * inv,
                  (m.a[0][1] * m.a[1][2] - m.a[0][2] * m.a[1][1]) * inv},
                 {(m.a[1][2] * m.a[2][0] - m.a[1][0] * m.a[2][2]) * inv, (m.a[0][0] * m.a[2][2] - m.a[0][2] * m.a[2][0]) * inv,
                  (m.a[0][2] * m.a[1][0] - m.a[0][0] * m.a[1][2]) * inv},
                 {(m.a[1][0] * m.a[2][1] - m.a[1][1] * m.a[2][0]) * inv, (m.a[0][1] * m.a[2][0] - m.a[0][0] * m.a[2][1]) * inv,
                  (m.a[0][0] * m.a[1][1] - m.a[0][1] * m.a[1][0]) * inv}}};
}

bool positive_definite(const Mat3& m, const Real det) noexcept
{
    return m.a[0][0] > 0 && m.a[0][0] * m.a[1][1] - m.a[0][1] * m.a[1][0] > 0 && det > 0;
}

}  // namespace

RigidBodyParams make_rigid_body_params(const Real mass, const Vec3& Jxx_Jyy_Jzz, const Vec3& Jxy_Jxz_Jyz) noexcept
{
    RigidBodyParams p{};
    p.mass = mass;
    p.inertia = Mat3{{{Jxx_Jyy_Jzz.a[0], -Jxy_Jxz_Jyz.a[0], -Jxy_Jxz_Jyz.a[1]},
                      {-Jxy_Jxz_Jyz.a[0], Jxx_Jyy_Jzz.a[1], -Jxy_Jxz_Jyz.a[2]},
                      {-Jxy_Jxz_Jyz.a[1], -Jxy_Jxz_Jyz.a[2], Jxx_Jyy_Jzz.a[2]}}};
    const Real det = determinant(p.inertia);
    p.inertia_inverse = positive_definite(p.inertia, det) ? inverse_with_determinant(p.inertia, det) : Mat3{{{0, 0, 0}, {0, 0, 0}, {0, 0, 0}}};
    return p;
}

bool is_valid(const RigidBodyParams& p) noexcept
{
    return p.mass > 0 && positive_definite(p.inertia, determinant(p.inertia));
}

RigidBodyState make_rigid_body_state(const Vec3& position_O, const Quat& orientation, const Vec3& velocity_O, const Vec3& angular_velocity_L) noexcept
{
    return RigidBodyState{position_O.a[0],        position_O.a[1],        position_O.a[2],        orientation.a[0],       orientation.a[1],
                          orientation.a[2],       orientation.a[3],       velocity_O.a[0],        velocity_O.a[1],        velocity_O.a[2],
                          angular_velocity_L.a[0], angular_velocity_L.a[1], angular_velocity_L.a[2]};
}

Vec3 position_O(const RigidBodyState& x) noexcept
{
    return Vec3{{x[rb::x], x[rb::y], x[rb::z]}};
}

Quat orientation(const RigidBodyState& x) noexcept
{
    return Quat{{x[rb::q0], x[rb::q1], x[rb::q2], x[rb::q3]}};
}

Vec3 velocity_O(const RigidBodyState& x) noexcept
{
    return Vec3{{x[rb::vx], x[rb::vy], x[rb::vz]}};
}

Vec3 angular_velocity_L(const RigidBodyState& x) noexcept
{
    return Vec3{{x[rb::wx], x[rb::wy], x[rb::wz]}};
}

Mat3 attitude_A_OL(const RigidBodyState& x) noexcept
{
    return frames::A_RL(frames::quat_normalized(orientation(x)));
}

RigidBodyState rigid_body_derivative(const RigidBodyParams& p, const RigidBodyState& x, const RigidBodyLoads& loads) noexcept
{
    const Vec3 omega = angular_velocity_L(x);
    const Quat dq = frames::quat_derivative(orientation(x), omega);
    const Vec3 acceleration = loads.force_O * (1 / p.mass);
    const Vec3 gyroscopic = qvm::cross(omega, p.inertia * omega);
    const Vec3 domega = p.inertia_inverse * (loads.torque_L - gyroscopic);

    return RigidBodyState{x[rb::vx], x[rb::vy], x[rb::vz], dq.a[0], dq.a[1], dq.a[2], dq.a[3],
                          acceleration.a[0], acceleration.a[1], acceleration.a[2], domega.a[0], domega.a[1], domega.a[2]};
}

void normalize_orientation(RigidBodyState& x) noexcept
{
    const Quat q = frames::quat_normalized(orientation(x));
    x[rb::q0] = q.a[0];
    x[rb::q1] = q.a[1];
    x[rb::q2] = q.a[2];
    x[rb::q3] = q.a[3];
}

Vec3 linear_momentum_O(const RigidBodyParams& p, const RigidBodyState& x) noexcept
{
    return velocity_O(x) * p.mass;
}

Vec3 angular_momentum_L(const RigidBodyParams& p, const RigidBodyState& x) noexcept
{
    return p.inertia * angular_velocity_L(x);
}

Vec3 angular_momentum_O(const RigidBodyParams& p, const RigidBodyState& x) noexcept
{
    return attitude_A_OL(x) * angular_momentum_L(p, x);
}

Real kinetic_energy_translational(const RigidBodyParams& p, const RigidBodyState& x) noexcept
{
    const Vec3 v = velocity_O(x);
    return Real{0.5} * p.mass * qvm::dot(v, v);
}

Real kinetic_energy_rotational(const RigidBodyParams& p, const RigidBodyState& x) noexcept
{
    const Vec3 w = angular_velocity_L(x);
    return Real{0.5} * qvm::dot(w, p.inertia * w);
}

}  // namespace hoverhere::dynamics
