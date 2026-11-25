#include "hoverhere/simulation/helicopter_sim.hpp"

#include <algorithm>
#include <array>
#include <atomic>
#include <chrono>
#include <cmath>
#include <cstring>
#include <exception>
#include <mutex>
#include <thread>
#include <utility>

#include "hoverhere/aero/aerodynamics.hpp"
#include "hoverhere/aero/rotor_bemt.hpp"
#include "hoverhere/aero/rotor_classic.hpp"
#include "hoverhere/aero/rotor_common.hpp"
#include "hoverhere/collision/ground_collision.hpp"
#include "hoverhere/core/frames.hpp"
#include "hoverhere/dynamics/drivetrain.hpp"
#include "hoverhere/dynamics/rigid_body.hpp"
#include "hoverhere/kinematics/kinematics.hpp"
#include "hoverhere/numerics/fixed_step_clock.hpp"
#include "hoverhere/numerics/rk4.hpp"
#include "hoverhere/numerics/smooth_step.hpp"

namespace hoverhere::simulation {

namespace qvm = boost::qvm;
using math::Mat3;
using math::Quat;
using math::Vec3;
namespace drv = dynamics::drv;

// ---------------------------------------------------------------------------------------------------------------------------------------
// defaults of the C structs
// ---------------------------------------------------------------------------------------------------------------------------------------
hh_sim_options default_sim_options() noexcept
{
    hh_sim_options o{};
    o.struct_size = sizeof(hh_sim_options);
    o.rotor_model = HH_ROTOR_BEMT;
    o.dt = 0.001;
    o.max_frame_dt = 0.25;
    o.time_scale = 1.0;
    o.control_delay_after_reset = 0;
    o.seed = 20240427;
    o.max_substeps = 1000;
    o.bemt_flags = 0;
    return o;
}

hh_reset_params default_reset_params() noexcept
{
    hh_reset_params r{};
    r.struct_size = sizeof(hh_reset_params);
    r.rotor_spooled_up = 1;
    r.position_O[1] = 1.0;
    r.orientation[0] = 1.0;
    return r;
}

hh_sim_inputs default_sim_inputs() noexcept
{
    hh_sim_inputs i{};
    i.struct_size = sizeof(hh_sim_inputs);
    i.motor_enabled = 1;
    return i;
}

namespace {

// ---------------------------------------------------------------------------------------------------------------------------------------
// state vector of the RK4: body 13, drivetrain 5, flapping of main and second rotor 2 + 2, rotor disc 6
// ---------------------------------------------------------------------------------------------------------------------------------------
constexpr std::size_t kStateSize = 28;
using SimState = numerics::StateVector<kStateSize>;
namespace idx {
constexpr std::size_t body = 0;
constexpr std::size_t drive = 13;
constexpr std::size_t flap_main = 18;
constexpr std::size_t flap_tail = 20;
constexpr std::size_t disc = 22;
}  // namespace idx
static_assert(idx::disc + 6 == kStateSize);

template <std::size_t N>
[[nodiscard]] numerics::StateVector<N> take(const SimState& s, const std::size_t offset) noexcept
{
    numerics::StateVector<N> v{};
    for (std::size_t i = 0; i < N; ++i) {
        v[i] = s[offset + i];
    }
    return v;
}

template <std::size_t N>
void put(SimState& s, const std::size_t offset, const numerics::StateVector<N>& v) noexcept
{
    for (std::size_t i = 0; i < N; ++i) {
        s[offset + i] = v[i];
    }
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// lock free triple buffer (one writer thread, one reader thread)
// ---------------------------------------------------------------------------------------------------------------------------------------
template <class T>
class TripleBuffer {
public:
    TripleBuffer() : slots_(std::make_unique<std::array<T, 3>>()) {}

    // writer
    [[nodiscard]] T& write_slot() noexcept { return (*slots_)[back_]; }
    void publish() noexcept
    {
        back_ = middle_.exchange(back_ | kDirty, std::memory_order_acq_rel) & kIndexMask;
        published_.store(true, std::memory_order_release);
    }

    // reader: true when a newer value than the last acquired one has arrived
    bool acquire() noexcept
    {
        if ((middle_.load(std::memory_order_acquire) & kDirty) == 0) {
            return false;
        }
        front_ = middle_.exchange(front_, std::memory_order_acq_rel) & kIndexMask;
        return true;
    }
    [[nodiscard]] const T& read_slot() const noexcept { return (*slots_)[front_]; }
    [[nodiscard]] bool has_published() const noexcept { return published_.load(std::memory_order_acquire); }
    void clear() noexcept { published_.store(false, std::memory_order_release); }

private:
    static constexpr unsigned kDirty = 4;
    static constexpr unsigned kIndexMask = 3;
    std::unique_ptr<std::array<T, 3>> slots_;
    unsigned back_ = 0;                  // writer side
    unsigned front_ = 2;                 // reader side
    std::atomic<unsigned> middle_{1};    // index of the middle slot, plus the dirty bit
    std::atomic<bool> published_{false};
};

[[nodiscard]] Vec3 to_vec(const double (&a)[3]) noexcept
{
    return Vec3{{a[0], a[1], a[2]}};
}

void from_vec(const Vec3& v, double (&a)[3]) noexcept
{
    a[0] = v.a[0];
    a[1] = v.a[1];
    a[2] = v.a[2];
}

template <std::size_t R, std::size_t C>
void from_vec_array(const std::array<Vec3, R>& v, double (&a)[R][C]) noexcept
{
    for (std::size_t i = 0; i < R; ++i) {
        a[i][0] = v[i].a[0];
        a[i][1] = v[i].a[1];
        a[i][2] = v[i].a[2];
    }
}

void from_mat(const Mat3& m, double (&a)[9]) noexcept
{
    for (std::size_t r = 0; r < 3; ++r) {
        for (std::size_t c = 0; c < 3; ++c) {
            a[3 * r + c] = m.a[r][c];
        }
    }
}

[[nodiscard]] bool all_finite(const SimState& s) noexcept
{
    return std::all_of(s.begin(), s.end(), [](const Real v) { return std::isfinite(v); });
}

constexpr Real kUpwardsDistance = 1000;

}  // namespace

// ---------------------------------------------------------------------------------------------------------------------------------------
// Impl
// ---------------------------------------------------------------------------------------------------------------------------------------
struct HelicopterSim::Impl {
    // What is held constant over one step.
    struct StepHold {
        Real omega_mo_target = 0;                      // [rad/s] soft started motor speed setpoint
        bool motor_enabled = true;
        Real stick_propeller = 0;
        Real stick_booster = 0;
        std::array<Real, collision::kGearGroups> gear_raise{};
        Vec3 wind_O{{0, 0, 0}};
    };

    // Rotor controls that follow from the servo positions.
    struct ControlCache {
        std::array<Real, kinematics::kVirtualBladeCount> blade_pitch{};
        aero::RotorControl main{};
        aero::RotorControl tail{};
        aero::RotorControl propeller{};
        kinematics::SwashplateCommand swashplate{};
    };

    // Result of one evaluation of the whole model.
    struct StageEvaluation {
        collision::ContactInputs contact_in{};
        collision::ContactEvaluation contact{};
        aero::AerodynamicsResult aero{};
        bool main_classic = false;
        bool tail_active = false;
        bool propeller_active = false;
        aero::ClassicRotorInputs main_in{}, tail_in{}, propeller_in{};
        aero::ClassicRotorOutputs main_out{}, tail_out{}, propeller_out{};
        aero::BemtInputs bemt_in{};
        aero::BemtOutputs bemt_out{};
        Vec3 main_force_O{{0, 0, 0}};
        Vec3 main_torque_L{{0, 0, 0}};
        Vec3 tail_force_O{{0, 0, 0}};
        Vec3 tail_torque_L{{0, 0, 0}};
        Vec3 propeller_force_O{{0, 0, 0}};
        Vec3 propeller_torque_L{{0, 0, 0}};
        Real booster_thrust = 0;
        int booster_count = 0;
        std::array<Vec3, 2> booster_force_L{};
        std::array<Vec3, 2> booster_position_L{};
        Vec3 unbalance_force_O{{0, 0, 0}};
        dynamics::DrivetrainInputs drive_in{};
        dynamics::DrivetrainEvaluation drive{};
        dynamics::RigidBodyLoads loads{};
        Vec3 specific_force_L{{0, 0, 0}};
        bool freewheeling = false;
        SimState derivative{};
    };

