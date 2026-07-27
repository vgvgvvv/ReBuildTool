#include "AppModule.h"

// Comes from the GeometryPackage package, not from this project's Source/.
#include "GeometryModule.h"

#include <cstdio>

void App::Run()
{
    GeometryRect rect;
    printf("area(2, 3) = %d\n", rect.Area(2, 3));
    printf("perimeter(2, 3) = %d\n", rect.Perimeter(2, 3));
}

int main()
{
    App app;
    app.Run();
    return 0;
}
