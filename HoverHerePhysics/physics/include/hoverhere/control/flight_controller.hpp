#pragma once

#include <array>
#include <optional>

#include "hoverhere/core/math.hpp"
#include "hoverhere/kinematics/kinematics.hpp"
#include "hoverhere/params/model_params.hpp"
#include "hoverhere/params/param_types.hpp"

// Flight controller interface and the flybarless (FBL) controller with tail gyro of the Unity simulator.
//
// The interface is the one of a real flight controller. The inputs are what such a device sees (IMU, rotor speed, radio
// sticks) and nothing of the internal physics state; the outputs are what it drives (servo positions and the motor speed
// setpoint). A C++ replica of Rotorflight 2 with the parameters of a NEXUS-XR, or a USB hardware-in-the-loop link to a real
// NEXUS-XR, plugs in behind the same interface.
//
//   evaluate()   pure: the same inputs give the same outputs, nothing is stored. Allowed in every RK4 stage.
//   post_step()  once per full step: integrators, derivative memory, filters and the low speed handling.
//   A hardware controller returns the last received frame from evaluate() and exchanges data in post_step().
//
// Servo angle convention: right-hand angle about the servo shaft (kinematics.hpp). Helicopter_ODE.cs feeds the mirrored
// mechanics (see there) with -15 deg / -0.0065 m / -30 deg; with the consistent convention the throws are positive.
namespace hoverhere::control {

using math::Vec3;
using params::Bank3;

// The Unity physics steps at about 1 ms; per step constants of the controller (decay of the integrators at low rotor speed) are
// scaled with dt / kControllerReferenceStep.
inline constexpr Real kControllerReferenceStep = 0.001;  // [s]

// What the controller measures.
struct ImuSample {
    Vec3 gyro_B{{0, 0, 0}};    // [rad/s] angular rate in the body frame (x: roll, y: yaw, z: pitch)
    Vec3 accel_B{{0, 0, 0}};   // [m/s^2] specific force (accelerometer) in the body frame
};

// Radio sticks, normalized to [-1, 1], and switches.
struct RcInputs {
    Real collective = 0;
    Real yaw = 0;
    Real pitch = 0;
    Real roll = 0;
    Real propeller = 0;        // pusher propeller collective
    Real booster = 0;
    int flight_bank = 0;       // 0..2: selects the gains and the rotor speed setpoint
    bool motor_enabled = true; // false: throttle hold
};

struct ControllerInputs {
    ImuSample imu{};
    Real omega_mr = 0;         // [rad/s] main rotor speed (RPM sensor of the governor)
    RcInputs rc{};
};

struct GovernorCommand {
    Real rotor_speed_target = 0;  // [rad/s] main rotor speed setpoint (sign is applied by the drivetrain: motor_speed_target)
    bool motor_enabled = true;
};

// Normalized actuator deflections ([-1, 1]) before the mapping to servo angles, for models without servo kinematics (classic
// rotor, tandem rotor helicopters).
struct NormalizedCommands {
    Real collective_mr = 0, lateral_mr = 0, longitudinal_mr = 0;
    Real collective_tr = 0, lateral_tr = 0, longitudinal_tr = 0;
    Real collective_pr = 0;
};

struct ControllerOutputs {
    std::array<Real, 4> sigma{};     // [rad] servo angle commands: three swashplate servos and the tail rotor servo (single main rotor only)
    NormalizedCommands normalized{};
    GovernorCommand governor{};
    Real booster = 0;                // [-1, 1] pass through of the booster stick
};

class IFlightController {
public:
    virtual ~IFlightController() = default;
    [[nodiscard]] virtual ControllerOutputs evaluate(const ControllerInputs& in, Real dt) const = 0;
    virtual void post_step(const ControllerInputs& in, Real dt) = 0;
    virtual void reset() = 0;
};

// ----------------------------------------------------------------------------------------------------------------
// FBL controller
// ----------------------------------------------------------------------------------------------------------------
struct FblConfig {
    int rotor_systems_configuration = 0;   // 0: single main rotor, 1: tandem rotor
    // flybarless controller (roll and pitch) and tail gyro (yaw), gains per flight bank
    Bank3 fbl_K_a{}, fbl_K_I{}, fbl_K_p{}, fbl_K_d{};
    Bank3 gyro_K_a{}, gyro_K_I{}, gyro_K_p{}, gyro_K_d{};
    bool direct_feedthrough_flybarless = false;  // sticks go straight to the servos
    bool direct_feedthrough_gyroscope = false;
    Bank3 target_rpm{};                    // [rad/s] main rotor speed setpoint per flight bank
    Real initial_roll_integral = 0;        // [rad] trim of the integrators after a reset
    Real initial_pitch_integral = 0;       // [rad]
    // mapping of the normalized outputs to the swashplate and the tail servo
    Real cyclic_throw_roll = 15 * 3.14159265358979323846 / 180;   // [rad] swashplate tilt at full lateral deflection
    Real cyclic_throw_pitch = 15 * 3.14159265358979323846 / 180;  // [rad] at full longitudinal deflection
    Real collective_stroke = 0.0065;       // [m] swashplate stroke at full collective deflection
    Real collective_stroke_offset = 0;     // [m] stroke at zero collective deflection (collective_stroke * Theta_col_0 / K_col)
    Real tail_throw = 30 * 3.14159265358979323846 / 180;          // [rad] tail servo angle at full yaw deflection
    Real speed_below_which_integrators_decay = 0;  // [rad/s] 70 % of the setpoint of bank 0
    // First order low pass on the differentiated error (exact discretization): the numerical derivative of a gyro signal amplifies the
    // noise of the sample by 1 / dt. 0: no filter (the raw difference quotient). 4 ms is a corner frequency of about 40 Hz, far above the
    // control bandwidth of the loop (a few Hz), and well below the shaft frequency.
    Real d_filter_time_constant = 0.004;           // [s]
};

[[nodiscard]] FblConfig make_fbl_config(const params::si::ModelParams& p) noexcept;

// Ideal CCPM mixer for helicopters without linkage geometry whose swashplate command -> blade pitch relation reproduces the
// classic rotor parameters: collective pitch K_col * deflection + Theta_col_0, cyclic gain A_lon / B_lat per full tilt.
[[nodiscard]] kinematics::CcpmMixer make_fallback_ccpm_mixer(const params::si::HelicopterParams& helicopter, const FblConfig& fbl) noexcept;

class FblController final : public IFlightController {
public:
    // `mechanics`: linkage geometry (servo angles from kinematics step 1); without it the ideal CCPM mixer is used.
    FblController(const FblConfig& config, const std::optional<params::mechanics::MainrotorMechanics>& mechanics, const kinematics::CcpmMixer& fallback_mixer);

