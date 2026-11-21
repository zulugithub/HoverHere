include_guard(GLOBAL)

add_library(hoverhere_warnings INTERFACE)
add_library(hoverhere::warnings ALIAS hoverhere_warnings)

if(MSVC)
    target_compile_options(hoverhere_warnings INTERFACE
        /W4
        /utf-8
        /external:W0
        $<$<COMPILE_LANGUAGE:CXX>:/permissive->
        $<$<COMPILE_LANGUAGE:CXX>:/Zc:__cplusplus>)
    if(HOVERHERE_WARNINGS_AS_ERRORS)
        target_compile_options(hoverhere_warnings INTERFACE /WX)
    endif()
else()
    target_compile_options(hoverhere_warnings INTERFACE
        -Wall
        -Wextra
        -Wpedantic
        -Wshadow
        -Wconversion
        -Wsign-conversion
        -Wdouble-promotion
        -Wformat=2
        -Wcast-align
        $<$<COMPILE_LANGUAGE:CXX>:-Wnon-virtual-dtor>
        $<$<COMPILE_LANGUAGE:CXX>:-Wold-style-cast>
        $<$<COMPILE_LANGUAGE:CXX>:-Woverloaded-virtual>)
    if(CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
        target_compile_options(hoverhere_warnings INTERFACE
            -Wlogical-op
            $<$<COMPILE_LANGUAGE:CXX>:-Wuseless-cast>)
    endif()
    if(HOVERHERE_WARNINGS_AS_ERRORS)
        target_compile_options(hoverhere_warnings INTERFACE -Werror)
    endif()
endif()
