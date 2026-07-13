// ReflectPlayer.h — a RECLASS-annotated class deriving from another RECLASS
// class (ReflectCharacter). Unlike the empty/field-only cases, a derived class
// makes the ResetEngineClassExtension plugin emit DEFINE_DERIVED_CLASS(...) and
// DEFINE_DERIVED_CLASS_IMP(...) into its generated .ext.body.h — which is why
// those two are stubbed in ReflectMacros.h. This case verifies the full derived-
// class codegen path compiles and links.
//
// HeaderTool contract: "<Name>.extension.h" must be the last #include in the
// header, and GENERATE_EXTENSION_BODY() must appear in the class body.
#pragma once

#include "ReflectCharacter.h"
#include "ReflectPlayer.extension.h"

RECLASS()
class ReflectPlayer : public ReflectCharacter
{
public:
    REFIELD()
    int Score;

    GENERATE_EXTENSION_BODY()
};