    [[nodiscard]] ControllerOutputs evaluate(const ControllerInputs& in, Real dt) const override;
    void post_step(const ControllerInputs& in, Real dt) override;
    void reset() override;

    // New gains and mechanism while the controller keeps running: the integrators and the derivative memory stay as they are.
    void update_config(const FblConfig& config, const std::optional<params::mechanics::MainrotorMechanics>& mechanics,
                       const kinematics::CcpmMixer& fallback_mixer);

    // integrator values [rad] for tests and logging: roll, yaw, pitch
    [[nodiscard]] std::array<Real, 3> integrals() const noexcept { return {integral_[0], integral_[1], integral_[2]}; }

private:
    struct Evaluation {
        std::array<Real, 3> error{};       // roll, yaw, pitch [rad/s]
        std::array<Real, 3> derivative{};  // low passed derivative of the error [rad/s^2]
        std::array<bool, 3> saturated{};   // the output of the channel is limited: no integration
        NormalizedCommands commands{};
    };
    [[nodiscard]] Evaluation compute(const ControllerInputs& in, Real dt) const noexcept;

    FblConfig cfg_;
    std::optional<params::mechanics::MainrotorMechanics> mechanics_;
    kinematics::CcpmMixer mixer_;
    std::array<Real, 3> integral_{};        // roll, yaw, pitch
    std::array<Real, 3> error_previous_{};
    std::array<Real, 3> derivative_filtered_{};   // [rad/s^2] low passed derivative of the error
    bool has_previous_ = false;
};

// ----------------------------------------------------------------------------------------------------------------
// servo dynamics between the controller and the mechanism
// ----------------------------------------------------------------------------------------------------------------
// PT1 lag of the four servos, exact discretization; advanced once per full step (never in an RK4 stage).
class ServoBank {
public:
    explicit ServoBank(const std::array<Real, 4>& time_constants = {}) : tau_(time_constants) {}
    void reset(const std::array<Real, 4>& sigma) noexcept { sigma_ = sigma; }
    [[nodiscard]] const std::array<Real, 4>& sigma() const noexcept { return sigma_; }
    void post_step(const std::array<Real, 4>& command, Real dt) noexcept;
    // Servo positions after `dt` of lag towards `command`, without changing the state: the value at the middle of a step for a model that
    // holds the servo position over the step (time centered instead of one half step late).
    [[nodiscard]] std::array<Real, 4> advanced(const std::array<Real, 4>& command, Real dt) const noexcept;

private:
    std::array<Real, 4> tau_{};
    std::array<Real, 4> sigma_{};
};

// The parameter files give a time constant per control axis; the three swashplate servos share their mean, the tail servo has
// the one of the tail collective. Below 5 ms the servo is treated as instantaneous (as in the Unity code).
[[nodiscard]] std::array<Real, 4> servo_time_constants(const params::si::ServoDamping& damping) noexcept;

// Blade pitch at the ten azimuth stations for given swashplate servo angles: the detailed mechanism (steps 2 and 3) when its
// geometry is available, otherwise the ideal CCPM mixer.
[[nodiscard]] std::array<Real, kinematics::kVirtualBladeCount> blade_pitch_from_servos(const std::optional<params::mechanics::MainrotorMechanics>& mechanics,
                                                                                         const kinematics::CcpmMixer& mixer,
                                                                                         const std::array<Real, kinematics::kServoCount>& sigma) noexcept;

}  // namespace hoverhere::control
