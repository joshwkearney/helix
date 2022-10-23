#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;
typedef struct _helix_int_array _helix_int_array;
typedef struct Point Point;

_helix_int externalFunc(_helix_int x);
_helix_int loops(_helix_int x_1, _helix_int_array r);
_helix_int fib(_helix_int x_2);
void test(_helix_int* x_3);
_helix_int expression_func();

struct _helix_int_array {
    _helix_int* data;
    _helix_int count;
};

struct Point {
    _helix_int x;
    _helix_int y;
};

_helix_int loops(_helix_int x_1, _helix_int_array r) {
    Point stackAllocation = { 45U, 89U };
    Point anotherOne = { 4U, 5U };
    Point $t_0 = anotherOne;
    _helix_int u = $t_0.x;
    _helix_int v = $t_0.y;
    _helix_int total = anotherOne.x;

    /* Line 21: While or for loop */
    while (1U) {
        /* Line 21: If statement */
        if (!((x_1 % 15U) != 8U)) { 
            break;
        } 

        _helix_int i = 0U;

        /* Line 22: While or for loop */
        while (1U) {
            /* Line 22: If statement */
            if ((i >= 15U)) { 
                break;
            } 

            total = (total + i);
            i = (i + 1U);
        }

        /* Line 26: If statement */
        if ((x_1 > 100U)) { 
            break;
        } 

        /* Line 31: If statement */
        _helix_int $F;
        if ((x_1 < 10U)) { 
            break;
            $F = 0U;
        } 
        else {
            $F = 10U;
        }

        _helix_int awfulFlowControl = $F;
    }

    _helix_int zero = 0U;
    _helix_int alsoZero = 0U;
    _helix_int $G = fib(total);

    return (total + $G);
}

_helix_int fib(_helix_int x_2) {
    /* Line 49: If statement */
    _helix_int $C;
    if ((x_2 <= 1U)) { 
        $C = x_2;
    } 
    else {
        _helix_int $A = fib((x_2 - 1U));
        _helix_int $B = fib((x_2 - 2U));
        $C = ($A + $B);
    }

    return $C;
}

void test(_helix_int* x_3) {
    (*x_3) = 45U;
}

_helix_int expression_func() {
    return 45U;
}

#if __cplusplus
}
#endif
