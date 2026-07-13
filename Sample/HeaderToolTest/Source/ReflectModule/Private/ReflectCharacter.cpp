// ReflectCharacter.cpp — compiles the annotated header and provides the
// REFUNCTION-declared method bodies so the generated reflection glue links
// cleanly with the stub macros in ReflectMacros.h.
#include "ReflectCharacter.h"

int ReflectCharacter::GetHealth() const
{
    return Health;
}

void ReflectCharacter::SetHealth(int InHealth)
{
    Health = InHealth;
}
