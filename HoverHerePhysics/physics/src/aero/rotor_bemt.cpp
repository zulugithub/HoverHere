#include "hoverhere/aero/rotor_bemt.hpp"

#include <algorithm>
#include <cmath>
#include <limits>
#include <cstdio>
#include <numbers>

#include "hoverhere/numerics/smooth_step.hpp"

namespace hoverhere::aero {

namespace qvm = boost::qvm;
using numerics::sign0;
using numerics::smooth_step;

namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;

// UnityEngine.Mathf helpers
Real clamp01(const Real x) noexcept
{
    return std::clamp(x, Real{0}, Real{1});
}
Real lerp(const Real a, const Real b, const Real t) noexcept
{
    return a + (b - a) * clamp01(t);
}
Real smooth01(const Real t) noexcept  // Mathf.SmoothStep(0, 1, t)
{
    const Real c = clamp01(t);
    return -2 * c * c * c + 3 * c * c;
}
Real inverse_lerp(const Real a, const Real b, const Real v) noexcept
{
    return a != b ? clamp01((v - a) / (b - a)) : Real{0};
}
Real unity_sign(const Real x) noexcept  // Mathf.Sign: +1 for 0
{
    return x >= 0 ? Real{1} : Real{-1};
}

// C1 continuous max(x, floor): identity above floor + blend, constant below floor - blend
Real smooth_floor(const Real x, const Real floor, const Real blend) noexcept
{
    if (x <= floor - blend) {
        return floor;
    }
    if (x >= floor + blend) {
        return x;
    }
    const Real d = x - floor + blend;
    return floor + d * d / (4 * blend);
}

}  // namespace

// ----------------------------------------------------------------------------------------------------------------
// Johnson vortex ring state model
// ----------------------------------------------------------------------------------------------------------------
Real johnson_momentum_theory(const Real Vz_scaled, const Real Vx_scaled, const bool use_upper_branch) noexcept
{
    if (std::abs(Vz_scaled) < 0.001 && std::abs(Vx_scaled) < 0.001) {
        return 1;
    }
    const Real discriminant = Vz_scaled * Vz_scaled + 4;
    if (discriminant < 0) {
        return 0;
    }
    const Real root = std::sqrt(discriminant);
    return use_upper_branch ? Real{0.5} * (-Vz_scaled + root) : Real{0.5} * (Vz_scaled + root);
}

Real johnson_baseline(const Real Vz_scaled, const Real Vx_scaled, const JohnsonVrsParams& p) noexcept
{
    // clamped so the A/B boundaries cannot run away at high forward speed
    const Real Vx_ratio = std::min(Real{1.2}, Vx_scaled / p.VxC);
    Real VzAID = p.VzA + Real{0.2} * Vx_ratio * Vx_ratio;
    Real VzBID = p.VzB + Real{0.2} * Vx_ratio * Vx_ratio;
    if (Vx_ratio > 0.5) {
        const Real factor = std::min(Real{3}, std::pow(2 * Vx_ratio - 1, Real{3}));
        VzBID = VzBID + Real{0.7} * (VzAID - VzBID) * factor;
    }

    // Boundary B crosses A and re-opens near Vx/vh = 0.8: order them and keep a minimum blend width
    Real Vz_min = std::min(VzBID, VzAID);
    Real Vz_max = std::max(VzBID, VzAID);
    const Real centre = Real{0.5} * (Vz_min + Vz_max);
    const Real width = smooth_floor(Vz_max - Vz_min, p.transition_min_width, p.transition_blend);
    Vz_min = centre - Real{0.5} * width;
    Vz_max = centre + Real{0.5} * width;

    if (Vz_scaled > Vz_min && Vz_scaled < Vz_max) {
        const Real v_A = johnson_momentum_theory(Vz_max, Vx_scaled, true);
        const Real v_B = johnson_momentum_theory(Vz_min, Vx_scaled, false);
        const Real t = (Vz_scaled - Vz_min) / (Vz_max - Vz_min);
        return lerp(v_B, v_A, smooth01(t));
    }
    if (Vz_scaled >= Vz_max) {
        return johnson_momentum_theory(Vz_scaled, Vx_scaled, true);
    }
    return johnson_momentum_theory(Vz_scaled, Vx_scaled, false);
}

