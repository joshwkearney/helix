# The Trophy Programming Language

This is the main repository for the Trophy programming langauge, my attempt at creating a modern systems programming language. Trophy is very much a work-in-progress, but enough is implemented to convey my vision and to demonstrate why Trophy will be an improvement over current languages. If you are curious about the syntax, please see the example programs at the end of this readme.

## Motivation

Before describing Trophy, it is worth explaining why creating another programming language is necessary. As programming languages have evolved over the years, they have become more abstract to handle increasingly complex problems, but these abstractions often have associated runtime costs. Simultaneously, the syntax of these languages has evolved to become easier to use, but often at the cost of losing compile-time type information. This leads us into our current odd situation where high-level languages have evolved a great deal and are extremely useful for a huge variety of problems, but faster systems languages have remained largely stagnant. Trophy attempts to bring many of the lessons learned from high-level languages back into the systems realm without sacrificing the features that make systems languages useful.  

## Design Principles

Trophy was designed with a few principles in mind, enumerated below:

- Simplicity. Trophy should be simple, in both syntax and semantics. A current pain-point of current systems languages like C++ is complexity, where features interact in non-obvious ways to make it difficult to reason about a program. Even in new systems languages like Rust (which has largely similar goals to Trophy), complexity can be a major barrier to adoption. 

- Performance. My original goal for Trophy was for it to be "faster than C". However, that statement is fraught in a number of ways. More accurately, I want the features Trophy shares with C to be as fast as their C equivalents. This is still a very ambitious performance goal, but because Trophy will include a few higher-level features (like closures) the distinction is important to make.

