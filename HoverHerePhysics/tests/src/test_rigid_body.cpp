#include <algorithm>
#include <cmath>

#include <doctest/doctest.h>

#include "hoverhere/core/frames.hpp"
#include "hoverhere/dynamics/rigid_body.hpp"
#include "hoverhere/numerics/rk4.hpp"

using hoverhere::Real;
using hoverhere::math::Mat3;
using hoverhere::math::Quat;
using hoverhere::math::Vec3;
using namespace hoverhere::dynamics;
namespace frames = hoverhere::frames;
namespace num = hoverhere::numerics;
namespace qvm = boost::qvm;

namespace {

constexpr Real kGravity = 9.81;

// asymmetric body with products of inertia, positive definite
RigidBodyParams tumbling_body()
{
    return make_rigid_body_params(4.2, Vec3{{0.05, 0.12, 0.10}}, Vec3{{0.004, -0.003, 0.002}});
}

Real max_abs_difference(const Vec3& a, const Vec3& b)
{
    return std::max({std::abs(a.a[0] - b.a[0]), std::abs(a.a[1] - b.a[1]), std::abs(a.a[2] - b.a[2])});
}

// advances the state with RK4 under a loads function that is a pure function of the state
template <class LoadsFn>
RigidBodyState integrate(const RigidBodyParams& p, RigidBodyState x, const LoadsFn& loads, const Real dt, const int steps)
{
    const auto f = [&](const Real, const RigidBodyState& s) { return rigid_body_derivative(p, s, loads(s)); };
    Real t = 0;
    for (int i = 0; i < steps; ++i) {
        x = num::rk4_step(f, t, x, dt);
        normalize_orientation(x);
        t += dt;
    }
    return x;
}

const auto kGravityOnly = [](const RigidBodyState&) { return RigidBodyLoads{Vec3{{0, -kGravity * 4.2, 0}}, Vec3{{0, 0, 0}}}; };

}  // namespace

TEST_CASE("the inertia tensor uses the standard sign of the products of inertia and is validated")
{
    const RigidBodyParams p = tumbling_body();
    CHECK(is_valid(p));
    CHECK(p.inertia.a[0][1] == -0.004);
    CHECK(p.inertia.a[0][2] == 0.003);
    CHECK(p.inertia.a[1][2] == -0.002);
    CHECK(p.inertia.a[1][0] == p.inertia.a[0][1]);

    const Mat3 identity_check = p.inertia * p.inertia_inverse;
    for (int r = 0; r < 3; ++r) {
        for (int c = 0; c < 3; ++c) {
            CHECK(identity_check.a[r][c] == doctest::Approx(r == c ? 1.0 : 0.0).epsilon(1e-12).scale(1.0));
        }
    }

    // not positive definite (products of inertia larger than the moments), no mass, negative moment: not physical bodies
    CHECK_FALSE(is_valid(make_rigid_body_params(1, Vec3{{0.01, 0.01, 0.01}}, Vec3{{0.02, 0, 0}})));
    CHECK_FALSE(is_valid(make_rigid_body_params(0, Vec3{{1, 1, 1}}, Vec3{{0, 0, 0}})));
    CHECK_FALSE(is_valid(make_rigid_body_params(1, Vec3{{-1, 1, 1}}, Vec3{{0, 0, 0}})));
}

