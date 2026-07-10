// Stub of engine's Misc/NonCopyable.h — the RHT-generated module-level glue
// (ResetEngineExtension/<Module>.h) derives its I<Module> singleton from
// NonCopyable. Provided here so the generated code compiles without the full
// engine runtime. Mirrors ResetEngine's definition.
#pragma once

class NonCopyable
{
public:
    NonCopyable() {}

private:
    NonCopyable(const NonCopyable&);
    NonCopyable& operator=(const NonCopyable&);
};
