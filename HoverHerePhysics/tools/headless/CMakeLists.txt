add_executable(hoverhere_headless_test src/main.cpp src/maneuver_runner.cpp src/maneuver_runner.hpp)
target_link_libraries(hoverhere_headless_test PRIVATE hoverhere::physics hoverhere::io hoverhere::warnings)

if(TARGET hoverhere::c_api)
    target_sources(hoverhere_headless_test PRIVATE src/c_api_check.c src/c_api_check.h)
    target_link_libraries(hoverhere_headless_test PRIVATE hoverhere::c_api)
    target_compile_definitions(hoverhere_headless_test PRIVATE HOVERHERE_WITH_C_API)
endif()

if(HOVERHERE_BUILD_TESTS)
    add_test(NAME smoke COMMAND hoverhere_headless_test --smoke)

    # short maneuver run with a real helicopter of the Unity project (skipped when the project assets are not there)
    set(_logo_resources "${PROJECT_SOURCE_DIR}/../HoverHere/Assets/Helicopters_Available/Logo600SE_V3/Resources")
    if(EXISTS "${_logo_resources}/Logo600SE_V3_parameter_file.xml")
        add_test(NAME headless_hover
            COMMAND hoverhere_headless_test run hover --heli "${_logo_resources}/Logo600SE_V3_parameter_file.xml"
                --rotor classic --duration 1 --trim 0.41 --grid-rate 0 --out "${CMAKE_CURRENT_BINARY_DIR}/hover_out")
    endif()
endif()
