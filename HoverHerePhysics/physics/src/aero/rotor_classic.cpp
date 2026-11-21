#include "hoverhere/aero/rotor_classic.hpp"

#include <algorithm>
#include <cmath>
#include <numbers>

#include "hoverhere/numerics/smooth_step.hpp"

namespace hoverhere::aero {

namespace qvm = boost::qvm;
using numerics::sign0;
using numerics::smooth_step;

namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;

// System.Math.Sign has sign(0) = 0, UnityEngine.Mathf.Sign has sign(0) = +1
Real unity_sign(const Real x) noexcept
{
    return x >= 0 ? Real{1} : Real{-1};
}

// ----------------------------------------------------------------------------------------------------------------
// thrust and induced velocity, initially proposed by Heffley (NASA 1986), Cai et al. (Unmanned Rotorcraft Systems, 2011, p. 102)
// ----------------------------------------------------------------------------------------------------------------
struct ThrustEvaluation {
    Real residual;  // [m/s] -v_i + v_i(thrust(v_i))
    Real thrust;    // [N]
};

struct ThrustProblem {
    const params::si::Rotor* rotor;
    Real rho_air;
    Real omega_shaft;
    Real u_a, v_a, w_a;  // [m/s] air velocity at the hub in the rotor frame (after the inflow tuning)
    Real Theta_col;
    Real a_s, b_s;
};

ThrustEvaluation thrust_and_induced_velocity(const ThrustProblem& p, const Real v_i) noexcept
{
    constexpr Real T_max = 10000;
    constexpr Real T_max_transition = T_max * 0.1;
    const params::si::Rotor& rotor = *p.rotor;

    // sign inverted compared to the documentation of Cai et al. because the rotor axis is our +y (his -z)
    const Real v_r = -p.v_a + p.a_s * p.u_a - p.b_s * p.w_a;
    const Real v_bl = v_r + (Real{2} / 3) * std::abs(p.omega_shaft) * rotor.R * p.Theta_col;
    const Real v_hat_2 = p.u_a * p.u_a + p.w_a * p.w_a + v_r * (v_r - 2 * v_i);

    Real thrust = ((p.rho_air * std::abs(p.omega_shaft) * rotor.R * rotor.R * rotor.C_l_alpha * static_cast<Real>(rotor.b) * rotor.c) / 4) * (v_bl - v_i);
    thrust = limit_symmetric(thrust, T_max, T_max_transition);

    const Real momentum = thrust / (2 * p.rho_air * kPi * rotor.R * rotor.R);
    Real temp_v_i = std::sqrt(std::abs(std::sqrt((v_hat_2 / 2) * (v_hat_2 / 2) + momentum * momentum) - v_hat_2 / 2));  // eq. 6.13
    if (sign0(thrust) != sign0(temp_v_i)) {
        temp_v_i = -temp_v_i;
    }
    return {-v_i + temp_v_i, thrust};
}

// Newton-Raphson scheme of Helicopter_Rotor_Physics.Newton_Raphson (relaxed, central differences): same iteration, same quirk
// that the reported thrust belongs to the last evaluated point.
void solve_thrust(const ThrustProblem& p, Real& thrust, Real& v_i_out) noexcept
{
    constexpr Real error_tol = 0.0001;
    constexpr int n_max = 50;
    constexpr Real f = 0.6;
    constexpr Real delta_v_i = 0.00001;

    int n = 0;
    Real error = error_tol * 2;
    Real v_i_t = 0;
    Real v_i_tp1_save = 0;
    thrust = 0;
    while (error >= error_tol && n++ <= n_max) {
        const Real plus = thrust_and_induced_velocity(p, v_i_t + delta_v_i).residual;
        const ThrustEvaluation minus = thrust_and_induced_velocity(p, v_i_t - delta_v_i);
        thrust = minus.thrust;
        const Real derivative = (plus - minus.residual) / (2 * delta_v_i);

        Real v_i_tp1;
        if (std::abs(derivative) > 0.00001) {
            const ThrustEvaluation here = thrust_and_induced_velocity(p, v_i_t);
            thrust = here.thrust;
            v_i_tp1 = v_i_t - f * here.residual / derivative;
        }
        else {
            v_i_tp1 = v_i_tp1_save;
        }
        error = std::abs(v_i_tp1 - v_i_t);
        v_i_t = v_i_tp1;
        v_i_tp1_save = v_i_tp1;
    }
    v_i_out = v_i_t;
}

// ----------------------------------------------------------------------------------------------------------------
// rotor torque from the power balance, Cai et al. p. 103
// ----------------------------------------------------------------------------------------------------------------
struct TorqueResult {
    Real torque;
    Real P_profile, P_induced, P_parasite, P_climb;
};

TorqueResult rotor_torque(const ClassicRotorConfig& cfg, const ClassicRotorInputs& in, const Vec3& hub_velocity_R, const Real v_rotor_hub_yO,
                          const Real v_i, const Real thrust) noexcept
{
    const params::si::Rotor& rotor = cfg.rotor;
    const Real u_a = hub_velocity_R.a[0];
    const Real v_a = hub_velocity_R.a[1];
    const Real w_a = hub_velocity_R.a[2];
    const Real omega = in.omega_shaft;

    TorqueResult r{};
    r.P_profile = ((in.rho_air * std::abs(omega) * rotor.R * rotor.R * rotor.C_D0 * static_cast<Real>(rotor.b) * rotor.c) / 8) *
                  ((omega * rotor.R) * (omega * rotor.R) + Real{4.6} * (u_a * u_a + w_a * w_a));
    r.P_induced = thrust * v_i;
    r.P_parasite = std::abs(in.force_fuselage_L.a[0] * u_a) + std::abs(in.force_fuselage_L.a[1] * (-v_a - v_i)) + std::abs(in.force_fuselage_L.a[2] * w_a);
    r.P_climb = cfg.mass_total * v_rotor_hub_yO * cfg.gravity * smooth_step(std::abs(v_rotor_hub_yO), 0.001, 0, 0.01, 1);

    const Real P_rotor = r.P_profile + r.P_induced + r.P_parasite * Real{0.01} + r.P_climb * Real{0.01};  // 0.01: tuning parameter of the Unity model
    Real N = std::abs(omega) > 0.0001 ? P_rotor / std::abs(omega) : P_rotor / 0.01;
    N *= smooth_step(std::abs(omega), 10, 0, 20, 1);  // numerical stability at start up
    r.torque = -N * unity_sign(omega);
    return r;
}

// vortex ring state: how strongly the rotor is in the settling-with-power region of the descent rate / forward speed diagram
Real vrs_development(const ClassicRotorConfig& cfg, Real strength, const Real horizontal_speed, const Real vertical_speed, const bool freewheeling, const Real dt) noexcept
{
    constexpr Real critical_descent_angle = 60 * kPi / 180;
    constexpr Real radius_factor_light = 1.2;
    constexpr Real radius_factor_severe = 0.8;
    constexpr Real light_turbulence_area_growth = 0.1;
    const Real rate = cfg.vortex_ring.strength_rising_speed_factor * (dt / kUnityReferenceStep);

    // centre of the area in the (horizontal speed, vertical speed) diagram where the vortex ring grows most
    const Real center_x = std::abs(cfg.vortex_ring.v_vertical / std::tan(critical_descent_angle));
    const Real center_y = -std::abs(cfg.vortex_ring.v_vertical);
    const Real radius_light = std::abs(center_x * radius_factor_light);
    const Real radius_severe = std::abs(center_x * radius_factor_severe);

    const Real dx = horizontal_speed - center_x;
    const Real dy = vertical_speed - center_y;
    const Real distance = std::sqrt(dx * dx + dy * dy);

    if (((dx < 0 && dy < radius_light && dy > -radius_light) || distance < radius_light) && !freewheeling) {
        if (strength < 0.5) {  // light turbulence: slow build up
            strength += rate * light_turbulence_area_growth;
        }
        if (distance < radius_severe) {  // severe: fast build up with a cosine profile
            strength += rate * ((std::cos((distance / radius_severe) * kPi) + 1) * Real{0.5} * (1 - light_turbulence_area_growth));
        }
    }
    else {
        strength -= rate * Real{0.5};
    }
    return std::clamp(strength, Real{0}, Real{1});
}

}  // namespace

