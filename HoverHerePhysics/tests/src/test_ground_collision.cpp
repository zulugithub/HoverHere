#include <algorithm>
#include <cmath>
#include <cstdint>
#include <vector>

#include <doctest/doctest.h>

#include "hoverhere/collision/ground_collision.hpp"
#include "hoverhere/core/frames.hpp"
#include "hoverhere/dynamics/rigid_body.hpp"
#include "hoverhere/numerics/perlin_noise.hpp"
#include "hoverhere/numerics/rk4.hpp"

using hoverhere::Real;
using hoverhere::math::Mat3;
using hoverhere::math::Vec3;
using namespace hoverhere::collision;
using namespace hoverhere::dynamics;
namespace frames = hoverhere::frames;
namespace num = hoverhere::numerics;
namespace qvm = boost::qvm;

namespace {

constexpr Real kGravity = 9.81;
constexpr Real kMass = 2.0;

// flat ground at y = 0 over [-10, 10]^2, normals pointing up
struct MeshData {
    std::vector<double> vertices;
    std::vector<std::uint32_t> indices;
};

MeshData flat_quad(const Real half = 10, const Real height = 0)
{
    MeshData m;
    m.vertices = {-half, height, -half, half, height, -half, half, height, half, -half, height, half};
    m.indices = {0, 3, 1, 1, 3, 2};  // (A, D, B) and (B, D, C): e0 x e1 points up
    return m;
}

// heightfield h(x, z) as a regular grid, two triangles per cell, counter clockwise seen from above
template <class Height>
MeshData heightfield(const int cells, const Real half, const Height& h)
{
    MeshData m;
    const int n = cells + 1;
    for (int j = 0; j < n; ++j) {
        for (int i = 0; i < n; ++i) {
            const Real x = -half + 2 * half * i / cells;
            const Real z = -half + 2 * half * j / cells;
            m.vertices.push_back(x);
            m.vertices.push_back(h(x, z));
            m.vertices.push_back(z);
        }
    }
    const auto id = [n](const int i, const int j) { return static_cast<std::uint32_t>(j * n + i); };
    for (int j = 0; j < cells; ++j) {
        for (int i = 0; i < cells; ++i) {
            const auto a = id(i, j);
            const auto b = id(i + 1, j);
            const auto c = id(i + 1, j + 1);
            const auto d = id(i, j + 1);
            m.indices.insert(m.indices.end(), {a, d, b, b, d, c});
        }
    }
    return m;
}

// four contact points under the body and the two hubs
CollisionConfig four_feet(const ContactType type)
{
    CollisionConfig c;
    const Real xs[4] = {0.3, -0.3, 0.3, -0.3};
    const Real zs[4] = {0.3, 0.3, -0.3, -0.3};
    for (int i = 0; i < 4; ++i) {
        c.points[c.point_count++] = ContactPointDefinition{Vec3{{xs[i], -0.1, zs[i]}}, type};
    }
    c.force_point_count = 4;
    c.points[c.point_count++] = ContactPointDefinition{Vec3{{0, 0.3, 0}}, ContactType::mainrotor_hub};
    c.points[c.point_count++] = ContactPointDefinition{Vec3{{-0.8, 0.1, 0}}, ContactType::tailrotor_hub};
    c.stiffness = 2000;
    c.damping = 20;
    c.aabb_radius = 1.0;
    return c;
}

// rigid body of 2 kg with gravity and the ground contact, advanced like the simulation does it: pure stages, memory in post_step
struct Rig {
    RigidBodyParams body = make_rigid_body_params(kMass, Vec3{{0.05, 0.08, 0.05}}, Vec3{{0, 0, 0}});
    GroundCollision collision;
    RigidBodyState x;
    Real t = 0;
    ContactStepControls controls;
    std::array<Real, kGearGroups> gear_raise{};

    explicit Rig(const CollisionConfig& config, const Vec3& position, const Vec3& velocity = Vec3{{0, 0, 0}}) : collision(config)
    {
        x = make_rigid_body_state(position, frames::quat_identity(), velocity, Vec3{{0, 0, 0}});
        controls.dt = 0.001;
    }

    ContactInputs inputs(const RigidBodyState& s) const
    {
        ContactInputs in;
        in.position_O = position_O(s);
        in.A_OL = attitude_A_OL(s);
        in.velocity_O = velocity_O(s);
        in.omega_L = angular_velocity_L(s);
        in.gear_raise = gear_raise;
        return in;
    }

