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
        _helix_void $C;
        if (!((x_1 % 15U) != 8U)) { 
            break;
            $C = 0U;
        } 
        else {
            $C = 0U;
        }

        _helix_int i = 0U;

        /* Line 22: While or for loop */
        while (1U) {
            /* Line 22: If statement */
            _helix_void $D;
            if ((i >= 15U)) { 
                break;
                $D = 0U;
            } 
            else {
                $D = 0U;
            }

            total = (total + i);
            i = (i + 1U);
        }
        break;
        break;

        /* Line 31: If statement */
        _helix_int $E;
        if ((x_1 < 10U)) { 
            $E = 0U;
        } 
        else {
            $E = 10U;
        }

        _helix_int awfulFlowControl = $E;
    }
    _helix_int zero = 0U;
    _helix_int alsoZero = 0U;
    _helix_int $0 = fib(total);

    return (total + $0);
}

_helix_int fib(_helix_int x_2) {
    _helix_int $1 = fib((x_2 - 1U));
    _helix_int $2 = fib((x_2 - 2U));

    /* Line 49: If statement */
    _helix_int $A;
    if ((x_2 <= 1U)) { 
        $A = x_2;
    } 
    else {
        $A = ($1 + $2);
    }

    /* Line 49: If statement */
    _helix_int $B;
    if ((x_2 <= 1U)) { 
        $B = x_2;
    } 
    else {
        $B = ($1 + $2);
    }

    return $B;
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
