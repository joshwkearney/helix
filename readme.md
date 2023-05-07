# Helix Programming Language

Welcome to the Helix repository! This page serves as an introduction to the language as well as  a basic language specification. See the code samples to get a general feel for Helix or the more detailed descriptions of language features for a more in-depth look.

## Table of Contents
1. [Code Samples](#code-samples)
    - [Fibonacci](#fibonacci)
2. [Project Overview](#project-overview)
3. [Major Features](#major-features)
    - [Region-based memory management](#region-based-memory-management)
    - [Automatic region assignment](#automatic-region-assignment)
    - [Type system and values as singleton types](#Type-system-and-values-as-singleton-types)
4. [Syntax Description](#syntax-description)
    - [Variables and pointers](#variables-and-pointers)
    - [Loops and control flow](#loops-and-control-flow)
    - [Functions](#functions)
    - [Structs](#structs)
    - [Put and new](#put-and-new)
    - [As operator](#as-operator)


## Code Samples
### Fibonacci
    func fib(let x as int) as int {
	    if x <= 1 
	    then x
	    else fib(x - 1) + fib(x - 2);
    };

### Selection sort

### Binary search


## Project Overview

This repository contains the compiler for the Helix language, which is a statically-typed, ahead-of-time compiled, small, intuitive systems language. The compiler is written in C# and Helix itself compiles to C for maximum compatibility and portability. The eventual goal will be to self-host the compiler in Helix but that is yet a ways off.

Helix is being designed to be a modern systems language that is as simple and intuitive as possible while still retaining best-in-class performance characteristics. The goal of this project is to significantly lower the conceptual barrier to entry for high-performance code and to make C-like performance availible in a language that feels as simple as Python. In order to accomplish this, Helix is being designed with the following principles in mind, in order of importance:

1. Simplicity. Every great programming language is conceptually and syntactically simple, even if that simplicity is abstracting over significant complexity under the hood. The primary goal of Helix is to offer a small set of modern features in a cohesive, intuitive package that is easy to learn and apply. This will not be a kitchen sink language, and in that sense Helix might be seen as a true successor to C.

2. Performance. 

3. Compatibility. 


## Major Features

### Region-based memory management

Helix uses a lexical, heirarchical, region-based memory management system for heap allocation. That's a lot of descriptors, but basically what it means is that Helix allocates all dynamic memory to one of several nested memory blocks called regions. Each region can be thought of as a simple bump allocator, and all the memory in a region is deallocated at once when the region goes out of scope. In helix a region is declared as follows: 

    region OptionalRegionName {
        // Code that uses memory in this region
    };
    
Because regions are lexical it is very easy to immediately see where memory is being allocated and deallocated in the program, and lexical regions also have the added benefit of freeing the programmer from managing individual object lifetimes one at a time. Regions are also extremely performant because they can bulk-deallocate data structures in constant time rather than traversing the entire thing. 

Helix's regions are also heirarchical, which means that its heap can best be conceptualized as a stack of regions. New regions are pushed to the bottom of the stack and must be freed before any region above them is freed; any the memory in lower regions has a shorter lifetime than all the memory in higher regions. 


### Automatic region assignment

Not many languages have experimented with region-based memory management ([Cyclone](https://cyclone.thelanguage.org) and [Cone](https://cone.jondgoodwin.com) being notable exceptions), and the languages that do must solve two primary problems: 1) How are memory allocations assigned to regions? 2) How do memory-complex programs operate around region lifetimes?

    region A {
        region B {
            region C {
                // Which region does this allocate to?
                let x = new int[45];
                
                ...
            };
        };
    };

For the first question, usually the strategy used is that regions are given names and those names are incorporates into the type system: a pointer `p` that points to a region `R` might have a different type than a similar pointer pointing to a region `E`. This brings all the benefits of static typing to memory management and lets the compiler statically verify that all memory accesses are valid by comparing types. This approach also allows great flexibility and opens the door to non-lexical regions, but it does have significant drawbacks: language complexity is greatly increased and code becomes context-dependent in a way that would violate the simplicity I'm aiming for with Helix.

Helix takes a different approach entirely: rather than the compiler asserting that each memory usage is valid according to the regions, the compiler looks at how memory is used and infers which region memory should be allocated to based on that usage. At first this sounds impossible, because no static analysis will every be able to determine full memory usage patterns at compile time due to the halting problem. However, never to let the impossible get in the way of decent memory management, Helix is able to avoid these issues due to the unique properties of heirarchical regions. Here's how it works:

Each level in the stack of regions is assigned an integer, with the first region being `0`, the second being `1`, etc. When a variable is allocated, like the array in the above example, the Helix compiler performs a flow analysis of the program and determines which regions that memory needs to outlive in order to garauntee memory safety. Consider the following code:

    region A {          // id= 1
        var x = ...
    
        region B {      // id= 2
            x = new int[10];
        };
    };
    
When the compiler gets to the array allocation, it will perform a flow analysis and determine that the new array must outlive the variable `x`, and because `x` does not escape its enclosing region the compiler will pick the first region enclosing `x`, which is `A` with `id= 1`. That will translate into the second entry in the region stack, which can be computed in constant time because regions are heirarchical. So far so good, but what happens in a more complicated example? What if an allocation needs to outlive two different variables? This is where the magic of heirarchical regions really comes into play, as well as another trick the Helix compiler is able to employ: region ids are not computed at compile time, but at runtime. Consider this second example:

    func region_test(let x as int[], let y as int[]) as int[][] -> {
        let result = new int[][2];
        
        result[0] = x;
        result[1] = y;
        
        result;
    };
    
This function presents a huge challenge to region inference algorithms because `result` must outlive both `x` and `y` because it contains references to both variables. If regions could be non-lexical this would present an unsolvable problem because the lifetimes of `x` and `y` might not overlap at all, but because Helix's regions are lexical and heirarchical it is garaunteed that `x` will outlive `y`, that `y` will outlive `x`, or that they are allocated in the same region. 

Pointers (and arrays) in Helix are "fat" in that they store both the address of memory being pointed to as well as an integer representing which region that memory is allocated on in the region stack. In the code above, the compiler will reach the array allocation, perform flow analysis, and determine that `result` must outlive `x` and `y`. It will then form an expression representing which region `result` should be allocated on, which will be `min(regionof(x), regionof(y))`. This will not be computed at compile time but at runtime, and because it is just a few integer instructions the performance cost will be minimal-to-none. 

This system allows Helix to mix static analysis with light runtime computations to infer memory regions for every allocation in a program with minimal overhead and no type annotations, which as far as I'm aware has not been accomplished in any previous programming language. Heirarchical regions give Helix a very unique combination of a simple, annotation-free syntax with deterministic, no-GC memory management. 

What about long-lived data structures? Don't many programs use data structures that live in the global scope and will never be deallocated? Indeed this is true, and it is a fine objection to lexical regions. In response I would say that most memory is lexically scoped, even if some is not, so in general Helix's memory management will be good enough for most use cases. In situations where non-lexical scoping is truly needed, I am planning on implementing a series of data structures in the standard library based on malloc/free that provide an "out" for these situations; these structures will use value-semantics to make sure that data in the data structures can never point to invalid memory. Every memory management technique is a balance of compromises, and for Helix, lexical regions makes the most sense. In the far future I might experiment with linear types or something similar but it is not a high priority at the moment. 


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
    

### Functions


### Structs

### Put and new

### As operator