    void reset()
    {
        collision.reset(inputs(x));
    }

    void step()
    {
        const ContactInputs in0 = inputs(x);
        ContactEvaluation e0;
        collision.evaluate(in0, e0);
        const auto f = [&](const Real, const RigidBodyState& s) {
            ContactEvaluation ev;
            collision.evaluate(inputs(s), ev);
            return rigid_body_derivative(body, s, RigidBodyLoads{ev.force_O + Vec3{{0, -kMass * kGravity, 0}}, ev.torque_L});
        };
        x = num::rk4_step(f, t, x, controls.dt);
        normalize_orientation(x);
        t += controls.dt;
        collision.post_step(in0, e0, controls);
    }

    void run(const Real seconds)
    {
        const int steps = static_cast<int>(std::lround(seconds / controls.dt));
        for (int i = 0; i < steps; ++i) {
            step();
        }
    }
};

}  // namespace

TEST_CASE("ground mesh: signed distance, contact and no ground")
{
    GroundMesh mesh;
    const MeshData q = flat_quad();
    REQUIRE(mesh.build(q.vertices, q.indices));
    CHECK(mesh.triangle_count() == 2);
    const Vec3 zero{{0, 0, 0}};

    PointProbe above = mesh.probe(Vec3{{1, 0.5, 2}}, zero, 1.0);
    CHECK_FALSE(above.in_contact);
    CHECK(above.distance == doctest::Approx(0.5));
    CHECK(above.normal.a[1] == doctest::Approx(1.0));

    PointProbe below = mesh.probe(Vec3{{1, -0.1, 2}}, zero, 1.0);
    CHECK(below.in_contact);
    CHECK(below.distance == doctest::Approx(-0.1));

    // outside of the mesh, or further away than the reach: no ground, the default distance and an upward normal
    PointProbe outside = mesh.probe(Vec3{{30, 0.2, 0}}, zero, 1.0);
    CHECK_FALSE(outside.in_contact);
    CHECK(outside.distance == kNoGroundDistance);
    PointProbe far_above = mesh.probe(Vec3{{0, 5, 0}}, zero, 1.0);
    CHECK(far_above.distance == kNoGroundDistance);
    PointProbe in_reach = mesh.probe(Vec3{{0, 5, 0}}, zero, 6.0);
    CHECK(in_reach.distance == doctest::Approx(5.0));
}

TEST_CASE("ground mesh: winding decides the normal, flip_winding turns it around, degenerate triangles are dropped")
{
    GroundMesh mesh;
    MeshData q = flat_quad();
    q.indices = {0, 1, 3, 1, 2, 3};  // opposite winding: normals point down
    REQUIRE(mesh.build(q.vertices, q.indices));
    const Vec3 zero{{0, 0, 0}};
    CHECK(mesh.probe(Vec3{{0, 0.3, 0}}, zero, 1.0).in_contact);  // a point above the downward facing ground is "below" it
    CHECK(mesh.probe(Vec3{{0, 0.3, 0}}, zero, 1.0).normal.a[1] == doctest::Approx(-1.0));

    MeshOptions flip;
    flip.flip_winding = true;
    REQUIRE(mesh.build(q.vertices, q.indices, flip));
    CHECK_FALSE(mesh.probe(Vec3{{0, 0.3, 0}}, zero, 1.0).in_contact);
    CHECK(mesh.probe(Vec3{{0, 0.3, 0}}, zero, 1.0).normal.a[1] == doctest::Approx(1.0));

    MeshData d = flat_quad();
    d.indices.insert(d.indices.end(), {0, 0, 1});  // a sliver without area
    REQUIRE(mesh.build(d.vertices, d.indices));
    CHECK(mesh.triangle_count() == 2);
    CHECK(mesh.dropped_triangle_count() == 1);

    CHECK_FALSE(mesh.build(d.vertices, std::vector<std::uint32_t>{0, 1, 99}));  // index out of range
    CHECK(mesh.empty());
    CHECK_FALSE(mesh.build(std::vector<double>{0, 0, 0, 1}, std::vector<std::uint32_t>{0, 0, 0}));
}

