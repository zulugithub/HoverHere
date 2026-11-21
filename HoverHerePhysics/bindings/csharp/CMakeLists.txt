# Layout test of the generated C# structs (needs the .NET SDK). Writes the raw default parameters with the C++ library and
# lets the C# project check offsets, blittability and the values of that blob.
find_program(HOVERHERE_DOTNET dotnet)

if(HOVERHERE_DOTNET AND HOVERHERE_BUILD_TESTS AND TARGET hoverhere_params_check)
    set(_blob "${CMAKE_CURRENT_BINARY_DIR}/default_params.bin")
    add_test(NAME csharp_layout_blob COMMAND hoverhere_params_check --write-default-blob "${_blob}")
    set_tests_properties(csharp_layout_blob PROPERTIES FIXTURES_SETUP hoverhere_default_blob)

    add_test(NAME csharp_layout
        COMMAND "${HOVERHERE_DOTNET}" run -c Release
            --project "${CMAKE_CURRENT_SOURCE_DIR}/HoverHere.Native.Check/HoverHere.Native.Check.csproj"
            -- "${_blob}")
    set_tests_properties(csharp_layout PROPERTIES FIXTURES_REQUIRED hoverhere_default_blob TIMEOUT 300)
    message(STATUS "C# layout test: ${HOVERHERE_DOTNET}")
else()
    message(STATUS "C# layout test disabled (.NET SDK 'dotnet' not found)")
endif()
