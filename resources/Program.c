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

Point$ptr test(int _return_region);

struct Point {
    int x;
    int y;
};

struct Point$ptr {
    Point* data;
    int region;
};

Point$ptr test(int _return_region) {
    /* Line 2: New variable declaration 'x' */
    Point* x = (Point*)_region_malloc(_return_region, sizeof(Point));
    (*x) = (Point){ 4U, 8U };

    return x;
}

#if __cplusplus
}
#endif