RotorControl classic_control_from_ccpm(const kinematics::CcpmMixer& mixer, const kinematics::SwashplateCommand& command) noexcept
{
    RotorControl c;
    c.Theta_col = mixer.beta_0 + mixer.c_collective * command.collective;
    c.Theta_cyc_a_s = mixer.c_cyclic * command.pitch;
    c.Theta_cyc_b_s = mixer.c_cyclic * command.roll;
    return c;
}

RotorControl classic_control_from_ccpm_servos(const kinematics::CcpmMixer& mixer, const std::array<Real, kinematics::kServoCount>& sigma) noexcept
{
    return classic_control_from_ccpm(mixer, kinematics::ccpm_swashplate(mixer, sigma));
}

RotorControl classic_control_from_tailrotor(const kinematics::TailrotorPose& pose, const Real theta_col_0) noexcept
{
    RotorControl c;
    c.Theta_col = theta_col_0 - pose.blade[0].beta;
    return c;
}

ClassicRotorConfig make_classic_rotor_config(const params::si::ModelParams& p, const RotorKind kind) noexcept
{
    const bool tandem = p.helicopter.rotor_systems_configuration == 1;
    ClassicRotorConfig c{};
    c.kind = kind;
    c.gravity = p.scenery.gravity;
    c.omega_reference = p.helicopter.governor.target_rpm[0];
    c.hub = p.helicopter.flapping;
    c.vortex_ring = p.tuning.vortex_ring_state;
    c.turbulence_config = make_turbulence_config(p.tuning.turbulence);

    switch (kind) {
    case RotorKind::Mainrotor:
        c.rotor = p.helicopter.mainrotor;
        c.inflow = p.tuning.rotor_tuning.mainrotor_tuning;
        c.flapping = c.vortex_ring_state = c.turbulence = c.ground_effect = true;
        c.mass_total = tandem ? p.helicopter.mass_total / 2 : p.helicopter.mass_total;
        break;
    case RotorKind::Tailrotor:
        c.rotor = p.helicopter.tailrotor;
        c.inflow = p.tuning.rotor_tuning.tailrotor_tuning;
        c.flapping = c.vortex_ring_state = c.turbulence = c.ground_effect = tandem;  // the second rotor of a tandem behaves like the first
        c.mass_total = tandem ? p.helicopter.mass_total / 2 : 0;
        if (p.helicopter.transmission.n_mr2tr != 0) {
            c.omega_reference = p.helicopter.governor.target_rpm[0] / p.helicopter.transmission.n_mr2tr;  // nominal speed of this rotor
        }
        break;
    case RotorKind::Propeller:
        c.rotor = p.helicopter.propeller;
        c.inflow = p.tuning.rotor_tuning.propeller_tuning;
        c.flapping = c.vortex_ring_state = c.turbulence = c.ground_effect = false;
        c.mass_total = 0;
        break;
    }
    return c;
}

