#pragma once

#include <type_traits>

#include <boost/qvm/mat.hpp>
#include <boost/qvm/mat_operations.hpp>
#include <boost/qvm/quat.hpp>
#include <boost/qvm/quat_operations.hpp>
#include <boost/qvm/quat_vec_operations.hpp>
#include <boost/qvm/vec.hpp>
#include <boost/qvm/vec_mat_operations.hpp>
#include <boost/qvm/vec_operations.hpp>

#include "hoverhere/core/types.hpp"

namespace hoverhere::math {

using Vec3 = boost::qvm::vec<Real, 3>;
using Mat3 = boost::qvm::mat<Real, 3, 3>;
using Quat = boost::qvm::quat<Real>;

static_assert(sizeof(Vec3) == 3 * sizeof(Real));
static_assert(sizeof(Mat3) == 9 * sizeof(Real));
static_assert(sizeof(Quat) == 4 * sizeof(Real));
static_assert(std::is_trivially_copyable_v<Vec3> && std::is_standard_layout_v<Vec3>);
static_assert(std::is_trivially_copyable_v<Mat3> && std::is_standard_layout_v<Mat3>);
static_assert(std::is_trivially_copyable_v<Quat> && std::is_standard_layout_v<Quat>);

}  // namespace hoverhere::math
