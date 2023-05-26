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
typedef struct int$ptr$array int$ptr$array;
typedef struct int$ptr$ptr int$ptr$ptr;

void test5(_Region* _return_region, int$ptr$array arr);

struct int$ptr {
    int* data;
    _Region* region;
};

struct int$ptr$array {
    int$ptr* data;
    _Region* region;
    int count;
};

struct int$ptr$ptr {
    int$ptr* data;
    _Region* region;
};

void test5(_Region* _return_region, int$ptr$array arr) {
    /* Line 2: New variable declaration 'x' */
    int* x = (int*)_region_malloc((arr.region), sizeof(int));
    (*x) = 45U;

    /* Line 4: Array to pointer conversion */
    int$ptr$ptr $A = (int$ptr$ptr){ ((arr.data) + 0U), (arr.region) };

    /* Line 4: Assignment statement */
    (*($A.data)) = (int$ptr){ x, (arr.region) };
}

#if __cplusplus
}
#endif
