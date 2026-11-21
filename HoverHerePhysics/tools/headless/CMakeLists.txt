add_executable(hoverhere_headless_test src/main.cpp)
target_link_libraries(hoverhere_headless_test PRIVATE hoverhere::physics hoverhere::warnings)

if(TARGET hoverhere::c_api)
    target_sources(hoverhere_headless_test PRIVATE src/c_api_check.c src/c_api_check.h)
    target_link_libraries(hoverhere_headless_test PRIVATE hoverhere::c_api)
    target_compile_definitions(hoverhere_headless_test PRIVATE HOVERHERE_WITH_C_API)
endif()

if(HOVERHERE_BUILD_TESTS)
    add_test(NAME smoke COMMAND hoverhere_headless_test --smoke)
endif()
