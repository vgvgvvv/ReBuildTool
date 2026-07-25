# RoadMap for CppBuilder

* [ ] 支持生成make文件
* [x] 支持生成 ninja 文件（Phase 1：扫描式依赖，`--UseNinjaBuild` opt-in；命令行复用 toolchain invocations）
* [x] ninja Phase 2：编译器驱动依赖（GCC/Clang `-MMD -MF` + `deps = gcc`，MSVC/clang-cl `/showIncludes` + `deps = msvc`，运行时探测本地化 `msvc_deps_prefix`）取代 #include 扫描，拿到递归 + 宏感知的可靠增量
* [ ] 支持自定义Config Provider
* [ ] 各个平台的测试