ClassicRotorOutputs classic_rotor_evaluate(const ClassicRotorConfig& cfg, const ClassicRotorFilterState& state, const ClassicRotorInputs& in) noexcept
{
    ClassicRotorOutputs out{};
    out.vrs_strength = state.vrs_strength;
    const params::si::Rotor& rotor = cfg.rotor;
    const RotorGeometry geometry = make_rotor_geometry(rotor);
    const Mat3& A_OL = in.body.A_OL;
    const Mat3 A_RL = frames::transposed(geometry.A_LR);  // body -> rotor frame

    // ---- relative air velocity at the hub, in the body frame and in the rotor frame
    const Vec3 wind_L = frames::transposed(A_OL) * in.wind_O;
    const Vec3 v_hub_L = in.body.velocity_L - wind_L + qvm::cross(in.body.omega_L, geometry.position_L);
    const Vec3 v_hub_R = A_RL * v_hub_L;
    out.horizontal_speed = std::sqrt(v_hub_R.a[0] * v_hub_R.a[0] + v_hub_R.a[2] * v_hub_R.a[2]);
    out.hub_speed = qvm::mag(v_hub_L);

    // vertical velocity of the hub parallel to gravity (energy based rotor torque)
    const Vec3 v_hub_O = A_OL * v_hub_L;
    const Vec3 axis_O = A_OL * geometry.axis_L;
    const Real v_rotor_hub_yO = std::abs(axis_O.a[1]) * v_hub_O.a[1];

    // The rotor does nothing below 0.1 rad/s (Unity: forces are only applied above it).
    if (!(std::abs(in.omega_shaft) > 0.1)) {
        return out;
    }

    // ---- thrust, induced velocity, torque
    ThrustProblem problem{};
    problem.rotor = &rotor;
    problem.rho_air = in.rho_air;
    problem.omega_shaft = in.omega_shaft;
    problem.Theta_col = in.control.Theta_col;
    problem.a_s = in.flapping[flap::a_s];
    problem.b_s = in.flapping[flap::b_s];
    // non physical inflow tuning
    problem.u_a = v_hub_R.a[0] * cfg.inflow.inflow_factor;
    problem.v_a = v_hub_R.a[1] * cfg.inflow.inflow_factor;
    problem.w_a = v_hub_R.a[2] * cfg.inflow.inflow_factor;
    problem.u_a += cfg.inflow.inflow_offset * sign0(problem.u_a);
    problem.w_a += cfg.inflow.inflow_offset * sign0(problem.w_a);

    Real v_i = 0;
    solve_thrust(problem, out.thrust, v_i);
    out.induced_velocity = v_i;
    const TorqueResult t = rotor_torque(cfg, in, v_hub_R, v_rotor_hub_yO, v_i, out.thrust);
    out.torque = t.torque;
    out.P_profile = t.P_profile;
    out.P_induced = t.P_induced;
    out.P_parasite = t.P_parasite;
    out.P_climb = t.P_climb;
    out.vertical_speed = v_hub_R.a[1] * sign0(v_i);

    // ---- force and torque on the fuselage
    // thrust vector of the tilted tip-path plane: about the rotor x-axis by b_s, then about the z-axis by a_s
    const Vec3 thrust_R = cfg.flapping ? frames::rot_z(in.flapping[flap::a_s]) * (frames::rot_x(in.flapping[flap::b_s]) * Vec3{{0, out.thrust, 0}})
                                       : Vec3{{0, out.thrust, 0}};
    const Vec3 shaft_torque_R = in.freewheeling ? Vec3{{0, 0, 0}} : Vec3{{0, out.torque, 0}};
    const Vec3 thrust_L = geometry.A_LR * thrust_R;

    Vec3 force_O = A_OL * thrust_L;
    Vec3 torque_L = geometry.A_LR * shaft_torque_R;
    torque_L = torque_L + qvm::cross(geometry.position_L, thrust_L);  // hub offset: the thrust acts at the hub, not at the centre of mass

    if (cfg.flapping) {  // restraint of the blade attachment (O-ring)
        const Real start = 3 * kPi / 180;
        const Vec3 stiffness_R{{nonlinear_hub_stiffness(cfg.hub.hub_stiffness_mr, start, in.flapping[flap::b_s]) * in.flapping[flap::b_s], 0,
                                nonlinear_hub_stiffness(cfg.hub.hub_stiffness_mr, start, in.flapping[flap::a_s]) * in.flapping[flap::a_s]}};
        torque_L = torque_L + geometry.A_LR * stiffness_R;
    }

    // ---- flapping dynamics
    if (cfg.flapping) {
        const Real gamma = (in.rho_air * rotor.c * rotor.C_l_alpha * std::pow(rotor.R, 4)) / cfg.hub.I_flapping;  // Lock number
        Real tau = 20;
        if (std::abs(in.omega_shaft) > 0.1) {
            tau = 16 / (gamma * std::abs(in.omega_shaft)) / (1 - (8 * cfg.hub.e) / (3 * rotor.R));
        }
        const Vec3 omega_R = A_RL * in.body.omega_L;
        out.flapping_tau = tau;
        out.dflapping[flap::a_s] = -omega_R.a[2] - (1 / tau) * in.flapping[flap::a_s] + cfg.hub.A_b_s * in.flapping[flap::b_s] +
                                   (1 / tau) * in.control.Theta_cyc_a_s + cfg.hub.A_a_r * v_hub_R.a[0];
        out.dflapping[flap::b_s] = -omega_R.a[0] + cfg.hub.B_a_s * in.flapping[flap::a_s] - (1 / tau) * in.flapping[flap::b_s] +
                                   (1 / tau) * in.control.Theta_cyc_b_s - cfg.hub.B_a_r * v_hub_R.a[2];
        // for low rotor speeds the effect of flapping is reduced
        const Real reduce = smooth_step(std::abs(in.omega_shaft), cfg.omega_reference * Real{0.1}, 0, cfg.omega_reference * Real{0.7}, 1);
        out.dflapping[flap::a_s] *= reduce;
        out.dflapping[flap::b_s] *= reduce;
    }

    // ---- vortex ring state reduces the force and torque of this rotor
    if (cfg.vortex_ring_state) {
        force_O = force_O * (1 - state.vrs_strength * cfg.vortex_ring.force_reduction_factor);
        torque_L = torque_L * (1 - state.vrs_strength * cfg.vortex_ring.torque_reduction_factor);
    }

    // ---- turbulence of the wake
    out.turbulence_strength = turbulence_strength(cfg.turbulence_config, in.body.omega_L, out.hub_speed, cfg.vortex_ring_state ? state.vrs_strength : 0);
    if (cfg.turbulence) {
        force_O = force_O + A_OL * state.turbulence.force_L;
        torque_L = torque_L + state.turbulence.torque_L;
    }

    // ---- ground effect increases the thrust
    if (cfg.ground_effect) {
        const Vec3 downwash_direction_O = axis_O * (-sign0(v_i));
        out.ground_effect_factor = ground_effect_factor(rotor.R, in.ground, downwash_direction_O, out.horizontal_speed, v_i);
        force_O = force_O * out.ground_effect_factor;
    }

    out.force_O = force_O;
    out.torque_L = torque_L;
    return out;
}

