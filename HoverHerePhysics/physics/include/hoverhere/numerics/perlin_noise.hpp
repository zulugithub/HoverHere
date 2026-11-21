#pragma once

#include <array>
#include <cmath>
#include <cstdint>

#include "hoverhere/core/types.hpp"

namespace hoverhere::numerics {

namespace detail {

// Permutation table of 256 entries: Fisher-Yates shuffle with a fixed linear congruential generator, evaluated at compile time. It is
// the same on every platform (the table of Ken Perlin is not needed, any permutation gives the gradient noise its character).
[[nodiscard]] constexpr std::array<std::uint8_t, 512> make_perlin_permutation() noexcept
{
    std::array<std::uint8_t, 256> p{};
    for (std::size_t i = 0; i < 256; ++i) {
        p[i] = static_cast<std::uint8_t>(i);
    }
    std::uint64_t state = 0x9E3779B97F4A7C15ull;
    for (std::size_t i = 255; i > 0; --i) {
        state = state * 6364136223846793005ull + 1442695040888963407ull;
        const std::size_t j = (state >> 33) % (i + 1);
        const std::uint8_t tmp = p[i];
        p[i] = p[j];
        p[j] = tmp;
    }
    std::array<std::uint8_t, 512> out{};
    for (std::size_t i = 0; i < 512; ++i) {
        out[i] = p[i & 255u];
    }
    return out;
}

inline constexpr std::array<std::uint8_t, 512> kPerlinPermutation = make_perlin_permutation();

[[nodiscard]] inline Real perlin_fade(const Real t) noexcept
{
    return t * t * t * (t * (t * 6 - 15) + 10);
}

[[nodiscard]] inline Real perlin_gradient(const unsigned hash, const Real x, const Real y) noexcept
{
    switch (hash & 7u) {  // eight gradient directions
    case 0: return x + y;
    case 1: return -x + y;
    case 2: return x - y;
    case 3: return -x - y;
    case 4: return x;
    case 5: return -x;
    case 6: return y;
    default: return -y;
    }
}

}  // namespace detail

// Smooth 2D gradient noise in [0, 1], the stand-in for UnityEngine.Mathf.PerlinNoise (which cannot be used outside of Unity). Deterministic,
// allocation free, continuous in x and y; the period is 256 in both directions. Mean value about 0.5.
[[nodiscard]] inline Real perlin_noise_2d(const Real x, const Real y) noexcept
{
    const Real fx = std::floor(x);
    const Real fy = std::floor(y);
    const auto xi = static_cast<unsigned>(static_cast<std::int64_t>(fx) & 255);
    const auto yi = static_cast<unsigned>(static_cast<std::int64_t>(fy) & 255);
    const Real xf = x - fx;
    const Real yf = y - fy;
    const Real u = detail::perlin_fade(xf);
    const Real v = detail::perlin_fade(yf);
    const auto& perm = detail::kPerlinPermutation;
    const unsigned aa = perm[perm[xi] + yi];
    const unsigned ab = perm[perm[xi] + yi + 1];
    const unsigned ba = perm[perm[xi + 1] + yi];
    const unsigned bb = perm[perm[xi + 1] + yi + 1];
    const Real x1 = detail::perlin_gradient(aa, xf, yf) * (1 - u) + detail::perlin_gradient(ba, xf - 1, yf) * u;
    const Real x2 = detail::perlin_gradient(ab, xf, yf - 1) * (1 - u) + detail::perlin_gradient(bb, xf - 1, yf - 1) * u;
    const Real n = x1 * (1 - v) + x2 * v;  // about [-1, 1]
    const Real scaled = Real{0.5} + Real{0.5} * n;
    return scaled < 0 ? Real{0} : (scaled > 1 ? Real{1} : scaled);
}

}  // namespace hoverhere::numerics