Real johnson_vrs_correction(const Real Vz_scaled, const Real Vx_scaled, const JohnsonVrsParams& p, Real& strength_vrs) noexcept
{
    strength_vrs = 0;
    if (Vz_scaled >= 0 || Vx_scaled >= p.VxM) {
        return 0;
    }
    const Real Vx_ratio = Vx_scaled / p.VxM;
    const Real alpha_x = std::pow(1 - Vx_ratio * Vx_ratio, Real{0.2});
    const Real alpha_x_1_5 = std::pow(1 - Vx_ratio * Vx_ratio, Real{1.5});

    const Real VzDID = p.VzD;
    const Real VzNID = Real{0.5} * (p.VzN + p.VzX) + Real{0.5} * (p.VzN - p.VzX) * alpha_x;
    const Real VzXID = Real{0.5} * (p.VzN + p.VzX) - Real{0.5} * (p.VzN - p.VzX) * alpha_x_1_5;
    const Real VzEID = p.VzE + (VzXID - p.VzX);
    if (Vz_scaled <= VzEID || Vz_scaled >= VzDID) {
        return 0;
    }

    // empirical target induced velocities measured against the baseline
    const Real delta_N = (p.Vz_plus_v_N - VzNID) - johnson_baseline(VzNID, Vx_scaled, p);
    const Real delta_X = (p.Vz_plus_v_X - VzXID) - johnson_baseline(VzXID, Vx_scaled, p);

    Real dv_delta = 0;
    if (Vz_scaled >= VzNID && Vz_scaled <= VzDID) {
        const Real denominator = VzNID - VzDID;
        if (std::abs(denominator) > 0.001) {
            const Real t = (Vz_scaled - VzDID) / denominator;
            dv_delta = delta_N * smooth01(t);
            strength_vrs = smooth01(t);
        }
    }
    else if (Vz_scaled >= VzXID && Vz_scaled < VzNID) {
        const Real denominator = VzNID - VzXID;
        if (std::abs(denominator) > 0.001) {
            const Real t = (Vz_scaled - VzXID) / denominator;
            dv_delta = lerp(delta_X, delta_N, smooth01(t));
            strength_vrs = 1;
        }
    }
    else if (Vz_scaled >= VzEID && Vz_scaled < VzXID) {
        const Real denominator = VzXID - VzEID;
        if (std::abs(denominator) > 0.001) {
            const Real t = (Vz_scaled - VzEID) / denominator;
            dv_delta = delta_X * smooth01(t);
            strength_vrs = smooth01(t);
        }
    }

    // fade the correction out when the forward speed approaches VxM instead of cutting it off
    const Real gate = smooth_step(Vx_scaled, Real{0.70} * p.VxM, 1, p.VxM, 0);
    strength_vrs *= gate;
    return dv_delta * gate;
}

Real johnson_table3(const Real Vz_scaled, const Real Vx_scaled, const JohnsonVrsParams& p, Real& strength_vrs) noexcept
{
    const Real baseline = johnson_baseline(Vz_scaled, Vx_scaled, p);
    const Real correction = johnson_vrs_correction(Vz_scaled, Vx_scaled, p, strength_vrs);
    return baseline + p.zeta * p.f * correction;  // zeta scales only the unstable correction, not the physical baseline
}

void johnson_time_lag(const Real v_current, const Real v_previous, const Real omega_mr, const Real tau_rotor_revolutions, const Real dt, Real& dv_lag_state,
                      Real& v_lagged) noexcept
{
    if (std::abs(omega_mr) < 0.1) {
        v_lagged = v_current;
        return;
    }
    const Real tau = std::clamp(tau_rotor_revolutions * (2 * kPi) / std::abs(omega_mr), Real{0.05}, Real{5});
    const Real error = v_current - v_previous;
    dv_lag_state += (error - dv_lag_state) / tau * dt;
    dv_lag_state = std::clamp(dv_lag_state, Real{-10}, Real{10});
    v_lagged = v_previous + dv_lag_state;
}

