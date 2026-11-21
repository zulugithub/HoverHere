#pragma once

#include <array>
#include <cstddef>
#include <cstdint>
#include <span>
#include <vector>

#include "hoverhere/core/frames.hpp"
#include "hoverhere/core/math.hpp"
#include "hoverhere/params/model_params.hpp"

// Ground contact of the landing gear, skids and the fuselage (port of the collision part of Helicopter_ODE.cs and Common.Helper).
//
//   GroundMesh       triangle soup of the ground in the right handed world frame. Built once (allocates), afterwards queried without any heap
//                    allocation. A uniform hash grid over the xz plane (CSR layout) limits the triangles that are tested per collision point.
//   GroundCollision  force model: spring-damper normal force along the triangle normal, sliding/stiction friction with an anchor point,
//                    wheel brake, bump noise, gear raising. evaluate() is a pure function of the state (RK4 stage safe); everything that has
//                    memory (stiction anchors, brake, wheel rolling, crash latch) moves once per step in post_step().
//
// Contact test of a point P with the triangle (v0, v1, v2): P has to project along the triangle normal into the triangle ("region 0" of
// the point-triangle distance of Eberly). The signed distance is the distance along the normal, negative below the triangle. A point below
// a triangle is in contact at any depth (the penetration is clamped to 1 m in the force law). A second probe point a few cm away closes
// the gaps of a non convex mesh. Per point the nearest of several overlapping triangles wins (the C# code takes the first of the mesh).
namespace hoverhere::collision {

using math::Mat3;
using math::Vec3;

inline constexpr std::size_t kMaxContactPoints = 56;     // 6 lists of 8 points and the two rotor hubs need 50
inline constexpr std::size_t kGearGroups = 5;            // left, right, steering center, steering left, steering right
inline constexpr Real kNoGroundDistance = 1000;          // [m] distance reported when no ground triangle is in reach

// Same numbers as hh_contact_type in sim_types.h.
enum class ContactType : std::int32_t {
    usual = 0,
    gear_left = 1,
    gear_right = 2,
    steering_center = 3,
    steering_left = 4,
    steering_right = 5,
    mainrotor_hub = 6,   // distance only
    tailrotor_hub = 7
};

// Index of the landing gear group of a contact type, -1 for the other types.
[[nodiscard]] constexpr int gear_group_of(const ContactType type) noexcept
{
    const auto t = static_cast<int>(type);
    return (t >= 1 && t <= 5) ? t - 1 : -1;
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// ground mesh
// ---------------------------------------------------------------------------------------------------------------------------------------
struct MeshOptions {
    Real cell_size = 0;          // [m] hash grid cell, 0: chosen from the size of the triangles
    bool flip_winding = false;   // swap two vertices of every triangle (mesh given in a left handed system)
};

struct PointProbe {
    bool in_contact = false;                // signed distance < 0 against one triangle
    Real distance = kNoGroundDistance;      // [m] signed distance along the normal; kNoGroundDistance: no triangle in reach
    Vec3 normal{{0, 1, 0}};                 // unit normal of the triangle
    std::int32_t triangle = -1;
};

class GroundMesh {
public:
    // Builds the triangle data and the grid. `vertices` holds x, y, z of every vertex, `indices` three vertex numbers per triangle.
    // Degenerate triangles are dropped. Returns false (and leaves the mesh empty) for malformed input. Allocates: not for the real time
    // path.
    bool build(std::span<const double> vertices, std::span<const std::uint32_t> indices, const MeshOptions& options = {});
    bool build(std::span<const float> vertices, std::span<const std::uint32_t> indices, const MeshOptions& options = {});
    void clear() noexcept;

    [[nodiscard]] bool empty() const noexcept { return triangles_.empty(); }
    [[nodiscard]] std::size_t triangle_count() const noexcept { return triangles_.size(); }
    [[nodiscard]] std::size_t dropped_triangle_count() const noexcept { return dropped_; }
    [[nodiscard]] Real cell_size() const noexcept { return cell_; }
    [[nodiscard]] std::size_t grid_cells() const noexcept { return static_cast<std::size_t>(nx_) * static_cast<std::size_t>(nz_); }
    [[nodiscard]] Vec3 bounds_min() const noexcept { return Vec3{{bmin_[0], bmin_[1], bmin_[2]}}; }
    [[nodiscard]] Vec3 bounds_max() const noexcept { return Vec3{{bmax_[0], bmax_[1], bmax_[2]}}; }

    // Contact search for the point `p`. Only triangles whose bounding box meets the box p +- reach are tested; `probe_offset` is the
    // position of the second probe point relative to p. Real time safe (no allocation, no exception).
    [[nodiscard]] PointProbe probe(const Vec3& p, const Vec3& probe_offset, Real reach) const noexcept;

    // Reference implementation without the grid (loops over all triangles), used by the tests.
    [[nodiscard]] PointProbe probe_brute_force(const Vec3& p, const Vec3& probe_offset, Real reach) const noexcept;

private:
    struct Triangle {
        double v0[3];
        double e0[3];
        double e1[3];
        double n[3];       // unit normal (e0 x e1)
        double a, b, c, det;
        double lo[3];      // bounding box
        double hi[3];
        std::int32_t cx0, cx1, cz0, cz1;  // grid cells covered (inclusive)
    };

    bool build_from_triangles(std::vector<Triangle>&& triangles, const MeshOptions& options);
    template <class Visitor>
    void for_candidates(bool use_grid, const double lo[3], const double hi[3], Visitor&& visit) const noexcept;
    [[nodiscard]] PointProbe probe_impl(const Vec3& p, const Vec3& probe_offset, Real reach, bool use_grid) const noexcept;
    [[nodiscard]] static bool signed_distance(const Triangle& t, const double p[3], double& distance) noexcept;
    [[nodiscard]] std::int32_t cell_x(double x) const noexcept;
    [[nodiscard]] std::int32_t cell_z(double z) const noexcept;

    std::vector<Triangle> triangles_;
    std::vector<std::uint32_t> cell_start_;   // nx * nz + 1 entries
    std::vector<std::uint32_t> cell_items_;
    std::vector<std::uint32_t> large_items_;  // triangles that cover too many cells, tested for every query
    static constexpr std::size_t kLargeTriangleCells = 64;  // triangles that cover more cells go to large_items_
    double bmin_[3] = {0, 0, 0};
    double bmax_[3] = {0, 0, 0};
    double cell_ = 1;
    std::int32_t nx_ = 0;
    std::int32_t nz_ = 0;
    std::size_t dropped_ = 0;
};

// ---------------------------------------------------------------------------------------------------------------------------------------
// configuration
// ---------------------------------------------------------------------------------------------------------------------------------------
struct ContactPointDefinition {
    Vec3 position_L{{0, 0, 0}};   // [m] in the body frame (lowered gear)
    ContactType type = ContactType::usual;
};

struct CollisionConfig {
    std::array<ContactPointDefinition, kMaxContactPoints> points{};
    std::size_t point_count = 0;        // including the two hubs, which are always the last two
    std::size_t force_point_count = 0;  // point_count - 2

    // landing gear groups: 0 left, 1 right, 2 steering center, 3 steering left, 4 steering right
    std::array<std::int32_t, kGearGroups> group_is_wheel{};   // 0: skid / support, 1: wheel
    std::array<Vec3, kGearGroups> raised_offset_L{};          // [m] offset of the points when the gear is raised

    Real friction_coeff = 0.5;
    Real friction_coeff_forward = 0.1;
    Real friction_coeff_sideward = 0.5;
    Real friction_coeff_steering = 0.1;
    Real stiffness = 1000;              // [N/m]
    Real damping = 10;                  // [N s/m]
    Real collision_force_max = 1e9;     // [N]

    Real V_h = 0.02;                    // [m/s] friction to stiction transition velocity
    Real U_max = 0.02;                  // [m] largest distance to the stiction anchor
    Real stiction_factor = 1.5;         // [-] static friction as multiple of the sliding friction

    Real bump_height = 0;               // [m]
    Real bump_density_scale = 1;        // [1/m]

    Real gear_time = 1;                 // [s] raising of the gear (shapes the collision offset only)
    Real gear_bay_time = 0;             // [s] closing of the bay doors, added
    Real gear_tilted_forward = 0;       // [rad] tilt of the gear mechanism when lowered

    Real wheel_brake_time = 1;          // [s] smooth transition of the brake
    Real aabb_radius = 1;               // [m] search reach of the contact points (largest point distance + 0.1 m)
    Real hub_search_range = 3;          // [m] search reach of the rotor hubs for the ground effect
    Vec3 probe_offset_O{{0.02, 0, 0}};  // [m] second probe point of the gap closing test (world frame)
};

[[nodiscard]] CollisionConfig make_collision_config(const params::si::ModelParams& p) noexcept;

// ---------------------------------------------------------------------------------------------------------------------------------------
// evaluation (pure)
// ---------------------------------------------------------------------------------------------------------------------------------------
struct ContactInputs {
    Vec3 position_O{{0, 0, 0}};       // [m] centre of mass
    Mat3 A_OL;                        // body -> world
    Vec3 velocity_O{{0, 0, 0}};       // [m/s]
    Vec3 omega_L{{0, 0, 0}};          // [rad/s]
    std::array<Real, kGearGroups> gear_raise{};  // [0, 1] raising state of the groups, as driven by the Unity animation
};

// The state memory of the model; only post_step() changes it (and reset()).
struct ContactMemory {
    std::array<Vec3, kMaxContactPoints> stiction_anchor_O{};
    Real wheel_brake_strength = 0;                 // [0, 1]
    Real wheel_brake_velocity = 0;                 // SmoothDamp state
    std::array<Real, kGearGroups> wheel_rolling_distance{};   // [m]
    std::array<Real, kGearGroups> wheel_rolling_velocity{};   // [m/s]
    std::array<Real, 3> steering_raw{{0.25, 0.25, 0.25}};     // heading of the center, left, right wheel [0, 1) per revolution before the mirror
    Real steering_center_idle_time = 0;            // [s] without contact force of the centre wheel
    std::array<Real, kGearGroups> gear_force_y{};  // [N] along the body y-axis
    bool crashed = false;                          // latched until reset
    Real max_normal_force = 0;                     // [N] largest value since reset
};

struct ContactPointResult {
    ContactType type = ContactType::usual;
    bool in_contact = false;
    Vec3 position_O{{0, 0, 0}};
    Vec3 normal_O{{0, 1, 0}};
    Real distance = kNoGroundDistance;            // [m] signed, negative below the ground
    Real penetration = 0;                         // [m] >= 0, after the bumps
    Vec3 velocity_O{{0, 0, 0}};
    Real velocity_normal = 0;                     // [m/s] along the normal
    Real velocity_forward = 0;                    // [m/s] in the projected body x-direction
    Real velocity_sideward = 0;                   // [m/s] in the projected body z-direction
    Vec3 velocity_parallel_O{{0, 0, 0}};          // [m/s] parallel to the triangle
    Vec3 normal_force_O{{0, 0, 0}};               // [N]
    Vec3 friction_force_O{{0, 0, 0}};             // [N]
    Real normal_force = 0;                        // [N] magnitude
    Real tangential_force = 0;                    // [N] magnitude
};

struct ContactEvaluation {
    std::size_t point_count = 0;
    std::array<ContactPointResult, kMaxContactPoints> points{};
    Vec3 force_O{{0, 0, 0}};                      // [N] sum of the contact and friction forces (without gravity)
    Vec3 torque_L{{0, 0, 0}};                     // [Nm] about the centre of mass
    std::array<Real, kGearGroups> gear_force_y{}; // [N] force of the group along the body y-axis
    Real hub_distance[2] = {kNoGroundDistance, kNoGroundDistance};   // [m] main, tail rotor
    Vec3 hub_normal_O[2] = {Vec3{{0, 1, 0}}, Vec3{{0, 1, 0}}};
    Real max_normal_force = 0;                    // [N]
    bool any_contact = false;
};

// Total of one step of the memory update, see GroundCollision::post_step.
struct ContactStepControls {
    Real wheel_brake_target = 0;                  // [0, 1]
    bool wheels_raised = false;                   // forces the brake fully on
    Real dt = 0.001;
};

class GroundCollision {
public:
    GroundCollision() = default;
    explicit GroundCollision(const CollisionConfig& config) : config_(config) {}

    void set_config(const CollisionConfig& config) { config_ = config; }
    [[nodiscard]] const CollisionConfig& config() const noexcept { return config_; }

    // The mesh stays owned by the caller of the C API through a copy; replacing it is not real time safe.
    [[nodiscard]] GroundMesh& mesh() noexcept { return mesh_; }
    [[nodiscard]] const GroundMesh& mesh() const noexcept { return mesh_; }

    // Contact points in the world frame, in the order of the configuration (the gear raising is applied).
    void point_positions(const ContactInputs& in, std::span<Vec3> out) const noexcept;

    // Pure function of the state and the memory: forces on the body and the per point results. No allocation.
    void evaluate(const ContactInputs& in, ContactEvaluation& out) const noexcept;

    // Updates the memory once per step from the evaluation at the start of the step: brake, stiction anchors, wheel rolling and steering,
    // crash latch and the gear forces for the animation. (The C# code does this in the first RK4 call; the anchors are one step older
    // here, 1 ms.)
    void post_step(const ContactInputs& in, const ContactEvaluation& evaluation, const ContactStepControls& controls) noexcept;

    // Zero memory; the stiction anchors are placed on the contact points, the brake and the wheels start at rest.
    void reset(const ContactInputs& in) noexcept;

    [[nodiscard]] const ContactMemory& memory() const noexcept { return memory_; }

    // Heading of the wheels for the animation [0, 1): center, left (mirrored), right.
    [[nodiscard]] std::array<Real, 3> wheel_steering() const noexcept;

    // Collision offset factor [0, 1] of a gear group from its raising state (Landing_Gear_Collision_Point_Transition).
    [[nodiscard]] Real gear_offset_factor(Real raise) const noexcept;

private:
    CollisionConfig config_{};
    GroundMesh mesh_;
    ContactMemory memory_{};
};

}  // namespace hoverhere::collision
