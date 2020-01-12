# Compiler

This repository contains my compiler project, a personal project that I have been working on since 2015. I've always liked the idea of creating my own programming language, so years ago I started to experiment with building interpreters and compilers. I had no idea what I was doing at first, but eventually I began to make some progress. There were many attempts where I experimented and refined my design; this repo documents 16 of these attempts but I suspect there were over 30 in total. 

I love fighting with a difficult problem, especially if I have the time, and building a working (if simplistic) compiler was the hardest I've encountered so far. Over the years this project has taught me so much about software design, design patterns, different styles of programming, language implementation, hardware layout, and so much more. The most difficult thing to tackle was closure conversion with reference counting, but I finally got it in Attempt15. My target language of choice has also been C, so naturally this project forced me to learn more about C and how it operates than I ever thought I would. By far the most fun aspect of writing a compiler is inventing the language you're compiling, and I've greatly enjoyed thinking about languages, their features, and what goes into a great language.

My most recent attempt, Attempt16, is not very complete (yet) but it holds the most promise. For this attempt, I decided to lower the complexity of the language I'm compiling from that of Attempt15, where the main headaches were closure conversion, generic functions, and reference counting (specifically, when all of those occurred at the same time). I thought it would be interesting for Attempt16 if I created a language that didn't use reference counting, garbage collection, or manual memory management. Essentially, I wanted to see if a viable language could be created only with the equivalent of C++'s unique_ptr. The jury is still out on that, but so far I do think it is possible. My language will use a combination of strict scoping rules, RAII, and move semantics to ensure that only valid memory accesses are made. My aspirational goal is for the language to be powerful, easy to use, and simple while at the same time being lightning fast. I will only use zero-cost abstractions, and each feature in my language will compile more-or-less straightforwardly to C.

So far, I've implemented the following in Attempt16:
- Top level functions (nested functions coming later)
- Basic integers/math
- Local scopes
- Local variables
- If statements
- While loops
- Pointers (with scope enforcement)
- Structs
- Out of order name resolution

The following features are things that I've planned out and know how to implement, but I haven't had the time to implement them all yet
- Namespaces
- Nested struct declarations
- Methods (functions in a struct)
- Move semantics / Heap allocation
- Union types (inheritance replacement)
- Closures (with scope enforcement)
- Type aliases
- Generic structs / unions

Below is a fully working code sample that can be compiled to C using Attempt16:
```javascript
function main () => int: {	
  var p <- new Point {
    x = 4
    y = 7
  }
	
  var c <- sum(p)
  
  mutate(@c)
  
  0
}

function mutate (var int some) => void: {
  @some <- 2 * some
}

function sum (Point p) => int: {
  p.x + p.y
}

function fib (int n) => int: {
  var a <- 0
  var b <- 1
  var count <- n

  while count do {
    var c <- a + b
    
    @a <- b
    @b <- c
    @count <- count - 1
  }
  
  count
}

struct Point {
  int x
  int y
}

struct A {
  var B b
}

struct B {
  var A a
}
```

This compiles to a header file and a c file:
```c
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

int mutate(int* some);

int fib(int n);

struct A {
    B* b;
};

struct B {
    A* a;
};

#endif
```

```c
#include "program.h"

int main() {
    Point _temp_structinit_0;
    _temp_structinit_0.x = 4;
    _temp_structinit_0.y = 7;
    
    Point p = _temp_structinit_0;
    
    int _temp_call_0 = sum(p);
    
    int c = _temp_call_0;
    
    char _temp_call_1 = mutate(&c);
    
    return 0;
}

int mutate(int* some) {
    *some = (2 * *some);
    
    return 0;
}

int sum(Point p) {
    return ((p->x) + (p->y));
}

int fib(int n) {
    int a = 0;
    
    int b = 1;
    
    int count = n;
    
    while (count) {
    
        int c = (a + b);
        
        a = b;
        
        b = c;
        
        count = (count - 1);
    }
    
    return count;
}
```