// ----------------------------------------------------------------------------------------------------------------
// airfoil and blade element balance
// ----------------------------------------------------------------------------------------------------------------
void airfoil_coefficients(const AirfoilCurves& a, const Real alpha, Real& cl, Real& cd) noexcept
{
    const Real abs_alpha = std::abs(alpha);
    Real s = smooth_step(abs_alpha, a.cl_trans_start, 1, a.cl_trans_end, 0);
    cl = (s * (a.cl_slope * abs_alpha) +
          (1 - s) * (+std::sin((Real{3} / 2) / (1 - (a.cl_first_max / 180)) * (abs_alpha - kPi)) * a.cl_amplitude)) *
         unity_sign(alpha);
    s = smooth_step(abs_alpha, a.cd_trans_start, 1, a.cd_trans_end, 0);
    cd = s * (a.cd_slope * std::abs(alpha * alpha * alpha) + a.cd_offset) +
         (1 - s) * (-std::cos((Real{4} / 2) / (1 - (a.cd_first_min / 180)) * (abs_alpha - kPi)) * a.cd_amplitude_or_offset + a.cd_amplitude_or_offset);
}

namespace {

struct ElementPhysics {
    Real residual;  // [N] factor2 Vi |V0| - dT: momentum balance in the product form without singularity at V0 = 0
    Real cl, cd, phi, alpha;
};

ElementPhysics element_function(const BemtConfig& cfg, const Real Vi, const Real Vc, Real V2, const Real beta, const Real factor1, const Real factor2) noexcept
{
    V2 = smooth_step(std::abs(V2), 0.01, 0.01, 0.10, std::abs(V2));
    const Real V0 = Vi + Vc;
    ElementPhysics e{};
    e.phi = std::atan2(V0, V2);
    e.alpha = beta - e.phi;
    if (e.alpha >= kPi) {  // wrap into the range of the curves
        e.alpha -= 2 * kPi;
    }
    if (e.alpha <= -kPi) {
        e.alpha += 2 * kPi;
    }
    airfoil_coefficients(cfg.airfoil, e.alpha, e.cl, e.cd);
    const Real Vlocal_squared = V0 * V0 + V2 * V2;
    const Real DtDr = factor1 * Vlocal_squared * (e.cl * std::cos(e.phi) - e.cd * std::sin(e.phi));
    e.residual = factor2 * Vi * std::abs(V0) - DtDr;
    return e;
}

struct ElementSolution {
    Real Vi;
    Real residual;
    int iterations;
};

// Damped Newton iteration with central differences; the best iterate of the whole path is returned.
ElementSolution solve_element(const BemtConfig& cfg, const Real Vi_start, const Real Vc, const Real V2, const Real beta, const Real factor1, const Real factor2) noexcept
{
    const NewtonSettings& n = cfg.newton;
    Real Vi = Vi_start;
    Real best_Vi = Vi;
    Real best_residual = std::numeric_limits<Real>::infinity();
    int sum = 0;
    bool finished = false;
    while (!finished) {
        const Real residual = element_function(cfg, Vi, Vc, V2, beta, factor1, factor2).residual;
        if (std::abs(residual) < best_residual) {
            best_residual = std::abs(residual);
            best_Vi = Vi;
        }
        if (std::abs(residual) <= n.residual_tolerance) {
            finished = true;
            break;
        }
        const Real residual_plus = element_function(cfg, Vi + n.delta, Vc, V2, beta, factor1, factor2).residual;
        const Real residual_minus = element_function(cfg, Vi - n.delta, Vc, V2, beta, factor1, factor2).residual;
        const Real slope = (residual_plus - residual_minus) / (2 * n.delta);
        if (std::abs(slope) < n.min_slope || !std::isfinite(slope)) {
            break;
        }
        const Real Vi_new = Vi - residual / slope;
        const Real step = std::clamp((Vi_new - Vi) * n.damping, -n.max_step, n.max_step);
        if (std::abs(step) < n.min_step || !std::isfinite(step)) {
            break;
        }
        Vi += step;
        if (sum++ >= n.max_iterations) {
            break;
        }
    }
    const Real final_residual = element_function(cfg, Vi, Vc, V2, beta, factor1, factor2).residual;
    if (std::abs(final_residual) < best_residual) {
        best_residual = std::abs(final_residual);
        best_Vi = Vi;
    }
    return {best_Vi, best_residual, sum};
}

Real hover_induced_velocity(const Real thrust, const Real rotor_radius, const Real rho_air) noexcept
{
    const Real area = kPi * rotor_radius * rotor_radius;
    if (area < 0.01) {
        return 1;
    }
    return std::clamp(std::sqrt(thrust / (2 * rho_air * area)), Real{0.5}, Real{50});
}

}  // namespace

