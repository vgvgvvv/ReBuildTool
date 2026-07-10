#include "AppModule.h"

#include "GreeterModule.h"

#include <cstdio>

void App::Run()
{
    Greeter greeter;
    printf("%s\n", greeter.Greeting());
    printf("1 + 2 = %d\n", greeter.Add(1, 2));
}

void main()
{
    App app;
    app.Run();
}
