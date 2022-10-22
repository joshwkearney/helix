#if __cplusplus
extern "C" {
#endif

typedef unsigned int _trophy_bool;
typedef unsigned int _trophy_void;
typedef unsigned int _trophy_int;
_trophy_int externalFunc(_trophy_int x);
_trophy_int loops(_trophy_int x_1);

_trophy_int loops(_trophy_int x_1) {
    _trophy_int $return;

    state1: ;
    _trophy_int total = 0U;
    _trophy_int i = 0U;
    goto state4;

    state4: ;
    if ((i >= 15U)) { 
        goto state11;
    } 
    else {
        goto state9;
    }

    state9: ;
    total = (total + i);
    i = (i + 1U);
    goto state4;

    state11: ;
    $return = total;
    goto state12;

    state12: ;

    return $return;
}

#if __cplusplus
}
#endif
