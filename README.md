# Compiler Project
## Introduction

This repository contains my compiler project, a personal project that I have been working on since 2015. I've always liked the idea of creating my own programming language, so years ago I started to experiment with building interpreters and compilers. I had no idea what I was doing at first, but eventually I began to make steady progress. There were many attempts where I experimented and refined my design; this repo documents 16 of these attempts but I suspect there were over 30 in total. 

My compiler is written in C# and compiles my language (name pending) to C. Later, I will probably change this to LLVM IR but I needed to learn C anyway, and this seemed like a good way to do it.

### Design Principles

A few years ago, I noticed that most langauges that are simple to use (Python, Javascript, ect) are also very abstracted from the machine's hardware and will naturally have lower performance. Conversely, languages that are close to the hardware (C, C++, Fortran, ect) are very performant, but are often type unsafe, memory unsafe, and are unpleasant to use when performance is not absolutely critical. Therefore, I wanted to see if I could design a language with the following goals in mind (in no particular order):

- Simplicity. I want my language to be simple and not overly bloated with complicated features or syntax. Reading and writing code should be fast and easy. This might sacrifice conciseness in some situations
- Performance. I don't need my language to be as fast as C in all situations, but I also don't want the programmer to worry about the penalty of the abstractions. All language features should compile to something fast and efficient with as much transparency as possible. This might limit expressiveness in some situations
- Intuitiveness. The language should be easy to learn and use. There shouldn't be any constructions that do weird or unexpected things that are seemingly not consistent with other language features. This might also limit expressiveness. 

In essence, I want a fast, simple language that is easy to learn and use. It will be much more locked down than something like C, but should also significantly outperform languages like Python and Java because of the reduced overhead. Because of these principles, certain elements of the language design became locked-in:

### Consequences of Design Principles

- No classes. Classes create uncessecary complexity at compile-time with inheritance, and also introduce an unacceptable penalty at runtime due to the extra layers of indirection.
- No garbage collection. Garbage collection causes resources to be undeterministically released, which has runtime overhead and makes managing external resources more difficult. RAII is simpler.
- No complicated type inference. I find that the way type inference works in functional langauges can be confounding to newcomers and difficult to work with. Types will be present where they're required and inferred when they can be.
- No unmanaged memory. Memory should be managed automatically somehow, as manual memory management is unsafe, hard to learn, and leads to pointless errors that programmers shouldn't have to think about.
- No buffer overflows. All array indexing should be bounds-checked.
- No dogmatic paradigms. Mutation is not evil, immutability has its place, and procedural code can get the job done. No paradigm will be held above all others, and all have their place in a practical langauge.
- No header files. Overly complicated and not suited to a modern language.

### More Goals

Additonally, here are some things that I would like to incorporate into the language:

- No nullable types. Null is annoying and should go away for good. Whatever needs to be done to get rid of it is acceptable.
- No reference counting. Reference counting causes cache misses, so if it is somehow possible to produce a reasonable language that doesn't need it, I would like to try. Even if this limits expressiveness, I think this is an interesting experiment.
- Pointers. I would like to create a conceptualization of pointers that is simpler and safer to use than traditional pointers.
- Simple syntax. I want the syntax to be a mix of Python and C (weird I know) that is lighter than C but more structured than Python.

So how did I manage to pull all of this together to make a coherent language? Below I will enumerate the main language features in increasing complexity and explain how they accomplish these goals.

## Language Featuers
### Flow Control
Every language needs it, and this language is no exception. So far, I've added if statements, if expressions, and while statements.

```javascript
function main () => int: {
    # If statements use an "if do" or "if do else" structure
    if true do: {
        # Body here
    };
    
    # If expressions use an "if then else structure" and return a value.
    var x <- if false then: 2 else: 6;

    # While loops are straightforward
    while true do: {
        # Body here
    };

    0;
};
```

### Variables
Variables in my language are first-class values: you can either choose to access with a variable's value or the variable itself. Under the hood variables compile to both locals and pointers depending on the situation, and the programmer is presented a much simpler interface for both concepts.

```javascript
function main () => int: {
    # This creates a local variable. The type is implicit.
    # "<-" means that you are storing a value inside of a variable.
    var x <- 4;

    # This creates an alias to another variable
    # "=" means that you are setting one variable equal to another. If one changes, they both change.
    # "@x" means that we are accessing x itself, not the value inside of x.
    var y = @x;

    # Variables can also be allocated on the heap
    var z = alloc 5;

    # Values are stored inside of variables, aliased variables, and heap-allocated variables in the same way.
    # Note the @ is required before the name of the variable, because we are storing a value into the variable itself, not into the value stored in the variable.
    @x <- 1;
    @y <- 2;
    @z <- 3;

    # Variables can be passed to functions. If the function changes the value inside the variable, the original also changes.
    # x is now 5
    mutate_variable(@x);

    0;
};

function mutate_variable (var int w) => void: {
    @w <- 5;
};
```

