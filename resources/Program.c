#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;
_helix_int externalFunc(_helix_int x);
_helix_int fib(_helix_int x_1);
void test(_helix_int* x_2);

_helix_int fib(_helix_int x_1) {
    _helix_int $return;
    _helix_int $0;

    state1: ;
    if ((x_1 <= 1U)) { 
        goto state2;
    } 
    else {
        goto state4;
    }

    state2: ;
    $0 = x_1;
    goto state6;

    state4: ;
    _helix_int $1 = fib((x_1 - 1U));
    _helix_int $2 = fib((x_1 - 2U));
    $0 = ($1 + $2);
    goto state6;

    state6: ;
    $return = $0;
    goto state7;

    state7: ;

    return $return;
}

void test(_helix_int* x_2) {
    _helix_void $return_1;

    state1: ;
    *x_2 = 45U;
    $return_1 = 0U;
    goto state2;

    state2: ;
}

#if __cplusplus
}
#endif