    // ---- configuration -------------------------------------------------------------------------------------------------------------
    bool configured = false;
    SimSetup setup;
    hh_sim_options options{};
    params::ModelParamsSI p{};
    dynamics::RigidBodyParams body_p{};
    dynamics::DrivetrainParams drive_p{};
    aero::ClassicRotorConfig cfg_main{}, cfg_tail{}, cfg_prop{};
    aero::BemtConfig cfg_bemt{};
    aero::BemtGrid bemt_grid{};
    aero::RotorGeometry geo_main{}, geo_tail{}, geo_prop{};
    aero::AerodynamicsConfig aero_cfg{};
    control::FblConfig fbl_cfg{};
    kinematics::CcpmMixer mixer{};
    std::optional<params::mechanics::MainrotorMechanics> mech_main;
    std::optional<params::mechanics::TailrotorMechanics> mech_tail;
    bool bemt_main = false;
    bool tandem = false;
    bool main_exists = true;
    std::array<Real, 6> channel_tau{};       // PT1 of the normalized channels (tandem rotor)
    collision::GroundCollision collision;
    std::unique_ptr<control::IFlightController> controller;
    Real dt = 0.001;

    // ---- state ---------------------------------------------------------------------------------------------------------------------
    SimState x{};
    SimState x_prev{};                       // state before the last step
    Real alpha = 1;                          // [0, 1] position of the rendered state between x_prev and x (rest of the frame time in the clock)
    Real t = 0;
    std::uint64_t steps = 0;
    bool diverged = false;
    numerics::FixedStepClock clock;
    aero::RotorRng rng{};
    aero::ClassicRotorFilterState filter_main{}, filter_tail{}, filter_prop{};
    aero::BemtFilterState filter_bemt{};
    control::ServoBank servos;
    std::array<Real, 6> channels{};          // normalized command channels after the PT1 (tandem rotor): col mr, lat mr, lon mr, col tr, lat tr, lon tr
    control::ControllerOutputs fc_out{};
    Real soft_target = 0;                    // [rad/s] soft started setpoint of the motor speed
    bool motor_was_enabled = true;
    bool soft_start_fast = false;
    Real motor_stopped_time = -1e9;
    bool unbalance_falling = false;
    Real omega_mr_old = 0;
    Vec3 accel_L{{0, 0, 0}};
    StepHold last_hold{};
    ControlCache last_control{};
    StageEvaluation nominal{};               // evaluation at the start of the last step (the data of the published outputs)
    bool has_nominal = false;
    hh_sim_inputs last_inputs{};

    // ---- threading -----------------------------------------------------------------------------------------------------------------
    mutable std::mutex mutex;
    std::thread worker;
    std::atomic<bool> stop_requested{false};
    std::atomic<bool> running{false};
    TripleBuffer<hh_sim_inputs> input_buffer;
    TripleBuffer<hh_sim_outputs> output_buffer;

    Impl() = default;
    ~Impl() { stop_thread(); }

    bool build(const SimSetup& s, const hh_sim_options& o, std::string* error);
    bool derive(const SimSetup& s, std::string* error);
    bool update(const SimSetup& s, std::string* error);
    void reset(const hh_reset_params& r);
    void step(const hh_sim_inputs& in);
    int advance(double frame_dt, const hh_sim_inputs& in);
    void fill_outputs(hh_sim_outputs& out) const;
    bool fill_bemt(hh_bemt_telemetry& out) const;

    void evaluate(const SimState& s, const StepHold& hold, const ControlCache& cc, StageEvaluation& e) const noexcept;
    [[nodiscard]] ControlCache make_control_cache(const StepHold& hold, const std::array<Real, 4>& sigma4) const noexcept;
    [[nodiscard]] SimState render_state() const noexcept;
    [[nodiscard]] kinematics::SwashplateCommand swashplate_command(const std::array<Real, 3>& sigma) const noexcept;
    [[nodiscard]] control::ControllerInputs controller_inputs(const hh_sim_inputs& in, Real fade) const noexcept;
    void post_step(const StageEvaluation& e0, const StepHold& hold, const control::ControllerInputs& ci, const hh_sim_inputs& in);
    void fill_mechanics(hh_sim_outputs& out) const;

