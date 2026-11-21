cmake_minimum_required(VERSION 3.25...4.0)

project(HoverHerePhysics
    VERSION 0.1.0
    DESCRIPTION "Platform independent flight, drivetrain and ground physics of the HoverHere RC helicopter simulator"
    LANGUAGES C CXX)

option(HOVERHERE_BUILD_C_API "Build the shared C API library (hoverhere_c_api)" ON)
option(HOVERHERE_BUILD_HEADLESS "Build the headless test executable (hoverhere_headless_test)" ON)
option(HOVERHERE_BUILD_VIEWER "Build the GLFW + Dear ImGui viewer (placeholder, not implemented yet)" OFF)
option(HOVERHERE_BUILD_TESTS "Build the unit tests and register all tests with CTest (the smoke and C# layout tests need HOVERHERE_BUILD_HEADLESS)" ON)
option(HOVERHERE_WARNINGS_AS_ERRORS "Treat compiler warnings as errors" OFF)
option(HOVERHERE_STATIC_MSVC_RUNTIME "Link the MSVC runtime statically (/MT) so the plugin needs no redistributable" ON)
option(HOVERHERE_STATIC_MINGW_RUNTIME "Link the GCC/Clang runtime statically on Windows (MinGW-w64, MSYS2) so no toolchain DLLs are needed" ON)
option(HOVERHERE_USE_SYSTEM_BOOST "Prefer an installed Boost.QVM over downloading one" ON)
option(HOVERHERE_FETCH_DEPENDENCIES "Download missing third-party dependencies at configure time" ON)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

set(CMAKE_CXX_SCAN_FOR_MODULES OFF)

set(CMAKE_C_VISIBILITY_PRESET hidden)
set(CMAKE_CXX_VISIBILITY_PRESET hidden)
set(CMAKE_VISIBILITY_INLINES_HIDDEN ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# vcpkg is consumed through its CMake toolchain only, not through the MSBuild integration of Visual Studio.
set(CMAKE_VS_GLOBALS "VcpkgEnabled=false")

if(HOVERHERE_STATIC_MSVC_RUNTIME)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

if(WIN32 AND NOT MSVC AND HOVERHERE_STATIC_MINGW_RUNTIME)
    add_link_options(-static)
endif()

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin")
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin")
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib")

list(APPEND CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake")
include(HoverHereWarnings)
include(HoverHereDependencies)
hoverhere_provide_qvm()
hoverhere_provide_pugixml()

if(HOVERHERE_BUILD_TESTS)
    enable_testing()
endif()

add_subdirectory(physics)
add_subdirectory(io)
add_subdirectory(schema)

if(HOVERHERE_BUILD_C_API)
    add_subdirectory(c_api)
endif()

if(HOVERHERE_BUILD_HEADLESS)
    add_subdirectory(tools/headless)
    add_subdirectory(tools/params_check)
endif()

if(HOVERHERE_BUILD_TESTS)
    add_subdirectory(tests)
    add_subdirectory(bindings/csharp)
endif()

if(HOVERHERE_BUILD_VIEWER)
    add_subdirectory(viewer)
endif()
