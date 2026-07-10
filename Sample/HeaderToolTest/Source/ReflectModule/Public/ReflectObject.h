// ReflectObject.h — a minimal RECLASS-annotated class that drives ResetHeaderTool
// codegen. The class annotation + GENERATE_EXTENSION_BODY() cause the
// ResetEngineClassExtension plugin to emit, under HeaderToolGen/Extension/:
//   - ReflectObject.extension.h        (defines CURRENT_EXTENSION_FILE_ID + body macro)
//   - ResetEngineExtension/ReflectObject.ext.body.h   (DEFINE_CLASS glue)
//   - ResetEngineExtension/ReflectObject.ext.gen.h
//   - ResetEngineExtension/ReflectObject.ext.gen.cpp  (compiled by the build)
//
// HeaderTool contract: "<Name>.extension.h" must be the last #include in the
// header, and GENERATE_EXTENSION_BODY() must appear in the class body.
#pragma once

#include "ReflectMacros.h"
#include "ReflectObject.extension.h"

RECLASS()
class ReflectObject
{
    GENERATE_EXTENSION_BODY()
};