TEST_CASE("ground mesh: the grid query gives exactly the brute force result")
{
    const auto bumpy = [](const Real x, const Real z) { return 0.5 * std::sin(0.7 * x) * std::cos(0.5 * z); };
    const MeshData m = heightfield(30, 15, bumpy);
    const Vec3 offset{{0.02, 0, 0}};

    for (const Real cell : {Real{0}, Real{0.7}, Real{3.0}, Real{100.0}}) {
        GroundMesh mesh;
        MeshOptions options;
        options.cell_size = cell;
        REQUIRE(mesh.build(m.vertices, m.indices, options));
        CHECK(mesh.grid_cells() >= 1);

        std::uint64_t state = 12345;
        const auto rnd = [&state]() {
            state = state * 6364136223846793005ull + 1442695040888963407ull;
            return static_cast<Real>(state >> 11) / 9007199254740992.0;
        };
        int contacts = 0;
        for (int i = 0; i < 4000; ++i) {
            const Vec3 p{{-18 + 36 * rnd(), -2 + 5 * rnd(), -18 + 36 * rnd()}};
            const Real reach = i % 3 == 0 ? 0.5 : 1.5;
            const PointProbe a = mesh.probe(p, offset, reach);
            const PointProbe b = mesh.probe_brute_force(p, offset, reach);
            REQUIRE(a.in_contact == b.in_contact);
            REQUIRE(a.triangle == b.triangle);
            REQUIRE(a.distance == b.distance);
            contacts += a.in_contact ? 1 : 0;
        }
        CHECK(contacts > 100);  // the test is not vacuous
    }
}

TEST_CASE("ground mesh: a huge triangle is found from everywhere (large triangle list)")
{
    GroundMesh mesh;
    MeshData m;
    m.vertices = {-1000, 0, -1000, -1000, 0, 1000, 1000, 0, -1000,  // big triangle, normal up
                  0, 0.5, 0, 0.5, 0.5, 0, 0, 0.5, 0.5};            // small one above
    m.indices = {0, 1, 2, 3, 5, 4};
    MeshOptions options;
    options.cell_size = 0.5;
    REQUIRE(mesh.build(m.vertices, m.indices, options));
    const Vec3 zero{{0, 0, 0}};
    const PointProbe p = mesh.probe(Vec3{{-300, -0.2, -250}}, zero, 1.0);
    CHECK(p.in_contact);
    CHECK(p.distance == doctest::Approx(-0.2));
    CHECK(mesh.probe(Vec3{{-300, 0.2, -250}}, zero, 1.0).distance == doctest::Approx(0.2));
    CHECK(mesh.probe_brute_force(Vec3{{-300, -0.2, -250}}, zero, 1.0).triangle == p.triangle);
}

TEST_CASE("ground mesh: the second probe point closes the gap below the line of a concave valley")
{
    // V shaped ground: below the valley line the prisms of the two slopes do not overlap, a point there projects into neither triangle
    GroundMesh mesh;
    MeshData m;
    m.vertices = {-1, 0.5, -1, 0, 0, -1, 0, 0, 1, -1, 0.5, 1,   // left slope, descending towards x = 0
                  0, 0, -1, 1, 0.5, -1, 1, 0.5, 1, 0, 0, 1};    // right slope, ascending
    m.indices = {0, 3, 1, 1, 3, 2, 4, 7, 5, 5, 7, 6};
    REQUIRE(mesh.build(m.vertices, m.indices));
    const Vec3 p{{-0.001, -0.02, 0}};
    const Vec3 none{{0, 0, 0}};
    const PointProbe without = mesh.probe(p, none, 1.0);
    const PointProbe with = mesh.probe(p, Vec3{{0.02, 0, 0}}, 1.0);
    CHECK_FALSE(without.in_contact);
    CHECK(with.in_contact);
    CHECK(with.distance < 0);
    CHECK(mesh.probe_brute_force(p, Vec3{{0.02, 0, 0}}, 1.0).triangle == with.triangle);
}

