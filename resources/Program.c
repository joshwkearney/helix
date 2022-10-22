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
    _helix_int $return;

    state1: ;
    Point stackAllocation = { 45U, 89U };
    Point anotherOne = { 4U, 5U };
    Point $t_0 = anotherOne;
    _helix_int u = $t_0.x;
    _helix_int v = $t_0.y;
    _helix_int total = anotherOne.x;
    goto state4;

    state4: ;
    if (!((x_1 % 15U) != 8U)) { 
        goto state26;
    } 
    else {
        goto state9;
    }

    state9: ;
    _helix_int i = 0U;
    goto state12;

    state12: ;
    if ((i >= 15U)) { 
        goto state19;
    } 
    else {
        goto state17;
    }

    state17: ;
    total = (total + i);
    i = (i + 1U);
    goto state12;

    state19: ;
    if ((x_1 > 100U)) { 
        goto state26;
    } 
    else {
        goto state4;
    }

    state26: ;
    _helix_int zero = 0U;
    _helix_int alsoZero = 0U;
    _helix_int $3 = fib(total);
    $return = (total + $3);
    goto state27;

    state27: ;

    return $return;
}

_helix_int fib(_helix_int x_2) {
    _helix_int $return_1;
    _helix_int $4;

    state1: ;
    if ((x_2 <= 1U)) { 
        goto state2;
    } 
    else {
        goto state4;
    }

    state2: ;
    $4 = x_2;
    goto state6;

    state4: ;
    _helix_int $5 = fib((x_2 - 1U));
    _helix_int $6 = fib((x_2 - 2U));
    $4 = ($5 + $6);
    goto state6;

    state6: ;
    $return_1 = $4;
    goto state7;

    state7: ;

    return $return_1;
}

void test(_helix_int* x_3) {
    _helix_void $return_2;

    state1: ;
    *x_3 = 45U;
    $return_2 = 0U;
    goto state2;

    state2: ;
}

_helix_int expression_func() {
    _helix_int $return_3;

    state1: ;
    $return_3 = 45U;
    goto state2;

    state2: ;

    return $return_3;
}

#if __cplusplus
}
#endif
