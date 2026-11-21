hoverhere_provide_doctest()

add_executable(hoverhere_tests
    src/test_main.cpp
    src/test_rk4.cpp
    src/test_fixed_step_clock.cpp
    src/test_frames.cpp
    src/test_rotor_bemt.cpp
    src/test_rotor_classic.cpp
    src/test_aerodynamics.cpp
    src/test_flight_controller.cpp
    src/test_rigid_body.cpp
    src/test_drivetrain.cpp
    src/test_kinematics.cpp
    src/test_params.cpp
    src/test_param_loader.cpp)
target_link_libraries(hoverhere_tests PRIVATE hoverhere::io hoverhere::physics hoverhere::doctest hoverhere::warnings)
if(NOT MSVC)
    # doctest includes <ciso646>, which libstdc++ marks with #warning for C++20 (the project's own code does not use it)
    target_compile_options(hoverhere_tests PRIVATE -Wno-cpp)
endif()
target_compile_definitions(hoverhere_tests PRIVATE
    HOVERHERE_TEST_DATA_DIR="${CMAKE_CURRENT_SOURCE_DIR}/data"
    HOVERHERE_UNITY_ASSETS_DIR="${PROJECT_SOURCE_DIR}/../HoverHere/Assets")

add_test(NAME unit_tests COMMAND hoverhere_tests)
