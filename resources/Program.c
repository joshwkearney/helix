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

int$ptr test10(_Region* _return_region);

struct int$ptr {
    int* data;
    _Region* region;
};

int$ptr test10(_Region* _return_region) {
    /* Line 2: New variable declaration 'x' */
    int x = 5U;

    /* Line 3: New variable declaration 'y' */
    int* y = (int*)_region_malloc(_return_region, sizeof(int));
    (*y) = 10U;

    /* Line 5: New variable declaration 'z' */
    int$ptr z = (int$ptr){ (&x), _return_region };

    /* Line 6: Assignment statement */
    z = (int$ptr){ y, _return_region };

    return z;
}

#if __cplusplus
}
#endif
