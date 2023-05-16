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

void test(int _return_region, Point$ptr$ptr z);

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

void test(int _return_region, Point$ptr$ptr z) {
    /* Line 2: New variable declaration 'y' */
    Point* y = (Point*)_region_malloc((z.region), sizeof(Point));
    (*y) = (Point){ 0U, 0U };

    /* Line 4: Assignment statement */
    (*(z.data)) = (Point$ptr){ y, (z.region) };
}

#if __cplusplus
}
#endif
