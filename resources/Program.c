#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;


extern int _region_min();
extern int _region_create();
extern void* _region_malloc(int region, int size);
extern void _region_delete(int region);

typedef struct int$ptr int$ptr;

int$ptr test10(int _return_region);

struct int$ptr {
    int* data;
    int region;
};

int$ptr test10(int _return_region) {
    /* Line 2: New variable declaration 'x' */
    int x = 5U;

    /* Line 3: New variable declaration 'y' */
    int* y = (int*)_region_malloc((_return_region.region), sizeof(int));
    (*y) = 10U;

    /* Line 5: New variable declaration 'z' */
    int$ptr z = (int$ptr){ (&x), _region_min() };

    /* Line 6: Assignment statement */
    z = (int$ptr){ y, (_return_region.region) };

    return z;
}

#if __cplusplus
}
#endif
