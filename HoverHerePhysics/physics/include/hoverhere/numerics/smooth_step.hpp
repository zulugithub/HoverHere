#pragma once

#include <cmath>

#include "hoverhere/core/types.hpp"

namespace hoverhere::numerics {

// Cubic smoothstep from h0 (x <= x0) to h1 (x >= x1), port of Helper.Step(x, x0, h0, x1, h1).
[[nodiscard]] inline Real smooth_step(const Real x, const Real x0, const Real h0, const Real x1, const Real h1) noexcept
{
    if (x <= x0) {
        return h0;
    }
    if (x >= x1) {
        return h1;
    }
    const Real a = (x - x0) / (x1 - x0);
    return h0 + (h1 - h0) * a * a * (3 - 2 * a);
}

// Rise from h0 to h1 between x0 and x1 and fall to h3 between x2 and x3 (x0 < x1 < x2 < x3), port of the eight argument
// Helper.Step(x, x0, h0, x1, h1, x2, x3, h3). The second step is a factor h3 / h1 relative to the plateau.
[[nodiscard]] inline Real smooth_step_rise_fall(const Real x, const Real x0, const Real h0, const Real x1, const Real h1, const Real x2,
                                                const Real x3, const Real h3) noexcept
{
    return smooth_step(x, x0, h0, x1, h1) * smooth_step(x, x2, 1, x3, h3 / h1);
}

// Sign with sign(0) = 0 (System.Math.Sign).
[[nodiscard]] inline Real sign0(const Real x) noexcept
{
    return static_cast<Real>((x > 0) - (x < 0));
}

}  // namespace hoverhere::numerics
