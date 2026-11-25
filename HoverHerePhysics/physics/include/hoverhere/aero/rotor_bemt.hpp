#pragma once

#include <array>
#include <cstddef>
#include <cstdint>
#include <filesystem>
#include <string>
#include <string_view>

#include "hoverhere/aero/rotor_common.hpp"
#include "hoverhere/kinematics/kinematics.hpp"
#include "hoverhere/numerics/rk4.hpp"
#include "hoverhere/params/model_params.hpp"

// Discretized blade element momentum rotor (BEMT) of the main rotor on a polar grid of virtual blades (documentation
// chapter 5, port of Helicopter_Rotor_Physics.Rotor_Disc_BEMT_Calculations). The rotor disc is a rigid body with three
// rotational degrees of freedom (RK4 state RotorDiscStateVector) that is connected to the hub by a spring and a damper.
//
//   Time structure
//   bemt_evaluate()    pure function of (config, filter state, inputs): safe in every RK4 stage. Contains the Newton solver
//                      for the induced velocity of every element. It reads, but never writes, the filter state.
//   bemt_post_step()   once per full step: Pitt-Peters inflow lag, the time lag of the Johnson vortex ring model and of the vortex
//                      ring strength (both PT1, implicit Euler), the Newton warm start and the turbulence random numbers. Nothing else changes the filter state.
//
//   Sign convention of the induced velocity: Vi > 0 is wake flow along the negative rotor axis (downwash of a lifting main
//   rotor, thrust along +axis); V0 = Vi + Vc is the axial flow through the disc relative to the blade. A rotor with inverted
//   collective has Vi < 0. The Johnson model (Vz_scaled, v_johnson) uses the same convention, with Vz_scaled = polarity *
//   Vc / v_h and the polarity of the collective pitch (+1 normal, -1 inverted), so that a descent is Vz_scaled < 0 for both.
//   Sign of the torque: aerodynamic torque on the rotor about its shaft, signed like the rotation (negative when driven).
//   Unity returns the opposite sign in BEMT mode, which would accelerate the rotor.
namespace hoverhere::aero {

inline constexpr std::size_t kBemtRadialCount = 4;
inline constexpr std::size_t kBemtAzimuthCount = kinematics::kVirtualBladeCount;  // 10 virtual blades, psi_c = c * 36 deg
inline constexpr std::size_t kBemtElementCount = kBemtRadialCount * kBemtAzimuthCount;

struct MainrotorAirfoilProfile {
    std::string_view name;
    std::string_view polar_filename;
};

inline constexpr std::array<MainrotorAirfoilProfile, 5> kMainrotorAirfoilProfiles{{
    {"NACA0012", "n0012-il_polar.xml"},
    {"NACA0009sm-il", "n0009sm-il_polar.xml"},
    {"NACA23012", "naca23012-il_polar.xml"},
    {"NACA8H12", "n8h12-il_polar.xml"},
    {"VR-7", "vr7-il_polar.xml"},
}};

// Index of grid point (radial station r, virtual blade c); the azimuth index runs fastest.
[[nodiscard]] constexpr std::size_t bemt_index(const std::size_t r, const std::size_t c) noexcept
{
    return r * kBemtAzimuthCount + c;
}

template <class T>
using ElementArray = std::array<T, kBemtElementCount>;  // structure of arrays: one array per quantity, one entry per grid point

// ----------------------------------------------------------------------------------------------------------------
// Johnson vortex ring state model (NASA TP-2005-213477 table 3 style inflow with a smooth baseline)
// ----------------------------------------------------------------------------------------------------------------
struct JohnsonVrsParams {
    // baseline boundary velocities (normalized by the hover induced velocity)
    Real VzA = -1.5;        // upper limit of the baseline blend zone
    Real VzB = -2.1;        // lower limit of the baseline blend zone
    Real VxC = 0.75;        // horizontal speed where the baseline boundaries start to move
    // polynomial region of the vortex ring
    Real VzD = -0.2;        // entry boundary
    Real VzN = -0.45;       // point of minimum core inflow
    Real VzX = -1.5;        // point of maximum core inflow
    Real VzE = -2.0;        // exit boundary
    Real VxM = 0.95;        // horizontal speed beyond which forward speed outruns the vortex
    Real Vz_plus_v_N = 0.85;
    Real Vz_plus_v_X = 1.25;
    Real zeta = 0.5;        // scales the correction term only
    Real f = 1.0;           // suppression factor of the correction
    Real tau_rotor_revolutions = 10;  // time lag of the development in rotor revolutions
    Real tau_strength = 0.4;          // [s] build-up / decay of the reported vortex ring strength (a ring needs several revolutions); 0: none
    Real transition_min_width = 0.4;
    Real transition_blend = 0.1;
};

[[nodiscard]] Real johnson_momentum_theory(Real Vz_scaled, Real Vx_scaled, bool use_upper_branch) noexcept;
[[nodiscard]] Real johnson_baseline(Real Vz_scaled, Real Vx_scaled, const JohnsonVrsParams& p) noexcept;
[[nodiscard]] Real johnson_vrs_correction(Real Vz_scaled, Real Vx_scaled, const JohnsonVrsParams& p, Real& strength_vrs) noexcept;
// Normalized induced velocity (baseline + zeta f correction); strength_vrs in [0, 1] is the weight of the correction region.
[[nodiscard]] Real johnson_table3(Real Vz_scaled, Real Vx_scaled, const JohnsonVrsParams& p, Real& strength_vrs) noexcept;
// First-order dynamics of the vortex ring development, advanced by dt: PT1 with the time constant tau_rotor_revolutions revolutions of
// the rotor (clamped to 0.05 .. 5 s), implicit Euler, unconditionally stable and independent of the step size. (The Unity version,
// Helicopter_Rotor.ApplyTimeLag, integrates its lag state once per call: that is a poorly damped oscillator of sqrt(1 / (tau dt))
// that rings at about 6 Hz for dt = 1 ms and switches the vortex ring on and off.)
[[nodiscard]] Real johnson_time_lag(Real v_target, Real v_previous, Real omega_mr, Real tau_rotor_revolutions, Real dt) noexcept;
// One implicit Euler step of a first-order lag x' = (target - x) / tau; tau <= 0: x follows the target instantly.
[[nodiscard]] Real first_order_lag_step(Real x, Real target, Real dt, Real tau) noexcept;

// ----------------------------------------------------------------------------------------------------------------
// Empirical full-circle continuation used after the selected XFOIL polar ends.
// ----------------------------------------------------------------------------------------------------------------
struct PostStallModel {
    Real cl_positive_peak_deg = 40.0;
    Real cl_negative_peak_deg = 40.0;
    Real cl_positive_amplitude = 1.10;
    Real cl_negative_amplitude = 1.10;
    Real cd_positive_first_min_deg = -1.0;
    Real cd_negative_first_min_deg = -1.0;
    Real cd_positive_amplitude_or_offset = 0.90;
    Real cd_negative_amplitude_or_offset = 0.90;
};

// Re / Ncrit lookup polar. The table blends into the 360-degree post-stall model.
struct AirfoilPolarTable {
    static constexpr std::size_t kReCount = 4;
    static constexpr std::size_t kNcritCount = 3;
    static constexpr Real kAlphaMinDeg = -22.0;
    static constexpr Real kAlphaMaxDeg = 22.0;
    static constexpr Real kAlphaStepDeg = 0.5;
    static constexpr std::size_t kAlphaCount = 89;
    static constexpr std::size_t kPolarCount = kReCount * kNcritCount;
    static constexpr Real kAlphaMinRad = kAlphaMinDeg * Real{3.14159265358979323846} / Real{180.0};
    static constexpr Real kAlphaMaxRad = kAlphaMaxDeg * Real{3.14159265358979323846} / Real{180.0};
    static constexpr Real kInvAlphaStep = Real{1} / (kAlphaStepDeg * Real{3.14159265358979323846} / Real{180.0});

