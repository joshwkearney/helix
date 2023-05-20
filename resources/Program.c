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
typedef struct int$ptr$ptr int$ptr$ptr;

int$ptr test10(int _return_region);

struct int$ptr {
    int* data;
    int region;
};

struct int$ptr$ptr {
    int$ptr* data;
    int region;
};

int$ptr test10(int _return_region) {
    /* Line 2: New variable declaration 'a' */
    int* a = (int*)_region_malloc((_return_region.region), sizeof(int));
    (*a) = 10U;

    /* Line 3: New variable declaration 'b' */
    int$ptr* b = (int$ptr*)_region_malloc((_return_region.region), sizeof(int$ptr));
    (*b) = (int$ptr){ a, (_return_region.region) };

    /* Line 4: New variable declaration 'c' */
    int$ptr$ptr c = (int$ptr$ptr){ b, (_return_region.region) };

    /* Line 6: Pointer dereference */
    int$ptr $deref_1 = (*(c.data));

    return $deref_1;
}

#if __cplusplus
}
#endif