TEST_CASE("angular acceleration equals the closed form of the Unity ODE")
{
    // Jxx..Jzz and the products of inertia of the parameter file, transcribed from Helicopter_ODE.cs dxdt[10..12]
    const Real Jx = 0.05, Jy = 0.12, Jz = 0.10, Jxy = 0.004, Jxz = -0.003, Jyz = 0.002;
    const Real Jyx = Jxy, Jzx = Jxz, Jzy = Jyz;
    const Real MLx = 0.7, MLy = -1.3, MLz = 0.4;
    const Real wLx = 2.0, wLy = -5.0, wLz = 3.0;

    const Real den = Jxy * Jyx * Jz - Jx * Jy * Jz + Jxz * Jy * Jzx + Jx * Jyz * Jzy + Jxy * Jyz * Jzx + Jxz * Jyx * Jzy;
    const Real TZ = MLz + wLx * (Jx * wLy + Jxy * wLx) - wLy * (Jxy * wLy + Jy * wLx) - wLz * (Jxz * wLy - Jyz * wLx);
    const Real TY = MLy - wLx * (Jx * wLz + Jxz * wLx) + wLy * (Jxy * wLz - Jyz * wLx) + wLz * (Jxz * wLz + Jz * wLx);
    const Real TX = MLx - wLx * (Jxy * wLz - Jxz * wLy) + wLy * (Jy * wLz + Jyz * wLy) - wLz * (Jyz * wLz + Jz * wLy);
    const Real d10 = -((Jxz * Jy + Jxy * Jyz) * TZ) / den - ((Jxy * Jz + Jxz * Jzy) * TY) / den - ((Jy * Jz - Jyz * Jzy) * TX) / den;
    const Real d11 = -((Jx * Jyz + Jxz * Jyx) * TZ) / den - ((Jx * Jz - Jxz * Jzx) * TY) / den - ((Jyx * Jz + Jyz * Jzx) * TX) / den;
    const Real d12 = -((Jx * Jy - Jxy * Jyx) * TZ) / den - ((Jx * Jzy + Jxy * Jzx) * TY) / den - ((Jy * Jzx + Jyx * Jzy) * TX) / den;

    const RigidBodyParams p = make_rigid_body_params(4.2, Vec3{{Jx, Jy, Jz}}, Vec3{{Jxy, Jxz, Jyz}});
    const RigidBodyState x = make_rigid_body_state(Vec3{{0, 0, 0}}, frames::quat_identity(), Vec3{{0, 0, 0}}, Vec3{{wLx, wLy, wLz}});
    const RigidBodyState dx = rigid_body_derivative(p, x, RigidBodyLoads{Vec3{{0, 0, 0}}, Vec3{{MLx, MLy, MLz}}});
    CHECK(dx[rb::wx] == doctest::Approx(d10).epsilon(1e-12));
    CHECK(dx[rb::wy] == doctest::Approx(d11).epsilon(1e-12));
    CHECK(dx[rb::wz] == doctest::Approx(d12).epsilon(1e-12));
}

TEST_CASE("free fall conserves the total energy and changes the momentum by exactly m g t")
{
    const RigidBodyParams p = tumbling_body();
    const Vec3 v0{{3.0, 1.5, -2.0}};
    const RigidBodyState x0 = make_rigid_body_state(Vec3{{10, 100, -5}}, frames::quat_identity(), v0, Vec3{{0, 0, 0}});
    const auto energy = [&](const RigidBodyState& x) {
        return kinetic_energy_translational(p, x) + p.mass * kGravity * x[rb::y] + kinetic_energy_rotational(p, x);
    };

    const Real dt = 0.01;
    const int steps = 300;  // 3 s
    const RigidBodyState x1 = integrate(p, x0, kGravityOnly, dt, steps);
    const Real t = dt * steps;

    // constant acceleration is integrated exactly by RK4
    CHECK(x1[rb::y] == doctest::Approx(100 + 1.5 * t - 0.5 * kGravity * t * t).epsilon(1e-12));
    CHECK(x1[rb::x] == doctest::Approx(10 + 3.0 * t).epsilon(1e-12));
    CHECK(x1[rb::z] == doctest::Approx(-5 - 2.0 * t).epsilon(1e-12));
    CHECK(energy(x1) == doctest::Approx(energy(x0)).epsilon(1e-12));

    const Vec3 dp = linear_momentum_O(p, x1) - linear_momentum_O(p, x0);
    CHECK(max_abs_difference(dp, Vec3{{0, -p.mass * kGravity * t, 0}}) < 1e-9);
}

TEST_CASE("a tumbling body keeps its rotational energy and its angular momentum in the world frame")
{
    const RigidBodyParams p = tumbling_body();
    const Vec3 omega0{{2.0, -5.0, 3.0}};
    const Quat q0 = frames::quat_from_A(frames::A_RL_S123(0.3, -0.5, 1.2));
    const RigidBodyState x0 = make_rigid_body_state(Vec3{{0, 50, 0}}, q0, Vec3{{1, 0, 0}}, omega0);

    // free fall: gravity acts in the centre of mass and exerts no torque
    const RigidBodyState x1 = integrate(p, x0, kGravityOnly, 1e-3, 5000);  // 5 s, the body turns many times

    CHECK(kinetic_energy_rotational(p, x1) == doctest::Approx(kinetic_energy_rotational(p, x0)).epsilon(1e-9));
    const Vec3 L0 = angular_momentum_O(p, x0);
    const Vec3 L1 = angular_momentum_O(p, x1);
    CHECK(max_abs_difference(L0, L1) < 1e-9 * qvm::mag(L0));
    CHECK(qvm::mag(angular_momentum_L(p, x1)) == doctest::Approx(qvm::mag(angular_momentum_L(p, x0))).epsilon(1e-9));

    // the angular velocity itself changes (the motion is a real tumble), the attitude stays a unit quaternion
    CHECK(max_abs_difference(angular_velocity_L(x1), omega0) > 0.5);
    CHECK(qvm::mag(orientation(x1)) == doctest::Approx(1.0).epsilon(1e-14));
}