TEST_CASE("perlin noise: deterministic, in [0, 1], continuous and about half on average")
{
    Real sum = 0;
    int n = 0;
    for (Real x = 0; x < 40; x += 0.37) {
        for (Real z = 0; z < 40; z += 0.41) {
            const Real v = num::perlin_noise_2d(x, z);
            REQUIRE(v >= 0);
            REQUIRE(v <= 1);
            REQUIRE(num::perlin_noise_2d(x, z) == v);
            REQUIRE(std::abs(num::perlin_noise_2d(x + 1e-4, z) - v) < 2e-3);
            sum += v;
            ++n;
        }
    }
    CHECK(sum / n > 0.4);
    CHECK(sum / n < 0.6);
    CHECK(num::perlin_noise_2d(-3.5, -7.25) >= 0);  // negative coordinates are fine
}

TEST_CASE("gear raising: collision offset factor follows the cosine and ends in the full offset")
{
    CollisionConfig c = four_feet(ContactType::gear_left);
    c.gear_time = 2;
    c.gear_bay_time = 1;
    c.gear_tilted_forward = 0.2;
    GroundCollision col(c);
    CHECK(col.gear_offset_factor(0) == doctest::Approx(1 - std::cos(0.2)));
    CHECK(col.gear_offset_factor(2.0 / 3 - 1e-9) == doctest::Approx(1.0).epsilon(1e-6));
    CHECK(col.gear_offset_factor(0.9) == 1.0);
    CHECK(col.gear_offset_factor(1.0) == 1.0);
    // the mechanism is tilted forward when lowered: the contact points first move down a little (factor falls to 0 at the vertical
    // position of the leg), then they rise monotonically
    const Real share = 2.0 / 3;
    const Real vertical = share * 0.2 / (frames::kPi / 2 + 0.2);
    CHECK(col.gear_offset_factor(vertical) == doctest::Approx(0.0).epsilon(1e-9));
    Real previous = 0;
    for (Real r = vertical; r < share; r += 0.005) {
        const Real f = col.gear_offset_factor(r);
        CHECK(f >= previous);
        previous = f;
    }
}

TEST_CASE("contact points follow the body pose and the raised gear offset")
{
    CollisionConfig c = four_feet(ContactType::gear_left);
    c.raised_offset_L[0] = Vec3{{0, 0.4, 0}};
    GroundCollision col(c);
    ContactInputs in;
    in.position_O = Vec3{{1, 2, 3}};
    in.A_OL = frames::rot_y(frames::kPi / 2);  // body x -> world -z
    std::array<Vec3, kMaxContactPoints> pos;
    col.point_positions(in, pos);
    // point 0 at (0.3, -0.1, 0.3) in the body: Ry(90 deg) maps (x, y, z) -> (z, y, -x)
    CHECK(pos[0].a[0] == doctest::Approx(1 + 0.3));
    CHECK(pos[0].a[1] == doctest::Approx(2 - 0.1 + 0.4 * col.gear_offset_factor(0)));
    CHECK(pos[0].a[2] == doctest::Approx(3 - 0.3));
    in.gear_raise[0] = 1;
    col.point_positions(in, pos);
    CHECK(pos[0].a[1] == doctest::Approx(2 - 0.1 + 0.4));
}

TEST_CASE("a resting body settles on its four feet at the static penetration and stays there")
{
    const MeshData q = flat_quad();
    Rig rig(four_feet(ContactType::usual), Vec3{{0, 0.1, 0}});
    REQUIRE(rig.collision.mesh().build(q.vertices, q.indices));
    rig.reset();
    rig.run(3.0);
    const Vec3 p = position_O(rig.x);
    const Real expected = 0.1 - kMass * kGravity / (4 * 2000.0);
    CHECK(p.a[1] == doctest::Approx(expected).epsilon(0.002));
    CHECK(std::abs(p.a[0]) < 1e-4);
    CHECK(std::abs(p.a[2]) < 1e-4);
    CHECK(qvm::mag(velocity_O(rig.x)) < 1e-3);
    CHECK(qvm::mag(angular_velocity_L(rig.x)) < 1e-3);
    CHECK_FALSE(rig.collision.memory().crashed);

    // the per point results add up to the total
    ContactEvaluation ev;
    rig.collision.evaluate(rig.inputs(rig.x), ev);
    Vec3 sum{{0, 0, 0}};
    int touching = 0;
    for (std::size_t i = 0; i < ev.point_count; ++i) {
        const ContactPointResult& r = ev.points[i];
        if (r.in_contact) {
            ++touching;
            sum = sum + r.normal_force_O + r.friction_force_O;
            CHECK(r.penetration == doctest::Approx(kMass * kGravity / 8000.0).epsilon(0.02));
            CHECK(r.normal_force == doctest::Approx(kMass * kGravity / 4).epsilon(0.02));
            CHECK(r.normal_O.a[1] == doctest::Approx(1.0));
        }
    }
    CHECK(touching == 4);
    CHECK(sum.a[1] == doctest::Approx(kMass * kGravity).epsilon(0.002));
    CHECK(ev.force_O.a[1] == doctest::Approx(sum.a[1]));
    CHECK(ev.hub_distance[0] == doctest::Approx(p.a[1] + 0.3));  // main hub above the ground
}

