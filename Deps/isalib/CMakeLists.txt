cmake_minimum_required(VERSION 3.18)
project(isalib VERSION 1.0.0)

add_library(isalib SHARED "src/isalib.cxx")
target_compile_features(isalib PUBLIC cxx_std_20)
target_compile_definitions(isalib PRIVATE NOMINMAX UNICODE _UNICODE)
target_link_directories(isalib PRIVATE "deps/isa-l/lib")
target_link_libraries(isalib PRIVATE "isa-l_static")
target_include_directories(isalib PUBLIC "deps/isa-l/include")
