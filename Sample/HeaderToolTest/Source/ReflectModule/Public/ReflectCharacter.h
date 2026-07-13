// ReflectCharacter.h — a RECLASS-annotated class that exercises the
// ResetHeaderTool parser with member annotations: REFIELD on fields and
// REFUNCTION on methods. The plugin only emits class-level codegen, so the
// generated HeaderToolGen/ tree has the same shape as ReflectObject's; the
// REFIELD/REFUNCTION annotations here drive the parser path that recognizes
// annotated members (verifying it does not choke on a non-empty class body).
//
// HeaderTool contract: "<Name>.extension.h" must be the last #include in the
// header, and GENERATE_EXTENSION_BODY() must appear in the class body.
#pragma once

#include "ReflectMacros.h"
#include "ReflectCharacter.extension.h"

RECLASS()
class ReflectCharacter
{
public:
    REFIELD()
    int Health;

    REFIELD()
    float Speed;

    REFUNCTION()
    int GetHealth() const;

    REFUNCTION()
    void SetHealth(int InHealth);

    GENERATE_EXTENSION_BODY()
};
