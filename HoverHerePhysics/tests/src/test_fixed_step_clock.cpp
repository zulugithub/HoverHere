#include <cmath>
#include <limits>

#include <doctest/doctest.h>

#include "hoverhere/numerics/fixed_step_clock.hpp"

using hoverhere::numerics::ClockConfig;
using hoverhere::numerics::FixedStepClock;

TEST_CASE("FixedStepClock converts frame times into whole 1 ms steps")
{
    FixedStepClock clock;
    CHECK(clock.dt() == 0.001);

    auto plan = clock.advance(0.0165);
    CHECK(plan.steps == 16);
    CHECK(plan.alpha == doctest::Approx(0.5));
    CHECK(plan.dropped_time == 0.0);

    plan = clock.advance(0.0005);  // the half step left over completes a step
    CHECK(plan.steps == 1);
    CHECK(plan.alpha == doctest::Approx(0.0).epsilon(1e-9));
    CHECK(clock.step_count() == 17);
}

TEST_CASE("FixedStepClock does not drift over many frames")
{
    FixedStepClock clock;
    for (int frame = 0; frame < 60000; ++frame) {
        (void)clock.advance(1.0 / 60.0);  // 1000 s of 60 Hz frames
    }
    // 1000 s / 1 ms = 1e6 steps, the clock may only lag by the rounding of the frame time to nanoseconds
    CHECK(clock.step_count() >= 999990);
    CHECK(clock.step_count() <= 1000000);
    CHECK(clock.time() == doctest::Approx(static_cast<double>(clock.step_count()) * 0.001));
}

TEST_CASE("FixedStepClock clamps long frames and reports the dropped time")
{
    {
        FixedStepClock clock;  // max_frame_dt 0.25 s
        const auto plan = clock.advance(1.0);
        CHECK(plan.steps == 250);
        CHECK(plan.dropped_time == doctest::Approx(0.75));
    }
    {
        ClockConfig config;
        config.max_frame_dt = 10.0;
        config.max_substeps = 100;
        FixedStepClock clock(config);
        const auto plan = clock.advance(1.0);
        CHECK(plan.steps == 100);
        CHECK(plan.dropped_time == doctest::Approx(0.9));
        CHECK(plan.alpha == doctest::Approx(0.0).epsilon(1e-9));  // no backlog is carried over
        CHECK(clock.advance(0.0).steps == 0);
    }
}

TEST_CASE("FixedStepClock applies the time scale")
{
    ClockConfig config;
    config.time_scale = 2.0;
    FixedStepClock clock(config);
    CHECK(clock.advance(0.010).steps == 20);

    clock.set_time_scale(0.5);
    CHECK(clock.advance(0.010).steps == 5);

    clock.set_time_scale(0.0);
    CHECK(clock.advance(1.0).steps == 0);
}

TEST_CASE("FixedStepClock ignores negative and non-finite frame times")
{
    FixedStepClock clock;
    CHECK(clock.advance(-1.0).steps == 0);
    CHECK(clock.advance(std::numeric_limits<double>::quiet_NaN()).steps == 0);
    CHECK(clock.advance(std::numeric_limits<double>::infinity()).steps == 0);
    CHECK(clock.step_count() == 0);
}

TEST_CASE("FixedStepClock is deterministic and can be reset")
{
    FixedStepClock a;
    FixedStepClock b;
    for (const double dt : {0.016, 0.0333, 0.0071, 0.0166, 0.2}) {
        CHECK(a.advance(dt).steps == b.advance(dt).steps);
    }
    a.reset();
    CHECK(a.step_count() == 0);
    CHECK(a.time() == 0.0);
}

TEST_CASE("FixedStepClock sanitizes an invalid configuration")
{
    ClockConfig config;
    config.dt = 0.0;
    config.max_substeps = 0;
    config.max_frame_dt = -1.0;
    FixedStepClock clock(config);
    CHECK(clock.dt() > 0.0);
    CHECK(clock.advance(0.001).steps <= 1);
}
