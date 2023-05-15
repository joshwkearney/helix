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

void test(int _return_region);

struct Point {
    int x;
    int y;
};

struct Point$ptr {
    Point* data;
    int region;
};

void test(int _return_region) {
    /* Line 2: New variable declaration 'x' */
    Point x = (Point){ 4U, 8U };

    /* Line 3: New variable declaration 'y' */
    Point$ptr y = (&x);
}

#if __cplusplus
}
#endif
