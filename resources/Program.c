#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;

typedef struct _Region {
	unsigned int depth;
} _Region;

extern _Region* _region_new();
extern void* _region_malloc(_Region* region, int size);
extern void _region_delete(_Region* region);

typedef struct Point Point;
typedef struct Point$ptr Point$ptr;
typedef struct Point$ptr$ptr Point$ptr$ptr;

void test14(_Region* _return_region, Point$ptr$ptr a, Point$ptr$ptr b);

struct Point {
    int x;
    int y;
};

struct Point$ptr {
    Point* data;
    _Region* region;
};

struct Point$ptr$ptr {
    Point$ptr* data;
    _Region* region;
};

void test14(_Region* _return_region, Point$ptr$ptr a, Point$ptr$ptr b) {
    /* Line 7: New variable declaration 'c' */
    Point$ptr$ptr c = a;

    /* Line 9: If statement */
    if (1U) { 
        /* Line 10: Assignment statement */
        c = b;
    } 

    /* Line 13: Region calculation */
    _Region* $B = (a.region);
    $B = ((($B->depth) <= ((b.region)->depth)) ? $B : (b.region));

    /* Line 13: New variable declaration 'x' */
    Point* x = (Point*)_region_malloc($B, sizeof(Point));
    (*x) = (Point){ 10U, 5U };

    /* Line 14: Assignment statement */
    (*(c.data)) = (Point$ptr){ x, $B };
}

#if __cplusplus
}
#endif
