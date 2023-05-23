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

Point$ptr test(_Region* _return_region, Point$ptr$ptr z);
Point$ptr create(_Region* _return_region);

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

Point$ptr test(_Region* _return_region, Point$ptr$ptr z) {
    /* Line 7: Region calculation */
    _Region* $A = _return_region;
    $A = ((($A->depth) <= ((z.region)->depth)) ? $A : (z.region));

    /* Line 7: Function call */
    Point$ptr $B = create($A);

    /* Line 7: New variable declaration 'p' */
    Point$ptr p = $B;

    /* Line 9: Assignment statement */
    (*(z.data)) = p;

    return p;
}

Point$ptr create(_Region* _return_region) {
    /* Line 14: New variable declaration 'p' */
    Point* p_1 = (Point*)_region_malloc(_return_region, sizeof(Point));
    (*p_1) = (Point){ 10U, 5U };

    return (Point$ptr){ p_1, _return_region };
}

#if __cplusplus
}
#endif
