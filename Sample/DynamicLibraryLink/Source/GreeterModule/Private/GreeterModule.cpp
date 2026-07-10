#include "GreeterModule.h"

int Greeter::Add(int a, int b)
{
    return a + b;
}

const char* Greeter::Greeting()
{
    return "hello from a dynamic library";
}
