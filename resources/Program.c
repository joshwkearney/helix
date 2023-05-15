#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;

extern int _region_create();
extern void* _region_malloc(int region, int size);
extern void _region_delete(int region);

typedef struct Point Point;
typedef struct Point$ptr Point$ptr;
typedef struct Point$ptr$array Point$ptr$array;
typedef struct Point$ptr$ptr Point$ptr$ptr;

Point$ptr test(int _return_region, Point$ptr$array array);

struct Point {
    int x;
    int y;
};

struct Point$ptr {
    Point* data;
    int region;
};

struct Point$ptr$array {
    Point$ptr* data;
    int region;
    int count;
};

struct Point$ptr$ptr {
    Point$ptr* data;
    int region;
};

Point$ptr test(int _return_region, Point$ptr$array array) {
    int $A = _return_region;
    int $B = (array.region);

    int $F = $A;
    $F = (($F <= $B) ? $F : $B);

    /* Line 2: New 'Point*' */
    Point* $E = (Point*)_region_malloc($F, sizeof(Point));
    (*$E) = (Point){ 6U, 8U };
    Point$ptr $G = (Point$ptr){ $E, $F };
    int $H = ($G.region);

    /* Line 2: New variable declaration 'test' */
    Point$ptr test = $G;

    /* Line 3: Array to pointer conversion */
    Point$ptr$ptr $I = (Point$ptr$ptr){ ((array.data) + 10U), (array.region) };

    /* Line 3: Assignment statement */
    (*($I.data)) = test;

    return test;
}

#if __cplusplus
}
#endif
