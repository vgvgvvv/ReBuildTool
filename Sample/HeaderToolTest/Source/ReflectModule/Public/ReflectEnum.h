// ReflectEnum.h — a REENUM-annotated standalone enum that drives the
// ResetHeaderTool parser's enum path. As with classes, the plugin only emits
// file/extension-level glue; this header exists to verify the parser accepts a
// REENUM annotation and still produces the expected HeaderToolGen/ tree.
//
// HeaderTool contract: "<Name>.extension.h" must be the last #include.
#pragma once

#include "ReflectMacros.h"
#include "ReflectEnum.extension.h"

REENUM()
enum class ReflectColor
{
    Red,
    Green,
    Blue
};