// ----------------------------------------------------------------------------------------------------------------
// configuration, grid, disc state
// ----------------------------------------------------------------------------------------------------------------
BemtConfig make_bemt_config(const params::si::ModelParams& p) noexcept
{
    BemtConfig c{};
    c.rotor = p.helicopter.mainrotor;
    c.disc_inertia_tilt = p.helicopter.flapping.I_flapping;
    c.disc_inertia_spin = p.helicopter.mainrotor.J;
    c.gravity = p.scenery.gravity;
    c.wake_inertia_fumble_factor = p.tuning.rotor_tuning.mainrotor_tuning.wake_inertia_fumble_factor;
    c.turbulence_config = make_turbulence_config(p.tuning.turbulence);
    return c;
}

BemtGrid make_bemt_grid(const BemtConfig& cfg) noexcept
{
    BemtGrid g{};
    const Real R = cfg.rotor.R;
    g.segment_length = (R - R * cfg.root_cutout) / (static_cast<Real>(kBemtRadialCount) * 2);  // half the width of one ring
    g.sector_angle = 2 * kPi / static_cast<Real>(kBemtAzimuthCount);
    for (std::size_t r = 0; r < kBemtRadialCount; ++r) {
        const Real radius = R * cfg.root_cutout + g.segment_length * static_cast<Real>((r + 1) * 2 - 1);
        for (std::size_t c = 0; c < kBemtAzimuthCount; ++c) {
            g.radius[bemt_index(r, c)] = radius;
            g.psi[bemt_index(r, c)] = g.sector_angle * static_cast<Real>(c);
        }
    }
    return g;
}

RotorDiscStateVector make_rotor_disc_state(const Quat& orientation, const Real wx, const Real wz) noexcept
{
    return RotorDiscStateVector{orientation.a[0], orientation.a[1], orientation.a[2], orientation.a[3], wx, wz};
}

Quat disc_orientation(const RotorDiscStateVector& x) noexcept
{
    return Quat{{x[disc::q0], x[disc::q1], x[disc::q2], x[disc::q3]}};
}

void normalize_disc_orientation(RotorDiscStateVector& x) noexcept
{
    const Quat q = frames::quat_normalized(disc_orientation(x));
    x[disc::q0] = q.a[0];
    x[disc::q1] = q.a[1];
    x[disc::q2] = q.a[2];
    x[disc::q3] = q.a[3];
}

Real pitt_peters_time_constant(const Real fumble_factor, const Real rotor_radius, const Real V2, const Real Vi_lag, const Real Vc) noexcept
{
    const Real U_local = std::max(std::sqrt(V2 * V2 + (Vi_lag + Vc) * (Vi_lag + Vc)), Real{0.1});
    return fumble_factor * (8 / (3 * kPi)) * rotor_radius / U_local;
}

Real pitt_peters_step(const Real Vi_lag, const Real Vi_target, const Real dt, const Real tau) noexcept
{
    if (!(tau > 0)) {  // no inertia: the induced velocity follows instantly
        return Vi_target;
    }
    const Real a = dt / tau;
    return (Vi_lag + a * Vi_target) / (1 + a);
}