TEST_CASE("sliding friction decelerates with mu g and the body comes to rest (stiction holds it)")
{
    const MeshData q = flat_quad(100);
    CollisionConfig c = four_feet(ContactType::usual);
    c.friction_coeff = 0.5;
    Rig rig(c, Vec3{{-20, 0.0976, 0}}, Vec3{{2, 0, 0}});
    REQUIRE(rig.collision.mesh().build(q.vertices, q.indices));
    rig.reset();
    rig.run(0.2);
    CHECK(velocity_O(rig.x).a[0] == doctest::Approx(2 - 0.5 * kGravity * 0.2).epsilon(0.05));
    rig.run(1.0);
    CHECK(std::abs(velocity_O(rig.x).a[0]) < 0.02);
    const Vec3 before = position_O(rig.x);
    rig.run(2.0);
    const Vec3 after = position_O(rig.x);
    CHECK(std::abs(after.a[0] - before.a[0]) < 0.003);
    CHECK(std::abs(after.a[2] - before.a[2]) < 0.003);
}

TEST_CASE("stiction keeps the body on a slope that is flatter than the friction angle")
{
    // plane y = x tan(15 deg), counter clockwise from above
    const Real slope = std::tan(15 * frames::kDegToRad);
    const MeshData m = heightfield(2, 50, [slope](const Real x, const Real) { return slope * x; });
    CollisionConfig c = four_feet(ContactType::usual);
    c.friction_coeff = 0.6;
    Rig rig(c, Vec3{{0, 0.1, 0}});
    REQUIRE(rig.collision.mesh().build(m.vertices, m.indices));
    rig.reset();
    rig.run(4.0);
    const Vec3 before = position_O(rig.x);
    rig.run(3.0);
    const Vec3 after = position_O(rig.x);
    CHECK(qvm::mag(after - before) < 0.003);
    CHECK(qvm::mag(velocity_O(rig.x)) < 0.01);
}

TEST_CASE("a body on a slope steeper than the friction angle slides down")
{
    const Real slope = std::tan(35 * frames::kDegToRad);
    const MeshData m = heightfield(2, 50, [slope](const Real x, const Real) { return slope * x; });
    CollisionConfig c = four_feet(ContactType::usual);
    c.friction_coeff = 0.3;
    Rig rig(c, Vec3{{0, 0.1, 0}});
    REQUIRE(rig.collision.mesh().build(m.vertices, m.indices));
    rig.reset();
    rig.run(2.0);
    CHECK(velocity_O(rig.x).a[0] < -1.0);  // downhill is -x
}

TEST_CASE("evaluate is a pure function: the same input gives the same output and the memory is not touched")
{
    const MeshData q = flat_quad();
    Rig rig(four_feet(ContactType::gear_left), Vec3{{0.5, 0.098, 0.2}}, Vec3{{0.3, -0.1, 0.05}});
    REQUIRE(rig.collision.mesh().build(q.vertices, q.indices));
    rig.reset();
    rig.run(0.3);
    const ContactMemory memory_before = rig.collision.memory();
    ContactEvaluation a;
    ContactEvaluation b;
    const ContactInputs in = rig.inputs(rig.x);
    rig.collision.evaluate(in, a);
    rig.collision.evaluate(in, b);
    CHECK(a.force_O.a[0] == b.force_O.a[0]);
    CHECK(a.force_O.a[1] == b.force_O.a[1]);
    CHECK(a.torque_L.a[2] == b.torque_L.a[2]);
    const ContactMemory& after = rig.collision.memory();
    CHECK(after.wheel_brake_strength == memory_before.wheel_brake_strength);
    CHECK(after.wheel_rolling_distance == memory_before.wheel_rolling_distance);
    CHECK(after.steering_raw == memory_before.steering_raw);
    for (std::size_t i = 0; i < 4; ++i) {
        CHECK(after.stiction_anchor_O[i].a[0] == memory_before.stiction_anchor_O[i].a[0]);
    }
}

