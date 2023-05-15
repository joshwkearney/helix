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
typedef struct Point$ptr$ptr Point$ptr$ptr;

Point$ptr test(int _return_region, Point$ptr$ptr z);

struct Point {
    int x;
    int y;
};

struct Point$ptr {
    Point* data;
    int region;
};

struct Point$ptr$ptr {
    Point$ptr* data;
    int region;
};

Point$ptr test(int _return_region, Point$ptr$ptr z) {
    /* Line 2: Region calculation */
    int $B = _return_region;
    $B = (($B <= (z.region)) ? $B : (z.region));

    /* Line 2: New variable declaration 'x' */
    Point* x = (Point*)_region_malloc($B, sizeof(Point));
    (*x) = (Point){ 4U, 8U };

    /* Line 4: Assignment statement */
    (*(z.data)) = (Point$ptr){ x, $B };

    return (Point$ptr){ x, $B };
}

#if __cplusplus
}
#endif