- Safety. Memory safety bugs account for 70-90% of all security vunerabilities in major software projects ([here](https://www.zdnet.com/article/microsoft-70-percent-of-all-security-bugs-are-memory-safety-issues/), [here](https://www.chromium.org/Home/chromium-security/memory-safety), and [here](https://alexgaynor.net/2020/may/27/science-on-memory-unsafety-and-security/)). Therefore, any kind of memory unsafety whatsoever is unacceptable in new languages.

## Salient Features

### Compilation
Currently, Trophy compiles to C89 (compatible with C++). This allows Trophy to be mixed into existing C and C++ projects, as well as ensuring a high level of portability.

### Variables and Arrays
Languages with pointers have a very rich interaction between variables, values, and arrays in a way that most high-level languages miss out on, and I wanted to preserve this richness in Trophy. Variables in trophy are declared using the `var` keyword for mutable variables and the `ref` keyword for immutable ones, and the types are inferred from the assigned value. 

```
var x = 5;
ref y = 8;
```

In Trophy, the name of a variable like `x` always refers to the value stored inside of that variable and never to the location of the variable itself. An assignment like `x = x + 4;` is invalid because you are trying to store the value of x, incremented by 4, back into the value of x. Insead, we must store values into the location of a variable, which is retrieved using the prefix `@` operator. Therefore, the above assignment should be written as `@x = x + 4;`. At first this might seem pointless, but in Trophy the locations of variables are first-class values of type `var[T]` or `ref[T]`. We can pass the location of a variable to a function like so:

```
func variable_example_1 (x as var[int]) as void => {
    @x = 45;
};
```

This function will take the location of a int variable and store 45 into it, effectively passing the variable by reference. This behavior becomes even more useful when considering arrays. Arrays are indexed normally with square brackets `arr[n]` to refer to the value at position `n` in an array. However, you can also index an array using the literal index operator `arr@[n]`, which refers to the location of the variable cell at position `n`. Therefore, we could call our above function like so `variable_example_1(arr@[0])`. Variable indicies are set using the following syntax: `arr@[4] = 4;`.

### Memory Management
Trophy uses [region-based memory management](https://en.wikipedia.org/wiki/Region-based_memory_management) in place of reference counting, a tracing GC, or manual memory management. Regions are named scopes that allocate all memory used in that scope in a contiguous buffer using a bump-allocator. All memory will be subsequently freed when the scope exists, emulating much of the benefit of a traditional garbage collector without any of the overhead. Below is an example of creating and using a region in Trophy.

By default, memory is allocated onto a special region called `stack`, but each function also has an implicit region called `heap` that can be used for allocating function return values. Precisely which region the `heap` refers to is determined by the function's caller.

```
func region_example () as void => {
    # Allocated on the stack
    var arr = [1, 2, 3];
    
    # Allocated on the heap, which is determined by the function caller.
    # Heap-allocated variables are used to return values from functions.
    var arr2 = from heap do [4, 5, 6];

    # Create a new region to handle memory allocations
    region R {
        # Allocates a new array onto the region "R"
        var arr3 = from R do [10, 9, 8, 7];
        
        # Error! Cannot assign memory from a region to a location outside of that region
        @arr = arr3;
    };
};
```

### Multithreading
Trophy using structured concurrency ([here](https://en.wikipedia.org/wiki/Structured_concurrency) and [here](https://vorpus.org/blog/notes-on-structured-concurrency-or-go-statement-considered-harmful/)) to support multithreading. Specifically, regions take on the role of a join-point in the program: any async tasks spawned within a region must terminate before that region exits. Below is an example:

```
func concurrency_example () as void => {
    # Create a new region, which will be functioning as an async join point.
    region {
        # Spawn two tasks for background work.
        async task1();
        async task2();
        
        # Run a third task synchronously.
        task3();
    };
    
    # The region will only exit to reach this point once all of task1, task2, and 
    # task3 have either returned or panicked. If not all tasks are finished upon 
    # reaching the end of the region, the region will block the current thread 
    # until they are.
};
```

## Roadmap

- [X] basic features (variables, conditionals, loops, etc)
- [X] region-based memory allocation lifetime verification 
- [X] arrays and array literals
- [X] functions and closures
- [X] structs and unions
- [X] basic multithreading
- [ ] generics
- [ ] basic dependent types
- [ ] interfaces
- [ ] operator overloading
- [ ] coroutines
- [ ] module system and independent compilation
- [ ] command-line interface
- [ ] VS code integration

## Example Programs

### Selection Sort
```
func selection_sort(arr as array[int, var]) as void => { 
    # Find the smallest element and put it in position i
    for i = 0 to arr.size-1 do {
        var smallest = arr[i];
        var smallest_index = i;

        # Find the smallest element
        for j = i + 1 to arr.size-1 do {
            if arr[j] < smallest do {
                @smallest_index = j;
                @smallest = arr[j];
            };
        };

        # Swap the smallest with the ith position
        ref swap = arr[i];

        arr@[i] = smallest;
        arr@[smallest_index] = swap;
    };
};
```

### Binary Search
```
func binary_search(arr as array[int, ref], to_find as int) as int => {
    var start = 0;
	var slice = arr;
	
	while slice.size > 0 do {
		ref mid = slice.size / 2;
		ref mid_value = slice[mid];
		
		if to_find == mid_value then {
			return start + mid;
		}
		else if to_find < mid_value then {
			@slice = slice@[:mid];
		}
		else {
			@slice = slice@[mid:];
			@start = start + mid;
		};
	};
	
	-1;
};
```

### Basic Arithmetic Parser
```
union Token {
    eof         as void;
    plus        as void;
    minus       as void;
    multiply    as void;
    open_paren  as void;
    close_paren as void;
    int_literal as int;
};

union Result {
    error   as void;
    success as int;

    func try_get (result as var[int]) as bool => {
        match this
            if error then false
            if success then {
                @result <- success;
                true;
            };
    };
};

func parse(input as array[int]) as Result => {
    var pos <- 0;

    add_expr(chars_to_tokens(input), @pos);
};

func chars_to_tokens (toks as array[int]) as array[Token] => {
    ref result <- from heap do new array[Token, var, 100];
    var result_pos <- 0;

    for i = 0 to toks.size-1 do {
        if toks[i] != 32 do {
            result@[result_pos] <- char_to_token(toks[i]);
            @result_pos <- result_pos + 1;
        };
    };

    result;
};

func char_to_token (c as int) as Token => {
    if c == 40                      then new Token { open_paren = void }
    else if c == 41                 then new Token { close_paren = void }
    else if c == 43                 then new Token { plus = void }
    else if c == 45                 then new Token { minus = void }
    else if c == 42                 then new Token { multiply = void }
    else if (c >= 48) and (c <= 57) then new Token { int_literal = c - 48 }
    else void;
};

func add_expr (toks as array[Token], pos as var[int]) as Result => {
    if !mult_expr(toks, @pos).try_get(var first <- 0) do {
        return void;
    };

    while (toks[pos] is plus) or (toks[pos] is minus) do {
        ref op <- toks[pos];
        @pos <- pos + 1;

        if !mult_expr(toks, @pos).try_get(var next <- 0) do {
            return void;    
        };

        @first <- if op is plus
            then first + next
            else first - next;
    };

    new Result { success = first };
};

func mult_expr (toks as array[Token], pos as var[int]) as Result => {
    if !atom(toks, @pos).try_get(var first <- 0) do {
        return void;
    };

    while toks[pos] is multiply do {
        @pos <- pos + 1;

        if !atom(toks, @pos).try_get(var next <- 0) do {
            return void;
        };

        @first <- first * next;
    };
    
    new Result { success = first };
};

func atom (toks as array[Token], pos as var[int]) as Result => {
    if toks[pos] is open_paren then {
        @pos <- pos + 1;

        ref result <- add_expr(toks, @pos);

        if toks[pos] is close_paren then {
            @pos <- pos + 1;
            result;
        }
        else {
            void;
        };
    }
    else if toks[pos] is int_literal then {
        var value <- 0;

        while toks[pos] is int_literal do {
            @value <- 10 * value + match toks[pos] if int_literal then int_literal else 0;
            @pos <- pos + 1;
        };

        new Result { success = value };
    }
    else {
        void;
    };
};
```
