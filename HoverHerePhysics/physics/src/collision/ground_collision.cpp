#include "hoverhere/collision/ground_collision.hpp"

#include <algorithm>
#include <cmath>
#include <limits>
#include <utility>

#include "hoverhere/numerics/perlin_noise.hpp"
#include "hoverhere/numerics/smooth_damp.hpp"
#include "hoverhere/numerics/smooth_step.hpp"

namespace hoverhere::collision {

namespace qvm = boost::qvm;
using frames::kPi;

namespace {

constexpr double kMaxGridCells = 1 << 20;

[[nodiscard]] double dot3(const double a[3], const double b[3]) noexcept
{
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
}

}  // namespace

// ---------------------------------------------------------------------------------------------------------------------------------------
// GroundMesh
// ---------------------------------------------------------------------------------------------------------------------------------------
void GroundMesh::clear() noexcept
{
    triangles_.clear();
    cell_start_.clear();
    cell_items_.clear();
    large_items_.clear();
    nx_ = nz_ = 0;
    dropped_ = 0;
    for (int k = 0; k < 3; ++k) {
        bmin_[k] = bmax_[k] = 0;
    }
}

bool GroundMesh::build(std::span<const double> vertices, std::span<const std::uint32_t> indices, const MeshOptions& options)
{
    clear();
    if (vertices.size() % 3 != 0 || indices.size() % 3 != 0 || indices.empty()) {
        return false;
    }
    const std::size_t vertex_count = vertices.size() / 3;
    for (const std::uint32_t i : indices) {
        if (i >= vertex_count) {
            return false;
        }
    }
    for (const double v : vertices) {
        if (!std::isfinite(v)) {
            return false;
        }
    }
    std::vector<Triangle> triangles;
    triangles.reserve(indices.size() / 3);
    std::size_t dropped = 0;
    for (std::size_t t = 0; t < indices.size(); t += 3) {
        const std::size_t i0 = indices[t];
        const std::size_t i1 = indices[options.flip_winding ? t + 2 : t + 1];
        const std::size_t i2 = indices[options.flip_winding ? t + 1 : t + 2];
        Triangle tri{};
        for (int k = 0; k < 3; ++k) {
            const double a = vertices[3 * i0 + static_cast<std::size_t>(k)];
            const double b = vertices[3 * i1 + static_cast<std::size_t>(k)];
            const double c = vertices[3 * i2 + static_cast<std::size_t>(k)];
            tri.v0[k] = a;
            tri.e0[k] = b - a;
            tri.e1[k] = c - a;
            tri.lo[k] = std::min({a, b, c});
            tri.hi[k] = std::max({a, b, c});
        }
        const double cx = tri.e0[1] * tri.e1[2] - tri.e0[2] * tri.e1[1];
        const double cy = tri.e0[2] * tri.e1[0] - tri.e0[0] * tri.e1[2];
        const double cz = tri.e0[0] * tri.e1[1] - tri.e0[1] * tri.e1[0];
        const double len = std::sqrt(cx * cx + cy * cy + cz * cz);
        tri.a = dot3(tri.e0, tri.e0);
        tri.b = dot3(tri.e0, tri.e1);
        tri.c = dot3(tri.e1, tri.e1);
        tri.det = tri.a * tri.c - tri.b * tri.b;
        if (!(len > 1e-12) || !(tri.det > 1e-18)) {
            ++dropped;
            continue;
        }
        tri.n[0] = cx / len;
        tri.n[1] = cy / len;
        tri.n[2] = cz / len;
        triangles.push_back(tri);
    }
    dropped_ = dropped;
    if (triangles.empty()) {
        return false;
    }
    return build_from_triangles(std::move(triangles), options);
}

bool GroundMesh::build(std::span<const float> vertices, std::span<const std::uint32_t> indices, const MeshOptions& options)
{
    std::vector<double> converted(vertices.begin(), vertices.end());
    return build(std::span<const double>(converted), indices, options);
}

bool GroundMesh::build_from_triangles(std::vector<Triangle>&& triangles, const MeshOptions& options)
{
    const std::size_t dropped = dropped_;
    triangles_ = std::move(triangles);
    dropped_ = dropped;

    for (int k = 0; k < 3; ++k) {
        bmin_[k] = std::numeric_limits<double>::max();
        bmax_[k] = std::numeric_limits<double>::lowest();
    }
    double extent_sum = 0;
    for (const Triangle& t : triangles_) {
        for (int k = 0; k < 3; ++k) {
            bmin_[k] = std::min(bmin_[k], t.lo[k]);
            bmax_[k] = std::max(bmax_[k], t.hi[k]);
        }
        extent_sum += std::max(t.hi[0] - t.lo[0], t.hi[2] - t.lo[2]);
    }
    const double mean_extent = extent_sum / static_cast<double>(triangles_.size());
    cell_ = options.cell_size > 0 ? options.cell_size : std::max(2 * mean_extent, 0.5);
    const double span_x = std::max(bmax_[0] - bmin_[0], 0.0);
    const double span_z = std::max(bmax_[2] - bmin_[2], 0.0);
    while ((std::floor(span_x / cell_) + 1) * (std::floor(span_z / cell_) + 1) > kMaxGridCells) {
        cell_ *= 1.5;
    }
    nx_ = static_cast<std::int32_t>(std::floor(span_x / cell_)) + 1;
    nz_ = static_cast<std::int32_t>(std::floor(span_z / cell_)) + 1;

    const std::size_t cells = static_cast<std::size_t>(nx_) * static_cast<std::size_t>(nz_);
    std::vector<std::uint32_t> counts(cells + 1, 0);
    large_items_.clear();
    for (std::size_t i = 0; i < triangles_.size(); ++i) {
        Triangle& t = triangles_[i];
        t.cx0 = cell_x(t.lo[0]);
        t.cx1 = cell_x(t.hi[0]);
        t.cz0 = cell_z(t.lo[2]);
        t.cz1 = cell_z(t.hi[2]);
        const std::size_t covered = static_cast<std::size_t>(t.cx1 - t.cx0 + 1) * static_cast<std::size_t>(t.cz1 - t.cz0 + 1);
        if (covered > kLargeTriangleCells) {
            large_items_.push_back(static_cast<std::uint32_t>(i));
            continue;
        }
        for (std::int32_t z = t.cz0; z <= t.cz1; ++z) {
            for (std::int32_t x = t.cx0; x <= t.cx1; ++x) {
                ++counts[static_cast<std::size_t>(z) * static_cast<std::size_t>(nx_) + static_cast<std::size_t>(x) + 1];
            }
        }
    }
    for (std::size_t c = 0; c < cells; ++c) {
        counts[c + 1] += counts[c];
    }
    cell_start_ = counts;
    cell_items_.assign(counts[cells], 0);
    std::vector<std::uint32_t> fill(cell_start_.begin(), cell_start_.end() - 1);
    for (std::size_t i = 0; i < triangles_.size(); ++i) {
        const Triangle& t = triangles_[i];
        const std::size_t covered = static_cast<std::size_t>(t.cx1 - t.cx0 + 1) * static_cast<std::size_t>(t.cz1 - t.cz0 + 1);
        if (covered > kLargeTriangleCells) {
            continue;
        }
        for (std::int32_t z = t.cz0; z <= t.cz1; ++z) {
            for (std::int32_t x = t.cx0; x <= t.cx1; ++x) {
                const std::size_t cell = static_cast<std::size_t>(z) * static_cast<std::size_t>(nx_) + static_cast<std::size_t>(x);
                cell_items_[fill[cell]++] = static_cast<std::uint32_t>(i);
            }
        }
    }
    return true;
}

std::int32_t GroundMesh::cell_x(const double x) const noexcept
{
    const double c = std::floor((x - bmin_[0]) / cell_);
    return static_cast<std::int32_t>(std::clamp(c, 0.0, static_cast<double>(nx_ - 1)));
}

std::int32_t GroundMesh::cell_z(const double z) const noexcept
{
    const double c = std::floor((z - bmin_[2]) / cell_);
    return static_cast<std::int32_t>(std::clamp(c, 0.0, static_cast<double>(nz_ - 1)));
}

// Region 0 of the point-triangle distance: the point has to project orthogonally into the triangle.
bool GroundMesh::signed_distance(const Triangle& t, const double p[3], double& distance) noexcept
{
    const double D[3] = {t.v0[0] - p[0], t.v0[1] - p[1], t.v0[2] - p[2]};  // D = B - P
    const double d = dot3(t.e0, D);
    const double e = dot3(t.e1, D);
    const double s = t.b * e - t.c * d;
    const double u = t.b * d - t.a * e;
    if (s >= 0.0 && u >= 0.0 && s + u <= t.det) {
        distance = -dot3(D, t.n);  // (P - v0) . n
        return true;
    }
    return false;
}

template <class Visitor>
void GroundMesh::for_candidates(const bool use_grid, const double lo[3], const double hi[3], Visitor&& visit) const noexcept
{
    const auto overlaps = [&](const Triangle& t) {
        return t.lo[0] <= hi[0] && t.hi[0] >= lo[0] && t.lo[1] <= hi[1] && t.hi[1] >= lo[1] && t.lo[2] <= hi[2] && t.hi[2] >= lo[2];
    };
    if (!use_grid) {
        for (std::size_t i = 0; i < triangles_.size(); ++i) {
            if (overlaps(triangles_[i])) {
                visit(static_cast<std::uint32_t>(i), triangles_[i]);
            }
        }
        return;
    }
    for (const std::uint32_t i : large_items_) {
        if (overlaps(triangles_[i])) {
            visit(i, triangles_[i]);
        }
    }
    if (hi[0] < bmin_[0] || lo[0] > bmax_[0] || hi[2] < bmin_[2] || lo[2] > bmax_[2]) {
        return;
    }
    const std::int32_t qx0 = cell_x(lo[0]);
    const std::int32_t qx1 = cell_x(hi[0]);
    const std::int32_t qz0 = cell_z(lo[2]);
    const std::int32_t qz1 = cell_z(hi[2]);
    for (std::int32_t z = qz0; z <= qz1; ++z) {
        for (std::int32_t x = qx0; x <= qx1; ++x) {
            const std::size_t cell = static_cast<std::size_t>(z) * static_cast<std::size_t>(nx_) + static_cast<std::size_t>(x);
            for (std::uint32_t k = cell_start_[cell]; k < cell_start_[cell + 1]; ++k) {
                const std::uint32_t i = cell_items_[k];
                const Triangle& t = triangles_[i];
                // A triangle that covers several cells of the query is handled in one of them only (the one at the lower corner of the
                // overlap), so no triangle is tested twice and no marker array is needed.
                if (std::max(t.cx0, qx0) != x || std::max(t.cz0, qz0) != z) {
                    continue;
                }
                if (overlaps(t)) {
                    visit(i, t);
                }
            }
        }
    }
}

PointProbe GroundMesh::probe_impl(const Vec3& p, const Vec3& probe_offset, const Real reach, const bool use_grid) const noexcept
{
    PointProbe result;
    if (triangles_.empty()) {
        return result;
    }
    const double pp[3] = {p.a[0], p.a[1], p.a[2]};
    const double qq[3] = {pp[0] + probe_offset.a[0], pp[1] + probe_offset.a[1], pp[2] + probe_offset.a[2]};
    double lo[3];
    double hi[3];
    for (int k = 0; k < 3; ++k) {
        lo[k] = std::min(pp[k], qq[k]) - reach;
        hi[k] = std::max(pp[k], qq[k]) + reach;
    }

    bool have_contact = false;
    double contact_distance = 0;
    std::uint32_t contact_index = 0;
    bool have_above = false;
    double above_distance = 0;
    std::uint32_t above_index = 0;

    for_candidates(use_grid, lo, hi, [&](const std::uint32_t index, const Triangle& t) {
        double d = 0;
        const bool main_hit = signed_distance(t, pp, d);
        if (main_hit && d < 0) {
            if (!have_contact || d > contact_distance || (d == contact_distance && index < contact_index)) {
                have_contact = true;
                contact_distance = d;
                contact_index = index;
            }
            return;
        }
        double d2 = 0;
        if (signed_distance(t, qq, d2) && d2 < 0) {  // second probe point closes the gaps between the prisms of the triangles
            if (!have_contact || d2 > contact_distance || (d2 == contact_distance && index < contact_index)) {
                have_contact = true;
                contact_distance = d2;
                contact_index = index;
            }
            return;
        }
        if (main_hit) {
            if (!have_above || d < above_distance || (d == above_distance && index < above_index)) {
                have_above = true;
                above_distance = d;
                above_index = index;
            }
        }
    });

    const auto set_normal = [&](const std::uint32_t index) {
        const Triangle& t = triangles_[index];
        result.normal = Vec3{{t.n[0], t.n[1], t.n[2]}};
        result.triangle = static_cast<std::int32_t>(index);
    };
    if (have_contact) {
        result.in_contact = true;
        result.distance = contact_distance;
        set_normal(contact_index);
    } else if (have_above) {
        result.distance = above_distance;
        set_normal(above_index);
    }
    return result;
}

PointProbe GroundMesh::probe(const Vec3& p, const Vec3& probe_offset, const Real reach) const noexcept
{
    return probe_impl(p, probe_offset, reach, true);
}

PointProbe GroundMesh::probe_brute_force(const Vec3& p, const Vec3& probe_offset, const Real reach) const noexcept
{
    return probe_impl(p, probe_offset, reach, false);
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// configuration
// ---------------------------------------------------------------------------------------------------------------------------------------
CollisionConfig make_collision_config(const params::si::ModelParams& p) noexcept
{
    const auto& heli = p.helicopter;
    const auto& col = heli.collision;
    CollisionConfig cfg;

    const auto add_list = [&cfg](const params::Vec3List<8>& list, const ContactType type) {
        const std::size_t n = static_cast<std::size_t>(std::clamp(list.count, 0, 8));
        for (std::size_t i = 0; i < n && cfg.point_count < kMaxContactPoints - 2; ++i) {
            cfg.points[cfg.point_count++] = ContactPointDefinition{list.items[i], type};
        }
    };
    add_list(col.positions_usual, ContactType::usual);
    add_list(col.positions_left, ContactType::gear_left);
    add_list(col.positions_right, ContactType::gear_right);
    add_list(col.positions_steering_center, ContactType::steering_center);
    add_list(col.positions_steering_left, ContactType::steering_left);
    add_list(col.positions_steering_right, ContactType::steering_right);
    cfg.force_point_count = cfg.point_count;
    cfg.points[cfg.point_count++] = ContactPointDefinition{heli.mainrotor.posLH, ContactType::mainrotor_hub};
    cfg.points[cfg.point_count++] = ContactPointDefinition{heli.tailrotor.posLH, ContactType::tailrotor_hub};

    cfg.group_is_wheel = {col.positions_left_type == 1, col.positions_right_type == 1, col.positions_steering_center_type == 1,
                          col.positions_steering_left_type == 1, col.positions_steering_right_type == 1};
    cfg.raised_offset_L = {col.positions_left_rised_offset, col.positions_right_rised_offset, col.positions_steering_center_rised_offset,
                           col.positions_steering_left_rised_offset, col.positions_steering_right_rised_offset};

    cfg.friction_coeff = col.friction_coeff;
    cfg.friction_coeff_forward = col.friction_coeff_forward;
    cfg.friction_coeff_sideward = col.friction_coeff_sideward;
    cfg.friction_coeff_steering = col.friction_coeff_steering;
    cfg.stiffness = col.stiffness_factor;
    cfg.damping = col.damping_factor;
    cfg.collision_force_max = col.collision_force_max;
    cfg.V_h = p.simulation.various.V_h;
    cfg.U_max = p.simulation.various.U_max;
    cfg.stiction_factor = p.simulation.various.stiction_factor;
    cfg.bump_height = p.scenery.ground.bump_height;
    cfg.bump_density_scale = p.scenery.ground.bump_density_scale;
    cfg.gear_time = heli.visual_effects.landing_gear_main_transition_time_gear;
    cfg.gear_bay_time = heli.visual_effects.landing_gear_main_transition_time_bay;
    cfg.gear_tilted_forward = heli.visual_effects.landing_gear_main_mechanism_tilted_forward;

    // search reach: the largest coordinate of a contact point in any gear position, plus 10 cm (Set_AABB_Helicopter)
    Real extreme = 0;
    for (std::size_t i = 0; i < cfg.force_point_count; ++i) {
        const int g = gear_group_of(cfg.points[i].type);
        for (int raised = 0; raised < 2; ++raised) {
            Vec3 pos = cfg.points[i].position_L;
            if (g >= 0 && raised != 0) {
                pos = pos + cfg.raised_offset_L[static_cast<std::size_t>(g)];
            }
            for (int k = 0; k < 3; ++k) {
                extreme = std::max(extreme, std::abs(pos.a[k]));
            }
        }
    }
    cfg.aabb_radius = extreme + Real{0.1};
    return cfg;
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// GroundCollision
// ---------------------------------------------------------------------------------------------------------------------------------------
Real GroundCollision::gear_offset_factor(const Real raise) const noexcept
{
    const Real t_total = config_.gear_time + config_.gear_bay_time;
    const Real share = t_total > Real{1e-9} ? config_.gear_time / t_total : Real{1};
    if (raise < share) {
        const Real status = share > 0 ? raise / share : Real{0};  // [0, 1)
        const Real angle = (kPi / 2 + config_.gear_tilted_forward) * status - config_.gear_tilted_forward;
        return 1 - std::cos(angle);
    }
    return 1;
}

void GroundCollision::point_positions(const ContactInputs& in, const std::span<Vec3> out) const noexcept
{
    const std::size_t n = std::min(config_.point_count, out.size());
    for (std::size_t i = 0; i < n; ++i) {
        Vec3 pos_L = config_.points[i].position_L;
        const int g = gear_group_of(config_.points[i].type);
        if (g >= 0) {
            const auto gi = static_cast<std::size_t>(g);
            pos_L = pos_L + config_.raised_offset_L[gi] * gear_offset_factor(in.gear_raise[gi]);
        }
        out[i] = in.position_O + in.A_OL * pos_L;
    }
}

namespace {

[[nodiscard]] Real sign_unity(const Real x) noexcept  // Mathf.Sign: +1 for zero
{
    return x >= 0 ? Real{1} : Real{-1};
}

}  // namespace

void GroundCollision::evaluate(const ContactInputs& in, ContactEvaluation& out) const noexcept
{
    const CollisionConfig& cfg = config_;
    const std::size_t n = cfg.point_count;
    out.point_count = n;
    out.force_O = Vec3{{0, 0, 0}};
    out.gear_force_y.fill(0);
    out.max_normal_force = 0;
    out.any_contact = false;
    out.hub_distance[0] = out.hub_distance[1] = kNoGroundDistance;
    out.hub_normal_O[0] = out.hub_normal_O[1] = Vec3{{0, 1, 0}};

    std::array<Vec3, kMaxContactPoints> pos;
    point_positions(in, pos);

    const Mat3 A = in.A_OL;
    const Mat3 AT = frames::transposed(A);
    const Vec3 forward_O{{A.a[0][0], A.a[1][0], A.a[2][0]}};   // body x
    const Vec3 sideward_O{{A.a[0][2], A.a[1][2], A.a[2][2]}};  // body z
    const Real brake = memory_.wheel_brake_strength;
    Vec3 torque_O{{0, 0, 0}};

    for (std::size_t i = 0; i < n; ++i) {
        ContactPointResult& r = out.points[i];
        r = ContactPointResult{};
        r.type = cfg.points[i].type;
        r.position_O = pos[i];

        const bool is_hub = i >= cfg.force_point_count;
        const Real reach = is_hub ? std::max(cfg.aabb_radius, cfg.hub_search_range) : cfg.aabb_radius;
        const PointProbe probe = mesh_.probe(pos[i], cfg.probe_offset_O, reach);
        r.in_contact = probe.in_contact;
        r.distance = probe.distance;
        r.normal_O = probe.normal;

        if (is_hub) {
            const std::size_t h = i - cfg.force_point_count;
            out.hub_distance[h] = probe.distance;
            out.hub_normal_O[h] = probe.normal;
            r.in_contact = false;  // no force on the hubs
            continue;
        }

        const Vec3 n_O = probe.normal;
        const Vec3 r_L = AT * (pos[i] - in.position_O);
        const Vec3 v_O = frames::point_velocity_R(in.velocity_O, A, in.omega_L, r_L);
        r.velocity_O = v_O;
        const Real v_n = qvm::dot(v_O, n_O);
        r.velocity_normal = v_n;
        const Vec3 v_par = v_O - n_O * v_n;
        r.velocity_parallel_O = v_par;

        // body x and z projected onto the triangle plane
        Vec3 f_hat{{0, 0, 0}};
        Vec3 s_hat{{0, 0, 0}};
        {
            const Vec3 fp = forward_O - n_O * qvm::dot(forward_O, n_O);
            const Real fl = qvm::mag(fp);
            if (fl > Real{1e-9}) {
                f_hat = fp * (1 / fl);
            }
            const Vec3 sp = sideward_O - n_O * qvm::dot(sideward_O, n_O);
            const Real sl = qvm::mag(sp);
            if (sl > Real{1e-9}) {
                s_hat = sp * (1 / sl);
            }
        }
        const Real v_f = qvm::dot(v_par, f_hat);
        const Real v_s = qvm::dot(v_par, s_hat);
        r.velocity_forward = v_f;
        r.velocity_sideward = v_s;

        if (!probe.in_contact) {
            continue;
        }
        out.any_contact = true;

        // bumps of the ground raise the surface
        Real bump = 0;
        if (cfg.bump_height > 0) {
            bump = std::clamp(cfg.bump_height * numerics::perlin_noise_2d(pos[i].a[0] * cfg.bump_density_scale, pos[i].a[2] * cfg.bump_density_scale),
                              Real{0}, cfg.bump_height);
        }
        const Real signed_penetration = std::clamp(probe.distance + bump, Real{-1}, Real{0});  // <= 0
        r.penetration = -signed_penetration;

        // spring and damper along the normal, a separating point is damped 100 times weaker
        const Real v_damp = v_n > 0 ? v_n * Real{0.01} : v_n;
        const Real normal_scalar = -cfg.stiffness * signed_penetration - cfg.damping * v_damp;
        const Vec3 F_normal = n_O * normal_scalar;
        const Real F_mag = std::abs(normal_scalar);

        // friction coefficients of this point
        Real mu_forward = Real{0.1};
        Real mu_sideward = Real{0.1};
        const int g = gear_group_of(r.type);
        const bool wheel = g >= 0 && cfg.group_is_wheel[static_cast<std::size_t>(g)] != 0;
        const auto blend = [brake](const Real rolling, const Real braking) { return (braking - rolling) * brake + rolling; };
        switch (r.type) {
        case ContactType::gear_left:
        case ContactType::gear_right:
            mu_forward = wheel ? blend(cfg.friction_coeff_forward, cfg.friction_coeff_sideward) : cfg.friction_coeff_forward;
            mu_sideward = cfg.friction_coeff_sideward;
            break;
        case ContactType::steering_center:
        case ContactType::steering_left:
        case ContactType::steering_right:
            if (wheel) {  // the steering wheel turns into the direction of travel, so both directions roll
                mu_forward = mu_sideward = blend(cfg.friction_coeff_steering, cfg.friction_coeff);
            } else {
                mu_forward = cfg.friction_coeff_forward;
                mu_sideward = cfg.friction_coeff_sideward;
            }
            break;
        case ContactType::usual:
            mu_forward = mu_sideward = cfg.friction_coeff;
            break;
        default:
            break;
        }

        // sliding friction with a smooth transition to rest and the stiction anchor model
        const Real V_h = std::max(cfg.V_h, Real{1e-9});
        const Real U_max = std::max(cfg.U_max, Real{1e-9});
        const Real abs_vf = std::abs(v_f);
        const Real abs_vs = std::abs(v_s);
        const Real beta_f = abs_vf < V_h ? std::sin((kPi / 2) * (v_f / V_h)) : Real{1};
        const Real beta_s = abs_vs < V_h ? std::sin((kPi / 2) * (v_s / V_h)) : Real{1};
        Real mu_v_f = std::abs(beta_f * mu_forward);
        Real mu_v_s = std::abs(beta_s * mu_sideward);
        mu_v_f *= numerics::smooth_step(abs_vf, V_h, cfg.stiction_factor, V_h * 2, 1);
        mu_v_s *= numerics::smooth_step(abs_vs, V_h, cfg.stiction_factor, V_h * 2, 1);
        Real mu_u_f = 0;
        Real mu_u_s = 0;
        if (!(abs_vf >= V_h || abs_vs >= V_h)) {
            const Vec3 U = memory_.stiction_anchor_O[i] - pos[i];
            const Real U_f = std::clamp(qvm::dot(U, f_hat), -U_max, U_max);
            const Real U_s = std::clamp(qvm::dot(U, s_hat), -U_max, U_max);
            mu_u_f = (1 - std::abs(beta_f)) * (mu_forward * cfg.stiction_factor) * std::sin((kPi / 2) * (U_f / U_max));
            mu_u_s = (1 - std::abs(beta_s)) * (mu_sideward * cfg.stiction_factor) * std::sin((kPi / 2) * (U_s / U_max));
        }
        const Vec3 F_friction = f_hat * ((mu_u_f - sign_unity(v_f) * mu_v_f) * F_mag) + s_hat * ((mu_u_s - sign_unity(v_s) * mu_v_s) * F_mag);

        const Vec3 F_total = F_normal + F_friction;
        r.normal_force_O = F_normal;
        r.friction_force_O = F_friction;
        r.normal_force = F_mag;
        r.tangential_force = qvm::mag(F_friction);
        out.force_O = out.force_O + F_total;
        torque_O = torque_O + qvm::cross(pos[i] - in.position_O, F_total);
        out.max_normal_force = std::max(out.max_normal_force, F_mag);
        if (g >= 0) {
            out.gear_force_y[static_cast<std::size_t>(g)] += qvm::dot(Vec3{{A.a[0][1], A.a[1][1], A.a[2][1]}}, F_total);  // body y component
        }
    }
    out.torque_L = AT * torque_O;
}

void GroundCollision::reset(const ContactInputs& in) noexcept
{
    memory_ = ContactMemory{};
    std::array<Vec3, kMaxContactPoints> pos;
    point_positions(in, pos);
    for (std::size_t i = 0; i < config_.point_count; ++i) {
        memory_.stiction_anchor_O[i] = pos[i];
    }
}

void GroundCollision::post_step(const ContactInputs& in, const ContactEvaluation& e, const ContactStepControls& controls) noexcept
{
    const CollisionConfig& cfg = config_;
    const Real dt = controls.dt;
    ContactMemory& m = memory_;

    // wheel brake: fully on when the gear is raised, smooth transition otherwise
    const Real target = controls.wheels_raised ? Real{1} : std::clamp(controls.wheel_brake_target, Real{0}, Real{1});
    m.wheel_brake_strength = numerics::smooth_damp(m.wheel_brake_strength, target, m.wheel_brake_velocity, cfg.wheel_brake_time, 1000, dt);

    const Mat3 AT = frames::transposed(in.A_OL);
    std::array<bool, kGearGroups> in_contact{};
    std::array<bool, kGearGroups> has_points{};
    std::array<Real, kGearGroups> roll_velocity{};
    const Real V_h = cfg.V_h;

    for (std::size_t i = 0; i < cfg.force_point_count; ++i) {
        const ContactPointResult& r = e.points[i];
        // the anchor of the stiction model follows the point while it moves
        if (std::abs(r.velocity_forward) >= V_h || std::abs(r.velocity_sideward) >= V_h || qvm::mag(r.velocity_O) >= V_h) {
            m.stiction_anchor_O[i] = r.position_O;
        }
        const int g = gear_group_of(r.type);
        if (g < 0) {
            continue;
        }
        const auto gi = static_cast<std::size_t>(g);
        has_points[gi] = true;
        if (!r.in_contact) {
            continue;
        }
        in_contact[gi] = true;
        const bool steering_group = g >= 2;
        roll_velocity[gi] = steering_group ? qvm::mag(r.velocity_parallel_O) : r.velocity_forward;
        if (steering_group && qvm::dot(r.velocity_parallel_O, r.velocity_parallel_O) > Real{0.005}) {
            const Vec3 vL = AT * r.velocity_parallel_O;
            const Real heading = std::fmod(std::atan2(vL.a[0], vL.a[2]) + 2 * kPi, 2 * kPi) / (2 * kPi);  // [0, 1)
            m.steering_raw[gi - 2] = heading;
        }
    }

    // rolling of the wheels; a wheel that left the ground keeps turning and is slowed down by friction, quickly with the brake
    const Real free_roll_friction = (Real{0.03} - Real{0.0006}) * m.wheel_brake_strength + Real{0.0006};
    for (std::size_t g = 0; g < kGearGroups; ++g) {
        if (!has_points[g]) {
            continue;
        }
        if (in_contact[g]) {
            m.wheel_rolling_velocity[g] = roll_velocity[g];
        } else {
            Real& v = m.wheel_rolling_velocity[g];
            if (v > free_roll_friction) {
                v -= free_roll_friction;
            }
            if (v < -free_roll_friction) {
                v += free_roll_friction;
            }
        }
        m.wheel_rolling_distance[g] += m.wheel_rolling_velocity[g] * dt;
    }

    // raised wheels turn to their initial direction, the centre wheel also after a while without ground contact
    for (std::size_t g = 2; g < kGearGroups; ++g) {
        if (in.gear_raise[g] >= Real{0.99}) {
            m.steering_raw[g - 2] = Real{0.25};
        }
    }
    if (std::abs(e.gear_force_y[2]) < Real{0.1}) {
        m.steering_center_idle_time += dt;
        if (m.steering_center_idle_time >= Real{0.5}) {
            m.steering_raw[0] = Real{0.25};
        }
    } else {
        m.steering_center_idle_time = 0;
    }

    m.gear_force_y = e.gear_force_y;
    m.max_normal_force = std::max(m.max_normal_force, e.max_normal_force);
    if (e.max_normal_force > cfg.collision_force_max) {
        m.crashed = true;
    }
}

std::array<Real, 3> GroundCollision::wheel_steering() const noexcept
{
    return {memory_.steering_raw[0], std::fmod(Real{0.5} - memory_.steering_raw[1], Real{1}), memory_.steering_raw[2]};
}

}  // namespace hoverhere::collision
