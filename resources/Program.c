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
    _helix_int total = anotherOne.x;
    goto state4;

    state4: ;
    if (!((x_1 % 15U) != 8U)) { 
        goto state10;
    } 
    else {
        goto state4;
    }

    state10: ;
    $return = total;
    goto state11;

    state11: ;

    return $return;
}

#if __cplusplus
}
#endif
