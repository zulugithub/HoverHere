#include <cmath>
#include <numbers>

#include <doctest/doctest.h>

#include "hoverhere/aero/aerodynamics.hpp"
#include "hoverhere/core/frames.hpp"
#include "hoverhere/params/params.hpp"

using hoverhere::Real;
using namespace hoverhere::aero;
using hoverhere::math::Mat3;
using hoverhere::math::Vec3;
namespace frames = hoverhere::frames;
namespace qvm = boost::qvm;

namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;
constexpr Real kDeg = kPi / 180;
constexpr Real kRho = 1.2;

hoverhere::params::si::Fuselage fuselage()
{
    hoverhere::params::si::Fuselage f{};
    f.S_fxyz = Vec3{{0.1, 0.4, 0.3}};
    f.downwash_factor_mainrotor = 0.8;
    f.downwash_factor_tailrotor = 0.5;
    return f;
}

hoverhere::params::si::Wing horizontal_fin()
{
    hoverhere::params::si::Wing w{};
    w.wing_exists = true;
    w.horizontal0_or_vertical1 = false;
    w.area = 0.05;
    w.drag_coeff = 1.0;
    w.C_l_alpha = 5.5;
    w.alpha_stall = 20 * kDeg;
    w.downwash_factor_mainrotor = 0.5;
    w.downwash_factor_tailrotor = 0.4;
    w.downwash_factor_propeller = 0.3;
    w.posLH = Vec3{{-1.0, 0.1, 0}};
    return w;
}

BodyKinematics upright(const Vec3& velocity_L)
{
    BodyKinematics b{};
    b.A_OL = Mat3{{{1, 0, 0}, {0, 1, 0}, {0, 0, 1}}};
    b.velocity_L = velocity_L;
    b.velocity_O = velocity_L;
    return b;
}

}  // namespace

TEST_CASE("the fuselage drag is quadratic in the air velocity along every body axis and opposes the motion")
{
    const auto f = fuselage();
    const RotorWake no_wake{};
    const Vec3 forward = fuselage_force_L(f, kRho, Vec3{{10, 0, 0}}, no_wake);
    CHECK(forward.a[0] == doctest::Approx(-kRho / 2 * 0.1 * 100));
    CHECK(forward.a[1] == 0);
    CHECK(forward.a[2] == 0);
    CHECK(fuselage_force_L(f, kRho, Vec3{{20, 0, 0}}, no_wake).a[0] == doctest::Approx(4 * forward.a[0]));
    CHECK(fuselage_force_L(f, kRho, Vec3{{-10, 0, 0}}, no_wake).a[0] == doctest::Approx(-forward.a[0]));  // backwards: the drag flips

    const Vec3 mixed = fuselage_force_L(f, kRho, Vec3{{3, -4, 5}}, no_wake);
    CHECK(mixed.a[0] == doctest::Approx(-kRho / 2 * 0.1 * 9));
    CHECK(mixed.a[1] == doctest::Approx(+kRho / 2 * 0.4 * 16));  // moving down: the drag pushes up
    CHECK(mixed.a[2] == doctest::Approx(-kRho / 2 * 0.3 * 25));
}

TEST_CASE("the main rotor downwash pushes the fuselage down, less so in inverted flight")
{
    const auto f = fuselage();
    RotorWake wake;
    wake.mainrotor = 4;  // downwash of a lifting rotor
    const Vec3 hover = fuselage_force_L(f, kRho, Vec3{{0, 0, 0}}, wake);
    CHECK(hover.a[1] == doctest::Approx(-kRho / 2 * 0.4 * (4 * 0.8) * (4 * 0.8)));  // the wake counts with the downwash factor
    CHECK(hover.a[1] < 0);

    // a rotor that blows upwards (inverted flight): only a third of the effect
    wake.mainrotor = -4;
    const Vec3 inverted = fuselage_force_L(f, kRho, Vec3{{0, 0, 0}}, wake);
    CHECK(inverted.a[1] > 0);
    CHECK(inverted.a[1] == doctest::Approx(kRho / 2 * 0.4 * (4 * 0.8 * 0.3333) * (4 * 0.8 * 0.3333)));

    // descending at the speed of the wake: the relative flow vanishes
    wake.mainrotor = 4;
    CHECK(std::abs(fuselage_force_L(f, kRho, Vec3{{0, -4 * 0.8, 0}}, wake).a[1]) < 1e-9);

    // the downwash factor scales the effect, the second rotor of a tandem adds its own
    auto no_factor = f;
    no_factor.downwash_factor_mainrotor = 0;
    CHECK(fuselage_force_L(no_factor, kRho, Vec3{{0, 0, 0}}, wake).a[1] == 0);
    wake.tailrotor = 2;
    CHECK(fuselage_force_L(f, kRho, Vec3{{0, 0, 0}}, wake).a[1] == doctest::Approx(-kRho / 2 * 0.4 * (4 * 0.8 + 2 * 0.5) * (4 * 0.8 + 2 * 0.5)));
}

