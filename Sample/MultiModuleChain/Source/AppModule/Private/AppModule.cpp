#include "AppModule.h"

#include "MiddleModule.h"

#include <cstdio>

void App::Run()
{
    MiddleUtil util;
    // HAS_BASE_MODULE is a PublicDefine declared by BaseModule two dependency hops away
    // (App depends on Middle, Middle depends on Base) - it's visible here because
    // PublicDefines propagate transitively through the module dependency chain.
#ifdef HAS_BASE_MODULE
    printf("[transitively defined by BaseModule] ");
#endif
    printf("SquareThenAddOne(5) = %d\n", util.SquareThenAddOne(5));
}

void main()
{
    App app;
    app.Run();
}