### Arrays
The challenge of designing arrays was that there are no nullable types in my language. If I instantiate an array with 10 integers, what should happen? That's easy, it should be zeroed out. If I instantiate an array with 10 arrays, what should happen? That's less straighforward. What I decided to do was create a concept of types that have a void value, meaning that if their memory is zeroed out they are still safe to access. Variables types do not have a void value, so you cannot create an empty array of 10 variables (but you can create arrays of variables in other ways). However, in order for 2D arrays to make sense, this meant that arrays themselves must have a void value. The solution I came up with is to make arrays compile to 16 bytes values: 8 bytes for the length and 8 bytes for the pointer to the data. The advantage of this is that if I instantiate a zeroed-out array, the length with be zero and bounds-checking will ensure that I never access the null pointer to the data. The downside is a larger stack size for arrays, but I think that is acceptable. Arrays are currently only heap-allocated, but later I want to add a pass that stack-allocates them if it's possible and reasonable.

```javascript
function main () => int: {
    # Array can be instantiated in the traditional way.
    # Gives an array of 10 zeroes.
    var array1 <- new int[10];
    
    # You can also use a literal syntax.
    var array2 <- [1, 2, 3, 4, 5];

    # 2D arrays work without issue. Later you will be able to specify both dimensions.
    var array3 <- new int[][10];

    # The length of an array can be accessed like this.
    var length <- array1.length;

    # The void value of types can be accessed like this.
    var empty_array <- void as int[];

    0;
};
```

### Scoping
New scopes in my language are introduced with curly braces, and all variables declared in a given scope will be deleted when that scope exists. This will allow for resource management to be tied to variable lifetime in the future, and also makes managing memory quite easy because you know when any given variable will be deleted. However, such strict scoping rules bring up some hard questions, such as:

#### How does scoping interact with variable values?
In my language there is a concept called "variable capturing", which occurs whenever the compiler detects that one variable could access the value stored inside of another variable. To make sure that improper memory accesses don't occur, the capturing variable is then bound to the same scope as the captured variable. Below are examples.

```javascript
function main () => int: {
    # x is bound to this function's scope. 
    var x <- 5;
    
    # y is also bound to this function's scope.
    # y also captures x, meaning that y could be used to access the value stored in x. Because y does not control when x is cleaned up, the lifetime of y cannot exceed that of x. In this case the point is moot because x and y have the same lifetime anyway. 
    var y <- @x;
    
    # A variable storing other variables.
    @ container captures x.
    var container <- @x;
    
    # A new scope is introduced
    {
        var z <- 8;
        
        # Error! z cannot be stored into conainer because z has a stricter scope and will not outlive container
        @container <- @z;
    };
    
    # The above errors prevents an invalid memory access here. What if container held the value of z, which was supposed to be deleted when the above scope ended?
    var access <- container;
};
```

#### How does scoping interact with function calls?
These scoping rules set up an interesting contract between functions and their callers: functions themselves don't need to worry about the scope of the arguments because the callers always handle the lifetimes of arguments and return values. This will be easier to demonstrate than explain.

```javascript
function main () => int: {
    # Bound to the scope of this function
    var array <- [1, 2, 3, 4];
    var bool_value <- true;
    
    # This function call takes no arguments and return an int[][]
    # The compiler deduces that the return type could not capture on anything in this scope. 
    # Thus, x is not bound to any variables in this scope but is still bound to this scope itself (as all variables are).
    var x <- make();
    
    # This function call takes a boolean variable and returns an int[][]
    # The compiler deduces that there is no way an int[][] could capture on a boolean variable.
    # Thus, y is not bound to bool_value, but is bound to this scope.
    var y <- make2(@bool_value);
    
    # This function call takes an int[] and returns an int[][]
    # The compiler deduces that an int[][] could capture an int[], which is given as an argument.
    # Thus, the return value must also capture this argument.
    # Thus, z is bound to both the lifetime of array and this scope.
    var z <- wrap(array);
};

function make () => int[][]: {
    [[1, 2, 3, 4]];
};

function make2 (var bool dummy) => int[][]: {
    [[1, 2, 3, 4]];
};

function wrap (int[] array) => int[][]: {
    [array];
};
```

#### How do I ever pass a reference type to an outer scope?
There are two cases for returning values from scopes: is the type a value type or a reference type? If the type is a value type like `int` or `bool`, then the value can be trivially copied out of a variable without capturing the variable itself. 

```javascript
function demo () => int: {
    # x is bound to the current scope
    var x <- 56;
    
    # Return the value stored in x. Because x is a value type, this copies the value, which does not depend on the current scope.
    x;
};
```

However, for reference types the story is more complicated. For this, we will need an entirely new concept: move semantics.

