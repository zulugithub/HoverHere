add_library(hoverhere_io STATIC
    include/hoverhere/io/param_loader.hpp
    src/param_loader.cpp)
add_library(hoverhere::io ALIAS hoverhere_io)

target_include_directories(hoverhere_io PUBLIC include)
target_link_libraries(hoverhere_io
    PUBLIC hoverhere::physics
    PRIVATE hoverhere::pugixml hoverhere::warnings)