TEST_CASE("a horizontal fin lifts against the vertical air velocity with the lift curve slope and drags when stalled")
{
    const auto fin = horizontal_fin();
    const RotorWake no_wake{};

    // flying forward at 20 m/s while sinking at 1 m/s: the air comes from below, the fin lifts
    const BodyKinematics sinking = upright(Vec3{{20, -1, 0}});
    const SurfaceLoads lifting = wing_loads(fin, kRho, sinking, Vec3{{0, 0, 0}}, no_wake);
    const Real alpha = std::atan(1.0 / 20.0);
    CHECK(lifting.force_L.a[1] == doctest::Approx(kRho / 2 * fin.C_l_alpha * alpha * fin.area * 1.0 * 20.0));
    CHECK(lifting.force_L.a[1] > 0);
    CHECK(lifting.force_L.a[0] == 0);
    CHECK(lifting.force_L.a[2] == 0);

    // climbing: the lift points down
    CHECK(wing_loads(fin, kRho, upright(Vec3{{20, 1, 0}}), Vec3{{0, 0, 0}}, no_wake).force_L.a[1] < 0);

    // stalled (flow almost perpendicular): flat plate drag with the drag coefficient
    const SurfaceLoads stalled = wing_loads(fin, kRho, upright(Vec3{{1, -10, 0}}), Vec3{{0, 0, 0}}, no_wake);
    CHECK(stalled.force_L.a[1] == doctest::Approx(kRho / 2 * fin.drag_coeff * fin.area * 100));

    // the lift moment about the centre of mass uses the position of the fin
    CHECK(lifting.torque_L.a[2] == doctest::Approx(fin.posLH.a[0] * lifting.force_L.a[1]));  // (-1, 0.1, 0) x (0, F, 0) = (0, 0, -F)
    CHECK(lifting.torque_L.a[2] < 0);
}

TEST_CASE("a vertical fin produces a side force against the lateral air velocity")
{
    auto fin = horizontal_fin();
    fin.horizontal0_or_vertical1 = true;
    fin.posLH = Vec3{{-1.0, 0.0, 0.0}};
    const RotorWake no_wake{};
    const SurfaceLoads loads = wing_loads(fin, kRho, upright(Vec3{{20, 0, 1}}), Vec3{{0, 0, 0}}, no_wake);  // sideslip to the right
    CHECK(loads.force_L.a[2] == doctest::Approx(-kRho / 2 * fin.C_l_alpha * std::atan(1.0 / 20.0) * fin.area * 1.0 * 20.0));
    CHECK(loads.force_L.a[2] < 0);
    CHECK(loads.force_L.a[1] == 0);
    CHECK(loads.torque_L.a[1] < 0);  // a force to the left (-z) behind the centre of mass turns the nose to the right: negative about +y
    CHECK(loads.torque_L.a[1] == doctest::Approx(-fin.posLH.a[0] * loads.force_L.a[2]));  // torque_y = r_z F_x - r_x F_z = -r_x F_z
}

TEST_CASE("the rotor wakes accelerate the air at a fin only where they blow onto it")
{
    const auto fin = horizontal_fin();
    RotorWake wake;
    wake.mainrotor = 5;
    // the main rotor downwash hits the horizontal fin from above
    const BodyKinematics rest = upright(Vec3{{20, 0, 0}});
    const SurfaceLoads blown = wing_loads(fin, kRho, rest, Vec3{{0, 0, 0}}, wake);
    const SurfaceLoads calm = wing_loads(fin, kRho, rest, Vec3{{0, 0, 0}}, RotorWake{});
    CHECK(blown.force_L.a[1] < calm.force_L.a[1]);  // pushed down
    // with the downwash factor of the fin: v = 5 * 0.5
    const Real v = 5 * fin.downwash_factor_mainrotor;
    const Real alpha = std::atan(v / 20.0);
    CHECK(alpha < fin.alpha_stall);
    CHECK(blown.force_L.a[1] == doctest::Approx(-kRho / 2 * fin.C_l_alpha * alpha * fin.area * v * 20.0));

    // a rotor that blows backwards (negative wake) does not act
    wake.mainrotor = -5;
    CHECK(wing_loads(fin, kRho, rest, Vec3{{0, 0, 0}}, wake).force_L.a[1] == doctest::Approx(calm.force_L.a[1]));
}

