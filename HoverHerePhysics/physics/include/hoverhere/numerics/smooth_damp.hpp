#pragma once

#include <algorithm>
#include <cmath>

#include "hoverhere/core/math.hpp"
#include "hoverhere/core/types.hpp"

namespace hoverhere::numerics {

// Port of UnityEngine.Mathf.SmoothDamp: critically damped spring that moves `current` towards `target` and never
// overshoots. `velocity` is the state of the filter and is advanced by the call, so it belongs to the filter state and
// must be advanced once per full step only (never inside an RK4 stage).
[[nodiscard]] inline Real smooth_damp(const Real current, const Real target, Real& velocity, Real smooth_time, const Real max_speed,
                                      const Real dt) noexcept
{
    smooth_time = std::max(Real{0.0001}, smooth_time);
    const Real omega = 2 / smooth_time;
    const Real x = omega * dt;
    const Real exp = 1 / (1 + x + Real{0.48} * x * x + Real{0.235} * x * x * x);
    const Real max_change = max_speed * smooth_time;
    const Real change = std::clamp(current - target, -max_change, max_change);
    const Real clamped_target = current - change;
    const Real temp = (velocity + omega * change) * dt;
    velocity = (velocity - omega * temp) * exp;
    Real output = clamped_target + (change + temp) * exp;
    if ((target - current > 0) == (output > target)) {  // prevent overshooting
        output = target;
        velocity = dt > 0 ? (output - target) / dt : 0;
    }
    return output;
}

// Port of UnityEngine.Vector3.SmoothDamp (the speed limit acts on the length of the change vector).
[[nodiscard]] inline math::Vec3 smooth_damp(const math::Vec3& current, const math::Vec3& target, math::Vec3& velocity, Real smooth_time,
                                            const Real max_speed, const Real dt) noexcept
{
    namespace qvm = boost::qvm;
    smooth_time = std::max(Real{0.0001}, smooth_time);
    const Real omega = 2 / smooth_time;
    const Real x = omega * dt;
    const Real exp = 1 / (1 + x + Real{0.48} * x * x + Real{0.235} * x * x * x);
    math::Vec3 change = current - target;
    const math::Vec3 original_target = target;
    const Real max_change = max_speed * smooth_time;
    const Real length = qvm::mag(change);
    if (length > max_change && length > 0) {
        change = change * (max_change / length);
    }
    const math::Vec3 clamped_target = current - change;
    const math::Vec3 temp = (velocity + change * omega) * dt;
    velocity = (velocity - temp * omega) * exp;
    math::Vec3 output = clamped_target + (change + temp) * exp;
    if (qvm::dot(original_target - current, output - original_target) > 0) {
        output = original_target;
        velocity = dt > 0 ? (output - original_target) * (1 / dt) : math::Vec3{{0, 0, 0}};
    }
    return output;
}

}  // namespace hoverhere::numerics
