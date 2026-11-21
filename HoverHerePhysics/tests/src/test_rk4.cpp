#include <cmath>
#include <vector>

#include <doctest/doctest.h>

#include "hoverhere/numerics/rk4.hpp"

using hoverhere::Real;
namespace num = hoverhere::numerics;

namespace {

using State1 = num::StateVector<1>;
using State2 = num::StateVector<2>;

// y' = -y, y(0) = 1  =>  y(T) = exp(-T)
Real decay_error(const Real step, const Real final_time)
{
    const auto f = [](const Real, const State1& x) { return State1{-x[0]}; };
    State1 x{1.0};
    const int n = static_cast<int>(std::lround(final_time / step));
    for (int i = 0; i < n; ++i) {
        x = num::rk4_step(f, step * i, x, step);
    }
    return std::abs(x[0] - std::exp(-final_time));
}

struct ConstFunctor {
    State1 operator()(Real, const State1& x) const { return x; }
};

struct MutatingFunctor {
    State1 operator()(Real, const State1& x) { return x; }
};

}  // namespace

TEST_CASE("rk4_step has fourth order of accuracy")
{
    const Real e1 = decay_error(0.1, 1.0);
    const Real e2 = decay_error(0.05, 1.0);
    const Real e3 = decay_error(0.025, 1.0);
    CHECK(e1 / e2 == doctest::Approx(16.0).epsilon(0.1));
    CHECK(e2 / e3 == doctest::Approx(16.0).epsilon(0.1));
    CHECK(e3 < 5e-9);
}

TEST_CASE("rk4_step conserves the energy of a harmonic oscillator over many periods")
{
    // x'' = -x, state (x, v), energy (x^2 + v^2) / 2
    const auto f = [](const Real, const State2& s) { return State2{s[1], -s[0]}; };
    State2 s{1.0, 0.0};
    const Real h = 0.01;
    for (int i = 0; i < 100000; ++i) {  // 1000 s = 159 periods
        s = num::rk4_step(f, h * i, s, h);
    }
    CHECK(0.5 * (s[0] * s[0] + s[1] * s[1]) == doctest::Approx(0.5).epsilon(1e-7));
}

TEST_CASE("rk4_step evaluates the derivative four times at the classical stage times")
{
    std::vector<Real> times;
    const auto f = [&times](const Real t, const State1& x) {
        times.push_back(t);
        return x;
    };
    const State1 x{1.0};
    (void)num::rk4_step(f, 2.0, x, 0.5);
    REQUIRE(times.size() == 4);
    CHECK(times[0] == doctest::Approx(2.0));
    CHECK(times[1] == doctest::Approx(2.25));
    CHECK(times[2] == doctest::Approx(2.25));
    CHECK(times[3] == doctest::Approx(2.5));
}

TEST_CASE("rk4_step is exact for a polynomial of degree four")
{
    // x' = 4 t^3, x(0) = 0  =>  x(t) = t^4 (a Runge-Kutta method of order 4 integrates this exactly)
    const auto f = [](const Real t, const State1&) { return State1{4 * t * t * t}; };
    const State1 x = num::rk4_step(f, 0.0, State1{0.0}, 1.3);
    CHECK(x[0] == doctest::Approx(1.3 * 1.3 * 1.3 * 1.3).epsilon(1e-14));
}

TEST_CASE("derivative functions must be callable through a const reference")
{
    static_assert(num::DerivativeFunction<ConstFunctor, State1>);
    static_assert(!num::DerivativeFunction<MutatingFunctor, State1>);

    [[maybe_unused]] const auto pure = [](const Real, const State1& x) { return x; };
    [[maybe_unused]] const auto stateful = [sum = 0.0](const Real, const State1& x) mutable {
        sum += x[0];  // a derivative that remembers earlier evaluations
        return State1{sum};
    };
    static_assert(num::DerivativeFunction<decltype(pure), State1>);
    static_assert(!num::DerivativeFunction<decltype(stateful), State1>);

    // a function with the wrong signature is rejected as well
    static_assert(!num::DerivativeFunction<decltype([](const State1&) { return State1{}; }), State1>);
    CHECK(true);
}
