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

int sum(Point p);

void mutate(int* some);

int fib(int n);

struct A {
    B* b;
};

struct B {
    A* a;
};

#endif