TEST_CASE("the torque of a fin is a body frame cross product, whatever the attitude of the fuselage is")
{
    const auto fin = horizontal_fin();
    const RotorWake no_wake{};
    // the fuselage is yawed by 90 degrees: Unity crossed the body frame position with the world frame force there
    BodyKinematics yawed = upright(Vec3{{20, -1, 0}});
    yawed.A_OL = frames::rot_y(kPi / 2);
    yawed.velocity_O = yawed.A_OL * yawed.velocity_L;
    const SurfaceLoads loads = wing_loads(fin, kRho, yawed, Vec3{{0, 0, 0}}, no_wake);
    const Vec3 expected = qvm::cross(fin.posLH, loads.force_L);
    CHECK(qvm::mag(loads.torque_L - expected) < 1e-12);
    // and the same for the upright fuselage: the attitude does not matter for loads in the body frame
    const SurfaceLoads upright_loads = wing_loads(fin, kRho, upright(Vec3{{20, -1, 0}}), Vec3{{0, 0, 0}}, no_wake);
    CHECK(qvm::mag(loads.torque_L - upright_loads.torque_L) < 1e-12);
}

TEST_CASE("the air velocity relative to the aircraft counts: wind and motion are equivalent")
{
    const auto f = fuselage();
    AerodynamicsConfig cfg{};
    cfg.fuselage = f;
    cfg.horizontal_fin = horizontal_fin();
    const RotorWake no_wake{};

    const AerodynamicsResult moving = aerodynamics_evaluate(cfg, upright(Vec3{{-6, 0.5, 2}}), Vec3{{0, 0, 0}}, kRho, no_wake);
    const AerodynamicsResult in_wind = aerodynamics_evaluate(cfg, upright(Vec3{{0, 0, 0}}), Vec3{{6, -0.5, -2}}, kRho, no_wake);
    CHECK(qvm::mag(moving.force_O - in_wind.force_O) < 1e-12);
    CHECK(qvm::mag(moving.torque_L - in_wind.torque_L) < 1e-12);
}

TEST_CASE("the total loads add the surfaces that exist and rotate the force into the world frame")
{
    AerodynamicsConfig cfg{};
    cfg.fuselage = fuselage();
    cfg.horizontal_fin = horizontal_fin();
    cfg.vertical_fin = horizontal_fin();
    cfg.vertical_fin.horizontal0_or_vertical1 = true;
    cfg.vertical_fin.wing_exists = false;  // does not exist: no contribution
    const RotorWake wake{3, 0, 0};
    BodyKinematics body = upright(Vec3{{15, -2, 1}});
    body.A_OL = frames::rot_y(0.7);
    body.velocity_O = body.A_OL * body.velocity_L;

    const AerodynamicsResult r = aerodynamics_evaluate(cfg, body, Vec3{{0, 0, 0}}, kRho, wake);
    const Vec3 fuselage_only = fuselage_force_L(cfg.fuselage, kRho, body.velocity_L, wake);
    const SurfaceLoads fin = wing_loads(cfg.horizontal_fin, kRho, body, Vec3{{0, 0, 0}}, wake);
    CHECK(qvm::mag(r.fuselage_force_L - fuselage_only) < 1e-12);
    CHECK(qvm::mag(r.force_O - body.A_OL * (fuselage_only + fin.force_L)) < 1e-12);
    CHECK(qvm::mag(r.torque_L - fin.torque_L) < 1e-12);  // the fuselage drag has no moment (it acts at the centre of mass)

    const auto config = make_aerodynamics_config(hoverhere::params::compile_to_si(hoverhere::params::make_default_params()).helicopter);
    CHECK(config.fuselage.S_fxyz.a[0] > 0);
}
