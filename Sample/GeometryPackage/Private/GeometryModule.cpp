#include "GeometryModule.h"

int GeometryRect::Area(int width, int height)
{
    return width * height;
}

int GeometryRect::Perimeter(int width, int height)
{
    return 2 * (width + height);
}
