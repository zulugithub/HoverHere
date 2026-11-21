#include "hoverhere/aero/aerodynamics.hpp"

#include <algorithm>
#include <cmath>
#include <numbers>

#include "hoverhere/numerics/smooth_step.hpp"

namespace hoverhere::aero {

namespace qvm = boost::qvm;
using numerics::smooth_step;

AerodynamicsConfig make_aerodynamics_config(const params::si::HelicopterParams& h) noexcept
{
    AerodynamicsConfig c{};
    c.fuselage = h.fuselage;
    c.horizontal_fin = h.horizontal_fin;
    c.vertical_fin = h.vertical_fin;
    c.horizontal_wing_left = h.horizontal_wing_left;
    c.horizontal_wing_right = h.horizontal_wing_right;
    return c;
}

Vec3 fuselage_force_L(const params::si::Fuselage& fuselage, const Real rho_air, const Vec3& air_velocity_L, const RotorWake& wake) noexcept
{
    const Real u = air_velocity_L.a[0];  // longitudinal
    const Real v = air_velocity_L.a[1];  // vertical
    const Real w = air_velocity_L.a[2];  // lateral

    // inverted flight (the wake blows upwards) has a reduced effect on the fuselage
    const Real inverted_mr = smooth_step(wake.mainrotor, -0.5, 0.3333, 0.5, 1.0);
    const Real inverted_tr = smooth_step(wake.tailrotor, -0.5, 0.3333, 0.5, 1.0);
    const Real v_with_wake = v + wake.mainrotor * fuselage.downwash_factor_mainrotor * inverted_mr + wake.tailrotor * fuselage.downwash_factor_tailrotor * inverted_tr;

    const Real half_rho = rho_air / 2;
    return Vec3{{-half_rho * fuselage.S_fxyz.a[0] * u * std::abs(u),
                 -half_rho * fuselage.S_fxyz.a[1] * v_with_wake * std::abs(v_with_wake),
                 -half_rho * fuselage.S_fxyz.a[2] * w * std::abs(w)}};
}

SurfaceLoads wing_loads(const params::si::Wing& wing, const Real rho_air, const BodyKinematics& body, const Vec3& wind_O, const RotorWake& wake) noexcept
{
    const Vec3 wind_L = frames::transposed(body.A_OL) * wind_O;
    const Vec3 velocity = body.velocity_L + qvm::cross(body.omega_L, wing.posLH) - wind_L;  // relative to the air, at the wing

    // the wakes only count where they blow away from the top / towards the sides, and not backwards
    const Real u = velocity.a[0] + (wake.propeller > 0 ? wake.propeller * wing.downwash_factor_propeller : Real{0});
    const Real v = velocity.a[1] + (wake.mainrotor > 0 ? wake.mainrotor * wing.downwash_factor_mainrotor : Real{0});
    const Real w = velocity.a[2] + (wake.tailrotor > 0 ? wake.tailrotor * wing.downwash_factor_tailrotor : Real{0});

    const Real half_rho = rho_air / 2;
    const Real stall = wing.alpha_stall;
    Vec3 force_L{{0, 0, 0}};
    if (!wing.horizontal0_or_vertical1) {  // horizontal surface: normal along the body y-axis
        const Real in_plane_squared = u * u + w * w;
        Real angle_of_attack = std::numbers::pi_v<Real> / 2;
        if (in_plane_squared > 0.00001) {
            angle_of_attack = std::atan(std::abs(v / std::sqrt(in_plane_squared)));
        }
        if (angle_of_attack <= stall) {
            force_L.a[1] = -half_rho * wing.C_l_alpha * angle_of_attack * wing.area * v * std::sqrt(in_plane_squared);
        }
        else {  // stalled: only the dynamic pressure of the cross flow acts
            force_L.a[1] = -half_rho * wing.drag_coeff * wing.area * v * std::abs(v);
        }
    }
    else {  // vertical surface: normal along the body z-axis
        const Real in_plane_squared = u * u + v * v;
        Real angle_of_attack = std::numbers::pi_v<Real> / 2;
        if (in_plane_squared > 0.00001) {
            angle_of_attack = std::atan(std::abs(w / std::sqrt(in_plane_squared)));
        }
        if (angle_of_attack <= stall) {
            force_L.a[2] = -half_rho * wing.C_l_alpha * angle_of_attack * wing.area * w * std::sqrt(in_plane_squared);
        }
        else {
            force_L.a[2] = -half_rho * wing.drag_coeff * wing.area * w * std::abs(w);
        }
    }

    SurfaceLoads loads;
    loads.force_L = force_L;
    loads.torque_L = qvm::cross(wing.posLH, force_L);  // Unity crosses the body position with the world force; here both are body vectors
    return loads;
}

AerodynamicsResult aerodynamics_evaluate(const AerodynamicsConfig& cfg, const BodyKinematics& body, const Vec3& wind_O, const Real rho_air, const RotorWake& wake) noexcept
{
    AerodynamicsResult r{};
    const Vec3 wind_L = frames::transposed(body.A_OL) * wind_O;
    r.fuselage_force_L = fuselage_force_L(cfg.fuselage, rho_air, body.velocity_L - wind_L, wake);
    Vec3 force_L = r.fuselage_force_L;
    Vec3 torque_L{{0, 0, 0}};
    for (const params::si::Wing* wing : {&cfg.horizontal_fin, &cfg.vertical_fin, &cfg.horizontal_wing_left, &cfg.horizontal_wing_right}) {
        if (wing->wing_exists) {
            const SurfaceLoads s = wing_loads(*wing, rho_air, body, wind_O, wake);
            force_L = force_L + s.force_L;
            torque_L = torque_L + s.torque_L;
        }
    }
    r.force_O = body.A_OL * force_L;
    r.torque_L = torque_L;
    return r;
}

}  // namespace hoverhere::aero
