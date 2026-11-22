#include "hoverhere/kinematics/kinematics.hpp"

#include <algorithm>
#include <cmath>
#include <numbers>
#include <numeric>
#include <utility>

namespace hoverhere::kinematics {

namespace qvm = boost::qvm;
namespace {

constexpr Real kPi = std::numbers::pi_v<Real>;

const Vec3 kAxisX{{1, 0, 0}};
const Vec3 kAxisY{{0, 1, 0}};
const Vec3 kAxisZ{{0, 0, 1}};
const Vec3 kNegAxisZ{{0, 0, -1}};

Real dot(const Vec3& a, const Vec3& b) noexcept
{
    return qvm::dot(a, b);
}

Vec3 cross(const Vec3& a, const Vec3& b) noexcept
{
    return qvm::cross(a, b);
}

Vec3 unit(const Vec3& v) noexcept
{
    return qvm::normalized(v);
}

Real length(const Vec3& v) noexcept
{
    return qvm::mag(v);
}

// The point on the sphere around `center` with radius `radius` that is closest to `point`.
Vec3 on_sphere(const Vec3& center, const Vec3& point, const Real radius) noexcept
{
    return center + unit(point - center) * radius;
}

// Frame with e_y = forward and e_x = normalize(upward x forward), see frames::A_RL_from_forward_upward.
Mat3 frame(const Vec3& forward, const Vec3& upward) noexcept
{
    return frames::A_RL_from_forward_upward(forward, upward);
}

Real max_of(const std::array<Real, kServoCount>& v) noexcept
{
    return *std::max_element(v.begin(), v.end());
}

}  // namespace

Real pt1_step(const Real y, const Real u, const Real dt, const Real time_constant) noexcept
{
    if (!(time_constant > 0)) {
        return u;
    }
    return y + (u - y) * (1 - std::exp(-dt / time_constant));
}

// ---------------------------------------------------------------------------------------------------------------
// step 1
// ---------------------------------------------------------------------------------------------------------------
Step1Result step1_swashplate_to_servos(const MainrotorMechanics& m, const SwashplateCommand& command, const IterationOptions& options) noexcept
{
    Step1Result result{};

    // A^LKO = Ax(roll) Az(pitch) (passive), the swashplate normal is its y-axis written in O
    const Mat3 A_com_OLK_tilt = frames::rot_z(command.pitch) * frames::rot_x(command.roll);  // (A^LKO)^T
    const Vec3 normal = A_com_OLK_tilt * kAxisY;
    const Vec3 r_KO_com{{0, m.r_KO_O.a[1] + command.collective, 0}};
    const Mat3 A_OLK = frame(normal, kNegAxisZ);  // upward = -z gives n_x = 0 (planar joint C_s)

    result.converged = true;
    std::array<Real, kServoCount> rod_error{};
    for (std::size_t i = 0; i < kServoCount; ++i) {
        const auto& limb = m.limb_stationary[i];
        const Vec3 arm_neutral = limb.A_OLAi * limb.r_BiAi_LAi;      // servo arm at sigma = 0
        const Vec3 axis = unit(limb.A_OLAi * kAxisZ);                // servo shaft
        const Vec3 C = r_KO_com + A_OLK * limb.r_CiK_LK;
        result.r_CiO_O[i] = C;

        Vec3 B = limb.r_AiO_O + arm_neutral;
        bool converged = false;
        for (int it = 0; it < options.max_iterations && !converged; ++it) {
            const Vec3 on_rod_sphere = on_sphere(C, B, limb.l_CiBi);                              // keep the rod length
            const Vec3 in_arm_plane = on_rod_sphere - axis * dot(on_rod_sphere - limb.r_AiO_O, axis);  // plane normal to the servo shaft
            const Vec3 B_next = on_sphere(limb.r_AiO_O, in_arm_plane, limb.l_BiAi);               // keep the arm length
            converged = length(B_next - B) < options.tolerance;
            B = B_next;
        }
        result.converged = result.converged && converged;
        result.r_BiO_O[i] = B;
        rod_error[i] = std::abs(length(B - C) - limb.l_CiBi);

        // right-hand angle about the servo shaft between the neutral arm and the actual arm
        const Vec3 arm = B - limb.r_AiO_O;
        const Vec3 e0 = unit(arm_neutral);
        const Vec3 e1 = cross(axis, e0);
        result.sigma[i] = std::atan2(dot(arm, e1), dot(arm, e0));
    }
    result.residual = max_of(rod_error);
    return result;
}

// ---------------------------------------------------------------------------------------------------------------
// step 2
// ---------------------------------------------------------------------------------------------------------------
SwashplatePose step2_servos_to_swashplate(const MainrotorMechanics& m, const std::array<Real, kServoCount>& sigma, const IterationOptions& options) noexcept
{
    SwashplatePose pose{};

    std::array<Vec3, kServoCount> C_on_sphere;
    for (std::size_t i = 0; i < kServoCount; ++i) {
        const auto& limb = m.limb_stationary[i];
        pose.r_BiO_O[i] = limb.r_AiO_O + limb.A_OLAi * (frames::rot_z(sigma[i]) * limb.r_BiAi_LAi);
        C_on_sphere[i] = pose.r_BiO_O[i] + kAxisY * limb.l_CiBi;  // start: all rods parallel to the shaft
    }

    pose.converged = false;
    for (int it = 0; it < options.max_iterations && !pose.converged; ++it) {
        // the plane through C0, C1, C2, its normal and its intersection with the shaft axis
        pose.normal_O = unit(cross(C_on_sphere[1] - C_on_sphere[0], C_on_sphere[2] - C_on_sphere[0]));
        pose.r_KO_O = Vec3{{0, dot(pose.normal_O, C_on_sphere[0]) / pose.normal_O.a[1], 0}};
        pose.A_OLK = frame(pose.normal_O, kNegAxisZ);

        // the rods follow the updated ball joint positions
        Real movement = 0;
        for (std::size_t i = 0; i < kServoCount; ++i) {
            const auto& limb = m.limb_stationary[i];
            pose.r_CiO_O[i] = pose.r_KO_O + pose.A_OLK * limb.r_CiK_LK;
            const Vec3 next = on_sphere(pose.r_BiO_O[i], pose.r_CiO_O[i], limb.l_CiBi);
            movement = std::max(movement, length(next - C_on_sphere[i]));
            C_on_sphere[i] = next;
        }
        pose.converged = movement < options.tolerance;
    }

    std::array<Real, kServoCount> rod_error{};
    for (std::size_t i = 0; i < kServoCount; ++i) {
        rod_error[i] = std::abs(length(pose.r_CiO_O[i] - pose.r_BiO_O[i]) - m.limb_stationary[i].l_CiBi);
    }
    pose.residual = max_of(rod_error);
    return pose;
}

SwashplateCommand swashplate_command_from_pose(const MainrotorMechanics& m, const SwashplatePose& pose) noexcept
{
    // n = (-sin(pitch) cos(roll), cos(pitch) cos(roll), sin(roll))
    SwashplateCommand c;
    c.roll = std::asin(std::clamp(pose.normal_O.a[2], Real{-1}, Real{1}));
    c.pitch = std::atan2(-pose.normal_O.a[0], pose.normal_O.a[1]);
    c.collective = pose.r_KO_O.a[1] - m.r_KO_O.a[1];
    return c;
}

// ---------------------------------------------------------------------------------------------------------------
// step 3
// ---------------------------------------------------------------------------------------------------------------
namespace {

Mat3 rotating_swashplate_frame(const SwashplatePose& pose, const Real omega_swash) noexcept
{
    // the second plane normal rotates with the shaft: (A_y^LR(Omega))^T (0, 0, -1)
    return frame(pose.normal_O, frames::rot_y(omega_swash) * kNegAxisZ);
}

}  // namespace

BladeLinkage step3_blade(const MainrotorMechanics& m, const SwashplatePose& pose, const Real omega_swash, const Real omega_blade, const std::size_t limb_index,
                         const IterationOptions& options) noexcept
{
    const auto& limb = m.limb_rotating[limb_index];
    const Mat3 A_OLJ = rotating_swashplate_frame(pose, omega_swash);

    BladeLinkage blade{};
    blade.r_DiO_O = pose.r_KO_O + A_OLJ * limb.r_DiJ_LJ;
    blade.n_blade_O = frames::rot_y(omega_blade) * kAxisX;

    // E_i starts at the blade angle beta = 0 of the parameter file and moves on the circle about the blade axis
    Vec3 E = frames::rot_y(omega_swash) * limb.r_EiO_O;
    const Vec3& n = blade.n_blade_O;
    bool converged = false;
    for (int it = 0; it < options.max_iterations && !converged; ++it) {
        const Vec3 on_rod_sphere = on_sphere(blade.r_DiO_O, E, limb.l_EiDi);
        const Vec3 in_blade_plane = on_rod_sphere - n * dot(on_rod_sphere, n);  // plane through O normal to the blade axis
        const Vec3 E_next = unit(in_blade_plane) * limb.l_EiO;
        converged = length(E_next - E) < options.tolerance;
        E = E_next;
    }
    blade.r_EiO_O = E;
    blade.residual = std::abs(length(E - blade.r_DiO_O) - limb.l_EiDi);

    // angle between the neutral arm direction (beta = 0) and E about the blade axis; positive when E is above the plane
    const Vec3 e0 = unit(cross(n, kAxisY));
    const Vec3 w = unit(kAxisY - n * dot(kAxisY, n));
    blade.beta = std::atan2(dot(E, w), dot(E, e0));
    return blade;
}

RotatingBlades step3_swashplate_to_blades(const MainrotorMechanics& m, const SwashplatePose& pose, const Real Omega, const IterationOptions& options) noexcept
{
    RotatingBlades result{};
    result.A_OLJ = rotating_swashplate_frame(pose, Omega);
    const Real spacing = 2 * kPi / static_cast<Real>(std::max(m.blades_count, 1));
    for (std::size_t i = 0; i < kRotatingLimbCount; ++i) {
        result.blade[i] = step3_blade(m, pose, Omega, Omega + spacing * static_cast<Real>(i), i, options);
    }
    return result;
}

Real step3_pitch_at_azimuth(const MainrotorMechanics& m, const SwashplatePose& pose, const Real psi, const IterationOptions& options) noexcept
{
    return step3_blade(m, pose, psi, psi, 0, options).beta;
}

std::array<Real, kVirtualBladeCount> step3_virtual_blade_angles(const MainrotorMechanics& m, const SwashplatePose& pose, const IterationOptions& options) noexcept
{
    std::array<Real, kVirtualBladeCount> beta{};
    for (std::size_t c = 0; c < kVirtualBladeCount; ++c) {
        beta[c] = step3_pitch_at_azimuth(m, pose, 2 * kPi * static_cast<Real>(c) / static_cast<Real>(kVirtualBladeCount), options);
    }
    return beta;
}

// ---------------------------------------------------------------------------------------------------------------
// step 4
// ---------------------------------------------------------------------------------------------------------------
std::array<DriverLimb, kRotatingLimbCount> step4_driver_visuals(const MainrotorMechanics& m, const SwashplatePose& pose, const RotatingBlades& blades, const Real Omega) noexcept
{
    std::array<DriverLimb, kRotatingLimbCount> out{};
    const Mat3 A_shaft = frames::rot_y(Omega);   // (A_y^LR)^T: 2D plane -> 3D
    const Mat3 A_plane = frames::rot_y(-Omega);  // A_y^LR: 3D -> 2D plane at Omega = 0

    for (std::size_t i = 0; i < kRotatingLimbCount; ++i) {
        const auto& driver = m.driver_rotating[i];
        const Real signum = i == 0 ? Real{1} : Real{-1};

        const Vec3 H = pose.r_KO_O + blades.A_OLJ * Vec3{{signum * driver.l_HiJ, 0, 0}};
        const Vec3 H_plane = A_plane * H;

        // intersection of the circles (F, l_GiFi) and (H, l_HiGi) in the plane
        const Real r1 = driver.l_GiFi;
        const Real r2 = driver.l_HiGi;
        const Real x1 = driver.r_FiO_O.a[0];
        const Real y1 = driver.r_FiO_O.a[1];
        const Real x2 = H_plane.a[0];
        const Real y2 = H_plane.a[1];
        const Real R2 = (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
        const Real root = 2 * (r1 * r1 + r2 * r2) / R2 - (r1 * r1 - r2 * r2) * (r1 * r1 - r2 * r2) / (R2 * R2) - 1;
        const Real shift = (r1 * r1 - r2 * r2) / (2 * R2);
        const Real half_chord = Real{0.5} * std::sqrt(std::max(root, Real{0}));
        const Real gx = Real{0.5} * (x1 + x2) + shift * (x2 - x1) - signum * half_chord * (y2 - y1);
        const Real gy = Real{0.5} * (y1 + y2) + shift * (y2 - y1) - signum * half_chord * (x1 - x2);

        out[i].r_FiO_O = A_shaft * driver.r_FiO_O;
        out[i].r_GiO_O = A_shaft * Vec3{{gx, gy, 0}};
        out[i].r_HiO_O = H;
        out[i].reachable = R2 > 0 && root >= 0;
    }
    return out;
}

// ---------------------------------------------------------------------------------------------------------------
// ideal linear CCPM mixer (Cyclic and Collective Pitch Mixing)
// ---------------------------------------------------------------------------------------------------------------
CcpmMixer make_default_ccpm_mixer() noexcept
{
    constexpr Real arm = 0.017;           // [m] servo arm
    constexpr Real servo_circle = 0.0359; // [m] ball joint radius on the stationary swashplate
    constexpr Real link_circle = 0.0241;  // [m] ball joint radius on the rotating swashplate
    constexpr Real pitch_arm = 0.031;     // [m] blade pitch arm

    CcpmMixer mixer{};
    mixer.gamma = {kPi, -kPi / 3, kPi / 3};
    mixer.servo_offset = {0, 0, 0};
    mixer.k_collective = -1 / arm;          // the servo arm of the Blender export turns upward with a negative angle
    mixer.k_tilt = -servo_circle / arm;
    mixer.beta_0 = 0;
    mixer.c_collective = 1 / pitch_arm;
    mixer.c_cyclic = link_circle / pitch_arm;
    mixer.phi = -kPi / 2;                   // the pitch links trail the blades by 90 degrees
    return mixer;
}

std::array<Real, kServoCount> ccpm_servos(const CcpmMixer& mixer, const SwashplateCommand& command) noexcept
{
    std::array<Real, kServoCount> sigma{};
    for (std::size_t i = 0; i < kServoCount; ++i) {
        sigma[i] = mixer.servo_offset[i] + mixer.k_collective * command.collective +
                   mixer.k_tilt * (command.pitch * std::cos(mixer.gamma[i]) + command.roll * std::sin(mixer.gamma[i]));
    }
    return sigma;
}

SwashplateCommand ccpm_swashplate(const CcpmMixer& mixer, const std::array<Real, kServoCount>& sigma) noexcept
{
    Mat3 M{};
    Vec3 rhs{};
    for (std::size_t i = 0; i < kServoCount; ++i) {
        M.a[i][0] = 1;
        M.a[i][1] = std::cos(mixer.gamma[i]);
        M.a[i][2] = std::sin(mixer.gamma[i]);
        rhs.a[i] = sigma[i] - mixer.servo_offset[i];
    }
    const Vec3 x = qvm::inverse(M) * rhs;
    return SwashplateCommand{x.a[2] / mixer.k_tilt, x.a[1] / mixer.k_tilt, x.a[0] / mixer.k_collective};
}

Real ccpm_pitch_at_azimuth(const CcpmMixer& mixer, const SwashplateCommand& command, const Real psi) noexcept
{
    return mixer.beta_0 + mixer.c_collective * command.collective +
           mixer.c_cyclic * (command.pitch * std::cos(psi + mixer.phi) + command.roll * std::sin(psi + mixer.phi));
}

CcpmMixer calibrate_ccpm_mixer(const MainrotorMechanics& m) noexcept
{
    constexpr std::size_t kStations = 36;
    constexpr Real h_collective = 1e-4;  // [m]
    constexpr Real h_tilt = 1e-2;        // [rad]

    struct Response {
        std::array<Real, kServoCount> sigma;
        std::array<Real, kStations> beta;
    };
    const auto respond = [&m](const SwashplateCommand& command) {
        Response r{};
        r.sigma = step1_swashplate_to_servos(m, command).sigma;
        const SwashplatePose pose = step2_servos_to_swashplate(m, r.sigma);
        for (std::size_t k = 0; k < kStations; ++k) {
            r.beta[k] = step3_pitch_at_azimuth(m, pose, 2 * kPi * static_cast<Real>(k) / static_cast<Real>(kStations));
        }
        return r;
    };
    const auto central_difference = [&](const SwashplateCommand& plus, const SwashplateCommand& minus, const Real h) {
        Response p = respond(plus);
        const Response n = respond(minus);
        for (std::size_t i = 0; i < kServoCount; ++i) {
            p.sigma[i] = (p.sigma[i] - n.sigma[i]) / (2 * h);
        }
        for (std::size_t k = 0; k < kStations; ++k) {
            p.beta[k] = (p.beta[k] - n.beta[k]) / (2 * h);
        }
        return p;
    };

    CcpmMixer mixer{};
    for (std::size_t i = 0; i < kServoCount; ++i) {
        mixer.gamma[i] = std::atan2(-m.limb_stationary[i].r_CiK_LK.a[2], m.limb_stationary[i].r_CiK_LK.a[0]);
    }
    const Response neutral = respond(SwashplateCommand{});
    mixer.servo_offset = neutral.sigma;
    mixer.beta_0 = std::accumulate(neutral.beta.begin(), neutral.beta.end(), Real{0}) / static_cast<Real>(kStations);

    const Response d_collective = central_difference({0, 0, h_collective}, {0, 0, -h_collective}, h_collective);
    const Response d_roll = central_difference({h_tilt, 0, 0}, {-h_tilt, 0, 0}, h_tilt);
    const Response d_pitch = central_difference({0, h_tilt, 0}, {0, -h_tilt, 0}, h_tilt);

    mixer.k_collective = std::accumulate(d_collective.sigma.begin(), d_collective.sigma.end(), Real{0}) / static_cast<Real>(kServoCount);
    mixer.c_collective = std::accumulate(d_collective.beta.begin(), d_collective.beta.end(), Real{0}) / static_cast<Real>(kStations);

    Real roll_num = 0, roll_den = 0, pitch_num = 0, pitch_den = 0;
    for (std::size_t i = 0; i < kServoCount; ++i) {
        roll_num += d_roll.sigma[i] * std::sin(mixer.gamma[i]);
        roll_den += std::sin(mixer.gamma[i]) * std::sin(mixer.gamma[i]);
        pitch_num += d_pitch.sigma[i] * std::cos(mixer.gamma[i]);
        pitch_den += std::cos(mixer.gamma[i]) * std::cos(mixer.gamma[i]);
    }
    mixer.k_tilt = Real{0.5} * (roll_num / roll_den + pitch_num / pitch_den);

    // first harmonic of the blade pitch response: roll gives c (sin psi cos phi + cos psi sin phi), pitch c (cos psi cos phi - sin psi sin phi)
    const auto first_harmonic = [](const Response& r) {
        Real a = 0, b = 0;
        for (std::size_t k = 0; k < kStations; ++k) {
            const Real psi = 2 * kPi * static_cast<Real>(k) / static_cast<Real>(kStations);
            a += r.beta[k] * std::cos(psi);
            b += r.beta[k] * std::sin(psi);
        }
        return std::pair<Real, Real>{2 * a / static_cast<Real>(kStations), 2 * b / static_cast<Real>(kStations)};
    };
    const auto [roll_a, roll_b] = first_harmonic(d_roll);
    const auto [pitch_a, pitch_b] = first_harmonic(d_pitch);
    const Real phi_roll = std::atan2(roll_a, roll_b);
    const Real phi_pitch = std::atan2(-pitch_b, pitch_a);
    mixer.c_cyclic = Real{0.5} * (std::hypot(roll_a, roll_b) + std::hypot(pitch_a, pitch_b));
    mixer.phi = std::atan2(std::sin(phi_roll) + std::sin(phi_pitch), std::cos(phi_roll) + std::cos(phi_pitch));
    return mixer;
}

// ---------------------------------------------------------------------------------------------------------------
// tail rotor
// ---------------------------------------------------------------------------------------------------------------
TailrotorPose tailrotor_forward_kinematics(const TailrotorMechanics& m, const Real sigma, const Real Omega) noexcept
{
    TailrotorPose pose{};
    pose.reachable = true;
    const auto& s = m.limb_stationary;

    // 1. servo arm, right-hand angle sigma about the servo shaft
    pose.r_BO_O = s.r_AO_O + s.A_OLA * (frames::rot_z(sigma) * s.r_BA_LA);

    // 2. rod end C: intersection of the sphere (B, l_CB) with the lever plane (through D, normal n_D) is a circle,
    //    the lever end moves on the circle (D, l_CD) in that plane
    const Real r = s.l_CD;
    const Real R = s.l_CB;
    const Vec3 B = pose.r_BO_O;
    const Vec3 D = s.r_DO_O;
    const Vec3 n_D = unit(s.A_OLD * kAxisZ);
    const Real d = dot(n_D, D - B);
    const Vec3 P = B + n_D * d;
    const Real u_sq = R * R - d * d;
    const Real u = std::sqrt(std::max(u_sq, Real{0}));
    const Real a = length(D - P);
    const Real h = a > 0 ? (a * a + u * u - r * r) / (2 * a) : Real{0};
    const Real l_sq = u * u - h * h;
    pose.reachable = u_sq >= 0 && a > 0 && l_sq >= 0;
    const Real l = std::sqrt(std::max(l_sq, Real{0}));
    const Vec3 toward_D = a > 0 ? (D - P) * (1 / a) : Vec3{{0, 0, 0}};
    const Vec3 Q = P + toward_D * h;
    const Vec3 t_hat = a > 0 ? unit(cross(D - P, n_D)) : Vec3{{0, 0, 0}};
    pose.r_CO_O = Q + t_hat * l;

    // 3. lever rotation about its axis and the lever end E at the sleeve
    const Vec3 lever_arm_neutral = s.A_OLD * s.r_CD_LD;
    const Vec3 r_CD_O = pose.r_CO_O - D;
    const Vec3 e0 = unit(lever_arm_neutral);
    const Vec3 e1 = cross(n_D, e0);
    pose.lever_angle = std::atan2(dot(r_CD_O, e1), dot(r_CD_O, e0));
    pose.r_EO_O = D + s.A_OLD * (frames::rot_z(pose.lever_angle) * s.r_ED_LD);

    // sleeve K on the shaft axis (y) at the height of E
    pose.r_KO_O = Vec3{{0, pose.r_EO_O.a[1], 0}};
    pose.stroke = pose.r_KO_O.a[1] - s.r_KO_O.a[1];

    // 4. blade angle from the pitch plate, first blade only (all blades have the same pitch). Iterate the small
    //    rotation alpha of the pitch plate relative to the blade.
    {
        const auto& limb = m.limb_rotating[0];
        const Vec3 J_y = limb.A_OLJ * kAxisY;
        Vec3 B_y = limb.A_OLBi * kAxisY;
        B_y.a[1] = 0;
        const Real theta = std::atan2(length(cross(J_y, B_y)), dot(J_y, B_y));
        const Vec3 r_IiO_O = limb.A_OLBi * limb.r_IiO_Bi;
        const Real l_I0O = std::abs(r_IiO_O.a[0]);

        Real alpha = 0;
        Mat3 rot_alpha = frames::rot_y(0);
        Vec3 r_HiO_O{};
        Vec3 r_HiIi_O{};
        for (int it = 0; it < 5; ++it) {
            const Real l_Hy0O = l_I0O / std::sin(theta + alpha);
            const Real a_ = l_Hy0O - std::abs(limb.r_GiJ_LJ.a[1]);
            const Real c = limb.l_HiGi;
            const Real b_sq = c * c - a_ * a_;
            pose.reachable = pose.reachable && b_sq >= 0;
            const Real b = std::sqrt(std::max(b_sq, Real{0}));

            const Vec3 r_HiJ_LJ = limb.r_GiJ_LJ + Vec3{{0, a_, b}};
            rot_alpha = frames::rot_y(-alpha);
            r_HiO_O = rot_alpha * (limb.A_OLJ * r_HiJ_LJ) + pose.r_KO_O;
            r_HiIi_O = r_HiO_O - r_IiO_O;

            Vec3 projected = unit(r_HiIi_O) * limb.l_HiIi;
            projected.a[1] = 0;
            alpha = std::abs(std::atan(l_I0O / projected.a[2])) - theta;
        }
        pose.alpha = alpha;

        // blade angle: angle between -z and the pitch link, positive when the link points up (+y is the axial direction)
        const Real beta = std::atan2(length(cross(kNegAxisZ, r_HiIi_O)), dot(kNegAxisZ, r_HiIi_O));
        const Real beta_signed = dot(r_HiIi_O, kAxisY) > 0 ? -beta : beta;

        // 5. shaft rotation of everything on the rotating side
        const Mat3 A_shaft = frames::rot_y(Omega);
        for (std::size_t i = 0; i < kRotatingLimbCount; ++i) {
            const auto& li = m.limb_rotating[i];
            auto& blade = pose.blade[i];
            blade.beta = beta_signed;
            const Real spacing = 2 * kPi / static_cast<Real>(std::max(m.blades_count, 1));
            const Vec3 r_Hi_shaft0 = i == 0 ? r_HiO_O : frames::rot_y(spacing * static_cast<Real>(i)) * r_HiO_O;
            blade.r_HiO_O = A_shaft * r_Hi_shaft0;
            blade.r_IiO_O = A_shaft * (li.A_OLBi * li.r_IiO_Bi);
            blade.r_GiO_O = A_shaft * (rot_alpha * li.r_GiO_O) + Vec3{{0, pose.stroke, 0}};
            const Vec3 r_JO_O = pose.r_KO_O;
            blade.normal_hinge_axis = cross(blade.r_GiO_O - r_JO_O, blade.r_GiO_O - blade.r_HiO_O);
            blade.n_blade_O = A_shaft * (li.A_OLBi * kNegAxisZ);
        }
    }
    return pose;
}

}  // namespace hoverhere::kinematics
