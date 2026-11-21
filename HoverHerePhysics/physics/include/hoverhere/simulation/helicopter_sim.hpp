#pragma once

#include <cstddef>
#include <cstdint>
#include <memory>
#include <optional>
#include <span>
#include <string>

#include "hoverhere/control/flight_controller.hpp"
#include "hoverhere/core/types.hpp"
#include "hoverhere/params/params.hpp"
#include "hoverhere/sim_types.h"

// The complete helicopter simulation: rigid body, drivetrain with freewheel and governor, main / tail rotor and propeller (classic or BEMT),
// rotor control kinematics and servos, fuselage aerodynamics, ground collision and a flight controller behind IFlightController.
//
// One physics step (dt = 1 ms by default) is
//   1. evaluate()  of the whole model at the start state, once. This evaluation is the first RK4 stage k1 AND the "nominal" evaluation
//                  for everything that has memory;
//   2. three more RK4 stages, each a pure function of the stage state. The controller output and the servo positions are held
//                  constant over the step (zero order hold);
//   3. post_step() exactly once: quaternion normalization, freewheel angle reset, the governor soft start, rotor filters (vortex ring
//                  lag, Pitt-Peters inflow, turbulence), flight controller integrators, servo lags (PT1), stiction anchors, wheel
//                  rolling, crash latch.
// Nothing in step 2 changes any state outside of the RK4 state vector, so the stages can be run in any order and the result of a step is
// a pure function of (state, memory, inputs). step_fixed() is deterministic: same parameters, seed and inputs give bit-identical output on
// one machine.
//
// Real time use: start_thread() runs the physics in a background thread driven by a FixedStepClock on the wall clock. The game thread
// hands in the sticks with post_inputs() and reads the newest published state with read_outputs(); both go through lock-free triple
// buffers, so neither side ever waits for the other. Structural changes (configure, reset, ground mesh) take a mutex that the physics
// thread holds for one batch of steps at a time (a few ms at most).
namespace hoverhere::simulation {

// Everything the model is built from.
struct SimSetup {
    params::ModelParams params = params::make_default_params();   // user units, compiled to SI by configure()
    std::optional<params::mechanics::MainrotorMechanics> mainrotor_mechanics;
    std::optional<params::mechanics::TailrotorMechanics> tailrotor_mechanics;
};

[[nodiscard]] hh_sim_options default_sim_options() noexcept;
[[nodiscard]] hh_reset_params default_reset_params() noexcept;
[[nodiscard]] hh_sim_inputs default_sim_inputs() noexcept;

class HelicopterSim {
public:
    HelicopterSim();
    ~HelicopterSim();
    HelicopterSim(const HelicopterSim&) = delete;
    HelicopterSim& operator=(const HelicopterSim&) = delete;

    // Builds the model (compiles the parameters, creates the FBL controller). Stops the physics thread, keeps the ground mesh. Returns
    // false with a message when the parameters do not describe a physical model. A reset() has to follow.
    bool configure(const SimSetup& setup, const hh_sim_options& options, std::string* error = nullptr);
    [[nodiscard]] bool configured() const noexcept;

    // Replaces the flight controller (the FBL controller of the parameter file is the default). Call reset() afterwards.
    void set_flight_controller(std::unique_ptr<control::IFlightController> controller);

    // Ground mesh in the right handed world frame (y up): x, y, z per vertex, three indices per triangle. Copies the data.
    bool set_ground_mesh(std::span<const double> vertices, std::span<const std::uint32_t> indices, bool flip_winding = false);
    bool set_ground_mesh(std::span<const float> vertices, std::span<const std::uint32_t> indices, bool flip_winding = false);
    void clear_ground_mesh();
    [[nodiscard]] std::size_t ground_triangle_count() const;

    // Places the helicopter and (optionally) spools up the drivetrain; clears all memory (filters, controller, contacts, clock).
    void reset(const hh_reset_params& reset);

    // Synchronous use (tests, headless runs): exactly one fixed step with the given inputs.
    void step_fixed(const hh_sim_inputs& inputs);
    // Synchronous use with a variable frame time: runs as many fixed steps as the clock gives (substep clamp, frame cut). Returns the number
    // of steps. Does nothing while the physics thread runs.
    int advance(double frame_dt, const hh_sim_inputs& inputs);

    // The newest state (synchronous mode). Allocation free.
    void fill_outputs(hh_sim_outputs& out) const;
    [[nodiscard]] bool fill_bemt_telemetry(hh_bemt_telemetry& out) const;
    // The 4 x 10 elements of the BEMT grid of the last step as CSV (header line first); empty for the classic rotor.
    [[nodiscard]] std::string bemt_elements_csv() const;

    // Physics thread.
    bool start_thread();
    void stop_thread();
    [[nodiscard]] bool thread_running() const noexcept;
    void post_inputs(const hh_sim_inputs& inputs);   // lock free
    bool read_outputs(hh_sim_outputs& out);          // lock free; copies the newest published state, false while nothing was published yet

    [[nodiscard]] double dt() const noexcept;
    [[nodiscard]] double time() const noexcept;
    [[nodiscard]] std::uint64_t step_count() const noexcept;
    [[nodiscard]] const params::ModelParamsSI& model_params() const noexcept;

private:
    struct Impl;
    std::unique_ptr<Impl> impl_;
};

}  // namespace hoverhere::simulation
