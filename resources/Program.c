#if __cplusplus
extern "C" {
#endif

#ifdef _MSC_VER
#define inline __inline
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

typedef struct int$ptr int$ptr;
typedef struct int$ptr$ptr int$ptr$ptr;

int$ptr test11(_Region* _return_region);

struct int$ptr {
    int* data;
    _Region* region;
};

struct int$ptr$ptr {
    int$ptr* data;
    _Region* region;
};

int$ptr test11(_Region* _return_region) {
    /* Line 2: New variable declaration 'a' */
    int* a = (int*)_region_malloc(_return_region, sizeof(int));
    (*a) = 10U;

    /* Line 3: New variable declaration 'b' */
    int$ptr* b = (int$ptr*)_region_malloc(_return_region, sizeof(int$ptr));
    (*b) = (int$ptr){ a, _return_region };

    /* Line 4: New variable declaration 'c' */
    int$ptr$ptr c = (int$ptr$ptr){ b, _return_region };

    /* Line 6: Pointer dereference */
    int$ptr $deref0 = (*(c.data));

    return $deref0;
}

#if __cplusplus
}
#endif