void classic_rotor_post_step(const ClassicRotorConfig& cfg, ClassicRotorFilterState& state, const ClassicRotorInputs& in,
                             const ClassicRotorOutputs& out, const Real time, const Real dt, RotorRng& rng) noexcept
{
    state.wake_velocity = out.induced_velocity;
    if (cfg.vortex_ring_state) {
        state.vrs_strength = vrs_development(cfg, state.vrs_strength, out.horizontal_speed, out.vertical_speed, in.freewheeling, dt);
    }
    else {
        state.vrs_strength = 0;
    }
    if (cfg.turbulence) {
        const Real strength = turbulence_strength(cfg.turbulence_config, in.body.omega_L, out.hub_speed, state.vrs_strength);
        turbulence_post_step(cfg.turbulence_config, state.turbulence, strength, time, dt, rng);
    }
    else {
        state.turbulence.elapsed_old = 0;
    }
}

void limit_flapping_at_low_rpm(FlappingState& flapping, const Real omega_shaft, const Real omega_reference, const Real dt) noexcept
{
    const Real below = omega_reference * Real{0.7};
    const Real completely = omega_reference * Real{0.1};
    const Real speed = std::abs(omega_shaft);
    if (speed < below) {
        const Real decay = smooth_step(speed, completely, 0.003, below, 0) * (dt / kUnityReferenceStep);  // [rad]
        for (Real& angle : flapping) {
            angle -= sign0(angle) * decay;
        }
    }
    if (speed < 60 * frames::kRpmToRadPerSec) {
        flapping = FlappingState{0, 0};
    }
}

}  // namespace hoverhere::aero
