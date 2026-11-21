include_guard(GLOBAL)

include(FetchContent)

set(HOVERHERE_BOOST_TAG "boost-1.92.0" CACHE STRING "Boost release tag used when Boost.QVM has to be downloaded")
mark_as_advanced(HOVERHERE_BOOST_TAG)

# Provides the INTERFACE target hoverhere::qvm (Boost.QVM is header-only and needs no other Boost library).
# Lookup order: CMake package boost_qvm (vcpkg, Boost installed via CMake), any include directory that
# contains boost/qvm (BoostConfig package, BOOST_ROOT, system headers), automatic download of boostorg/qvm.
function(hoverhere_provide_qvm)
    if(TARGET hoverhere::qvm)
        return()
    endif()

    add_library(hoverhere_qvm INTERFACE)
    add_library(hoverhere::qvm ALIAS hoverhere_qvm)

    if(HOVERHERE_USE_SYSTEM_BOOST)
        find_package(boost_qvm CONFIG QUIET)
        if(TARGET Boost::qvm)
            target_link_libraries(hoverhere_qvm INTERFACE Boost::qvm)
            message(STATUS "Boost.QVM: CMake package boost_qvm")
            return()
        endif()

        find_package(Boost QUIET)
        set(_hints ${Boost_INCLUDE_DIRS} ${Boost_INCLUDE_DIR})
        if(TARGET Boost::headers)
            get_target_property(_headers_includes Boost::headers INTERFACE_INCLUDE_DIRECTORIES)
            if(_headers_includes)
                list(APPEND _hints ${_headers_includes})
            endif()
        endif()
        find_path(HOVERHERE_QVM_INCLUDE_DIR
            NAMES boost/qvm/vec.hpp
            HINTS ${_hints} ${BOOST_ROOT} ${BOOST_INCLUDEDIR} $ENV{BOOST_ROOT} $ENV{BOOST_INCLUDEDIR}
            PATH_SUFFIXES include)
        mark_as_advanced(HOVERHERE_QVM_INCLUDE_DIR)
        if(HOVERHERE_QVM_INCLUDE_DIR)
            target_include_directories(hoverhere_qvm SYSTEM INTERFACE "${HOVERHERE_QVM_INCLUDE_DIR}")
            message(STATUS "Boost.QVM: ${HOVERHERE_QVM_INCLUDE_DIR}")
            return()
        endif()
    endif()

    if(NOT HOVERHERE_FETCH_DEPENDENCIES)
        message(FATAL_ERROR
            "Boost.QVM was not found and HOVERHERE_FETCH_DEPENDENCIES is OFF. "
            "Install it (vcpkg install boost-qvm), point BOOST_ROOT at a Boost tree, "
            "or enable HOVERHERE_FETCH_DEPENDENCIES.")
    endif()

    find_package(Git QUIET)
    if(GIT_FOUND)
        set(_download
            GIT_REPOSITORY https://github.com/boostorg/qvm.git
            GIT_TAG "${HOVERHERE_BOOST_TAG}"
            GIT_SHALLOW TRUE
            GIT_PROGRESS TRUE)
    else()
        set(_download
            URL "https://github.com/boostorg/qvm/archive/refs/tags/${HOVERHERE_BOOST_TAG}.tar.gz"
            DOWNLOAD_EXTRACT_TIMESTAMP TRUE)
    endif()

    # The repository's own CMakeLists.txt expects the Boost super-project, so it is only downloaded, never added.
    set(FETCHCONTENT_UPDATES_DISCONNECTED_BOOST_QVM ON)
    FetchContent_Declare(boost_qvm ${_download} SOURCE_SUBDIR hoverhere_header_only)
    FetchContent_MakeAvailable(boost_qvm)
    FetchContent_GetProperties(boost_qvm SOURCE_DIR _qvm_source_dir)

    target_include_directories(hoverhere_qvm SYSTEM INTERFACE "${_qvm_source_dir}/include")
    message(STATUS "Boost.QVM: downloaded ${HOVERHERE_BOOST_TAG} to ${_qvm_source_dir}")
endfunction()

