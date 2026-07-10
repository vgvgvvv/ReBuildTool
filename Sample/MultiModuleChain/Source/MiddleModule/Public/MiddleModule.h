#pragma once

#include "MiddleModule.internal.h"

// Resolvable because BaseModule's PublicIncludePaths propagate through this
// module's own dependency declaration.
#include "BaseModule.h"

class MIDDLEMODULE_API MiddleUtil
{
public:
    int SquareThenAddOne(int x);
};