TEST_CASE("wheels: the brake raises the rolling friction, rolling distance and speed are reported")
{
    const MeshData q = flat_quad(100);
    CollisionConfig c = four_feet(ContactType::gear_left);
    c.group_is_wheel[0] = 1;
    c.friction_coeff_forward = 0.02;   // rolling
    c.friction_coeff_sideward = 0.8;   // braking along x, and sideways
    Rig free(c, Vec3{{-50, 0.0976, 0}}, Vec3{{3, 0, 0}});
    Rig braked(c, Vec3{{-50, 0.0976, 0}}, Vec3{{3, 0, 0}});
    REQUIRE(free.collision.mesh().build(q.vertices, q.indices));
    REQUIRE(braked.collision.mesh().build(q.vertices, q.indices));
    free.reset();
    braked.reset();
    braked.controls.wheel_brake_target = 1;
    free.run(1.0);
    braked.run(1.0);
    CHECK(velocity_O(free.x).a[0] > 2.7);       // rolls on, only 0.02 g
    CHECK(velocity_O(braked.x).a[0] < 2.0);     // the brake took 1 s to build up, then it decelerates
    CHECK(braked.collision.memory().wheel_brake_strength > 0.5);
    CHECK(free.collision.memory().wheel_rolling_distance[0] > 2.5);
    CHECK(free.collision.memory().wheel_rolling_velocity[0] == doctest::Approx(velocity_O(free.x).a[0]).epsilon(0.05));
    braked.run(3.0);
    CHECK(braked.collision.memory().wheel_brake_strength > 0.97);

    // raised gear: the brake is fully on whatever the target is
    Rig raised(c, Vec3{{0, 5, 0}});
    raised.reset();
    raised.controls.wheels_raised = true;
    raised.run(4.0);
    CHECK(raised.collision.memory().wheel_brake_strength > 0.97);
}

TEST_CASE("a wheel that lost the ground keeps turning and slows down")
{
    const MeshData q = flat_quad(100);
    CollisionConfig c = four_feet(ContactType::gear_left);
    c.group_is_wheel[0] = 1;
    c.friction_coeff_forward = 0.02;
    Rig rig(c, Vec3{{-30, 0.0976, 0}}, Vec3{{3, 0, 0}});
    REQUIRE(rig.collision.mesh().build(q.vertices, q.indices));
    rig.reset();
    rig.run(0.3);
    const Real v_ground = rig.collision.memory().wheel_rolling_velocity[0];
    CHECK(v_ground > 2.9);

    rig.x = make_rigid_body_state(position_O(rig.x) + Vec3{{0, 5, 0}}, frames::quat_identity(), velocity_O(rig.x), Vec3{{0, 0, 0}});
    const Real distance_before = rig.collision.memory().wheel_rolling_distance[0];
    rig.run(0.5);
    const Real v_air = rig.collision.memory().wheel_rolling_velocity[0];
    CHECK(v_air < v_ground - 0.28);   // 0.0006 per step without brake
    CHECK(v_air > v_ground - 0.32);
    CHECK(rig.collision.memory().wheel_rolling_distance[0] - distance_before > 1.2);
}

TEST_CASE("a hard impact latches the crash flag, a soft landing does not")
{
    const MeshData q = flat_quad();
    CollisionConfig c = four_feet(ContactType::usual);
    c.collision_force_max = 100;
    Rig hard(c, Vec3{{0, 2.1, 0}});
    REQUIRE(hard.collision.mesh().build(q.vertices, q.indices));
    hard.reset();
    hard.run(1.5);
    CHECK(hard.collision.memory().crashed);
    CHECK(hard.collision.memory().max_normal_force > 100);
    hard.reset();
    CHECK_FALSE(hard.collision.memory().crashed);

    Rig soft(c, Vec3{{0, 0.11, 0}});
    REQUIRE(soft.collision.mesh().build(q.vertices, q.indices));
    soft.reset();
    soft.run(1.5);
    CHECK_FALSE(soft.collision.memory().crashed);
}

