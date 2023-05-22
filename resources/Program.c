#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;

typedef struct _Region {
	unsigned int depth;
} _Region;

extern _Region* _region_create();
extern void* _region_malloc(_Region* region, int size);
extern void _region_delete(_Region* region);

typedef struct Point Point;
typedef struct Point$ptr Point$ptr;

void test(_Region* _return_region);
Point$ptr create(_Region* _return_region);

struct Point {
    int x;
    int y;
};

struct Point$ptr {
    Point* data;
    _Region* region;
};

void test(_Region* _return_region) {
    Point$ptr $A = create(_pool);

    /* Line 7: New variable declaration 'p' */
    Point$ptr p = $A;
}

Point$ptr create(_Region* _return_region) {
    /* Line 11: New variable declaration 'p' */
    Point* p_1 = (Point*)_region_malloc(_return_region, sizeof(Point));
    (*p_1) = (Point){ 0U, 0U };

    return (Point$ptr){ p_1, _return_region };
}

#if __cplusplus
}
#endif
