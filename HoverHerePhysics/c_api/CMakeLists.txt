add_library(hoverhere_c_api SHARED
    include/hoverhere/sim_api.h
    src/sim_api.cpp)
add_library(hoverhere::c_api ALIAS hoverhere_c_api)

target_include_directories(hoverhere_c_api PUBLIC include)
target_compile_definitions(hoverhere_c_api PRIVATE HOVERHERE_C_API_BUILD)
target_link_libraries(hoverhere_c_api PRIVATE hoverhere::physics hoverhere::warnings)
set_target_properties(hoverhere_c_api PROPERTIES
    OUTPUT_NAME hoverhere
    WINDOWS_EXPORT_ALL_SYMBOLS OFF)
if(WIN32)
    # MinGW would otherwise name the library libhoverhere.dll; Unity's P/Invoke expects hoverhere.dll on Windows.
    set_target_properties(hoverhere_c_api PROPERTIES PREFIX "")
endif()