    std::array<Real, kReCount> re_grid{{50000, 100000, 200000, 500000}};
    std::array<Real, kNcritCount> ncrit_grid{{1.0, 5.0, 9.0}};
    std::array<Real, kPolarCount> alpha_min_deg{};
    std::array<Real, kPolarCount> alpha_max_deg{};
    std::array<Real, kPolarCount * kAlphaCount> cl_table{};
    std::array<Real, kPolarCount * kAlphaCount> cd_table{};

    [[nodiscard]] static constexpr std::size_t index(std::size_t r, std::size_t n, std::size_t a) noexcept
    {
        return (r * kNcritCount + n) * kAlphaCount + a;
    }

    [[nodiscard]] static constexpr std::size_t polar_index(std::size_t r, std::size_t n) noexcept
    {
        return r * kNcritCount + n;
    }
};

struct AirfoilSlice {
    std::array<Real, AirfoilPolarTable::kAlphaCount> cl{};
    std::array<Real, AirfoilPolarTable::kAlphaCount> cd{};
};

template <std::size_t N>
inline void find_grid_bracket(const std::array<Real, N>& grid, Real value, std::size_t& index0, Real& fraction) noexcept
{
    if (value <= grid[0]) {
        index0 = 0;
        fraction = 0;
        return;
    }
    if (value >= grid[N - 1]) {
        index0 = N - 2;
        fraction = 1;
        return;
    }
    for (std::size_t i = 0; i + 1 < N; ++i) {
        if (value <= grid[i + 1]) {
            index0 = i;
            fraction = (value - grid[i]) / (grid[i + 1] - grid[i]);
            return;
        }
    }
    index0 = N - 2;
    fraction = 1;
}

[[nodiscard]] AirfoilSlice make_airfoil_slice(const AirfoilPolarTable& table, Real re, Real ncrit) noexcept;
void evaluate_airfoil_polar(const PostStallModel& post_stall, const AirfoilSlice& slice, Real alpha, Real& cl, Real& cd) noexcept;
[[nodiscard]] bool load_airfoil_polar_xml(const std::filesystem::path& file, AirfoilPolarTable& table,
                                          PostStallModel& post_stall) noexcept;
[[nodiscard]] bool load_airfoil_polar_xml(const std::filesystem::path& file, AirfoilPolarTable& table) noexcept;

struct NewtonSettings {
    Real residual_tolerance = 0.001;   // [N]
    Real delta = 0.001;                // [m/s] difference quotient
    Real damping = 0.5;
    Real max_step = 3.0;               // [m/s]
    Real min_step = 1.0e-5;
    Real min_slope = 1.0e-4;
    int max_iterations = 60;
};

// ----------------------------------------------------------------------------------------------------------------
// configuration
// ----------------------------------------------------------------------------------------------------------------
struct BemtConfig {
    // individually switchable components
    bool vortex_ring_state = true;     // Johnson inflow blended in during descent (off: pure blade element solution)
    bool ground_effect = true;
    bool turbulence = true;
    bool dynamic_inflow = true;        // Pitt-Peters lag of the induced velocity (off: the induced velocity follows instantly)
    bool quasi_static = false;         // steady state physics for sweeps and tests: no lags, no descent coherence gate (Unity is_testing)