// ----------------------------------------------------------------------------------------------------------------
// evaluation
// ----------------------------------------------------------------------------------------------------------------
BemtOutputs bemt_evaluate(const BemtConfig& cfg, const BemtGrid& grid, const BemtFilterState& state, const BemtInputs& in) noexcept
{
    BemtOutputs out{};
    const params::si::Rotor& rotor = cfg.rotor;
    const RotorGeometry geometry = make_rotor_geometry(rotor);
    const Mat3 A_OL = in.body.A_OL;
    const Mat3 A_LR = geometry.A_LR;
    const Mat3 A_RL = frames::transposed(A_LR);
    const Mat3 A_OR = A_OL * A_LR;
    const Mat3 A_OD = frames::A_RL(frames::quat_normalized(disc_orientation(in.disc)));
    const Mat3 A_DO = frames::transposed(A_OD);
    const Real wx_disc = in.disc[disc::wx];
    const Real wz_disc = in.disc[disc::wz];

    // ---- disc <-> hub: spring and damper (the disc may tilt against the shaft)
    const Mat3 A_RD = A_RL * frames::transposed(A_OL) * A_OD;  // disc -> rotor frame
    const Vec3 e_y_disc_R = A_RD * Vec3{{0, 1, 0}};
    const Vec3 T_stiff_R{{+e_y_disc_R.a[2] * cfg.hub_stiffness, 0, -e_y_disc_R.a[0] * cfg.hub_stiffness}};

    Vec3 omega_hub_R = A_RL * in.body.omega_L;
    omega_hub_R.a[1] = 0;
    Vec3 omega_disc_R = A_RD * Vec3{{wx_disc, 0, wz_disc}};
    omega_disc_R.a[1] = 0;
    const Vec3 delta_omega_R = omega_disc_R - omega_hub_R;
    const Vec3 T_damp_R{{delta_omega_R.a[0] * cfg.hub_damping, 0, delta_omega_R.a[2] * cfg.hub_damping}};

    const Vec3 T_hub_R = T_stiff_R + T_damp_R;                      // actio at the fuselage
    out.torque_hub_on_body_L = A_LR * T_hub_R;
    out.torque_hub_on_disc_D = -(frames::transposed(A_RD) * T_hub_R);  // reactio at the disc

    // ---- non rotating frame of the virtual blades: its x-axis stays in the plane normal to the hub's z-axis
    const Vec3 upward = A_OR * Vec3{{0, 0, -1}};
    const Vec3 forward = A_OD * Vec3{{0, 1, 0}};
    out.A_O_norot = frames::A_RL_from_forward_upward(forward, upward);
    const Mat3& A_ON = out.A_O_norot;
    const Mat3 A_NO = frames::transposed(A_ON);

    // ---- air velocity at the hub, and of every blade element
    const Vec3 v_hub_O = in.body.velocity_O - in.wind_O + A_OL * qvm::cross(in.body.omega_L, geometry.position_L);  // relative to the air: minus the wind
    const Vec3 v_hub_R = frames::transposed(A_OR) * v_hub_O;
    out.horizontal_speed = std::sqrt(v_hub_R.a[0] * v_hub_R.a[0] + v_hub_R.a[2] * v_hub_R.a[2]);
    const Real hub_speed = qvm::mag(v_hub_O);

    const Vec3 omega_disc_O = A_OD * Vec3{{wx_disc, in.omega_shaft, wz_disc}};
    const Vec3 omega_disc_N = A_NO * omega_disc_O;

    for (std::size_t r = 0; r < kBemtRadialCount; ++r) {
        for (std::size_t c = 0; c < kBemtAzimuthCount; ++c) {
            const std::size_t i = bemt_index(r, c);
            const Mat3 Ry = frames::rot_y(grid.psi[i]);
            const Vec3 r_blade_N = Ry * Vec3{{grid.radius[i], 0, 0}};
            const Vec3 v_element_O = v_hub_O + A_ON * qvm::cross(omega_disc_N, r_blade_N);
            const Vec3 v_element_B = frames::transposed(Ry) * (A_NO * v_element_O);  // blade element frame: x radial, y axial, z tangential
            out.Vc[i] = v_element_B.a[1];
            out.V2[i] = v_element_B.a[2];
            // at (very) low rotor speed the pitch is replaced by a fixed value for numerical robustness
            out.beta[i] = smooth_step(std::abs(in.omega_shaft), cfg.low_rpm_start, cfg.low_rpm_pitch, cfg.low_rpm_end, in.blade_pitch[c]);
        }
    }

    // ---- vortex ring state: global Johnson inflow of the descending disc
    Real Vc_mean = 0;
    int count_positive = 0;
    int count_negative = 0;
    for (std::size_t i = 0; i < kBemtElementCount; ++i) {
        Vc_mean += out.Vc[i];
        constexpr Real deadband = 0.05;  // [m/s] near zero axial velocities do not flip the descent coherence
        if (out.Vc[i] > deadband) {
            ++count_positive;
        }
        else if (out.Vc[i] < -deadband) {
            ++count_negative;
        }
    }
    Vc_mean /= static_cast<Real>(kBemtElementCount);
    const Real consistency = static_cast<Real>(std::max(count_positive, count_negative)) / static_cast<Real>(kBemtElementCount);
    out.use_vrs = smooth01(inverse_lerp(0.70, 0.90, consistency));

    Real beta_mean = 0;
    for (const Real b : in.blade_pitch) {
        beta_mean += b;
    }
    beta_mean /= static_cast<Real>(kBemtAzimuthCount);
    Real polarity = unity_sign(beta_mean);  // +1 normal, -1 inverted collective
    if (beta_mean == 0 && Vc_mean != 0) {
        polarity = -unity_sign(Vc_mean);    // zero collective has no thrust direction: take the one that makes the flow a descent
    }

    // The hover induced velocity of the weight is a stable, non recursive reference for the vortex ring window.
    const Real v_h = hover_induced_velocity(in.mass_total * cfg.gravity, rotor.R, in.rho_air);
    out.hover_induced_velocity = v_h;
    out.Vz_scaled = v_h > 0.1 ? polarity * Vc_mean / v_h : 0;
    out.Vx_scaled = v_h > 0.1 ? out.horizontal_speed / v_h : 0;
    const Real v_quasi_static = johnson_table3(out.Vz_scaled, out.Vx_scaled, cfg.vrs, out.strength_vrs);
    out.v_johnson_target = polarity * v_quasi_static * v_h;
    out.v_johnson_lagged = (cfg.quasi_static || !state.lag_initialized) ? out.v_johnson_target : state.v_johnson_lagged;

    // The Johnson inflow is blended in smoothly over the whole descent (none at Vz/vh = 0, full at Vz/vh <= -2), in flight
    // gated by how coherent the descent is over the disc.
    out.descent_blend_weight = smooth_step(out.Vz_scaled, -2, 1, 0, 0) * (cfg.quasi_static ? Real{1} : out.use_vrs);

    // ---- blade elements
    const Real chord = rotor.c;
    const int blades = rotor.b;
    const Real factor1 = Real{0.5} * in.rho_air * static_cast<Real>(blades) * chord / static_cast<Real>(kBemtAzimuthCount);
    const Real segment = grid.segment_length * 2;  // radial width of a ring
    const Real dPsi = grid.sector_angle;
    const bool use_lag = cfg.dynamic_inflow && !cfg.quasi_static && state.lag_initialized;

    Vec3 force_O{{0, 0, 0}};
    Vec3 torque_D{{0, 0, 0}};
    Real Vi_area_sum = 0;
    Real area_sum = 0;
    Real max_residual = 0;
    int nonconverged = 0;

    for (std::size_t r = 0; r < kBemtRadialCount; ++r) {
        const Real radius = grid.radius[bemt_index(r, 0)];
        const Real factor2 = (kPi * 4) * radius * in.rho_air / static_cast<Real>(kBemtAzimuthCount);
        for (std::size_t c = 0; c < kBemtAzimuthCount; ++c) {
            const std::size_t i = bemt_index(r, c);

            // Newton start: the last raw solution of this element; only a cold start uses the stateless Johnson bridge
            Real Vi_start = state.Vi_raw_previous[i];
            if (Vi_start == 0) {
                const Real local_Vz = v_h > 0.1 ? polarity * out.Vc[i] / v_h : 0;
                const Real local_Vx = v_h > 0.1 ? out.horizontal_speed / v_h : 0;
                Real unused = 0;
                const Real bridge = johnson_baseline(local_Vz, local_Vx, cfg.vrs) + cfg.vrs.zeta * cfg.vrs.f * johnson_vrs_correction(local_Vz, local_Vx, cfg.vrs, unused);
                Vi_start = polarity * bridge * v_h;
            }
            const ElementSolution solution = solve_element(cfg, Vi_start, out.Vc[i], out.V2[i], out.beta[i], factor1, factor2);
            out.Vi_raw[i] = solution.Vi;
            out.residual[i] = solution.residual;
            out.iterations[i] = static_cast<std::uint8_t>(std::min(solution.iterations, 255));
            if (std::isfinite(solution.residual)) {
                max_residual = std::max(max_residual, solution.residual);
                nonconverged += solution.residual > 0.001 ? 1 : 0;
            }

            // elements the solver could not converge fall back to the Johnson inflow, in descent it is blended in
            const Real fallback_weight = inverse_lerp(0.5, 2.0, solution.residual);
            out.vrs_weight[i] = cfg.vortex_ring_state ? clamp01(out.descent_blend_weight + fallback_weight) : Real{0};
            out.Vi_target[i] = lerp(out.Vi_raw[i], out.v_johnson_lagged, out.vrs_weight[i]);
            out.Vi[i] = use_lag ? state.Vi_lag[i] : out.Vi_target[i];

            // blade forces with the induced velocity that is actually in the flow
            const ElementPhysics e = element_function(cfg, out.Vi[i], out.Vc[i], out.V2[i], out.beta[i], factor1, factor2);
            out.phi[i] = e.phi;
            out.alpha[i] = e.alpha;
            out.cl[i] = e.cl;
            out.cd[i] = e.cd;
            out.V0[i] = out.Vi[i] + out.Vc[i];
            const Real Vlocal_squared = out.V0[i] * out.V0[i] + out.V2[i] * out.V2[i];
            out.dL[i] = factor1 * Vlocal_squared * e.cl;
            out.dD[i] = factor1 * Vlocal_squared * e.cd;
            out.dT[i] = factor1 * Vlocal_squared * (e.cl * std::cos(e.phi) - e.cd * std::sin(e.phi)) * cfg.tip_loss;
            out.dQ[i] = factor1 * Vlocal_squared * (e.cd * std::cos(e.phi) + e.cl * std::sin(e.phi));

            // sum up thrust (world frame) and the moment about the hub (disc frame)
            const Mat3 Ry = frames::rot_y(grid.psi[i]);
            const Vec3 F_blade{{0, out.dT[i] * segment, out.dQ[i] * segment}};
            force_O = force_O + A_ON * (Ry * F_blade);
            torque_D = torque_D + A_DO * (A_ON * (Ry * qvm::cross(Vec3{{radius, 0, 0}}, F_blade)));

            const Real dA = radius * segment * dPsi;
            Vi_area_sum += out.Vi[i] * dA;
            area_sum += dA;
        }
    }
    out.Vi_mean = Vi_area_sum / area_sum;
    out.max_residual = max_residual;
    out.nonconverged_elements = nonconverged;

    // ---- ground effect increases the thrust, not the torque
    out.ground_effect_factor = 1;
    if (cfg.ground_effect) {
        const Vec3 downwash_direction_O = forward * sign0(-out.Vi_mean);
        out.ground_effect_factor = ground_effect_factor(rotor.R, in.ground, downwash_direction_O, out.horizontal_speed, out.Vi_mean);
        force_O = force_O * out.ground_effect_factor;
    }

    out.force_O = force_O;
    out.force_D = A_DO * force_O;
    out.torque_D = torque_D;
    out.thrust = qvm::mag(force_O);
    out.shaft_torque = torque_D.a[1];
    out.turbulence_strength = turbulence_strength(cfg.turbulence_config, in.body.omega_L, hub_speed, cfg.vortex_ring_state ? out.strength_vrs * out.use_vrs : Real{0});
    return out;
}

