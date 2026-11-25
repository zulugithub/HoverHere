configure_file(src/core/version.cpp.in "${CMAKE_CURRENT_BINARY_DIR}/generated/version.cpp" @ONLY)

# The files marked "generated" are written by schema/tools/generate_params.py and checked in (ctest: schema_up_to_date).
add_library(hoverhere_physics STATIC
    include/hoverhere/aero/aerodynamics.hpp
    include/hoverhere/aero/rotor_bemt.hpp
    include/hoverhere/aero/rotor_classic.hpp
    include/hoverhere/aero/rotor_common.hpp
    include/hoverhere/collision/ground_collision.hpp
    include/hoverhere/control/flight_controller.hpp
    include/hoverhere/core/frames.hpp
    include/hoverhere/core/math.hpp
    include/hoverhere/core/types.hpp
    include/hoverhere/dynamics/drivetrain.hpp
    include/hoverhere/dynamics/rigid_body.hpp
    include/hoverhere/kinematics/kinematics.hpp
    include/hoverhere/numerics/fixed_step_clock.hpp
    include/hoverhere/numerics/perlin_noise.hpp
    include/hoverhere/numerics/rk4.hpp
    include/hoverhere/numerics/smooth_damp.hpp
    include/hoverhere/numerics/smooth_step.hpp
    include/hoverhere/params/mechanics_params.hpp          # generated
    include/hoverhere/params/model_params.hpp              # generated
    include/hoverhere/params/param_documents.hpp           # generated
    include/hoverhere/params/param_types.hpp
    include/hoverhere/params/params.hpp
    ../c_api/include/hoverhere/sim_types.h
    include/hoverhere/simulation/helicopter_sim.hpp
    include/hoverhere/version.hpp
    src/aero/aerodynamics.cpp
    src/aero/rotor_bemt.cpp
    src/aero/rotor_classic.cpp
    src/aero/rotor_common.cpp
    src/collision/ground_collision.cpp
    src/control/flight_controller.cpp
    src/dynamics/drivetrain.cpp
    src/dynamics/rigid_body.cpp
    src/kinematics/kinematics.cpp
    src/params/mechanics_params.cpp                        # generated
    src/params/model_params.cpp                            # generated
    src/params/model_params_util.cpp
    src/params/param_access.cpp
    src/params/param_documents.cpp                         # generated
    src/simulation/helicopter_sim.cpp
    "${CMAKE_CURRENT_BINARY_DIR}/generated/version.cpp")
add_library(hoverhere::physics ALIAS hoverhere_physics)

# sim_types.h (plain C structs of the simulation inputs and outputs) lives with the C API headers
target_include_directories(hoverhere_physics PUBLIC include ${CMAKE_CURRENT_SOURCE_DIR}/../c_api/include)
target_compile_features(hoverhere_physics PUBLIC cxx_std_20)
target_link_libraries(hoverhere_physics
    PUBLIC hoverhere::qvm
    PRIVATE hoverhere::warnings hoverhere::pugixml)
