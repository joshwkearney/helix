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

int$ptr test1(_Region* _return_region);

struct int$ptr {
    int* data;
    _Region* region;
};

int$ptr test1(_Region* _return_region) {
    /* Line 2: New variable declaration 'x' */
    int* x = (int*)_region_malloc(_return_region, sizeof(int));
    (*x) = 45U;

    return (int$ptr){ x, _return_region };
}

#if __cplusplus
}
#endif