set(HOVERHERE_PUGIXML_TAG "v1.16" CACHE STRING "pugixml release tag used when pugixml has to be downloaded")
set(HOVERHERE_DOCTEST_TAG "v2.4.12" CACHE STRING "doctest release tag used when doctest has to be downloaded")
mark_as_advanced(HOVERHERE_PUGIXML_TAG HOVERHERE_DOCTEST_TAG)

# Downloads a git repository without running its CMake project (the sources are used directly).
function(_hoverhere_download name url tag out_dir_var)
    find_package(Git QUIET)
    if(GIT_FOUND)
        set(_download GIT_REPOSITORY ${url} GIT_TAG "${tag}" GIT_SHALLOW TRUE GIT_PROGRESS TRUE)
    else()
        string(REGEX REPLACE "\\.git$" "" _url_base "${url}")
        set(_download URL "${_url_base}/archive/refs/tags/${tag}.tar.gz" DOWNLOAD_EXTRACT_TIMESTAMP TRUE)
    endif()
    string(TOUPPER "${name}" _upper_name)
    set("FETCHCONTENT_UPDATES_DISCONNECTED_${_upper_name}" ON)
    FetchContent_Declare(${name} ${_download} SOURCE_SUBDIR hoverhere_download_only)
    FetchContent_MakeAvailable(${name})
    FetchContent_GetProperties(${name} SOURCE_DIR _dir)
    set(${out_dir_var} "${_dir}" PARENT_SCOPE)
endfunction()

# Provides the static library target hoverhere::pugixml (XML parser of the parameter loaders; XPath is not used).
# Lookup order: CMake package pugixml, automatic download. The library is built without the project's warning flags.
function(hoverhere_provide_pugixml)
    if(TARGET hoverhere::pugixml)
        return()
    endif()
    if(HOVERHERE_USE_SYSTEM_BOOST)
        find_package(pugixml CONFIG QUIET)
        if(TARGET pugixml::pugixml)
            add_library(hoverhere::pugixml ALIAS pugixml::pugixml)
            message(STATUS "pugixml: CMake package")
            return()
        endif()
    endif()
    if(NOT HOVERHERE_FETCH_DEPENDENCIES)
        message(FATAL_ERROR "pugixml was not found and HOVERHERE_FETCH_DEPENDENCIES is OFF.")
    endif()
    _hoverhere_download(pugixml https://github.com/zeux/pugixml.git "${HOVERHERE_PUGIXML_TAG}" _pugixml_dir)
    add_library(hoverhere_pugixml STATIC "${_pugixml_dir}/src/pugixml.cpp")
    add_library(hoverhere::pugixml ALIAS hoverhere_pugixml)
    target_include_directories(hoverhere_pugixml SYSTEM PUBLIC "${_pugixml_dir}/src")
    target_compile_definitions(hoverhere_pugixml PUBLIC PUGIXML_NO_XPATH)
    target_compile_features(hoverhere_pugixml PUBLIC cxx_std_17)
    message(STATUS "pugixml: downloaded ${HOVERHERE_PUGIXML_TAG} to ${_pugixml_dir}")
endfunction()

# Provides the INTERFACE target hoverhere::doctest (unit test framework, header only).
function(hoverhere_provide_doctest)
    if(TARGET hoverhere::doctest)
        return()
    endif()
    if(HOVERHERE_USE_SYSTEM_BOOST)
        find_package(doctest CONFIG QUIET)
        if(TARGET doctest::doctest)
            add_library(hoverhere::doctest ALIAS doctest::doctest)
            message(STATUS "doctest: CMake package")
            return()
        endif()
    endif()
    if(NOT HOVERHERE_FETCH_DEPENDENCIES)
        message(FATAL_ERROR "doctest was not found and HOVERHERE_FETCH_DEPENDENCIES is OFF.")
    endif()
    _hoverhere_download(doctest https://github.com/doctest/doctest.git "${HOVERHERE_DOCTEST_TAG}" _doctest_dir)
    add_library(hoverhere_doctest INTERFACE)
    add_library(hoverhere::doctest ALIAS hoverhere_doctest)
    target_include_directories(hoverhere_doctest SYSTEM INTERFACE "${_doctest_dir}")
    message(STATUS "doctest: downloaded ${HOVERHERE_DOCTEST_TAG} to ${_doctest_dir}")
endfunction()