    params::si::Rotor rotor{};
    Real tip_loss = 0.97;              // [-] thrust factor (Prandtl tip loss)
    Real root_cutout = 0.2;            // [-] inner radius as a fraction of R
    Real disc_inertia_tilt = 0.03487;  // [kg m^2] inertia of the disc about its x and z axis (I_flapping)
    Real disc_inertia_spin = 0.01;     // [kg m^2] inertia of the disc about its shaft (mainrotor.J)
    Real hub_stiffness = 150;          // [Nm/rad] disc to hub
    Real hub_damping = 3.5;            // [Nm s/rad]
    Real wake_inertia_fumble_factor = 1.0;  // [-] multiplies the Pitt-Peters time constant
    Real gravity = 9.81;               // [m/s^2] (hover induced velocity of the vortex ring scaling)
    Real low_rpm_pitch = 0 * 3.14159265358979323846 / 180;  // [rad] pitch used at (very) low rotor speed instead of the commanded one
    Real low_rpm_start = 30 * 3.14159265358979323846 / 30;  // [rad/s] below: low_rpm_pitch
    Real low_rpm_end = 80 * 3.14159265358979323846 / 30;    // [rad/s] above: the commanded pitch
    PostStallModel post_stall{};
    AirfoilPolarTable airfoil_polar{};
    Real kinematic_viscosity = 1.48e-5;  // [m^2/s] air at about 15 deg C
    JohnsonVrsParams vrs{};
    NewtonSettings newton{};
    TurbulenceConfig turbulence_config{};
};

[[nodiscard]] BemtConfig make_bemt_config(const params::si::ModelParams& p);

// ----------------------------------------------------------------------------------------------------------------
// state
// ----------------------------------------------------------------------------------------------------------------
// RK4 state of the rotor disc: orientation quaternion (w, x, y, z, disc frame -> world) and the angular velocity about the
// disc x- and z-axis. The spin about the disc axis is the shaft speed of the drivetrain.
namespace disc {
inline constexpr std::size_t q0 = 0;
inline constexpr std::size_t q1 = 1;
inline constexpr std::size_t q2 = 2;
inline constexpr std::size_t q3 = 3;
inline constexpr std::size_t wx = 4;  // [rad/s]
inline constexpr std::size_t wz = 5;  // [rad/s]
}  // namespace disc
using RotorDiscStateVector = numerics::StateVector<6>;
[[nodiscard]] RotorDiscStateVector make_rotor_disc_state(const Quat& orientation, Real wx, Real wz) noexcept;
[[nodiscard]] Quat disc_orientation(const RotorDiscStateVector& x) noexcept;
void normalize_disc_orientation(RotorDiscStateVector& x) noexcept;  // between two RK4 steps

// Slow states; they are advanced by bemt_post_step() only.
struct BemtFilterState {
    ElementArray<Real> Vi_raw_previous{};   // [m/s] last solution of the blade element balance (Newton warm start); 0 = cold start
    ElementArray<Real> Vi_lag{};            // [m/s] Pitt-Peters lagged induced velocity of every element
    bool lag_initialized = false;
    Real v_johnson_lagged = 0;              // [m/s] lagged Johnson vortex ring inflow
    Real vrs_strength = 0;                  // [0, 1] PT1 lagged vortex ring strength (strength_vrs * use_vrs)
    TurbulenceState turbulence{};
    Real wake_velocity = 0;                 // [m/s] mean induced velocity of the last step, published for the fuselage aerodynamics (one step delay)
};

struct BemtInputs {
    BodyKinematics body{};
    RotorDiscStateVector disc{};                    // RK4 state
    Real omega_shaft = 0;                           // [rad/s] rotor speed (drivetrain)
    std::array<Real, kBemtAzimuthCount> blade_pitch{};  // [rad] beta(psi_c) of the virtual blades (swashplate kinematics / CCPM mixer)
    Vec3 wind_O{{0, 0, 0}};                         // [m/s]
    Real rho_air = 1.27;                            // [kg/m^3]
    Real mass_total = 1;                            // [kg] helicopter mass (hover induced velocity that scales the vortex ring model)
    GroundEffectInput ground{};
};

// ----------------------------------------------------------------------------------------------------------------
// results of one evaluation, structure of arrays over the grid
// ----------------------------------------------------------------------------------------------------------------
struct BemtGrid {                       // constant geometry of the grid
    ElementArray<Real> radius{};        // [m]
    ElementArray<Real> psi{};           // [rad] azimuth of the virtual blade
    Real segment_length = 0;            // [m] radial width of one ring
    Real sector_angle = 0;              // [rad]
};
[[nodiscard]] BemtGrid make_bemt_grid(const BemtConfig& cfg) noexcept;

struct BemtOutputs {
    // per element
    ElementArray<Real> beta{};          // [rad] local blade pitch beta(psi)
    ElementArray<Real> Vc{};            // [m/s] axial air velocity of the element along the thrust direction (climb)
    ElementArray<Real> V2{};            // [m/s] tangential velocity of the element (negative for a normally turning rotor)
    ElementArray<Real> Vi_raw{};        // [m/s] blade element momentum solution
    ElementArray<Real> Vi_target{};     // [m/s] after the vortex ring blend: where the inflow lag is heading
    ElementArray<Real> Vi{};            // [m/s] induced velocity used for the blade forces
    ElementArray<Real> V0{};            // [m/s] axial inflow, Vi + Vc
    ElementArray<Real> phi{};           // [rad] inflow angle
    ElementArray<Real> alpha{};         // [rad] angle of attack
    ElementArray<Real> cl{};
    ElementArray<Real> cd{};
    ElementArray<Real> dL{};            // [N/m] lift per unit radius (all blades, one azimuth sector)
    ElementArray<Real> dD{};            // [N/m] drag
    ElementArray<Real> dT{};            // [N/m] thrust grading, dL cos(phi) - dD sin(phi), with tip loss
    ElementArray<Real> dQ{};            // [N/m] torque grading, dD cos(phi) + dL sin(phi)
    ElementArray<Real> vrs_weight{};    // [0, 1] weight of the Johnson inflow in Vi_target
    ElementArray<Real> residual{};      // [N] of the selected solution
    ElementArray<std::uint8_t> iterations{};

