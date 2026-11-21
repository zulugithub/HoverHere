# Schema tooling (lint, up-to-date check, regeneration). The generated C++/C# sources are checked in, so building does not
# need Python; the checks below run when Python 3 with PyYAML is available.
set(Python3_FIND_STRATEGY LOCATION)
find_package(Python3 COMPONENTS Interpreter QUIET)

set(_hoverhere_have_yaml FALSE)
if(Python3_FOUND)
    execute_process(COMMAND "${Python3_EXECUTABLE}" -c "import yaml"
        RESULT_VARIABLE _yaml_result OUTPUT_QUIET ERROR_QUIET)
    if(_yaml_result EQUAL 0)
        set(_hoverhere_have_yaml TRUE)
    endif()
endif()

if(_hoverhere_have_yaml)
    set(_generator "${CMAKE_CURRENT_SOURCE_DIR}/tools/generate_params.py")
    add_custom_target(hoverhere_generate_params
        COMMAND "${Python3_EXECUTABLE}" "${_generator}" generate
        COMMENT "Generating the parameter structs from schema/*.yaml"
        VERBATIM)
    if(HOVERHERE_BUILD_TESTS)
        add_test(NAME schema_lint COMMAND "${Python3_EXECUTABLE}" "${_generator}" lint)
        add_test(NAME schema_lint_selftest COMMAND "${Python3_EXECUTABLE}" "${CMAKE_CURRENT_SOURCE_DIR}/tools/test_schema_lint.py")
        add_test(NAME schema_up_to_date COMMAND "${Python3_EXECUTABLE}" "${_generator}" check)

        # The Unity application still owns its own parameter classes: the schema must stay equal to them (defaults, bounds,
        # units, options) and every C# parameter has to be classified as physics or Unity-only.
        find_program(HOVERHERE_DOTNET dotnet)
        set(_unity_parameters "${PROJECT_SOURCE_DIR}/../HoverHere/Assets/Scripting/Helicopter_Parameter.cs")
        if(HOVERHERE_DOTNET AND EXISTS "${_unity_parameters}")
            set(_seed "${CMAKE_CURRENT_BINARY_DIR}/csharp_seed.json")
            add_test(NAME schema_seed_from_csharp
                COMMAND "${HOVERHERE_DOTNET}" run -c Release
                    --project "${PROJECT_SOURCE_DIR}/tools/csharp_reference/csharp_reference.csproj"
                    -- schema-seed --out "${_seed}")
            set_tests_properties(schema_seed_from_csharp PROPERTIES FIXTURES_SETUP hoverhere_csharp_seed TIMEOUT 300)
            add_test(NAME schema_matches_csharp COMMAND "${Python3_EXECUTABLE}" "${_generator}" lint --seed "${_seed}" --strict)
            set_tests_properties(schema_matches_csharp PROPERTIES FIXTURES_REQUIRED hoverhere_csharp_seed)
        endif()
    endif()
    message(STATUS "Schema tools: ${Python3_EXECUTABLE}")
else()
    message(STATUS "Schema tools: Python 3 with PyYAML not found, the schema_lint / schema_up_to_date tests and the "
                   "hoverhere_generate_params target are disabled (pip install pyyaml)")
endif()
