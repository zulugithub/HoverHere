#pragma once

#include <algorithm>
#include <cmath>
#include <cstdint>

#include "hoverhere/core/types.hpp"

namespace hoverhere::numerics {

struct ClockConfig {
    Real dt = 0.001;                     // fixed physics step [s]
    std::int32_t max_substeps = 1000;    // upper limit of steps per advance() call
    Real max_frame_dt = 0.25;            // longer frames are cut to this length [s] (avoids the spiral of death)
    Real time_scale = 1.0;               // simulated seconds per real second
};

// Converts the variable frame time of the host (Unity, viewer, test driver) into a whole number of fixed physics
// steps. Time is accumulated in integer nanoseconds, so the step count does not drift and is reproducible.
class FixedStepClock {
public:
    struct Plan {
        std::int32_t steps;   // fixed steps to run now
        Real dropped_time;    // simulated time that was discarded by the clamps [s]
        Real alpha;           // fraction of a step left over in the accumulator, for interpolating the rendered state [0, 1)
    };

    explicit FixedStepClock(const ClockConfig& config = {}) noexcept
        : dt_ns_(std::max<std::int64_t>(1, to_ns(config.dt))),
          max_substeps_(std::max<std::int32_t>(1, config.max_substeps)),
          max_frame_ns_(std::max<std::int64_t>(dt_ns_, to_ns(config.max_frame_dt))),
          time_scale_(config.time_scale > 0 ? config.time_scale : 0)
    {
    }

    // Adds `frame_dt` seconds of real time and returns how many fixed steps the caller has to run now. The returned
    // steps are counted as done. Negative or non-finite frame times count as zero.
    [[nodiscard]] Plan advance(const Real frame_dt) noexcept
    {
        std::int64_t incoming = std::isfinite(frame_dt) && frame_dt > 0 ? to_ns(frame_dt * time_scale_) : 0;
        std::int64_t dropped = 0;
        if (incoming > max_frame_ns_) {
            dropped += incoming - max_frame_ns_;
            incoming = max_frame_ns_;
        }
        accumulator_ns_ += incoming;

        std::int64_t steps = accumulator_ns_ / dt_ns_;
        if (steps > max_substeps_) {
            dropped += (steps - max_substeps_) * dt_ns_;
            steps = max_substeps_;
            accumulator_ns_ = accumulator_ns_ % dt_ns_;
        } else {
            accumulator_ns_ -= steps * dt_ns_;
        }
        step_count_ += static_cast<std::uint64_t>(steps);
        return Plan{static_cast<std::int32_t>(steps), static_cast<Real>(dropped) / 1e9,
                    static_cast<Real>(accumulator_ns_) / static_cast<Real>(dt_ns_)};
    }

    void reset() noexcept
    {
        accumulator_ns_ = 0;
        step_count_ = 0;
    }

    void set_time_scale(const Real time_scale) noexcept { time_scale_ = time_scale > 0 ? time_scale : 0; }

    [[nodiscard]] Real dt() const noexcept { return static_cast<Real>(dt_ns_) / 1e9; }
    [[nodiscard]] Real time_scale() const noexcept { return time_scale_; }
    [[nodiscard]] std::uint64_t step_count() const noexcept { return step_count_; }
    // Simulated time of the steps handed out so far.
    [[nodiscard]] Real time() const noexcept { return static_cast<Real>(step_count_) * dt(); }

private:
    static std::int64_t to_ns(const Real seconds) noexcept
    {
        const long long ns = std::llround(std::min(seconds, Real{1e9}) * 1e9);  // int64_t is long or long long, depending on the platform
        return ns;
    }

    std::int64_t dt_ns_;
    std::int32_t max_substeps_;
    std::int64_t max_frame_ns_;
    Real time_scale_;
    std::int64_t accumulator_ns_ = 0;
    std::uint64_t step_count_ = 0;
};

}  // namespace hoverhere::numerics
