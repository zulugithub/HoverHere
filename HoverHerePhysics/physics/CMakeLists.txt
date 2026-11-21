configure_file(src/core/version.cpp.in "${CMAKE_CURRENT_BINARY_DIR}/generated/version.cpp" @ONLY)

# The files marked "generated" are written by schema/tools/generate_params.py and checked in (ctest: schema_up_to_date).
add_library(hoverhere_physics STATIC
    include/hoverhere/core/frames.hpp
    include/hoverhere/core/math.hpp
    include/hoverhere/core/types.hpp
    include/hoverhere/numerics/fixed_step_clock.hpp
    include/hoverhere/numerics/rk4.hpp
    include/hoverhere/params/mechanics_params.hpp          # generated
    include/hoverhere/params/model_params.hpp              # generated
    include/hoverhere/params/param_documents.hpp           # generated
    include/hoverhere/params/param_types.hpp
    include/hoverhere/params/params.hpp
    include/hoverhere/version.hpp
    src/params/mechanics_params.cpp                        # generated
    src/params/model_params.cpp                            # generated
    src/params/model_params_util.cpp
    src/params/param_access.cpp
    src/params/param_documents.cpp                         # generated
    "${CMAKE_CURRENT_BINARY_DIR}/generated/version.cpp")
add_library(hoverhere::physics ALIAS hoverhere_physics)

target_include_directories(hoverhere_physics PUBLIC include)
target_compile_features(hoverhere_physics PUBLIC cxx_std_20)
target_link_libraries(hoverhere_physics
    PUBLIC hoverhere::qvm
    PRIVATE hoverhere::warnings)
