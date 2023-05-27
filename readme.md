# Helix Programming Language

Welcome to the Helix repository! This page serves as an introduction to the language as well as  a basic language specification.

## Table of Contents
1. [Project Overview](#project-overview)
    - [Why Another Language?](#why-another-language?)
    - [Design Philosophy](#design-philosophy)
2. [Major Features](#major-features)
    - [Region-based memory management](#region-based-memory-management)
    - [Automatic region assignment](#automatic-region-assignment)
    - [Type system and values as singleton types](#Type-system-and-values-as-singleton-types)
3. [Syntax Description](#syntax-description)
    - [Variables and pointers](#variables-and-pointers)
    - [Functions](#functions)
    - [Structs](#structs)
    - [Unions](#unions)
    - [Loops and control flow](#loops-and-control-flow)
    - [New operator](#New-operator)
    - [As operator](#as-operator)

## Project Overview

### Why Another Language?
Helix is a new small, statically typed, compiled systems programming language. Whenever a new language is announced the inevitable question comes up: why do we need another new language? The specifics of the answer will be described below, but in general Helix is attempting to explore a part of the programming language design space that is just now being recognized as important. Rust recently brought compile-time memory management into the zeitgeist of programming, but at the cost of including an overbearing borrow checker into the language. However borrow checking is only one possible answer to memory management and Helix is attempting to explore a different solution with different tradeoffs to existing languages. See the sections on memory management for details.

In the world of new systems languages there has been much ink spilled over the idea of "replacing" C or creating a "better" C. To clear the air, Helix is not an attempt to replace C by any stretch of the imagination nor is it targeting the same problem space as C; the Helix compiler even uses C as an intermediate representation so replacing it is out of the question. 

### Design Philosophy

My design philosophy for Helix is probably very different from other systems languages and particularly the newer systems languages like Zig, Nim, Odin, and Lobster. Those projects are attempting to take some useful "systems" capabilities from C and C++ and wrap them in a better, more ergonomic language. That is a noble goal, but with Helix I am approaching the problem from the other direction entirely: rather than start with low-level concepts and building upwards I am starting with higher-level concepts and building downwards. 

Helix is supposed to be an efficient higher-level language rather than an expressive low level language; in practical terms this has rather large implications, like making heavy use of safe implicit conversions, local type inference, implicit allocations, operator overloading, etc. To spell this out in more detail, here are some of the design principles I have used when creating Helix: 

- **Simplicity is Primary** - Every language has a story to tell, and every good language has a simple story. C, Python, Java, ML, and Lisp all have one thing in common: those languages have a perspective on the world and use that perspective to greatly simplify programming. In Java everything is an object, in Lisp all code is data. Helix will attempt something similar and make a series of design decisions upfront and then exploit those decisions for the greatest simplicity possible.

- **Implicit is Better than Explicit** - Python's rule for perferring explicit code rather than implicity has perhaps been taken too far or out of context, so this principle is here as a counter-balance. Recently among Rustacians it has been a fad to state things such as "no implicit conversions", "no hidden control flow", and "no hidden allocations". While not having misleading code is desirable, it is the primary job of a programming language to abstract away mechanical details so that the developer can focus on the problem at hand. When it is possible to cleanly and efficiently abstract details from the programmer Helix will do so; when it is not, explicit code is of course preferred.

- **Heisenberg's Compiler** - A programming language is a technology of the mind that should help you solve problems faster, not slower. There is nothing worse than imagining the solution to a problem but having to fight the compiler to translate that idea into code. The compiler of a good language should act like a quantum object: when left alone it does it job correctly and when observed directly it jumps out of the way. In practice this means having fast compile times, good error messages, and deferring compile-time errors to runtime checks in debug mode for faster prototyping.

- **Structured Programming** - An old classic, structured programming is the idea that programs should be understandable with local reasoning only. Historically this was understood in terms of control flow (ie, no gotos) but Helix takes with principle one step further with [region-based memory management](#region-based-memory-management). Every block returns to its caller, each allocation is freed by its region, and every resource is cleaned up by its owner. 


## Major Features

### Region-based memory management

Helix uses a lexical, heirarchical, region-based memory management system for heap allocation. That's a lot of descriptors, but basically what it means is that Helix allocates all dynamic memory to one of several nested memory blocks called regions. Each region can be thought of as a simple bump allocator, and all the memory in a region is deallocated at once when the region goes out of scope. In helix a region is declared as follows: 

    region OptionalRegionName {
        // Code that uses memory in this region
    };
    
Because regions are lexical it is very easy to immediately see where memory is being allocated and deallocated in the program, and lexical regions also have the added benefit of freeing the programmer from managing individual object lifetimes one at a time. Regions are also extremely performant because they can bulk-deallocate data structures in constant time rather than traversing the entire thing. 

Helix's regions are strictly heirarchical, which means that its heap can best be conceptualized as a stack of regions. New regions are pushed to the bottom of the stack and must be freed before any region above them is freed; any the memory in lower regions has a shorter lifetime than all the memory in higher regions. 


### Automatic region assignment

Not many languages have experimented with region-based memory management ([ML](http://mlton.org/Regions), [Cyclone](https://cyclone.thelanguage.org) and [Cone](https://cone.jondgoodwin.com) being notable exceptions), and the languages that do must solve one primary problems: How are memory allocations assigned to regions?

    region A {
        region B {
            region C {
                // Which region does this allocate to?
                let x = new int[45];
                
                ...
            };
        };
    };

For the first question, usually the strategy used is that regions are given names and those names are incorporated into the type system: a pointer `p` that points to a region `R` might have a different type (`p'R`) than a similar pointer pointing to a region `E` (`p'E`). This brings all the benefits of static typing to memory management and lets the compiler statically verify that all memory accesses are valid by comparing types. This approach also allows great flexibility and opens the door to non-lexical regions, but it does have significant drawbacks: language complexity is greatly increased and code becomes context-dependent in a way that makes local reasoning and separate compilation very difficult.

Helix takes a different approach entirely: rather than the compiler asserting that each memory usage is valid according to the regions, the compiler looks at how memory is used and infers which region memory should be allocated to based on that usage. At first this sounds impossible, because no static analysis will every be able to determine full memory usage patterns at compile time (see: [the halting problem](https://en.wikipedia.org/wiki/Halting_problem)). However, never to let the impossible get in the way of decent memory management, Helix is able to avoid these issues due to the unique properties of heirarchical regions. Here's how it works:

Each level in the stack of regions is assigned an integer, with the first region being `0`, the second being `1`, etc. When a variable is allocated, like the array in the above example, the Helix compiler performs a flow analysis of the program and determines which regions that memory needs to outlive in order to garauntee memory safety. Consider the following code:

    region A {          // id= 1
        var x = ...
    
        region B {      // id= 2
            x = new int[10];
        };
    };
    
When the compiler gets to the array allocation, it will perform a flow analysis and determine that the new array must outlive the variable `x`, and because `x` does not escape its enclosing region the compiler will pick the first region enclosing `x`, which is `A` with `id= 1`. That will translate into the second entry in the region stack, which can be computed in constant time because regions are heirarchical. So far so good, but what happens in a more complicated example? What if an allocation needs to outlive two different variables? This is where the magic of heirarchical regions really comes into play, as well as another trick the Helix compiler is able to employ: region ids are not computed at compile time, but at runtime. Consider this second example:

    // In Helix int^ is a reference to an int. Think of it like a pointer
    // int[]^ is a reference to an int array
    func set_arrays_(let array1 as int[]^, let array2 as int[]^) {
        let result = [ 10, 9, 8, 7, 6 ];
        
        array1^ = result;     
        array2^ = result;           
    };
    
While admittidely a contrived example, this function presents a huge challenge to region inference algorithms because `result` must outlive both `array1` and `array2`, which could have very different lifetimes. If regions could be non-lexical this would present an unsolvable problem because the lifetimes of `array1` and `array2` might not overlap at all. Because Helix's regions are lexical and heirarchical it is garaunteed that `array1` will outlive `array2`, that `array2` will outlive `array1`, or that they are allocated in the same region and have the same lifetime.

References (and arrays) in Helix are "fat" in that they store both the address of the memory being pointed to as well as an integer representing which region that memory is allocated on in the region stack. In the code above, the compiler will reach the array allocation, perform flow analysis, and determine that `result` must outlive `array1` and `array2`. It will then form an expression representing which region `result` should be allocated on, which will be `min(regionof(array1), regionof(array2))`. This will not be computed at compile time but at runtime and because it is just a few integer instructions the performance cost will be minimal-to-none. 

This system allows Helix to mix static analysis with light runtime computations to infer memory regions for every allocation in a program, all with minimal overhead and no type annotations. Heirarchical regions give Helix a very unique combination of a simple syntax and complex features like closures without the baggage of a high latency garbage collector.

### Type system and values as singleton types

The Helix language was designed with metaprogramming in mind, and consequently it has a very unique type system that is quite different from most mainstream languages. In essence the biggest difference is that, in Helix, types and values are the same concept. 

Consider the integers `1`, `2`, and `3`. In Helix's world each of these integers is its own unique entity, so `1` is a type, `2` is another type, and `3` is yet another type. 

    let x = 45;
    
In this program, the constant `x` will actually have type `45` rather than type `int` like most languages, but because types are inferred bottom-up this never concerns the programmer. In Helix `int` is conceptualized almost like an interface that defines arithmetic, where each integer type `1`, `2`, `3`, etc is an implementation of that interface. In practice the compiler has a robust implicit-type conversion system so individual integer types will be cast to `int` whenever necessary, with no thought required of the programmer.

This principle of values being singleton types extends not just to integers, but the entire language. In Helix every possible value of an integer, a struct, a union, etc has its own type that can be easily upcast by the compiler when necessary. 

At first this might seem like an unnecessarily decadent type system, but consider the ramifications: all information about constant values will be exposed by the type system itself within the compiler. This means that for any expression that has no run-time dependence, the compiler can use the type system to fully evaluate it during type checking, store the resulting value in a type, and continue type checking like nothing happened. This is the holy grail of metaprogramming: the ability to seamlessly blend compile-time computation and runtime computation with the same language and syntax. 

Helix actually gets its name from this process: types and values can be imagined as two sides of a double helix, constantly switching places as the compiler alternates between static analysis and runtime computation within the same compilation process. In the future this type system will allow such things as powerful generics, partial evaluation, and seamless metaprogramming. Imagine the following code snippet:

    func generic_function(let x as T as Any) as int -> {
        if T == int then x + 1 else 0;
    };
    
Generics will blend seamlessly with normal code without messy angle brackets, computations on types can be easily mixed with runtime code using the same syntax, function specialization will happen transparently, and specialized functions will be partially evaluated automatically. This is the future of Helix and is where I intent to take this project in the future.


## Syntax Description

### Variables and pointers

Variables in Helix come in two flavors: constants and variables. Constants are defined by `let` while variables are defined by `var`, and types are inferred for both. Constants always have the same type as their value, whereas for variables the compiler will abstract individual integer types like `1`, `2`, etc into the more generic `int` type for convienence (see description of type system for details).

    let x = 45;
    var y = 16;
    
    y = 45;    

### Functions

### Structs

### Unions

### Loops and control flow

Helix comes with a standard assortment of control flow, including `for` loops, `while` loops, `if` expressions, and match statements.

    // A while loop
    while x < 10 do x++;
    
    // A basic for loop and if expression
    var total = 0;    
    for i = 0 to 10 do {
        total += if i < 10 then 0 else 1;
    };
    
    // The statement version of an if expression
    if x < y do {
        ...
    };

### New operator

### As operator