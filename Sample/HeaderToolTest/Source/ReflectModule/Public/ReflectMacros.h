// ReflectMacros.h — stub macro definitions so that RECLASS annotations and the
// code emitted by ResetHeaderTool compile without the full engine reflection
// runtime. Mirrors the macro shape from ResetEngine's ObjectMacro.h, but every
// macro expands to nothing (codegen still runs; we only need it to compile).
//
// The annotation contract used by the ResetEngineClassExtension plugin:
//   - RECLASS(...) marks a class for reflection codegen (empty expansion).
//   - GENERATE_EXTENSION_BODY() must appear inside the class body; it expands to
//     GENERATE_EXTENSION_BODY_<FILE_ID>_<lineno>() which the generated
//     .extension.h #defines.
//   - The generated .ext.body.h/.ext.gen.cpp reference DEFINE_CLASS(...) and
//     DEFINE_CLASS_IMP(...); they are stubbed here so the generated sources link.
#pragma once

// Reflection annotations — no runtime effect in this stub.
#define RECLASS(...)
#define REENUM(...)
#define REFUNCTION(...)
#define REFIELD(...)
#define REPARAM(...)
#define REPRAGMA(...)

// GENERATE_EXTENSION_BODY pastes together a macro name from CURRENT_EXTENSION_FILE_ID
// (defined by the generated .extension.h) and the current source line, matching
// the #define the generator emits.
#define BODY_MACRO_COMBINE_INNER(A, B, C, D) A##B##C##D()
#define BODY_MACRO_COMBINE(A, B, C, D) BODY_MACRO_COMBINE_INNER(A, B, C, D)
#define GENERATE_EXTENSION_BODY(...) \
    BODY_MACRO_COMBINE(GENERATE_EXTENSION_BODY, _, CURRENT_EXTENSION_FILE_ID, __LINE__);

// Class registration macros referenced by generated reflection glue. Stubbed so
// the generated .ext.gen.cpp / .ext.body.h compile and link without a runtime.
#define DEFINE_CLASS(ClassName)
#define DEFINE_CLASS_IMP(ClassName)