### Move Semantics
References types, like `var int` and `int[]` are interesting in my language because they are both strictly bound to their containing scope, but can also be freely passed to and from functions. Their ownership is tightly controlled but they can be shared with other code, almost in the same way as references in C++ (but not quite; you can't return references). This model is sufficient for many tasks and doesn't introduce a lot of the headaches that Rust's borrowing system does (nevertheless, this system does come with headaches, that is unavoidable). However, this model completely fails whenever ownership of a reference type needs to be transferred between scopes, which is where move semantics come in. Move semantics allow this ownership transfer to occur with complete transparency and also preserves the automatic memory management from the scoping rules.

#### Move semantics and variables
```javascript
function main () => int: {
    var x <- 56;
    var y = @x;
    var z = alloc 89;
    var w <- alloc 23;
    
    # Only allocated variables can be moved.
    # move Q return a variable of whatever type Q is. That's why "=" is used instead of "<-" to assign moved variables.
    var a1 = move @x;       # Error. Cannot move a local.
    var a5 = move x;        # Error. Cannot move value types. 
    var a2 = move @y;       # Error. Cannot move non-allocated variables
    var a3 = move @z;       # Success. 
    var a6 = move @w;       # Error. Cannot move a local.
    var a4 = move w;        # Success. 
    
    # After a move, the moved variable cannot be accessed. Ownership is completely passed to the new location.
    var b = move @y;
    @y <- 5;                # Error. Cannot access a moved variable.
    
    # Variables can now be returned from inner scopes.
    var c = {
        var inner = alloc 4;
    
        move @inner;
    };
    
    # When a variable is moved, the new location does not capture the old variable, as the old variable is gone.
    # d does NOT capture y because y has been moved. Therefore, d is only bound to the current scope, but not to y.
    var d <- move @y;
    
    # This impacts function calls as well    
    # e1 will capture y because of the ruled defined in the scoping section
    # This means that e1 cannot outlive y
    # A consequence of this is that e1 cannot be returned from the function because it is bound to a local.
    var e1 <- test(@y);
    
    # However, e2 will not capture y because y has been moved.
    # This means that e2 can outlive y, and can be used as a return value.
    var e2 <- test(move @y);
    
    0;
};

function test (var int x) => var int: {
    # Parameters are scoped to the current function, but they can also be moved to a different scope.
    # This is only possible because the caller will manage the lifetime of the return value depending on the arguments passed.
    move @x;
};
```

#### Move semantics and arrays
```javascript
function main () => int: {
    # Unlike variables, you can move all arrays because all arrays are allocated on the heap. Otherwise, it works in much the same way.
    var array <- [1, 2, 3, 4];
    
    # Ownership of array has been transferred to a
    # Notice that it's not "@array", but just "array". This is because we're moving the value stored in the array variable, and not the array variable itself.
    var a = move array;
    
    # You can return arrays from an inner scope    
    var b <- {
        var inner <- [1, 2, 3];
    
        return move inner;
    };
    
    # c will be bound to the lifetime of array.
    var c <- test(array);
    
    # d will not be bound to the lifetime of array.
    var d <- test(move array);
    
    0;
};

function test (int[] x) => int[]: {
    # Same story as before, but this time we're moving an array instead of a variable.
    move x;
};
```

#### Implementation of move semantics
Move semantics are fantastic, and solve most of the expressiveness issues that the strict scoping created. However, it also introduces a number of challenges for the compiler that took a while to sort out. Specifically:

- All reference types can either be bound to the current scope or moved away. If they are moved, the current scope should not try to delete them.
- Ownership of a reference type can be passed to a function. Thus, all functions must clean up any moved arguments.

The way I solved these two problems was by adding a way to track, at runtime, which reference types need to be cleaned up by the current scope and which ones don't. When a variable is moved, the original variable is set to not needing cleanup, and the new variable is set to needing a cleanup. 

This was done by hiding a single bit inside of the pointers that variables and arrays compile to. When this bit is zero, no cleanup is necessary; when the bit is one, the scope must clean up the value. When a variable is moved, the old pointer is zeroed out (setting the bit to 0) and the new pointer's bit is set to one. I decided that this bit should be the lowest-order bit of the pointer, which also means that no pointer can use an odd address. If all the memory in my program in aligned to 2 bytes, effectively I will get a free bit at the end as long as this bit is flipped before a dereference. Because of pipelining and the relatively high cost of memory accesses, this should have no tangible effect on performance and is much preferrable to fat pointers. This allows ownership to be tracked and managed by the program at runtime, and allows the freeing of resources to still happen as soon as a variable goes out of scope. At the end of every scope, the cleanup code simply needs to check this bit before deleting any memory. 

This brings us to a very interesting situation: we have reference types that are automatically memory managed, that can be freely passed to and returned from functions without an ownership change, that can be transferred between scopes, and that are not reference counted or garbage collected. I might be overly optimistic here, but I beleive this might just be a viable strategy for memory management that offers enough flexibility to be useful while still being very simple, lightweight, and deterministic. 