void bemt_post_step(const BemtConfig& cfg, BemtFilterState& state, const BemtInputs& in, const BemtOutputs& out, const Real time, const Real dt, RotorRng& rng) noexcept
{
    // Newton warm start
    state.Vi_raw_previous = out.Vi_raw;
    state.wake_velocity = out.Vi_mean;

    // Pitt-Peters apparent mass: the induced velocity of every element follows its target with the time constant of the
    // local flow; implicit Euler is unconditionally stable
    if (!cfg.dynamic_inflow || cfg.quasi_static || !state.lag_initialized) {
        state.Vi_lag = out.Vi_target;
    }
    else {
        for (std::size_t i = 0; i < kBemtElementCount; ++i) {
            const Real tau = pitt_peters_time_constant(cfg.wake_inertia_fumble_factor, cfg.rotor.R, out.V2[i], state.Vi_lag[i], out.Vc[i]);
            state.Vi_lag[i] = pitt_peters_step(state.Vi_lag[i], out.Vi_target[i], dt, tau);
        }
    }

    // time lag of the vortex ring development
    if (cfg.quasi_static || !state.lag_initialized) {
        state.v_johnson_lagged = out.v_johnson_target;
        state.v_johnson_dv_lag = 0;
    }
    else {
        Real lagged = state.v_johnson_lagged;
        johnson_time_lag(out.v_johnson_target, state.v_johnson_lagged, in.omega_shaft, cfg.vrs.tau_rotor_revolutions, dt, state.v_johnson_dv_lag, lagged);
        state.v_johnson_lagged = lagged;
    }
    state.lag_initialized = true;

    // turbulence of the wake
    if (cfg.turbulence) {
        turbulence_post_step(cfg.turbulence_config, state.turbulence, out.turbulence_strength, time, dt, rng);
    }
    else {
        state.turbulence.elapsed_old = 0;
    }
}

