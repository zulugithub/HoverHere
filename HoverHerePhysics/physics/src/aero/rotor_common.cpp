#include "hoverhere/aero/rotor_common.hpp"

#include <algorithm>
#include <cmath>

#include "hoverhere/numerics/smooth_damp.hpp"
#include "hoverhere/numerics/smooth_step.hpp"

namespace hoverhere::aero {

namespace qvm = boost::qvm;

BodyKinematics body_kinematics(const dynamics::RigidBodyState& x) noexcept
{
    BodyKinematics k{};
    k.position_O = dynamics::position_O(x);
    k.A_OL = dynamics::attitude_A_OL(x);
    k.velocity_O = dynamics::velocity_O(x);
    k.velocity_L = frames::transposed(k.A_OL) * k.velocity_O;
    k.omega_L = dynamics::angular_velocity_L(x);
    return k;
}

params::si::Rotor as_rotor(const params::si::Mainrotor& source) noexcept
{
    params::si::Rotor rotor{};
    rotor.rotor_exists = source.rotor_exists;
    rotor.b = source.b;
    rotor.R = source.R;
    rotor.C_l_alpha = source.C_l_alpha;
    rotor.c = source.c;
    rotor.J = source.J;
    rotor.C_D0 = source.C_D0;
    rotor.K_col = source.K_col;
    rotor.Theta_col_0 = source.Theta_col_0;
    rotor.posLH = source.posLH;
    rotor.oriLH = source.oriLH;
    rotor.dirLH = source.dirLH;
    return rotor;
}

RotorGeometry make_rotor_geometry(const params::si::Rotor& rotor) noexcept
{
    RotorGeometry g{};
    g.position_L = rotor.posLH;
    g.A_LR = frames::A_RL_S123(rotor.oriLH);
    g.axis_L = g.A_LR * Vec3{{0, 1, 0}};
    return g;
}

Vec3 wind_velocity_O(const params::si::Weather& weather) noexcept
{
    return frames::rot_y(weather.wind_direction) * Vec3{{0, 0, weather.wind_speed}} + Vec3{{0, weather.wind_speed_vertical, 0}};
}

Real limit_symmetric(const Real x, const Real limit, const Real transition) noexcept
{
    if (x < -limit - transition) {
        return -limit;
    }
    if (x < -limit + transition) {  // quadratic blend from the saturation (slope 0) into the linear range (slope 1)
        const Real d = x - (-limit - transition);
        return -limit + d * d / (4 * transition);
    }
    if (x <= limit - transition) {
        return x;
    }
    if (x < limit + transition) {
        const Real d = x - (limit - transition);
        return limit - transition + d - d * d / (4 * transition);
    }
    return limit;
}

Real nonlinear_hub_stiffness(const Real stiffness, const Real start_nonlinearity, const Real flapping_angle) noexcept
{
    if (flapping_angle > start_nonlinearity) {
        const Real s = stiffness * (flapping_angle - start_nonlinearity);
        return stiffness + s * s;
    }
    if (flapping_angle < -start_nonlinearity) {
        const Real s = stiffness * std::abs(flapping_angle + start_nonlinearity);
        return stiffness + s * s;
    }
    return stiffness;
}

Real ground_effect_factor(const Real rotor_radius, const GroundEffectInput& ground, const Vec3& downwash_direction_O,
                          const Real horizontal_speed, const Real induced_velocity) noexcept
{
    const Real perpendicular = qvm::dot(downwash_direction_O, -ground.surface_normal_O);
    const Real vi = std::abs(induced_velocity);
    if (!(perpendicular > 0) || !(vi > 0)) {
        return 1;
    }
    const Real ratio = rotor_radius / (ground.hub_distance_to_ground + rotor_radius / 3);
    const Real speed_ratio = horizontal_speed / vi;
    return ((1 / (1 - (Real{1} / 16) * ratio * ratio / (1 + speed_ratio * speed_ratio))) - 1) * 2 + 1;
}

TurbulenceConfig make_turbulence_config(const params::si::Turbulence& p) noexcept
{
    TurbulenceConfig c{};
    c.rotational_velocity_limit = p.rotational_velocity_limit;
    c.translational_velocity_limit = p.translational_velocity_limit;
    c.frequency = p.frequency;
    c.force_strength = p.force_strength;
    c.torque_strength = p.torque_strength;
    return c;
}

Real turbulence_strength(const TurbulenceConfig& c, const Vec3& omega_L, const Real translational_speed, const Real base_strength) noexcept
{
    // rotation about the shaft (y) disturbs less than the other axes
    const Real rotational = std::sqrt(omega_L.a[0] * omega_L.a[0] + Real{0.2} * omega_L.a[1] * omega_L.a[1] + omega_L.a[2] * omega_L.a[2]);
    const Real s = base_strength +
                   numerics::smooth_step(rotational, c.rotational_velocity_limit / 2, 0, c.rotational_velocity_limit, 1) *
                       numerics::smooth_step(translational_speed, c.translational_velocity_limit / 2, 1, c.translational_velocity_limit, Real{0.25});
    return std::clamp(s, Real{0}, Real{1});
}

void turbulence_post_step(const TurbulenceConfig& c, TurbulenceState& s, const Real strength, const Real time, const Real dt, RotorRng& rng) noexcept
{
    const Real period = 1 / c.frequency;
    const Real elapsed = std::fmod(time, period);
    if (elapsed < s.elapsed_old) {  // a new period began: draw new random values
        const auto draw = [&](const Vec3& amplitude) {
            const Real x = rng.symmetric() * strength * amplitude.a[0];
            const Real y = rng.symmetric() * strength * amplitude.a[1];
            const Real z = rng.symmetric() * strength * amplitude.a[2];
            return Vec3{{x, y, z}};
        };
        s.force_L = draw(c.force_strength);
        s.torque_target_L = draw(c.torque_strength);
    }
    s.elapsed_old = elapsed;
    s.torque_L = numerics::smooth_damp(s.torque_L, s.torque_target_L, s.torque_velocity, c.torque_smooth_time, c.torque_max_speed, dt);
}

}  // namespace hoverhere::aero
