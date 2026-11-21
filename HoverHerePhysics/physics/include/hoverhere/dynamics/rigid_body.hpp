#pragma once

#include <cstddef>

#include "hoverhere/core/frames.hpp"
#include "hoverhere/core/math.hpp"
#include "hoverhere/numerics/rk4.hpp"

// 6-DOF rigid body of the helicopter fuselage (documentation chapters 1 and 2): Newton-Euler equations about the
// center of mass and the quaternion kinematics, as a pure derivative function for the RK4 integrator.
//
// Frames: O is the inertial (world) frame, L is the body frame at the center of mass (x forward, y up, z right).
// Everything is right handed; the Unity bridge flips z.
//
//   r' = v^O
//   q' = 1/2 q (x) (0, omega^L)                         A^OL = A(q)
//   v' = F^O / m                                        (F contains gravity)
//   omega' = J^-1 ( M^L - omega x (J omega) )           (J is the inertia tensor about the center of mass in L)
//
// The inertia tensor is built from the parameter file entries with the standard sign convention
//   J = ( Jxx -Jxy -Jxz ; -Jxy Jyy -Jyz ; -Jxz -Jyz Jzz )        (Jxy = integral of x y dm, ...)
// which is the one the closed form equations in Helicopter_ODE.cs use.
namespace hoverhere::dynamics {

inline constexpr std::size_t kRigidBodyStateSize = 13;

// State layout, identical to x_states[0..12] of the Unity code.
namespace rb {
inline constexpr std::size_t x = 0;   // position r^O [m]
inline constexpr std::size_t y = 1;
inline constexpr std::size_t z = 2;
inline constexpr std::size_t q0 = 3;  // orientation quaternion (w, x, y, z), A^OL(q)
inline constexpr std::size_t q1 = 4;
inline constexpr std::size_t q2 = 5;
inline constexpr std::size_t q3 = 6;
inline constexpr std::size_t vx = 7;  // velocity of the center of mass v^O [m/s]
inline constexpr std::size_t vy = 8;
inline constexpr std::size_t vz = 9;
inline constexpr std::size_t wx = 10;  // angular velocity omega^L [rad/s]
inline constexpr std::size_t wy = 11;
inline constexpr std::size_t wz = 12;
}  // namespace rb

using RigidBodyState = numerics::StateVector<kRigidBodyStateSize>;

struct RigidBodyParams {
    Real mass;                  // [kg]
    math::Mat3 inertia;         // [kg m^2] J^L about the center of mass
    math::Mat3 inertia_inverse; // [1/(kg m^2)] (J^L)^-1, filled by make_rigid_body_params
};

// Total external load on the body, evaluated by the caller from the state (a pure function of it in every RK4 stage).
struct RigidBodyLoads {
    math::Vec3 force_O;   // [N] including gravity
    math::Vec3 torque_L;  // [Nm] about the center of mass
};

// Builds the inertia tensor from (Jxx, Jyy, Jzz) and (Jxy, Jxz, Jyz) of the parameter file and its inverse. Check
// is_valid() before use: a non positive definite tensor (not a physical body) gives a zero inverse.
[[nodiscard]] RigidBodyParams make_rigid_body_params(Real mass, const math::Vec3& Jxx_Jyy_Jzz, const math::Vec3& Jxy_Jxz_Jyz) noexcept;
[[nodiscard]] bool is_valid(const RigidBodyParams& p) noexcept;

[[nodiscard]] RigidBodyState make_rigid_body_state(const math::Vec3& position_O, const math::Quat& orientation, const math::Vec3& velocity_O,
                                                   const math::Vec3& angular_velocity_L) noexcept;

[[nodiscard]] math::Vec3 position_O(const RigidBodyState& x) noexcept;
[[nodiscard]] math::Quat orientation(const RigidBodyState& x) noexcept;  // as stored, not normalized
[[nodiscard]] math::Vec3 velocity_O(const RigidBodyState& x) noexcept;
[[nodiscard]] math::Vec3 angular_velocity_L(const RigidBodyState& x) noexcept;
[[nodiscard]] math::Mat3 attitude_A_OL(const RigidBodyState& x) noexcept;  // A^OL of the normalized quaternion

// f(x) = dx/dt of the equations above for the loads that are constant over the evaluation.
[[nodiscard]] RigidBodyState rigid_body_derivative(const RigidBodyParams& p, const RigidBodyState& x, const RigidBodyLoads& loads) noexcept;

// Keeps the quaternion a unit quaternion. Call it between RK4 steps, never inside a stage.
void normalize_orientation(RigidBodyState& x) noexcept;

// Conserved quantities of the torque free motion and bookkeeping for the tests / the energy monitor.
[[nodiscard]] math::Vec3 linear_momentum_O(const RigidBodyParams& p, const RigidBodyState& x) noexcept;
[[nodiscard]] math::Vec3 angular_momentum_L(const RigidBodyParams& p, const RigidBodyState& x) noexcept;  // J omega
[[nodiscard]] math::Vec3 angular_momentum_O(const RigidBodyParams& p, const RigidBodyState& x) noexcept;  // A^OL J omega
[[nodiscard]] Real kinetic_energy_translational(const RigidBodyParams& p, const RigidBodyState& x) noexcept;
[[nodiscard]] Real kinetic_energy_rotational(const RigidBodyParams& p, const RigidBodyState& x) noexcept;

}  // namespace hoverhere::dynamics