std::string bemt_elements_csv(const BemtGrid& grid, const BemtOutputs& out)
{
    std::string csv = "r_index,psi_index,r,psi,beta,Vc,V2,Vi_raw,Vi_target,Vi,alpha,cl,cd,dL,dD,dT,dQ,vrs_weight,residual,iterations\n";
    char row[512];
    for (std::size_t r = 0; r < kBemtRadialCount; ++r) {
        for (std::size_t c = 0; c < kBemtAzimuthCount; ++c) {
            const std::size_t i = bemt_index(r, c);
            std::snprintf(row, sizeof row, "%zu,%zu,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%.9g,%d\n", r, c, grid.radius[i],
                          grid.psi[i], out.beta[i], out.Vc[i], out.V2[i], out.Vi_raw[i], out.Vi_target[i], out.Vi[i], out.alpha[i], out.cl[i], out.cd[i],
                          out.dL[i], out.dD[i], out.dT[i], out.dQ[i], out.vrs_weight[i], out.residual[i], static_cast<int>(out.iterations[i]));
            csv += row;
        }
    }
    return csv;
}

RotorDiscStateVector rotor_disc_derivative(const BemtConfig& cfg, const RotorDiscStateVector& x, const Real omega_shaft, const BemtOutputs& out) noexcept
{
    const Vec3 omega{{x[disc::wx], omega_shaft, x[disc::wz]}};
    const Quat dq = frames::quat_derivative(disc_orientation(x), omega);
    const Vec3 moment = out.torque_D + out.torque_hub_on_disc_D;
    const Real Jx = cfg.disc_inertia_tilt;
    const Real Jy = cfg.disc_inertia_spin;
    const Real Jz = cfg.disc_inertia_tilt;
    return RotorDiscStateVector{dq.a[0], dq.a[1], dq.a[2], dq.a[3],
                                (moment.a[0] + omega.a[1] * omega.a[2] * (Jy - Jz)) / Jx,
                                (moment.a[2] + omega.a[0] * omega.a[1] * (Jx - Jy)) / Jz};
}

BemtFuselageLoads bemt_loads_on_fuselage(const BemtConfig& cfg, const RotorGeometry& geometry, const BodyKinematics& body, const BemtFilterState& state,
                                         const BemtOutputs& out, const bool freewheeling) noexcept
{
    BemtFuselageLoads loads;
    const Vec3 force_L = frames::transposed(body.A_OL) * out.force_O;
    loads.force_O = out.force_O;
    loads.torque_L = out.torque_hub_on_body_L + qvm::cross(geometry.position_L, force_L);  // the thrust acts at the hub
    if (!freewheeling) {
        loads.torque_L = loads.torque_L + geometry.A_LR * Vec3{{0, out.shaft_torque, 0}};
    }
    if (cfg.turbulence) {
        loads.force_O = loads.force_O + body.A_OL * state.turbulence.force_L;
        loads.torque_L = loads.torque_L + state.turbulence.torque_L;
    }
    return loads;
}

}  // namespace hoverhere::aero
