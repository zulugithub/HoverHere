#pragma once

#include <array>
#include <concepts>
#include <cstddef>
#include <iterator>
#include <type_traits>

#include "hoverhere/core/types.hpp"

namespace hoverhere::numerics {

// Fixed size state vector of an ODE (no heap allocation).
template <std::size_t N>
using StateVector = std::array<Real, N>;

// A state that is a fixed size, trivially copyable sequence of Real with indexed access.
template <class State>
concept OdeState = std::is_trivially_copyable_v<State> && requires(State s, const State& c, std::size_t i) {
    { s[i] } -> std::convertible_to<Real&>;
    { c[i] } -> std::convertible_to<Real>;
    { std::size(c) } -> std::convertible_to<std::size_t>;
};

// The derivative function f(t, x) -> dx/dt of a Runge-Kutta stage must be a pure function of its arguments. The
// integrator evaluates it several times per step, so it may not advance any persistent state (filters, random
// numbers, integrators of controllers, ...). The type system can only enforce part of that: the function object must
// be callable through a const reference, so a functor with a non-const operator() or a `mutable` lambda is rejected.
template <class F, class State>
concept DerivativeFunction = std::is_invocable_r_v<State, const F&, Real, const State&>;

// One classical Runge-Kutta step of size h: x(t + h) from x(t). Four evaluations of f, order of accuracy 4.
template <OdeState State, DerivativeFunction<State> F>
[[nodiscard]] State rk4_step(const F& f, const Real t, const State& x, const Real h) noexcept(
    std::is_nothrow_invocable_v<const F&, Real, const State&>)
{
    const Real half = h / 2;
    const std::size_t n = std::size(x);

    const State k1 = f(t, x);
    State stage = x;
    for (std::size_t i = 0; i < n; ++i) {
        stage[i] = x[i] + half * k1[i];
    }
    const State k2 = f(t + half, stage);
    for (std::size_t i = 0; i < n; ++i) {
        stage[i] = x[i] + half * k2[i];
    }
    const State k3 = f(t + half, stage);
    for (std::size_t i = 0; i < n; ++i) {
        stage[i] = x[i] + h * k3[i];
    }
    const State k4 = f(t + h, stage);

    State next = x;
    for (std::size_t i = 0; i < n; ++i) {
        next[i] = x[i] + h / 6 * (k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i]);
    }
    return next;
}

}  // namespace hoverhere::numerics