    // geometry and forces of the elements in the world frame (debug display of the host application)
    ElementArray<Vec3> position_O{};        // [m] element position
    ElementArray<Vec3> induced_velocity_O{};  // [m/s] induced velocity of the element: Vi along the negative disc axis (the green lines of the Unity debug display)
    ElementArray<Vec3> force_thrust_O{};    // [N] thrust force of the element (all blades of its sector)
    ElementArray<Vec3> force_torque_O{};    // [N] tangential force of the element

    // disc
    Vec3 force_O{{0, 0, 0}};            // [N] rotor force on the disc, world frame (ground effect applied)
    Vec3 force_D{{0, 0, 0}};            // [N] same in the disc frame
    Vec3 torque_D{{0, 0, 0}};           // [Nm] aerodynamic moment about the hub in the disc frame (y: shaft torque, negative when driven)
    Mat3 A_O_norot{};                   // non rotating frame of the virtual blades -> world
    Vec3 torque_hub_on_body_L{{0, 0, 0}};   // [Nm] hub spring + damper torque on the fuselage, body frame
    Vec3 torque_hub_on_disc_D{{0, 0, 0}};   // [Nm] hub spring + damper torque on the disc, disc frame
    Real thrust = 0;                    // [N] |force|
    Real shaft_torque = 0;              // [Nm] torque_D.y
    Real Vi_mean = 0;                   // [m/s] area weighted mean of Vi
    Real horizontal_speed = 0;          // [m/s] air speed in the rotor plane
    Real ground_effect_factor = 1;

