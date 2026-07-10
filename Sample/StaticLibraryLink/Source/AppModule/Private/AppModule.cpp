#include "AppModule.h"

#include "MathCoreModule.h"

#include <cstdio>

void App::Run()
{
    MathCore core;
    printf("2 + 3 = %d\n", core.Add(2, 3));
    printf("2 * 3 = %d\n", core.Multiply(2, 3));
}

void main()
{
    App app;
    app.Run();
}