    bool start_thread();
    void stop_thread();
    void thread_main();
};

// ---------------------------------------------------------------------------------------------------------------------------------------
// configuration
// ---------------------------------------------------------------------------------------------------------------------------------------
bool HelicopterSim::Impl::build(const SimSetup& s, const hh_sim_options& o, std::string* error)
{
    const auto fail = [error](const char* message) {
        if (error != nullptr) {
            *error = message;
        }
        return false;
    };
    stop_thread();
    configured = false;
    options = o;
    if (options.dt <= 0 || options.dt > 0.02) {
        return fail("dt has to be in (0, 0.02] s");
    }
    if (!(options.time_scale >= 0) || !std::isfinite(options.time_scale)) {
        return fail("time_scale has to be a finite number >= 0");
    }
    if (!derive(s, error)) {
        return false;
    }
    const auto& h = p.helicopter;
    controller = std::make_unique<control::FblController>(fbl_cfg, mech_main, control::make_fallback_ccpm_mixer(h, fbl_cfg));
    servos = control::ServoBank(control::servo_time_constants(h.servo_damping));

    numerics::ClockConfig cc;
    cc.dt = options.dt;
    cc.max_substeps = options.max_substeps > 0 ? options.max_substeps : 1000;
    cc.max_frame_dt = options.max_frame_dt > 0 ? options.max_frame_dt : 0.25;
    cc.time_scale = options.time_scale;
    clock = numerics::FixedStepClock(cc);
    dt = clock.dt();

    configured = true;
    has_nominal = false;
    return true;
}

// Everything that follows from the parameters (SI parameters, rotor and aerodynamics configurations, mechanism, collision points). Validates
// before it changes anything, so a failing update leaves the running model untouched.
bool HelicopterSim::Impl::derive(const SimSetup& s, std::string* error)
{
    const auto fail = [error](const char* message) {
        if (error != nullptr) {
            *error = message;
        }
        return false;
    };
    const params::ModelParamsSI new_p = params::compile_to_si(s.params);
    const auto& h = new_p.helicopter;
    const dynamics::RigidBodyParams new_body = dynamics::make_rigid_body_params(h.mass_total, h.Jxx_Jyy_Jzz, h.Jxy_Jxz_Jyz);
    if (!dynamics::is_valid(new_body)) {
        return fail("mass and inertia tensor do not describe a physical body");
    }
    const dynamics::DrivetrainParams new_drive = dynamics::make_drivetrain_params(h);
    if (!dynamics::is_valid(new_drive)) {
        return fail("the drivetrain parameters are not valid (gear ratios, inertias or motor constants)");
    }
    const bool new_tandem = h.rotor_systems_configuration == 1;
    const bool new_main_exists = h.mainrotor.rotor_exists;
    const bool new_bemt_main = options.rotor_model == HH_ROTOR_BEMT && !new_tandem && new_main_exists;
    if (configured && (new_tandem != tandem || new_main_exists != main_exists || new_bemt_main != bemt_main)) {
        return fail("the rotor system changed, the model has to be rebuilt with a reset");
    }

    aero::BemtConfig new_cfg_bemt{};
    try {
        new_cfg_bemt = aero::make_bemt_config(new_p);
    } catch (const std::exception& e) {
        if (error != nullptr) {
            *error = e.what();
        }
        return false;
    }

    setup = s;
    p = new_p;
    body_p = new_body;
    drive_p = new_drive;
    tandem = new_tandem;
    main_exists = h.mainrotor.rotor_exists;
    bemt_main = options.rotor_model == HH_ROTOR_BEMT && !tandem && main_exists;
    cfg_main = aero::make_classic_rotor_config(p, aero::RotorKind::Mainrotor);
    cfg_tail = aero::make_classic_rotor_config(p, aero::RotorKind::Tailrotor);
    cfg_prop = aero::make_classic_rotor_config(p, aero::RotorKind::Propeller);
    cfg_bemt = std::move(new_cfg_bemt);
    cfg_bemt.vortex_ring_state = (options.bemt_flags & HH_BEMT_NO_VORTEX_RING) == 0;
    cfg_bemt.ground_effect = (options.bemt_flags & HH_BEMT_NO_GROUND_EFFECT) == 0;
    cfg_bemt.turbulence = (options.bemt_flags & HH_BEMT_NO_TURBULENCE) == 0;
    cfg_bemt.dynamic_inflow = (options.bemt_flags & HH_BEMT_NO_DYNAMIC_INFLOW) == 0;
    cfg_bemt.quasi_static = (options.bemt_flags & HH_BEMT_QUASI_STATIC) != 0;
    bemt_grid = aero::make_bemt_grid(cfg_bemt);
    geo_main = aero::make_rotor_geometry(aero::as_rotor(h.mainrotor));
    geo_tail = aero::make_rotor_geometry(h.tailrotor);
    geo_prop = aero::make_rotor_geometry(h.propeller);
    aero_cfg = aero::make_aerodynamics_config(h);

    fbl_cfg = control::make_fbl_config(p);
    mech_main = s.mainrotor_mechanics;
    mech_tail = s.tailrotor_mechanics;
    const kinematics::CcpmMixer fallback = control::make_fallback_ccpm_mixer(h, fbl_cfg);
    mixer = mech_main ? kinematics::calibrate_ccpm_mixer(*mech_main) : fallback;

    const auto& sd = h.servo_damping;
    channel_tau = {sd.servo_col_mr_time_constant, sd.servo_lat_mr_time_constant, sd.servo_lon_mr_time_constant,
                   sd.servo_col_tr_time_constant, sd.servo_lat_tr_time_constant, sd.servo_lon_tr_time_constant};
    for (Real& tau : channel_tau) {
        tau = tau > Real{0.005} ? tau : Real{0};  // below 5 ms the servo is treated as instantaneous (as in Unity)
    }

    collision.set_config(collision::make_collision_config(p));
    return true;
}

// New parameters for the running simulation: state, filters, controller integrators and servo positions are kept.
bool HelicopterSim::Impl::update(const SimSetup& s, std::string* error)
{
    if (!configured) {
        if (error != nullptr) {
            *error = "the simulation is not configured yet";
        }
        return false;
    }
    if (!derive(s, error)) {
        return false;
    }
    const auto& h = p.helicopter;
    if (auto* fbl = dynamic_cast<control::FblController*>(controller.get())) {
        fbl->update_config(fbl_cfg, mech_main, control::make_fallback_ccpm_mixer(h, fbl_cfg));
    }
    control::ServoBank new_servos(control::servo_time_constants(h.servo_damping));
    new_servos.reset(servos.sigma());
    servos = new_servos;
    return true;
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// control chain: sticks -> controller -> servos -> rotor controls
// ---------------------------------------------------------------------------------------------------------------------------------------
kinematics::SwashplateCommand HelicopterSim::Impl::swashplate_command(const std::array<Real, 3>& sigma) const noexcept
{
    if (mech_main) {
        const kinematics::SwashplatePose pose = kinematics::step2_servos_to_swashplate(*mech_main, sigma);
        return kinematics::swashplate_command_from_pose(*mech_main, pose);
    }
    return kinematics::ccpm_swashplate(mixer, sigma);
}

HelicopterSim::Impl::ControlCache HelicopterSim::Impl::make_control_cache(const StepHold& hold, const std::array<Real, 4>& sigma4) const noexcept
{
    ControlCache cc;
    const auto& h = p.helicopter;
    const std::array<Real, 3> sigma{sigma4[0], sigma4[1], sigma4[2]};

    if (tandem) {
        // no servo kinematics: the normalized commands drive both rotors like the Unity code does
        cc.main.Theta_col = h.mainrotor.K_col * channels[0] + h.mainrotor.Theta_col_0;
        cc.main.Theta_cyc_b_s = h.flapping.B_lat * channels[1];
        cc.main.Theta_cyc_a_s = h.flapping.A_lon * channels[2];
        cc.tail.Theta_col = h.tailrotor.K_col * channels[3] + h.tailrotor.Theta_col_0;
        cc.tail.Theta_cyc_b_s = h.flapping.B_lat * channels[4];
        cc.tail.Theta_cyc_a_s = h.flapping.A_lon * channels[5];
    } else {
        cc.swashplate = swashplate_command(sigma);
        if (bemt_main) {
            cc.blade_pitch = control::blade_pitch_from_servos(mech_main, mixer, sigma);
        } else {
            cc.main = aero::classic_control_from_ccpm(mixer, cc.swashplate);
            for (std::size_t c = 0; c < kinematics::kVirtualBladeCount; ++c) {
                cc.blade_pitch[c] = kinematics::ccpm_pitch_at_azimuth(
                    mixer, cc.swashplate, static_cast<Real>(c) * 2 * frames::kPi / static_cast<Real>(kinematics::kVirtualBladeCount));
            }
        }
        if (mech_tail) {
            const kinematics::TailrotorPose pose = kinematics::tailrotor_forward_kinematics(*mech_tail, sigma4[3], 0);
            cc.tail = aero::classic_control_from_tailrotor(pose, h.tailrotor.Theta_col_0);
        } else {
            const Real deflection = fbl_cfg.tail_throw != 0 ? sigma4[3] / fbl_cfg.tail_throw : Real{0};
            cc.tail.Theta_col = h.tailrotor.K_col * deflection + h.tailrotor.Theta_col_0;
        }
    }
    cc.propeller.Theta_col = h.propeller.K_col * hold.stick_propeller + h.propeller.Theta_col_0;
    return cc;
}

control::ControllerInputs HelicopterSim::Impl::controller_inputs(const hh_sim_inputs& in, const Real fade) const noexcept
{
    control::ControllerInputs ci;
    ci.imu.gyro_B = dynamics::angular_velocity_L(take<13>(x, idx::body));
    ci.imu.accel_B = accel_L;
    ci.omega_mr = x[idx::drive + drv::omega_mr];
    ci.rc.collective = in.collective * fade;
    ci.rc.yaw = in.yaw * fade;
    ci.rc.pitch = in.pitch * fade;
    ci.rc.roll = in.roll * fade;
    ci.rc.propeller = in.propeller * fade;
    ci.rc.booster = in.booster * fade;
    ci.rc.flight_bank = std::clamp(in.flight_bank, 0, 2);
    ci.rc.motor_enabled = in.motor_enabled != 0;
    return ci;
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// the model: a pure function of the state, the held inputs and the memory
// ---------------------------------------------------------------------------------------------------------------------------------------
void HelicopterSim::Impl::evaluate(const SimState& s, const StepHold& hold, const ControlCache& cc, StageEvaluation& e) const noexcept
{
    const auto& h = p.helicopter;
    const dynamics::RigidBodyState rb = take<13>(s, idx::body);
    const dynamics::DrivetrainState dr = take<5>(s, idx::drive);
    const aero::BodyKinematics body = aero::body_kinematics(rb);
    const Real rho = p.scenery.weather.rho_air;
    const Real omega_mr = dr[drv::omega_mr];
    const Real omega_tr = drive_p.n_mr2tr != 0 ? omega_mr / drive_p.n_mr2tr : Real{0};
    const Real omega_pr = drive_p.n_mr2pr != 0 ? omega_mr / drive_p.n_mr2pr : Real{0};
    e.freewheeling = dynamics::is_freewheeling(drive_p, dr);

    // ground contact
    e.contact_in.position_O = body.position_O;
    e.contact_in.A_OL = body.A_OL;
    e.contact_in.velocity_O = body.velocity_O;
    e.contact_in.omega_L = body.omega_L;
    e.contact_in.gear_raise = hold.gear_raise;
    collision.evaluate(e.contact_in, e.contact);

    // fuselage, fins and wings in the wake of the rotors of the last step
    aero::RotorWake wake{};
    wake.mainrotor = bemt_main ? filter_bemt.wake_velocity : filter_main.wake_velocity;
    wake.tailrotor = filter_tail.wake_velocity;
    wake.propeller = filter_prop.wake_velocity;
    e.aero = aero::aerodynamics_evaluate(aero_cfg, body, hold.wind_O, rho, wake);

    Vec3 force_O = e.contact.force_O + e.aero.force_O;
    Vec3 torque_L = e.contact.torque_L + e.aero.torque_L;
    Real torque_mr = 0;
    Real torque_tr = 0;
    Real torque_pr = 0;

    // main rotor
    e.main_classic = false;
    e.main_force_O = Vec3{{0, 0, 0}};
    e.main_torque_L = Vec3{{0, 0, 0}};
    numerics::StateVector<6> disc_derivative{};
    if (main_exists && bemt_main) {
        e.bemt_in = aero::BemtInputs{};
        e.bemt_in.body = body;
        e.bemt_in.disc = take<6>(s, idx::disc);
        e.bemt_in.omega_shaft = omega_mr;
        e.bemt_in.blade_pitch = cc.blade_pitch;
        e.bemt_in.wind_O = hold.wind_O;
        e.bemt_in.rho_air = rho;
        e.bemt_in.mass_total = h.mass_total;
        e.bemt_in.ground.hub_distance_to_ground = e.contact.hub_distance[0];
        e.bemt_in.ground.surface_normal_O = e.contact.hub_normal_O[0];
        e.bemt_out = aero::bemt_evaluate(cfg_bemt, bemt_grid, filter_bemt, e.bemt_in);
        const aero::BemtFuselageLoads loads = aero::bemt_loads_on_fuselage(cfg_bemt, geo_main, body, filter_bemt, e.bemt_out, e.freewheeling);
        e.main_force_O = loads.force_O;
        e.main_torque_L = loads.torque_L;
        torque_mr = e.bemt_out.shaft_torque;
        disc_derivative = aero::rotor_disc_derivative(cfg_bemt, e.bemt_in.disc, omega_mr, e.bemt_out);
    } else if (main_exists) {
        e.main_classic = true;
        e.main_in = aero::ClassicRotorInputs{};
        e.main_in.body = body;
        e.main_in.wind_O = hold.wind_O;
        e.main_in.rho_air = rho;
        e.main_in.omega_shaft = omega_mr;
        e.main_in.control = cc.main;
        e.main_in.flapping = take<2>(s, idx::flap_main);
        e.main_in.force_fuselage_L = e.aero.fuselage_force_L;
        e.main_in.ground.hub_distance_to_ground = e.contact.hub_distance[0];
        e.main_in.ground.surface_normal_O = e.contact.hub_normal_O[0];
        e.main_in.freewheeling = e.freewheeling;
        e.main_out = aero::classic_rotor_evaluate(cfg_main, filter_main, e.main_in);
        e.main_force_O = e.main_out.force_O;
        e.main_torque_L = e.main_out.torque_L;
        torque_mr = e.main_out.torque;
    }
    force_O = force_O + e.main_force_O;
    torque_L = torque_L + e.main_torque_L;

    // tail rotor (the second rotor of a tandem helicopter)
    e.tail_active = h.tailrotor.rotor_exists;
    e.tail_force_O = Vec3{{0, 0, 0}};
    e.tail_torque_L = Vec3{{0, 0, 0}};
    if (e.tail_active) {
        e.tail_in = aero::ClassicRotorInputs{};
        e.tail_in.body = body;
        e.tail_in.wind_O = hold.wind_O;
        e.tail_in.rho_air = rho;
        e.tail_in.omega_shaft = omega_tr;
        e.tail_in.control = cc.tail;
        e.tail_in.flapping = take<2>(s, idx::flap_tail);
        e.tail_in.force_fuselage_L = e.aero.fuselage_force_L;
        e.tail_in.ground.hub_distance_to_ground = e.contact.hub_distance[1];
        e.tail_in.ground.surface_normal_O = e.contact.hub_normal_O[1];
        e.tail_in.freewheeling = e.freewheeling;
        e.tail_out = aero::classic_rotor_evaluate(cfg_tail, filter_tail, e.tail_in);
        e.tail_force_O = e.tail_out.force_O;
        e.tail_torque_L = e.tail_out.torque_L;
        torque_tr = e.tail_out.torque;
    }
    force_O = force_O + e.tail_force_O;
    torque_L = torque_L + e.tail_torque_L;

    // pusher propeller
    e.propeller_active = h.propeller.rotor_exists;
    e.propeller_force_O = Vec3{{0, 0, 0}};
    e.propeller_torque_L = Vec3{{0, 0, 0}};
    if (e.propeller_active) {
        e.propeller_in = aero::ClassicRotorInputs{};
        e.propeller_in.body = body;
        e.propeller_in.wind_O = hold.wind_O;
        e.propeller_in.rho_air = rho;
        e.propeller_in.omega_shaft = omega_pr;
        e.propeller_in.control = cc.propeller;
        e.propeller_in.force_fuselage_L = e.aero.fuselage_force_L;
        e.propeller_in.ground.hub_distance_to_ground = kUpwardsDistance;
        e.propeller_in.ground.surface_normal_O = Vec3{{0, 1, 0}};
        e.propeller_in.freewheeling = e.freewheeling;
        e.propeller_out = aero::classic_rotor_evaluate(cfg_prop, filter_prop, e.propeller_in);
        e.propeller_force_O = e.propeller_out.force_O;
        e.propeller_torque_L = e.propeller_out.torque_L;
        torque_pr = e.propeller_out.torque;
    }
    force_O = force_O + e.propeller_force_O;
    torque_L = torque_L + e.propeller_torque_L;

    // booster (Airwolf): thrust along its axis, a mirrored twin when symmetric
    e.booster_thrust = 0;
    e.booster_count = 0;
    if (h.booster.booster_exists) {
        e.booster_thrust = h.booster.thrust * hold.stick_booster;
        const int count = h.booster.booster_symmetric ? 2 : 1;
        for (int i = 0; i < count; ++i) {
            const Vec3 scale{{1, 1, i == 0 ? Real{1} : Real{-1}}};
            const Vec3 dir{{h.booster.dirLH.a[0] * scale.a[0], h.booster.dirLH.a[1] * scale.a[1], h.booster.dirLH.a[2] * scale.a[2]}};
            const Vec3 pos{{h.booster.posLH.a[0] * scale.a[0], h.booster.posLH.a[1] * scale.a[1], h.booster.posLH.a[2] * scale.a[2]}};
            const Vec3 force_L = dir * e.booster_thrust;
            e.booster_force_L[static_cast<std::size_t>(i)] = force_L;
            e.booster_position_L[static_cast<std::size_t>(i)] = pos;
            e.booster_count = count;
            force_O = force_O + body.A_OL * force_L;
            torque_L = torque_L + qvm::cross(pos, force_L);
        }
    }

    // rotating unbalance of the main rotor during the start (vibration that fades out above the turn-off speed)
    e.unbalance_force_O = Vec3{{0, 0, 0}};
    {
        const Real turn_off = p.tuning.startup_rotating_unbalance.turn_off_rpm;
        const Real speed = std::abs(omega_mr);
        if (main_exists && turn_off > 0 && speed < turn_off && speed > Real{0.1}) {
            Real magnitude = p.tuning.startup_rotating_unbalance.mass * speed * speed;
            if (unbalance_falling) {
                magnitude *= Real{0.1};  // only a rising speed shakes
            }
            magnitude *= numerics::smooth_step(speed, turn_off * Real{0.9}, 1, turn_off, 0);
            const Real angle = dr[drv::Omega_mr];
            const Vec3 force_L = geo_main.A_LR * Vec3{{std::cos(angle) * magnitude, 0, std::sin(angle) * magnitude}};
            e.unbalance_force_O = body.A_OL * force_L;
            force_O = force_O + e.unbalance_force_O;
            torque_L = torque_L + qvm::cross(geo_main.position_L, force_L);
        }
    }

    // everything but gravity is what an accelerometer feels
    e.specific_force_L = frames::transposed(body.A_OL) * force_O * (1 / h.mass_total);

    force_O = force_O + Vec3{{0, -h.mass_total * p.scenery.gravity, 0}};
    e.loads = dynamics::RigidBodyLoads{force_O, torque_L};

    // drivetrain
    e.drive_in = dynamics::DrivetrainInputs{};
    e.drive_in.omega_mo_target = hold.omega_mo_target;
    e.drive_in.motor_enabled = hold.motor_enabled;
    e.drive_in.torque_mainrotor_mr = torque_mr;
    e.drive_in.torque_tailrotor_tr = torque_tr;
    e.drive_in.torque_propeller_pr = torque_pr;
    e.drive = dynamics::evaluate_drivetrain(drive_p, e.drive_in, dr);

    // derivative of the complete state
    e.derivative.fill(0);
    put<13>(e.derivative, idx::body, dynamics::rigid_body_derivative(body_p, rb, e.loads));
    put<5>(e.derivative, idx::drive, e.drive.derivative);
    if (e.main_classic) {
        put<2>(e.derivative, idx::flap_main, e.main_out.dflapping);
    }
    if (e.tail_active) {
        put<2>(e.derivative, idx::flap_tail, e.tail_out.dflapping);
    }
    if (main_exists && bemt_main) {
        put<6>(e.derivative, idx::disc, disc_derivative);
    }
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// reset
// ---------------------------------------------------------------------------------------------------------------------------------------
void HelicopterSim::Impl::reset(const hh_reset_params& r)
{
    if (!configured) {
        return;
    }
    const auto& h = p.helicopter;
    diverged = false;
    t = 0;
    steps = 0;
    clock.reset();
    rng = aero::RotorRng(options.seed);
    filter_main = filter_tail = filter_prop = aero::ClassicRotorFilterState{};
    filter_bemt = aero::BemtFilterState{};
    unbalance_falling = false;
    omega_mr_old = 0;
    accel_L = Vec3{{0, 0, 0}};
    last_inputs = default_sim_inputs();

    Quat q = frames::quat_identity();
    const double norm2 = r.orientation[0] * r.orientation[0] + r.orientation[1] * r.orientation[1] + r.orientation[2] * r.orientation[2] +
                         r.orientation[3] * r.orientation[3];
    if (norm2 > 1e-12) {
        q = frames::quat_normalized(Quat{{r.orientation[0], r.orientation[1], r.orientation[2], r.orientation[3]}});
    }
    x.fill(0);
    put<13>(x, idx::body, dynamics::make_rigid_body_state(to_vec(r.position_O), q, to_vec(r.velocity_O), Vec3{{0, 0, 0}}));
    const Mat3 A_OL = frames::A_RL(q);
    if (main_exists && bemt_main) {
        put<6>(x, idx::disc, aero::make_rotor_disc_state(frames::quat_from_A(A_OL * geo_main.A_LR), 0, 0));
    } else {
        put<6>(x, idx::disc, aero::make_rotor_disc_state(frames::quat_identity(), 0, 0));
    }

    // controller, servos at the neutral stick position
    controller->reset();
    control::ControllerInputs neutral;
    neutral.omega_mr = 0;
    neutral.rc.motor_enabled = true;
    fc_out = controller->evaluate(neutral, dt);
    servos.reset(fc_out.sigma);
    channels = {fc_out.normalized.collective_mr, fc_out.normalized.lateral_mr, fc_out.normalized.longitudinal_mr,
                fc_out.normalized.collective_tr, fc_out.normalized.lateral_tr, fc_out.normalized.longitudinal_tr};

    // drivetrain: at rest, or spooled up at the setpoint with the governor integral of a steady state (the Unity code solves the drivetrain
    // for 5 s with the loads of the initial state)
    const Real target = dynamics::motor_speed_target(drive_p, fc_out.governor.rotor_speed_target);
    motor_was_enabled = true;
    soft_start_fast = false;
    motor_stopped_time = -1e9;
    collision::ContactInputs ci;
    ci.position_O = to_vec(r.position_O);
    ci.A_OL = A_OL;
    collision.reset(ci);
    StepHold hold;
    hold.wind_O = aero::wind_velocity_O(p.scenery.weather);
    hold.motor_enabled = true;
    if (r.rotor_spooled_up != 0) {
        dynamics::DrivetrainState dr{};
        dr[drv::omega_mo] = target;
        dr[drv::omega_mr] = drive_p.n_mo2mr != 0 ? target / drive_p.n_mo2mr : Real{0};
        put<5>(x, idx::drive, dr);
        soft_target = target;
        hold.omega_mo_target = target;
        const ControlCache cc = make_control_cache(hold, servos.sigma());
        StageEvaluation e;
        evaluate(x, hold, cc, e);
        dynamics::DrivetrainInputs din = e.drive_in;
        const Real h_pre = 0.002;
        for (int i = 0; i < static_cast<int>(5 / h_pre); ++i) {
            const dynamics::DrivetrainState d = dynamics::drivetrain_derivative(drive_p, din, dr);
            for (std::size_t k = 0; k < dr.size(); ++k) {
                dr[k] += d[k] * h_pre;
            }
        }
        put<5>(x, idx::drive, dr);
    } else {
        soft_target = 0;
    }

    hold.omega_mo_target = soft_target;
    last_hold = hold;
    last_control = make_control_cache(hold, servos.sigma());
    evaluate(x, hold, last_control, nominal);
    x_prev = x;
    alpha = 1;
    has_nominal = true;
    accel_L = nominal.specific_force_L;
    (void)h;
    input_buffer.clear();
    output_buffer.clear();
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// one step
// ---------------------------------------------------------------------------------------------------------------------------------------
void HelicopterSim::Impl::step(const hh_sim_inputs& in)
{
    if (!configured || diverged || !has_nominal) {
        return;
    }
    const auto& h = p.helicopter;
    last_inputs = in;

    // sticks fade in after a reset
    Real fade = 1;
    const Real delay = std::abs(options.control_delay_after_reset);
    if (delay > 0 && t < delay) {
        fade = numerics::smooth_step(t, delay / 2, 0, delay, 1);
    }

    // controller (pure) and the held values of this step
    const control::ControllerInputs ci = controller_inputs(in, fade);
    fc_out = controller->evaluate(ci, dt);

    StepHold hold;
    hold.motor_enabled = ci.rc.motor_enabled && fc_out.governor.motor_enabled;
    if (!hold.motor_enabled && motor_was_enabled) {
        motor_stopped_time = t;
    }
    if (hold.motor_enabled && !motor_was_enabled) {
        soft_start_fast = (t - motor_stopped_time) <= h.governor.engine_restart_time;  // a quick restart spools up fast
    }
    if (hold.motor_enabled) {
        const Real target = dynamics::motor_speed_target(drive_p, fc_out.governor.rotor_speed_target);
        soft_target = dynamics::soft_start_step(soft_target, target, dt,
                                                soft_start_fast ? h.governor.engine_restart_factor : h.governor.soft_start_factor);
    } else {
        soft_target = 0;
    }
    motor_was_enabled = hold.motor_enabled;
    hold.omega_mo_target = soft_target;
    hold.stick_propeller = fc_out.normalized.collective_pr;
    hold.stick_booster = fc_out.booster;
    for (std::size_t g = 0; g < collision::kGearGroups; ++g) {
        hold.gear_raise[g] = std::clamp(in.gear_transition[g], 0.0, 1.0);
    }
    hold.wind_O = in.use_wind_override != 0 ? to_vec(in.wind_O) : aero::wind_velocity_O(p.scenery.weather);

    // the servo position held over the step is the one at the middle of the step
    const ControlCache cc = make_control_cache(hold, servos.advanced(fc_out.sigma, dt / 2));

    // stage 1 is the nominal evaluation of the start state
    StageEvaluation e0;
    evaluate(x, hold, cc, e0);

    const auto stage = [&](const Real, const SimState& s) {
        StageEvaluation e;
        evaluate(s, hold, cc, e);
        return e.derivative;
    };
    SimState next = numerics::rk4_step_with_k1(stage, t, x, e0.derivative, dt);
    if (!all_finite(next)) {
        diverged = true;
        return;
    }
    x_prev = x;
    x = next;
    t += dt;
    ++steps;
    post_step(e0, hold, ci, in);
    last_hold = hold;
    last_control = cc;
    nominal = e0;
}

void HelicopterSim::Impl::post_step(const StageEvaluation& e0, const StepHold& hold, const control::ControllerInputs& ci, const hh_sim_inputs& in)
{
    const auto& h = p.helicopter;

    // state projections between the steps
    dynamics::RigidBodyState rb = take<13>(x, idx::body);
    dynamics::normalize_orientation(rb);
    put<13>(x, idx::body, rb);
    aero::RotorDiscStateVector disc = take<6>(x, idx::disc);
    aero::normalize_disc_orientation(disc);
    put<6>(x, idx::disc, disc);

    dynamics::DrivetrainState dr = take<5>(x, idx::drive);
    dynamics::drivetrain_post_step(drive_p, e0.drive_in, dr);
    put<5>(x, idx::drive, dr);

    aero::FlappingState fm = take<2>(x, idx::flap_main);
    aero::FlappingState ft = take<2>(x, idx::flap_tail);
    const Real omega_mr = dr[drv::omega_mr];
    aero::limit_flapping_at_low_rpm(fm, omega_mr, cfg_main.omega_reference, cfg_main.hub.low_rpm_flapping_fade_upper, cfg_main.hub.low_rpm_flapping_fade_lower, dt);
    aero::limit_flapping_at_low_rpm(ft, drive_p.n_mr2tr != 0 ? omega_mr / drive_p.n_mr2tr : Real{0}, cfg_tail.omega_reference,
                                   cfg_tail.hub.low_rpm_flapping_fade_upper, cfg_tail.hub.low_rpm_flapping_fade_lower, dt);
    put<2>(x, idx::flap_main, fm);
    put<2>(x, idx::flap_tail, ft);

    // rotor filters with the nominal evaluation of the step that has just been taken
    if (main_exists && bemt_main) {
        aero::bemt_post_step(cfg_bemt, filter_bemt, e0.bemt_in, e0.bemt_out, t, dt, rng);
    } else if (main_exists) {
        aero::classic_rotor_post_step(cfg_main, filter_main, e0.main_in, e0.main_out, t, dt, rng);
    }
    if (e0.tail_active) {
        aero::classic_rotor_post_step(cfg_tail, filter_tail, e0.tail_in, e0.tail_out, t, dt, rng);
    }
    if (e0.propeller_active) {
        aero::classic_rotor_post_step(cfg_prop, filter_prop, e0.propeller_in, e0.propeller_out, t, dt, rng);
    }

    // controller memory and the actuator lags
    controller->post_step(ci, dt);
    servos.post_step(fc_out.sigma, dt);
    const std::array<Real, 6> command = {fc_out.normalized.collective_mr, fc_out.normalized.lateral_mr, fc_out.normalized.longitudinal_mr,
                                         fc_out.normalized.collective_tr, fc_out.normalized.lateral_tr, fc_out.normalized.longitudinal_tr};
    for (std::size_t i = 0; i < channels.size(); ++i) {
        channels[i] = kinematics::pt1_step(channels[i], command[i], dt, channel_tau[i]);
    }

    // ground contact memory
    collision::ContactStepControls controls;
    controls.dt = dt;
    controls.wheel_brake_target = in.wheel_brake;
    controls.wheels_raised = in.wheels_raised != 0;
    collision.post_step(e0.contact_in, e0.contact, controls);

    // the unbalance only shakes while the speed rises
    unbalance_falling = std::abs(omega_mr) < omega_mr_old;
    omega_mr_old = std::abs(omega_mr);
    accel_L = e0.specific_force_L;
    (void)h;
    (void)hold;
}

int HelicopterSim::Impl::advance(const double frame_dt, const hh_sim_inputs& in)
{
    if (!configured) {
        return 0;
    }
    const auto plan = clock.advance(frame_dt);
    for (std::int32_t i = 0; i < plan.steps; ++i) {
        step(in);
    }
    alpha = plan.alpha;  // the rendered state lies this far between the last two steps
    return plan.steps;
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// outputs
// ---------------------------------------------------------------------------------------------------------------------------------------
// The physics runs with a fixed step, the host renders with its own frame time. The part of the frame that is left in the clock decides where
// between the last two steps the displayed state lies; interpolating removes the jitter of 0 or 1 extra step per frame (a millimeter or
// more per frame at high speed) at the price of showing the state one step (1 ms) late.
SimState HelicopterSim::Impl::render_state() const noexcept
{
    if (!(alpha < 1) || !has_nominal) {
        return x;
    }
    SimState r{};
    for (std::size_t i = 0; i < kStateSize; ++i) {
        r[i] = x_prev[i] + (x[i] - x_prev[i]) * alpha;
    }
    return r;
}

void HelicopterSim::Impl::fill_mechanics(hh_sim_outputs& out) const
{
    out.mainrotor_mechanics = hh_mainrotor_mechanics{};
    out.tailrotor_mechanics = hh_tailrotor_mechanics{};
    const auto& sigma4 = servos.sigma();
    const std::array<Real, 3> sigma{sigma4[0], sigma4[1], sigma4[2]};
    const SimState xr = render_state();
    const dynamics::DrivetrainState dr = take<5>(xr, idx::drive);

    if (mech_main && !tandem) {
        const kinematics::SwashplatePose pose = kinematics::step2_servos_to_swashplate(*mech_main, sigma);
        const kinematics::RotatingBlades blades = kinematics::step3_swashplate_to_blades(*mech_main, pose, dr[drv::Omega_mr]);
        const auto drivers = kinematics::step4_driver_visuals(*mech_main, pose, blades, dr[drv::Omega_mr]);
        hh_mainrotor_mechanics& m = out.mainrotor_mechanics;
        from_vec_array(pose.r_BiO_O, m.servo_arm_B);
        from_vec_array(pose.r_CiO_O, m.rod_C);
        from_vec(pose.r_KO_O, m.swashplate_K);
        from_mat(pose.A_OLK, m.swashplate_A_OLK);
        from_mat(blades.A_OLJ, m.swashplate_A_OLJ);
        for (std::size_t i = 0; i < kinematics::kRotatingLimbCount; ++i) {
            from_vec(blades.blade[i].r_DiO_O, m.swashplate_D[i]);
            from_vec(blades.blade[i].r_EiO_O, m.blade_E[i]);
            from_vec(blades.blade[i].n_blade_O, m.blade_axis[i]);
            from_vec(drivers[i].r_FiO_O, m.driver_F[i]);
            from_vec(drivers[i].r_GiO_O, m.driver_G[i]);
            from_vec(drivers[i].r_HiO_O, m.driver_H[i]);
            m.blade_beta[i] = blades.blade[i].beta;
        }
        m.valid = 1;
    }
    if (mech_tail) {
        const Real Omega_tr = drive_p.n_mr2tr != 0 ? dr[drv::Omega_mr] / drive_p.n_mr2tr : Real{0};
        const kinematics::TailrotorPose pose = kinematics::tailrotor_forward_kinematics(*mech_tail, sigma4[3], Omega_tr);
        hh_tailrotor_mechanics& m = out.tailrotor_mechanics;
        from_vec(pose.r_BO_O, m.servo_arm_B);
        from_vec(pose.r_CO_O, m.rod_C);
        from_vec(pose.r_EO_O, m.lever_E);
        from_vec(pose.r_KO_O, m.sleeve_K);
        for (std::size_t i = 0; i < kinematics::kRotatingLimbCount; ++i) {
            from_vec(pose.blade[i].r_GiO_O, m.hinge_G[i]);
            from_vec(pose.blade[i].r_HiO_O, m.pitch_link_H[i]);
            from_vec(pose.blade[i].r_IiO_O, m.pitch_arm_I[i]);
            from_vec(pose.blade[i].normal_hinge_axis, m.hinge_axis[i]);
            from_vec(pose.blade[i].n_blade_O, m.blade_axis[i]);
        }
        m.lever_angle = pose.lever_angle;
        m.stroke = pose.stroke;
        m.blade_beta = pose.blade[0].beta;
        m.valid = pose.reachable ? 1 : 0;
    }
}

namespace {

void fill_rotor_output_classic(const aero::ClassicRotorOutputs& r, const aero::FlappingState& flap, hh_rotor_output& o) noexcept
{
    o.thrust = r.thrust;
    o.torque = r.torque;
    o.induced_velocity = r.induced_velocity;
    from_vec(r.force_O, o.force_O);
    from_vec(r.torque_L, o.torque_L);
    o.flapping_a_s = flap[aero::flap::a_s];
    o.flapping_b_s = flap[aero::flap::b_s];
    o.ground_effect_factor = r.ground_effect_factor;
    o.vortex_ring_strength = r.vrs_strength;
    o.turbulence_strength = r.turbulence_strength;
}

}  // namespace

void HelicopterSim::Impl::fill_outputs(hh_sim_outputs& out) const
{
    out = hh_sim_outputs{};
    out.struct_size = sizeof(hh_sim_outputs);
    if (!configured || !has_nominal) {
        return;
    }
    const StageEvaluation& e = nominal;
    const SimState xr = render_state();
    const auto& h = p.helicopter;
    const dynamics::RigidBodyState rb = take<13>(xr, idx::body);
    const dynamics::DrivetrainState dr = take<5>(xr, idx::drive);
    const Mat3 A_OL = dynamics::attitude_A_OL(rb);
    const Quat q = frames::quat_normalized(dynamics::orientation(rb));

    std::uint32_t flags = 0;
    if (dynamics::is_freewheeling(drive_p, dr)) flags |= HH_FLAG_FREEWHEELING;
    if (collision.memory().crashed) flags |= HH_FLAG_CRASHED;
    if (e.drive.saturated) flags |= HH_FLAG_GOVERNOR_SATURATED;
    if (last_hold.motor_enabled) flags |= HH_FLAG_MOTOR_ENABLED;
    if (e.contact.any_contact) flags |= HH_FLAG_ANY_CONTACT;
    if (bemt_main) flags |= HH_FLAG_MAINROTOR_BEMT;
    if (mech_main && !tandem) flags |= HH_FLAG_MECHANICS_MAINROTOR;
    if (mech_tail) flags |= HH_FLAG_MECHANICS_TAILROTOR;
    if (diverged) flags |= HH_FLAG_DIVERGED;
    out.flags = flags;
    out.time = t;
    out.step_count = steps;

    from_vec(dynamics::position_O(rb), out.position_O);
    out.orientation[0] = q.a[0];
    out.orientation[1] = q.a[1];
    out.orientation[2] = q.a[2];
    out.orientation[3] = q.a[3];
    from_vec(dynamics::velocity_O(rb), out.velocity_O);
    from_vec(frames::transposed(A_OL) * dynamics::velocity_O(rb), out.velocity_L);
    from_vec(dynamics::angular_velocity_L(rb), out.omega_L);
    from_vec(accel_L, out.accel_L);

    // drivetrain
    const Real n_tr = drive_p.n_mr2tr != 0 ? drive_p.n_mr2tr : Real{1};
    const Real n_pr = drive_p.n_mr2pr != 0 ? drive_p.n_mr2pr : Real{1};
    out.motor_speed = dr[drv::omega_mo];
    out.motor_angle = dr[drv::Omega_mo];
    out.mainrotor_speed = dr[drv::omega_mr];
    out.mainrotor_angle = dr[drv::Omega_mr];
    out.tailrotor_speed = dr[drv::omega_mr] / n_tr;
    out.tailrotor_angle = dr[drv::Omega_mr] / n_tr;
    out.propeller_speed = dr[drv::omega_mr] / n_pr;
    out.propeller_angle = dr[drv::Omega_mr] / n_pr;
    out.governor_integral = dr[drv::governor_int];
    out.governor_target = soft_target;
    out.motor_voltage_applied = e.drive.V_s_out;
    out.motor_power_electric = e.drive.P_mo_el;
    out.motor_power_mechanical = e.drive.P_mo_mech;

    // rotors
    const aero::FlappingState fm = take<2>(xr, idx::flap_main);
    const aero::FlappingState ft = take<2>(xr, idx::flap_tail);
    hh_rotor_output& mr = out.mainrotor;
    if (main_exists && bemt_main) {
        // the BEMT thrust is the length of the force vector: give it the sign of the force along the shaft
        const Vec3 axis_O = A_OL * geo_main.axis_L;
        mr.thrust = qvm::dot(e.bemt_out.force_O, axis_O) < 0 ? -e.bemt_out.thrust : e.bemt_out.thrust;
        mr.torque = e.bemt_out.shaft_torque;
        mr.induced_velocity = e.bemt_out.Vi_mean;
        from_vec(e.main_force_O, mr.force_O);
        from_vec(e.main_torque_L, mr.torque_L);
        mr.ground_effect_factor = e.bemt_out.ground_effect_factor;
        mr.vortex_ring_strength = e.bemt_out.vrs_strength;
        mr.turbulence_strength = e.bemt_out.turbulence_strength;
    } else if (main_exists) {
        fill_rotor_output_classic(e.main_out, fm, mr);
    }
    if (e.tail_active) {
        fill_rotor_output_classic(e.tail_out, ft, out.tailrotor);
    }
    if (e.propeller_active) {
        fill_rotor_output_classic(e.propeller_out, aero::FlappingState{}, out.propeller);
    }
    out.booster_thrust = e.booster_thrust;
    const Quat disc_q = frames::quat_normalized(aero::disc_orientation(take<6>(xr, idx::disc)));
    if (bemt_main) {
        out.rotor_disc_orientation[0] = disc_q.a[0];
        out.rotor_disc_orientation[1] = disc_q.a[1];
        out.rotor_disc_orientation[2] = disc_q.a[2];
        out.rotor_disc_orientation[3] = disc_q.a[3];
        out.rotor_disc_rate_x = xr[idx::disc + aero::disc::wx];
        out.rotor_disc_rate_z = xr[idx::disc + aero::disc::wz];
    } else {
        out.rotor_disc_orientation[0] = 1;
    }

    // audio
    hh_audio& a = out.audio;
    a.mainrotor_speed = out.mainrotor_speed;
    a.tailrotor_speed = out.tailrotor_speed;
    a.motor_speed = out.motor_speed;
    a.propeller_speed = out.propeller_speed;
    a.vortex_ring = mr.vortex_ring_strength;
    a.turbulence = std::max(mr.turbulence_strength, out.tailrotor.turbulence_strength);
    a.mainrotor_noise = 1;
    a.mainrotor_stall = 0;
    if (bemt_main) {
        Real noise = 0;
        Real stall = 0;
        for (std::size_t i = 0; i < aero::kBemtElementCount; ++i) {
            noise += numerics::smooth_step(std::abs(e.bemt_out.cd[i]), 0.005, 0, 0.12, 1) / static_cast<Real>(aero::kBemtElementCount) * 10;
            stall += numerics::smooth_step(std::abs(e.bemt_out.alpha[i]), 12 * frames::kDegToRad, 0, 26 * frames::kDegToRad, 1) /
                     static_cast<Real>(aero::kBemtElementCount);
        }
        a.mainrotor_noise = std::clamp(noise, Real{0.15}, Real{1});
        a.mainrotor_stall = stall;
    }
    const Real tail_theta = last_control.tail.Theta_col;
    a.tailrotor_pitch_volume = numerics::smooth_step(std::abs(tail_theta), 10 * frames::kDegToRad, 0.5, 25 * frames::kDegToRad, 1.0);
    a.motor_current = e.drive.I_mo;
    a.motor_load = h.accumulator.I_max > 0 ? std::clamp(std::abs(e.drive.I_mo) / h.accumulator.I_max, Real{0}, Real{1}) : Real{0};

    // controls and kinematics
    const auto& sigma4 = servos.sigma();
    for (std::size_t i = 0; i < HH_SERVOS; ++i) {
        out.servo_command[i] = fc_out.sigma[i];
        out.servo_actual[i] = sigma4[i];
    }
    out.swashplate_roll = last_control.swashplate.roll;
    out.swashplate_pitch = last_control.swashplate.pitch;
    out.swashplate_collective = last_control.swashplate.collective;
    {
        const Vec3 n{{-std::sin(last_control.swashplate.pitch) * std::cos(last_control.swashplate.roll),
                      std::cos(last_control.swashplate.pitch) * std::cos(last_control.swashplate.roll), std::sin(last_control.swashplate.roll)}};
        from_vec(n, out.swashplate_normal);
    }
    for (std::size_t c = 0; c < HH_BLADE_STATIONS; ++c) {
        out.blade_pitch[c] = last_control.blade_pitch[c];
    }
    out.tailrotor_pitch = last_control.tail.Theta_col;
    out.commands[0] = fc_out.normalized.collective_mr;
    out.commands[1] = fc_out.normalized.lateral_mr;
    out.commands[2] = fc_out.normalized.longitudinal_mr;
    out.commands[3] = fc_out.normalized.collective_tr;
    out.commands[4] = fc_out.normalized.lateral_tr;
    out.commands[5] = fc_out.normalized.longitudinal_tr;
    out.commands[6] = fc_out.normalized.collective_pr;
    fill_mechanics(out);

    // ground contact
    const collision::ContactMemory& mem = collision.memory();
    out.contact_count = static_cast<std::int32_t>(std::min<std::size_t>(e.contact.point_count, HH_MAX_CONTACTS));
    for (std::size_t i = 0; i < static_cast<std::size_t>(out.contact_count); ++i) {
        const collision::ContactPointResult& r = e.contact.points[i];
        hh_contact& c = out.contacts[i];
        c.type = static_cast<std::int32_t>(r.type);
        c.in_contact = r.in_contact ? 1 : 0;
        from_vec(r.position_O, c.position_O);
        from_vec(r.normal_O, c.normal_O);
        c.distance = r.distance;
        c.penetration = r.penetration;
        from_vec(r.velocity_O, c.velocity_O);
        from_vec(r.normal_force_O, c.normal_force_O);
        from_vec(r.friction_force_O, c.friction_force_O);
        c.normal_force = r.normal_force;
        c.tangential_force = r.tangential_force;
    }
    for (std::size_t g = 0; g < HH_GEAR_GROUPS; ++g) {
        out.gear_force_y[g] = mem.gear_force_y[g];
        out.wheel_rolling_distance[g] = mem.wheel_rolling_distance[g];
        out.wheel_rolling_velocity[g] = mem.wheel_rolling_velocity[g];
    }
    const auto steering = collision.wheel_steering();
    out.wheel_steering[0] = steering[0];
    out.wheel_steering[1] = steering[1];
    out.wheel_steering[2] = steering[2];
    out.wheel_brake_strength = mem.wheel_brake_strength;
    out.hub_distance_to_ground[0] = e.contact.hub_distance[0];
    out.hub_distance_to_ground[1] = e.contact.hub_distance[1];

    // ---- debug display
    const Real mean_pitch = [&] {
        Real sum = 0;
        for (const Real b : last_control.blade_pitch) {
            sum += b;
        }
        return sum / static_cast<Real>(last_control.blade_pitch.size());
    }();
    const auto classic_debug = [&](const aero::ClassicRotorOutputs& r, const Real rho, const Real radius, const Real collective, hh_rotor_debug& d) {
        d.power_profile = r.P_profile;
        d.power_induced = r.P_induced;
        d.power_parasite = r.P_parasite;
        d.power_climb = r.P_climb;
        d.flapping_tau = r.flapping_tau;
        const Real v_h = std::sqrt(std::max(std::abs(r.thrust), Real{0.01}) / (2 * rho * frames::kPi * radius * radius));
        d.vz_scaled = r.vertical_speed / v_h;
        d.vx_scaled = r.horizontal_speed / v_h;
        d.vortex_ring_use = 1;
        d.vortex_ring_strength = r.vrs_strength;
        d.turbulence = r.turbulence_strength;
        d.collective_pitch = collective;
        d.horizontal_speed = r.horizontal_speed;
    };
    const Real rho = p.scenery.weather.rho_air;
    hh_rotor_debug& dm = out.mainrotor_debug;
    if (bemt_main) {
        dm.power_profile = -e.bemt_out.shaft_torque * dr[drv::omega_mr];
        dm.vz_scaled = e.bemt_out.Vz_scaled;
        dm.vx_scaled = e.bemt_out.Vx_scaled;
        dm.vortex_ring_use = e.bemt_out.use_vrs;
        dm.vortex_ring_strength = e.bemt_out.strength_vrs;
        dm.turbulence = e.bemt_out.turbulence_strength;
        dm.collective_pitch = mean_pitch;
        dm.horizontal_speed = e.bemt_out.horizontal_speed;
        from_vec(e.bemt_out.torque_hub_on_body_L, dm.hub_torque_L);
    } else if (main_exists) {
        classic_debug(e.main_out, rho, h.mainrotor.R, last_control.main.Theta_col, dm);
    }
    if (e.tail_active) {
        classic_debug(e.tail_out, rho, h.tailrotor.R, last_control.tail.Theta_col, out.tailrotor_debug);
    }
    if (e.propeller_active) {
        classic_debug(e.propeller_out, rho, h.propeller.R, last_control.propeller.Theta_col, out.propeller_debug);
    }
    from_vec(e.contact_in.position_O + A_OL * geo_main.position_L, dm.position_O);
    from_vec(e.contact_in.position_O + A_OL * geo_tail.position_L, out.tailrotor_debug.position_O);
    from_vec(e.contact_in.position_O + A_OL * geo_prop.position_L, out.propeller_debug.position_O);

    from_vec(frames::transposed(A_OL) * last_hold.wind_O, out.wind_L);
    from_vec(A_OL * e.aero.fuselage_force_L, out.fuselage_force_O);
    for (std::size_t s = 0; s < e.aero.surface.size(); ++s) {
        from_vec(A_OL * e.aero.surface[s].force_L, out.surface_force_O[s]);
    }
    from_vec(e.loads.torque_L - e.contact.torque_L, out.nonground_torque_L);
    if (const auto* fbl = dynamic_cast<const control::FblController*>(controller.get())) {
        const auto integrals = fbl->integrals();
        out.controller_integral[0] = integrals[0];
        out.controller_integral[1] = integrals[1];
        out.controller_integral[2] = integrals[2];
    }
    out.motor_voltage_terminal = e.drive.V_mo;
    out.motor_efficiency = e.drive.eta_mo;
    out.booster_count = e.booster_count;
    for (int i = 0; i < e.booster_count; ++i) {
        const auto k = static_cast<std::size_t>(i);
        from_vec(A_OL * e.booster_force_L[k], out.booster_force_O[k]);
        from_vec(e.contact_in.position_O + A_OL * e.booster_position_L[k], out.booster_position_O[k]);
    }
}

bool HelicopterSim::Impl::fill_bemt(hh_bemt_telemetry& out) const
{
    const std::uint32_t size = out.struct_size;
    out = hh_bemt_telemetry{};
    out.struct_size = size != 0 ? size : static_cast<std::uint32_t>(sizeof(hh_bemt_telemetry));
    if (!configured || !has_nominal || !bemt_main) {
        return false;
    }
    const aero::BemtOutputs& b = nominal.bemt_out;
    out.valid = 1;
    out.time = t;
    for (std::size_t i = 0; i < aero::kBemtElementCount; ++i) {
        out.radius[i] = bemt_grid.radius[i];
        out.psi[i] = bemt_grid.psi[i];
        out.beta[i] = b.beta[i];
        out.Vc[i] = b.Vc[i];
        out.V2[i] = b.V2[i];
        out.Vi_raw[i] = b.Vi_raw[i];
        out.Vi_target[i] = b.Vi_target[i];
        out.Vi[i] = b.Vi[i];
        out.alpha[i] = b.alpha[i];
        out.cl[i] = b.cl[i];
        out.cd[i] = b.cd[i];
        out.dL[i] = b.dL[i];
        out.dD[i] = b.dD[i];
        out.dT[i] = b.dT[i];
        out.dQ[i] = b.dQ[i];
        out.vrs_weight[i] = b.vrs_weight[i];
        out.residual[i] = b.residual[i];
    }
    for (std::size_t i = 0; i < aero::kBemtElementCount; ++i) {
        from_vec(b.position_O[i], out.position_O[i]);
        from_vec(b.induced_velocity_O[i], out.induced_velocity_O[i]);
        from_vec(b.force_thrust_O[i], out.force_thrust_O[i]);
        from_vec(b.force_torque_O[i], out.force_torque_O[i]);
    }
    out.thrust = b.thrust;
    out.shaft_torque = b.shaft_torque;
    out.Vi_mean = b.Vi_mean;
    out.Vz_scaled = b.Vz_scaled;
    out.Vx_scaled = b.Vx_scaled;
    out.use_vrs = b.use_vrs;
    out.strength_vrs = b.strength_vrs;
    out.descent_blend_weight = b.descent_blend_weight;
    out.v_johnson_target = b.v_johnson_target;
    out.v_johnson_lagged = b.v_johnson_lagged;
    out.hover_induced_velocity = b.hover_induced_velocity;
    out.ground_effect_factor = b.ground_effect_factor;
    out.turbulence_strength = b.turbulence_strength;
    out.max_residual = b.max_residual;
    out.nonconverged_elements = b.nonconverged_elements;
    return true;
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// thread
// ---------------------------------------------------------------------------------------------------------------------------------------
bool HelicopterSim::Impl::start_thread()
{
    if (!configured || running.load()) {
        return false;
    }
    stop_requested.store(false);
    running.store(true);
    worker = std::thread([this] { thread_main(); });
    return true;
}

void HelicopterSim::Impl::stop_thread()
{
    if (!worker.joinable()) {
        return;
    }
    stop_requested.store(true);
    worker.join();
    running.store(false);
}

void HelicopterSim::Impl::thread_main()
{
    using Clock = std::chrono::steady_clock;
    hh_sim_inputs in = default_sim_inputs();
    Clock::time_point last = Clock::now();
    while (!stop_requested.load(std::memory_order_relaxed)) {
        const Clock::time_point now = Clock::now();
        const double frame = std::chrono::duration<double>(now - last).count();
        last = now;
        {
            std::lock_guard<std::mutex> lock(mutex);
            if (input_buffer.acquire()) {
                in = input_buffer.read_slot();
            }
            advance(frame, in);
            fill_outputs(output_buffer.write_slot());
            output_buffer.publish();
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
}

// ---------------------------------------------------------------------------------------------------------------------------------------
// public interface
// ---------------------------------------------------------------------------------------------------------------------------------------
HelicopterSim::HelicopterSim() : impl_(std::make_unique<Impl>()) {}
HelicopterSim::~HelicopterSim() = default;

bool HelicopterSim::configure(const SimSetup& setup, const hh_sim_options& options, std::string* error)
{
    impl_->stop_thread();
    std::lock_guard<std::mutex> lock(impl_->mutex);
    return impl_->build(setup, options, error);
}

bool HelicopterSim::configured() const noexcept
{
    return impl_->configured;
}

bool HelicopterSim::update_parameters(const SimSetup& setup, std::string* error)
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    return impl_->update(setup, error);
}

void HelicopterSim::set_flight_controller(std::unique_ptr<control::IFlightController> controller)
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    if (controller) {
        impl_->controller = std::move(controller);
    }
}

bool HelicopterSim::set_ground_mesh(const std::span<const double> vertices, const std::span<const std::uint32_t> indices, const bool flip_winding)
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    collision::MeshOptions options;
    options.flip_winding = flip_winding;
    return impl_->collision.mesh().build(vertices, indices, options);
}

bool HelicopterSim::set_ground_mesh(const std::span<const float> vertices, const std::span<const std::uint32_t> indices, const bool flip_winding)
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    collision::MeshOptions options;
    options.flip_winding = flip_winding;
    return impl_->collision.mesh().build(vertices, indices, options);
}

void HelicopterSim::clear_ground_mesh()
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    impl_->collision.mesh().clear();
}

std::size_t HelicopterSim::ground_triangle_count() const
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    return impl_->collision.mesh().triangle_count();
}

void HelicopterSim::reset(const hh_reset_params& reset)
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    impl_->reset(reset);
}

void HelicopterSim::step_fixed(const hh_sim_inputs& inputs)
{
    if (impl_->running.load()) {
        return;
    }
    std::lock_guard<std::mutex> lock(impl_->mutex);
    impl_->step(inputs);
    impl_->alpha = 1;  // no frame time involved: the output is the state of the last step itself
}

int HelicopterSim::advance(const double frame_dt, const hh_sim_inputs& inputs)
{
    if (impl_->running.load()) {
        return 0;
    }
    std::lock_guard<std::mutex> lock(impl_->mutex);
    return impl_->advance(frame_dt, inputs);
}

void HelicopterSim::fill_outputs(hh_sim_outputs& out) const
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    impl_->fill_outputs(out);
}

bool HelicopterSim::fill_bemt_telemetry(hh_bemt_telemetry& out) const
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    return impl_->fill_bemt(out);
}

std::string HelicopterSim::bemt_elements_csv() const
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    if (!impl_->configured || !impl_->has_nominal || !impl_->bemt_main) {
        return {};
    }
    return aero::bemt_elements_csv(impl_->bemt_grid, impl_->nominal.bemt_out);
}

bool HelicopterSim::start_thread()
{
    std::lock_guard<std::mutex> lock(impl_->mutex);
    return impl_->start_thread();
}

void HelicopterSim::stop_thread()
{
    impl_->stop_thread();
}

bool HelicopterSim::thread_running() const noexcept
{
    return impl_->running.load();
}

void HelicopterSim::post_inputs(const hh_sim_inputs& inputs)
{
    impl_->input_buffer.write_slot() = inputs;
    impl_->input_buffer.publish();
}

bool HelicopterSim::read_outputs(hh_sim_outputs& out)
{
    impl_->output_buffer.acquire();
    if (!impl_->output_buffer.has_published()) {
        return false;
    }
    out = impl_->output_buffer.read_slot();
    return true;
}

double HelicopterSim::dt() const noexcept
{
    return impl_->dt;
}

double HelicopterSim::time() const noexcept
{
    return impl_->t;
}

std::uint64_t HelicopterSim::step_count() const noexcept
{
    return impl_->steps;
}

const params::ModelParamsSI& HelicopterSim::model_params() const noexcept
{
    return impl_->p;
}

}  // namespace hoverhere::simulation