TEST_CASE("a body spinning about a principal axis returns to its attitude after one revolution")
{
    const RigidBodyParams p = make_rigid_body_params(1.0, Vec3{{0.05, 0.12, 0.10}}, Vec3{{0, 0, 0}});
    const Real rate = 2.0 * 3.14159265358979323846;  // one revolution per second about the body y-axis
    const Quat q0 = frames::quat_from_A(frames::A_RL_S123(0.4, 0.2, -0.9));
    const RigidBodyState x0 = make_rigid_body_state(Vec3{{0, 0, 0}}, q0, Vec3{{0, 0, 0}}, Vec3{{0, rate, 0}});
    const auto no_loads = [](const RigidBodyState&) { return RigidBodyLoads{Vec3{{0, 0, 0}}, Vec3{{0, 0, 0}}}; };

    const RigidBodyState x1 = integrate(p, x0, no_loads, 1e-3, 1000);
    const Mat3 A0 = attitude_A_OL(x0);
    const Mat3 A1 = attitude_A_OL(x1);
    Real worst = 0;
    for (int r = 0; r < 3; ++r) {
        for (int c = 0; c < 3; ++c) {
            worst = std::max(worst, std::abs(A1.a[r][c] - A0.a[r][c]));
        }
    }
    CHECK(worst < 1e-10);
    CHECK(max_abs_difference(angular_velocity_L(x1), Vec3{{0, rate, 0}}) < 1e-10);
}

TEST_CASE("forces act in the world frame and torques in the body frame")
{
    const RigidBodyParams p = make_rigid_body_params(2.0, Vec3{{0.1, 0.1, 0.1}}, Vec3{{0, 0, 0}});
    const Quat yawed = frames::quat_from_A(frames::rot_z(1.0));
    const RigidBodyState x = make_rigid_body_state(Vec3{{0, 0, 0}}, yawed, Vec3{{0, 0, 0}}, Vec3{{0, 0, 0}});

    const RigidBodyState dx = rigid_body_derivative(p, x, RigidBodyLoads{Vec3{{4, 0, 0}}, Vec3{{0.5, 0, 0}}});
    CHECK(dx[rb::vx] == doctest::Approx(2.0));  // F^O / m, independent of the attitude
    CHECK(dx[rb::vy] == doctest::Approx(0.0));
    CHECK(dx[rb::wx] == doctest::Approx(5.0));  // M^L / J about the body x-axis
    CHECK(dx[rb::wy] == doctest::Approx(0.0));

    // angular velocity of a symmetric body under a constant torque grows linearly
    const auto torque_only = [](const RigidBodyState&) { return RigidBodyLoads{Vec3{{0, 0, 0}}, Vec3{{0, 0, 0.5}}}; };
    const RigidBodyState x1 = integrate(p, x, torque_only, 1e-2, 200);
    CHECK(x1[rb::wz] == doctest::Approx(0.5 / 0.1 * 2.0).epsilon(1e-12));
}

TEST_CASE("the rigid body derivative works inside the RK4 integrator as a pure function")
{
    static_assert(num::OdeState<RigidBodyState>);
    const RigidBodyParams p = tumbling_body();
    const RigidBodyState x = make_rigid_body_state(Vec3{{1, 2, 3}}, frames::quat_identity(), Vec3{{0, 0, 0}}, Vec3{{0.1, 0.2, 0.3}});
    const RigidBodyLoads loads{Vec3{{1, 2, 3}}, Vec3{{0.1, 0.2, 0.3}}};
    const auto f = [&](const Real, const RigidBodyState& s) { return rigid_body_derivative(p, s, loads); };
    static_assert(num::DerivativeFunction<decltype(f), RigidBodyState>);
    const RigidBodyState a = f(0, x);
    const RigidBodyState b = f(0, x);
    CHECK(a == b);
}
