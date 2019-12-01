#include "Language.h"

#ifndef PROGRAM_H
#define PROGRAM_H

typedef struct Point Point;

typedef struct A A;

typedef struct B B;

int main();

struct Point {
    int x;
    int y;
};

Point f(Point p);

struct A {
    B* b;
};

struct B {
    A* a;
};

#endif