    // vortex ring state
    Real Vz_scaled = 0;
    Real Vx_scaled = 0;
    Real use_vrs = 0;                   // [0, 1] coherence of the descent over the disc
    Real strength_vrs = 0;              // [0, 1] weight of the Johnson correction (instantaneous, follows the sink rate)
    Real vrs_strength = 0;              // [0, 1] strength_vrs * use_vrs through the PT1 build-up / decay lag: reported strength and wake turbulence
    Real descent_blend_weight = 0;
    Real v_johnson_target = 0;          // [m/s] quasi static Johnson inflow (input of the lag filter)
    Real v_johnson_lagged = 0;          // [m/s] lagged value that is blended in
    Real hover_induced_velocity = 0;    // [m/s]
    Real turbulence_strength = 0;       // [0, 1]
    Real max_residual = 0;              // [N]
    int nonconverged_elements = 0;
};

// Every grid point as one CSV row (radial station, azimuth index, r, psi, beta, Vc, V2, Vi_raw, Vi_target, Vi, alpha, cl, cd, dL, dD,
// dT, dQ, VRS weight, residual, iterations), for the test harness and for plots. The first line is the header.
[[nodiscard]] std::string bemt_elements_csv(const BemtGrid& grid, const BemtOutputs& out);

[[nodiscard]] BemtOutputs bemt_evaluate(const BemtConfig& cfg, const BemtGrid& grid, const BemtFilterState& state, const BemtInputs& in) noexcept;

// Once per full step with the outputs of the evaluation at the start of the step (the first RK4 stage); `time` is the time at
// the end of the step.
void bemt_post_step(const BemtConfig& cfg, BemtFilterState& state, const BemtInputs& in, const BemtOutputs& out, Real time, Real dt, RotorRng& rng) noexcept;

// Time constant of the Pitt-Peters inflow lag of one element: tau = k 8 / (3 pi) R / U_local, with the through flow of the disc
// U_local = max(sqrt(horizontal_speed^2 + (Vi_lag + Vc)^2), 0.5 m/s). The blade speed V2 = Omega r is not part of it.
[[nodiscard]] Real pitt_peters_time_constant(Real fumble_factor, Real rotor_radius, Real horizontal_speed, Real Vi_lag, Real Vc) noexcept;

// Implicit Euler step of the inflow lag: unconditionally stable.
[[nodiscard]] Real pitt_peters_step(Real Vi_lag, Real Vi_target, Real dt, Real tau) noexcept;

// Derivative of the rotor disc state: rotation of the disc under the aerodynamic moment and the hub torque.
[[nodiscard]] RotorDiscStateVector rotor_disc_derivative(const BemtConfig& cfg, const RotorDiscStateVector& x, Real omega_shaft, const BemtOutputs& out) noexcept;

// Force and torque the rotor exerts on the fuselage (world force, body torque about the centre of mass): thrust (at the hub),
// aerodynamic shaft torque (not while freewheeling), hub spring and damper, turbulence of the wake.
struct BemtFuselageLoads {
    Vec3 force_O{{0, 0, 0}};
    Vec3 torque_L{{0, 0, 0}};
};
[[nodiscard]] BemtFuselageLoads bemt_loads_on_fuselage(const BemtConfig& cfg, const RotorGeometry& geometry, const BodyKinematics& body, const BemtFilterState& state,
                                                       const BemtOutputs& out, bool freewheeling) noexcept;

}  // namespace hoverhere::aero