TEST_CASE("rotor hubs only report their distance to the ground, they get no force")
{
    const MeshData q = flat_quad();
    CollisionConfig c = four_feet(ContactType::usual);
    Rig rig(c, Vec3{{0, 0.6, 0}});
    REQUIRE(rig.collision.mesh().build(q.vertices, q.indices));
    ContactEvaluation e;
    rig.collision.evaluate(rig.inputs(rig.x), e);
    CHECK(e.hub_distance[0] == doctest::Approx(0.9));
    CHECK(e.hub_distance[1] == doctest::Approx(0.7));
    CHECK(e.hub_normal_O[0].a[1] == doctest::Approx(1.0));
    CHECK_FALSE(e.any_contact);
    CHECK(e.force_O.a[1] == 0);

    // a hub below the surface (the helicopter is on its side) does not produce a force either
    Rig low(c, Vec3{{0, -0.5, 0}});
    REQUIRE(low.collision.mesh().build(q.vertices, q.indices));
    low.collision.evaluate(low.inputs(low.x), e);
    CHECK(e.hub_distance[0] < 0);
    CHECK(e.points[4].normal_force == 0);
    CHECK(e.points[5].normal_force == 0);
}

TEST_CASE("bumps raise the ground within the bump height and never create a pulling force")
{
    const MeshData q = flat_quad();
    CollisionConfig c = four_feet(ContactType::usual);
    c.bump_height = 0.01;
    c.bump_density_scale = 20;
    ContactEvaluation e;
    int touching_points = 0;
    for (int i = 0; i < 40; ++i) {
        Rig r(c, Vec3{{0.017 * i, 0.0999 - 0.005, 0.011 * i}});
        REQUIRE(r.collision.mesh().build(q.vertices, q.indices));
        r.collision.evaluate(r.inputs(r.x), e);
        for (std::size_t k = 0; k < 4; ++k) {
            const ContactPointResult& p = e.points[k];
            CHECK(p.penetration >= 0);
            CHECK(p.penetration <= 0.005 + 1e-3 + 0.01);
            CHECK(p.normal_force >= 0);
            touching_points += p.in_contact ? 1 : 0;
        }
    }
    CHECK(touching_points == 160);  // every point is "in contact", the bump only changes the depth
}

TEST_CASE("config from the parameters: point lists, wheel types and the search reach")
{
    hoverhere::params::si::ModelParams p{};
    p.helicopter.collision.positions_usual.count = 1;
    p.helicopter.collision.positions_usual.items[0] = Vec3{{0.2, -0.3, 0}};
    p.helicopter.collision.positions_left.count = 2;
    p.helicopter.collision.positions_left.items[0] = Vec3{{0.1, -0.4, -0.2}};
    p.helicopter.collision.positions_left.items[1] = Vec3{{-0.1, -0.4, -0.2}};
    p.helicopter.collision.positions_left_type = 1;
    p.helicopter.collision.positions_left_rised_offset = Vec3{{0, 0.9, 0}};
    p.helicopter.collision.positions_steering_center.count = 1;
    p.helicopter.collision.positions_steering_center.items[0] = Vec3{{0.7, -0.35, 0}};
    p.helicopter.mainrotor.posLH = Vec3{{0, 0.5, 0}};
    p.helicopter.tailrotor.posLH = Vec3{{-1.2, 0.1, 0}};
    p.helicopter.collision.stiffness_factor = 1234;
    p.simulation.various.V_h = 0.03;

    const CollisionConfig c = make_collision_config(p);
    CHECK(c.force_point_count == 4);
    CHECK(c.point_count == 6);
    CHECK(c.points[0].type == ContactType::usual);
    CHECK(c.points[1].type == ContactType::gear_left);
    CHECK(c.points[3].type == ContactType::steering_center);
    CHECK(c.points[4].type == ContactType::mainrotor_hub);
    CHECK(c.points[5].type == ContactType::tailrotor_hub);
    CHECK(c.group_is_wheel[0] == 1);
    CHECK(c.group_is_wheel[1] == 0);
    CHECK(c.stiffness == 1234);
    CHECK(c.V_h == 0.03);
    CHECK(c.aabb_radius == doctest::Approx(0.7 + 0.1));  // the raised left gear reaches y = 0.5 at most
}
