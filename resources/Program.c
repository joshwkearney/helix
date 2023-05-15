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
typedef struct Point$ptr$ptr$ptr Point$ptr$ptr$ptr;

void test(int _return_region, Point$ptr$ptr$ptr array);

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

struct Point$ptr$ptr$ptr {
    Point$ptr$ptr* data;
    int region;
};

void test(int _return_region, Point$ptr$ptr$ptr array) {
    /* Line 4: Pointer dereference */
    Point$ptr$ptr $deref_3 = (*(array.data));

    /* Line 4: New 'Point*' */
    Point* $C = (Point*)_region_malloc(($deref_3.region), sizeof(Point));
    (*$C) = (Point){ 0U, 0U };
    Point$ptr $D = (Point$ptr){ $C, ($deref_3.region) };

    /* Line 4: Assignment statement */
    (*($deref_3.data)) = $D;
}

#if __cplusplus
}
#endif
