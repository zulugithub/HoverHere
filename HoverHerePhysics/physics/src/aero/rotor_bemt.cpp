#include "hoverhere/aero/rotor_bemt.hpp"

#include <algorithm>
#include <cmath>
#include <limits>
#include <cstdio>
#include <fstream>
#include <numbers>
#include <stdexcept>
#include <string>

#include <pugixml.hpp>
#include <sstream>

#if defined(_WIN32)
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#endif

#include "hoverhere/numerics/smooth_step.hpp"

namespace hoverhere::aero {

namespace qvm = boost::qvm;
using numerics::sign0;
using numerics::smooth_step;

namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;

#if defined(_WIN32)
char kModuleLocationAnchor = 0;

std::filesystem::path module_directory()
{
    HMODULE module = nullptr;
    const DWORD flags = GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT;
    if (!GetModuleHandleExW(flags, reinterpret_cast<LPCWSTR>(&kModuleLocationAnchor), &module)) {
        return {};
    }
    std::array<wchar_t, 32768> path{};
    const DWORD length = GetModuleFileNameW(module, path.data(), static_cast<DWORD>(path.size()));
    if (length == 0 || length >= path.size()) {
        return {};
    }
    return std::filesystem::path(std::wstring(path.data(), length)).parent_path();
}
#endif

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

Real first_order_lag_step(const Real x, const Real target, const Real dt, const Real tau) noexcept
{
    if (!(tau > 0)) {
        return target;
    }
    const Real a = dt / tau;
    return (x + a * target) / (1 + a);
}

Real johnson_time_lag(const Real v_target, const Real v_previous, const Real omega_mr, const Real tau_rotor_revolutions, const Real dt) noexcept
{
    if (std::abs(omega_mr) < 0.1) {
        return v_target;
    }
    const Real tau = std::clamp(tau_rotor_revolutions * (2 * kPi) / std::abs(omega_mr), Real{0.05}, Real{5});
    return first_order_lag_step(v_previous, v_target, dt, tau);
}

Real endpoint_slope(const Real d0, const Real d1) noexcept
{
    Real slope = (Real{3} * d0 - d1) / 2;
    if ((d0 > 0 && slope < 0) || (d0 < 0 && slope > 0)) {
        return 0;
    }
    if (((d0 > 0) != (d1 > 0)) && std::abs(slope) > Real{3} * std::abs(d0)) {
        slope = Real{3} * d0;
    }
    return slope;
}

template <std::size_t N>
Real monotone_slope(const std::array<Real, N>& y, const std::size_t i) noexcept
{
    if (i == 0) {
        return endpoint_slope(y[1] - y[0], y[2] - y[1]);
    }
    if (i == N - 1) {
        return endpoint_slope(y[N - 1] - y[N - 2], y[N - 2] - y[N - 3]);
    }
    const Real left = y[i] - y[i - 1];
    const Real right = y[i + 1] - y[i];
    if (left * right <= 0) {
        return 0;
    }
    return (2 * left * right) / (left + right);
}

template <std::size_t N>
Real eval_monotone_cubic_uniform(const std::array<Real, N>& y, const Real pos) noexcept
{
    if (pos <= Real{0}) {
        return y[0];
    }
    if (pos >= static_cast<Real>(N - 1)) {
        return y[N - 1];
    }
    const std::size_t i = static_cast<std::size_t>(pos);
    const Real t = pos - static_cast<Real>(i);
    const Real m0 = monotone_slope(y, i);
    const Real m1 = monotone_slope(y, i + 1);
    const Real t2 = t * t;
    const Real t3 = t2 * t;
    return (2 * t3 - 3 * t2 + 1) * y[i] + (t3 - 2 * t2 + t) * m0 +
           (-2 * t3 + 3 * t2) * y[i + 1] + (t3 - t2) * m1;
}

static void post_stall_coefficients(const PostStallModel& model, const Real alpha, Real& cl, Real& cd) noexcept
{
    const bool positive_alpha = alpha >= 0;
    const Real side_alpha = std::abs(alpha);
    const Real cl_peak_deg = positive_alpha ? model.cl_positive_peak_deg : model.cl_negative_peak_deg;
    const Real cl_amplitude = positive_alpha ? model.cl_positive_amplitude : model.cl_negative_amplitude;
    const Real cd_first_min_deg = positive_alpha ? model.cd_positive_first_min_deg : model.cd_negative_first_min_deg;
    const Real cd_amplitude = positive_alpha ? model.cd_positive_amplitude_or_offset : model.cd_negative_amplitude_or_offset;
    cl = (positive_alpha ? Real{1} : Real{-1}) *
         std::sin((Real{3} / 2) / (1 - (cl_peak_deg / 180)) * (side_alpha - kPi)) * cl_amplitude;
    cd = -std::cos((Real{4} / 2) / (1 - (cd_first_min_deg / 180)) * (side_alpha - kPi)) * cd_amplitude + cd_amplitude;
}

AirfoilSlice make_airfoil_slice(const AirfoilPolarTable& table, const Real re, const Real ncrit) noexcept
{
    std::size_t r0, n0;
    Real tr, tn;
    find_grid_bracket(table.re_grid, re, r0, tr);
    find_grid_bracket(table.ncrit_grid, ncrit, n0, tn);
    const std::size_t r1 = r0 + 1;
    const std::size_t n1 = n0 + 1;
    const Real w00 = (1 - tr) * (1 - tn);
    const Real w10 = tr * (1 - tn);
    const Real w01 = (1 - tr) * tn;
    const Real w11 = tr * tn;

    AirfoilSlice slice{};
    for (std::size_t a = 0; a < AirfoilPolarTable::kAlphaCount; ++a) {
        const std::size_t i00 = AirfoilPolarTable::index(r0, n0, a);
        const std::size_t i10 = AirfoilPolarTable::index(r1, n0, a);
        const std::size_t i01 = AirfoilPolarTable::index(r0, n1, a);
        const std::size_t i11 = AirfoilPolarTable::index(r1, n1, a);
        slice.cl[a] = w00 * table.cl_table[i00] + w10 * table.cl_table[i10] + w01 * table.cl_table[i01] + w11 * table.cl_table[i11];
        slice.cd[a] = w00 * table.cd_table[i00] + w10 * table.cd_table[i10] + w01 * table.cd_table[i01] + w11 * table.cd_table[i11];
    }
    return slice;
}

void evaluate_airfoil_polar(const PostStallModel& post_stall, const AirfoilSlice& slice, const Real alpha,
                            Real& cl, Real& cd) noexcept
{
    if (alpha <= AirfoilPolarTable::kAlphaMinRad || alpha >= AirfoilPolarTable::kAlphaMaxRad) {
        post_stall_coefficients(post_stall, alpha, cl, cd);
        return;
    }
    const Real pos = std::clamp((alpha - AirfoilPolarTable::kAlphaMinRad) * AirfoilPolarTable::kInvAlphaStep, Real{0},
                                static_cast<Real>(AirfoilPolarTable::kAlphaCount - 1));
    cl = eval_monotone_cubic_uniform(slice.cl, pos);
    cd = eval_monotone_cubic_uniform(slice.cd, pos);
}

bool load_airfoil_polar_xml(const std::filesystem::path& file, AirfoilPolarTable& table, PostStallModel& post_stall) noexcept
{
    pugi::xml_document doc;
    if (doc.load_file(file.c_str()).status != pugi::status_ok) {
        return false;
    }
    AirfoilPolarTable loaded{};
    const pugi::xml_node root = doc.document_element();
    const pugi::xml_node re_node = root.child("ReGrid");
    const pugi::xml_node ncrit_node = root.child("NcritGrid");
    if (!re_node || !ncrit_node) {
        return false;
    }
    const pugi::xml_node post_stall_node = root.child("PostStall");
    if (!post_stall_node) {
        return false;
    }

    PostStallModel loaded_post_stall{};
    const auto read_post_stall_value = [&post_stall_node](const char* name, Real& value) {
        const pugi::xml_attribute attribute = post_stall_node.attribute(name);
        if (!attribute) {
            return false;
        }
        value = attribute.as_float();
        return std::isfinite(value);
    };
    if (!read_post_stall_value("cl_positive_peak_deg", loaded_post_stall.cl_positive_peak_deg) ||
        !read_post_stall_value("cl_negative_peak_deg", loaded_post_stall.cl_negative_peak_deg) ||
        !read_post_stall_value("cl_positive_amplitude", loaded_post_stall.cl_positive_amplitude) ||
        !read_post_stall_value("cl_negative_amplitude", loaded_post_stall.cl_negative_amplitude) ||
        !read_post_stall_value("cd_positive_first_min_deg", loaded_post_stall.cd_positive_first_min_deg) ||
        !read_post_stall_value("cd_negative_first_min_deg", loaded_post_stall.cd_negative_first_min_deg) ||
        !read_post_stall_value("cd_positive_amplitude_or_offset", loaded_post_stall.cd_positive_amplitude_or_offset) ||
        !read_post_stall_value("cd_negative_amplitude_or_offset", loaded_post_stall.cd_negative_amplitude_or_offset) ||
        loaded_post_stall.cl_positive_peak_deg <= 0 || loaded_post_stall.cl_positive_peak_deg >= 180 ||
        loaded_post_stall.cl_negative_peak_deg <= 0 || loaded_post_stall.cl_negative_peak_deg >= 180 ||
        loaded_post_stall.cl_positive_amplitude <= 0 || loaded_post_stall.cl_negative_amplitude <= 0 ||
        loaded_post_stall.cd_positive_amplitude_or_offset <= 0 || loaded_post_stall.cd_negative_amplitude_or_offset <= 0) {
        return false;
    }

    std::istringstream re_stream(re_node.child_value());
    std::istringstream ncrit_stream(ncrit_node.child_value());
    for (Real& re : loaded.re_grid) {
        if (!(re_stream >> re)) {
            return false;
        }
    }
    for (Real& ncrit : loaded.ncrit_grid) {
        if (!(ncrit_stream >> ncrit)) {
            return false;
        }
    }

    std::array<bool, AirfoilPolarTable::kPolarCount> loaded_slices{};
    for (const pugi::xml_node slice : root.child("Slices").children("Slice")) {
        const Real re = slice.attribute("re").as_float(0);
        const Real ncrit = slice.attribute("ncrit").as_float(0);
        std::size_t r = 0, n = 0;
        while (r < loaded.re_grid.size() && loaded.re_grid[r] != re) ++r;
        while (n < loaded.ncrit_grid.size() && loaded.ncrit_grid[n] != ncrit) ++n;
        if (r == loaded.re_grid.size() || n == loaded.ncrit_grid.size()) {
            continue;
        }
        const std::size_t polar = AirfoilPolarTable::polar_index(r, n);
        if (loaded_slices[polar]) {
            return false;
        }
        const Real alpha_min = slice.attribute("alpha_min_deg").as_float(AirfoilPolarTable::kAlphaMinDeg);
        const Real alpha_max = slice.attribute("alpha_max_deg").as_float(AirfoilPolarTable::kAlphaMaxDeg);
        if (!(alpha_min < 0 && alpha_max > 0) || alpha_min < AirfoilPolarTable::kAlphaMinDeg ||
            alpha_max > AirfoilPolarTable::kAlphaMaxDeg || !std::isfinite(alpha_min) || !std::isfinite(alpha_max)) {
            return false;
        }
        loaded.alpha_min_deg[polar] = alpha_min;
        loaded.alpha_max_deg[polar] = alpha_max;
        std::istringstream cl_stream(slice.child("Cl").child_value());
        std::istringstream cd_stream(slice.child("Cd").child_value());
        for (std::size_t a = 0; a < AirfoilPolarTable::kAlphaCount; ++a) {
            Real cl, cd;
            if (!(cl_stream >> cl) || !(cd_stream >> cd)) {
                return false;
            }
            loaded.cl_table[AirfoilPolarTable::index(r, n, a)] = cl;
            loaded.cd_table[AirfoilPolarTable::index(r, n, a)] = cd;
        }
        loaded_slices[polar] = true;
    }
    if (!std::all_of(loaded_slices.begin(), loaded_slices.end(), [](const bool loaded_slice) { return loaded_slice; })) {
        return false;
    }

    for (std::size_t r = 0; r < AirfoilPolarTable::kReCount; ++r) {
        for (std::size_t n = 0; n < AirfoilPolarTable::kNcritCount; ++n) {
            const std::size_t polar = AirfoilPolarTable::polar_index(r, n);
            const Real alpha_max_end = std::min(loaded.alpha_max_deg[polar], Real{21.5});
            const Real alpha_max_start = std::min(Real{16}, alpha_max_end - Real{3});
            const Real alpha_min_end = std::max(loaded.alpha_min_deg[polar], Real{-21.5});
            const Real alpha_min_start = std::max(Real{-16}, alpha_min_end + Real{3});
            for (std::size_t a = 0; a < AirfoilPolarTable::kAlphaCount; ++a) {
                const Real alpha_deg = AirfoilPolarTable::kAlphaMinDeg + static_cast<Real>(a) * AirfoilPolarTable::kAlphaStepDeg;
                const Real alpha = alpha_deg * kPi / Real{180};
                const Real lut_weight = alpha_deg >= 0
                                            ? smooth_step(alpha_deg, alpha_max_start, 1, alpha_max_end, 0)
                                            : smooth_step(-alpha_deg, -alpha_min_start, 1, -alpha_min_end, 0);
                Real cl_post, cd_post;
                post_stall_coefficients(loaded_post_stall, alpha, cl_post, cd_post);
                const std::size_t i = AirfoilPolarTable::index(r, n, a);
                loaded.cl_table[i] = lut_weight * loaded.cl_table[i] + (1 - lut_weight) * cl_post;
                loaded.cd_table[i] = lut_weight * loaded.cd_table[i] + (1 - lut_weight) * cd_post;
            }
        }
    }
    table = loaded;
    post_stall = loaded_post_stall;
    return true;
}

bool load_airfoil_polar_xml(const std::filesystem::path& file, AirfoilPolarTable& table) noexcept
{
    PostStallModel post_stall{};
    return load_airfoil_polar_xml(file, table, post_stall);
}

namespace {

struct ElementPhysics {
    Real residual;  // [N] factor2 Vi |V0| - dT: momentum balance in the product form without singularity at V0 = 0
    Real cl, cd, phi, alpha;
};

ElementPhysics element_function(const BemtConfig& cfg, const AirfoilSlice& slice, const Real Vi, const Real Vc, Real V2,
                                const Real beta, const Real factor1, const Real factor2) noexcept
{
    V2 = smooth_step(std::abs(V2), 0.01, 0.01, 0.10, std::abs(V2));
    const Real V0 = Vi + Vc;
    ElementPhysics e{};
    e.phi = std::atan2(V0, V2);
    e.alpha = beta - e.phi;
    if (e.alpha >= kPi) e.alpha -= 2 * kPi;
    if (e.alpha <= -kPi) e.alpha += 2 * kPi;
    evaluate_airfoil_polar(cfg.post_stall, slice, e.alpha, e.cl, e.cd);
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
ElementSolution solve_element(const BemtConfig& cfg, const AirfoilSlice& slice, const Real Vi_start, const Real Vc, const Real V2,
                             const Real beta, const Real factor1, const Real factor2) noexcept
{
    const NewtonSettings& n = cfg.newton;
    Real Vi = Vi_start;
    Real best_Vi = Vi;
    Real best_residual = std::numeric_limits<Real>::infinity();
    int sum = 0;
    bool finished = false;
    while (!finished) {
        const Real residual = element_function(cfg, slice, Vi, Vc, V2, beta, factor1, factor2).residual;
        if (std::abs(residual) < best_residual) {
            best_residual = std::abs(residual);
            best_Vi = Vi;
        }
        if (std::abs(residual) <= n.residual_tolerance) {
            finished = true;
            break;
        }
        const Real residual_plus = element_function(cfg, slice, Vi + n.delta, Vc, V2, beta, factor1, factor2).residual;
        const Real residual_minus = element_function(cfg, slice, Vi - n.delta, Vc, V2, beta, factor1, factor2).residual;
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
    const Real final_residual = element_function(cfg, slice, Vi, Vc, V2, beta, factor1, factor2).residual;
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
BemtConfig make_bemt_config(const params::si::ModelParams& p)
{
    BemtConfig c{};
    c.rotor = as_rotor(p.helicopter.mainrotor);
    c.disc_inertia_tilt = p.helicopter.flapping.I_flapping;
    c.disc_inertia_spin = p.helicopter.mainrotor.J;
    c.hub_stiffness = p.helicopter.flapping.hub_stiffness_mr; // what about tailrotor? (Unity uses the same value for both rotors)
    c.gravity = p.scenery.gravity;
    c.wake_inertia_fumble_factor = p.tuning.rotor_tuning.mainrotor_tuning.wake_inertia_fumble_factor;
    c.vrs.tau_strength = p.tuning.rotor_tuning.mainrotor_tuning.vrs_time_constant;
    c.turbulence_config = make_turbulence_config(p.tuning.turbulence);
    const std::int32_t airfoil = p.helicopter.mainrotor.airfoil;
    if (airfoil < 0 || static_cast<std::size_t>(airfoil) >= kMainrotorAirfoilProfiles.size()) {
        throw std::invalid_argument("invalid mainrotor airfoil selection");
    }
    const std::filesystem::path polar_file{
        std::string(kMainrotorAirfoilProfiles[static_cast<std::size_t>(airfoil)].polar_filename)};
    bool loaded = false;
    const auto try_load = [&](const std::filesystem::path& candidate) {
        return load_airfoil_polar_xml(candidate, c.airfoil_polar, c.post_stall);
    };
#if defined(_WIN32)
    std::filesystem::path ancestor = module_directory();
    for (int depth = 0; depth < 6 && !ancestor.empty(); ++depth) {
        for (const std::filesystem::path& candidate : {
                 ancestor / "Assets/StreamingAssets/AirfoilPolars" / polar_file,
                 ancestor / "StreamingAssets/AirfoilPolars" / polar_file,
                 ancestor / "data/airfoil_polars" / polar_file}) {
            if (try_load(candidate)) {
                loaded = true;
                break;
            }
        }
        if (loaded) {
            break;
        }
        ancestor = ancestor.parent_path();
    }
#endif
    for (const std::filesystem::path& candidate : {
             std::filesystem::path{"data/airfoil_polars"} / polar_file,
             std::filesystem::path{"../data/airfoil_polars"} / polar_file,
             std::filesystem::path{"../../data/airfoil_polars"} / polar_file}) {
        if (loaded) {
            break;
        }
        if (try_load(candidate)) {
            loaded = true;
            break;
        }
    }
    if (!loaded) {
        throw std::runtime_error("could not load mainrotor airfoil polar '" + polar_file.string() +
                                 "' from Unity StreamingAssets or data/airfoil_polars");
    }
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

Real pitt_peters_time_constant(const Real fumble_factor, const Real rotor_radius, const Real horizontal_speed, const Real Vi_lag, const Real Vc) noexcept
{
    // The apparent mass is the air column that is pushed through the disc: its reference speed is the through flow of the disc (the
    // translational inflow in the disc plane and the axial flow Vi + Vc), not the blade speed Omega r (V2): that one is 60..80 m/s and
    // would shrink tau to about 10 ms, so that the air column could be reversed without any damping.
    const Real axial = Vi_lag + Vc;
    const Real U_local = std::max(std::sqrt(horizontal_speed * horizontal_speed + axial * axial), Real{0.5});
    return fumble_factor * (8 / (3 * kPi)) * rotor_radius / U_local;
}

Real pitt_peters_step(const Real Vi_lag, const Real Vi_target, const Real dt, const Real tau) noexcept
{
    return first_order_lag_step(Vi_lag, Vi_target, dt, tau);  // tau <= 0: no inertia, the induced velocity follows instantly
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
    const Vec3 hub_O = in.body.position_O + A_OL * geometry.position_L;
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
    // A vortex ring builds up and decays over several revolutions: what is reported and what excites the wake turbulence is the
    // PT1 lagged strength, never the instantaneous one that follows the sink rate. The blade inflow itself stays fast (Pitt-Peters).
    out.vrs_strength = (cfg.quasi_static || !state.lag_initialized || !(cfg.vrs.tau_strength > 0)) ? out.strength_vrs * out.use_vrs : state.vrs_strength;
    out.v_johnson_lagged = (cfg.quasi_static || !state.lag_initialized) ? out.v_johnson_target : state.v_johnson_lagged;
    out.turbulence_strength = turbulence_strength(cfg.turbulence_config, in.body.omega_L, hub_speed,
                                                  cfg.vortex_ring_state ? out.vrs_strength : Real{0});

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

    // The blade element equations are written for a blade that moves towards -z of the element frame (V2 < 0, rotation about +y of the
    // disc). A rotor with the opposite sense of rotation (invert_mainrotor_rotation_direction) is the mirror image: the element sees
    // the mirrored tangential velocity and the tangential (drag) force changes its direction, so the aerodynamic torque still
    // opposes the rotation. The raw V2 stays in the outputs.
    const Real rotation_sign = in.omega_shaft < 0 ? Real{-1} : Real{1};

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
            const Real V0_est = use_lag ? state.Vi_lag[i] + out.Vc[i] : out.Vi_target[i] + out.Vc[i];
            const Real V_local_est = std::sqrt(V0_est * V0_est + out.V2[i] * out.V2[i]);
            const Real re = cfg.kinematic_viscosity > 0
                                 ? (V_local_est * chord) / cfg.kinematic_viscosity
                                 : Real{200000};
            const Real ncrit = 9 - 8 * clamp01(out.turbulence_strength * Real{0.5} + out.vrs_strength);
            const AirfoilSlice slice = make_airfoil_slice(cfg.airfoil_polar, re, ncrit);
            const ElementSolution solution = solve_element(cfg, slice, Vi_start, out.Vc[i], rotation_sign * out.V2[i],
                                                           out.beta[i], factor1, factor2);
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
            const ElementPhysics e = element_function(cfg, slice, out.Vi[i], out.Vc[i], rotation_sign * out.V2[i],
                                                      out.beta[i], factor1, factor2);
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
            const Vec3 F_blade{{0, out.dT[i] * segment, rotation_sign * out.dQ[i] * segment}};
            force_O = force_O + A_ON * (Ry * F_blade);
            out.position_O[i] = hub_O + A_ON * (Ry * Vec3{{radius, 0, 0}});
            out.induced_velocity_O[i] = A_ON * (Ry * Vec3{{0, -out.Vi[i], 0}});
            out.force_thrust_O[i] = A_ON * (Ry * Vec3{{0, out.dT[i] * segment, 0}});
            out.force_torque_O[i] = A_ON * (Ry * Vec3{{0, 0, rotation_sign * out.dQ[i] * segment}});
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
            const Real tau = pitt_peters_time_constant(cfg.wake_inertia_fumble_factor, cfg.rotor.R, out.horizontal_speed, state.Vi_lag[i], out.Vc[i]);
            state.Vi_lag[i] = pitt_peters_step(state.Vi_lag[i], out.Vi_target[i], dt, tau);
        }
    }

    // time lag of the vortex ring development: the inflow of the Johnson model and the strength follow their instantaneous values with PT1 lags
    const Real vrs_instantaneous = out.strength_vrs * out.use_vrs;
    if (cfg.quasi_static || !state.lag_initialized) {
        state.v_johnson_lagged = out.v_johnson_target;
        state.vrs_strength = vrs_instantaneous;
    }
    else {
        state.v_johnson_lagged = johnson_time_lag(out.v_johnson_target, state.v_johnson_lagged, in.omega_shaft, cfg.vrs.tau_rotor_revolutions, dt);
        state.vrs_strength = clamp01(first_order_lag_step(state.vrs_strength, vrs_instantaneous, dt, cfg.vrs.tau_strength));
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
