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
typedef struct Test2Struct Test2Struct;

void test2(_Region* _return_region, Test2Struct n);

struct int$ptr {
    int* data;
    _Region* region;
};

struct int$ptr$ptr {
    int$ptr* data;
    _Region* region;
};

struct Test2Struct {
    int$ptr$ptr field;
};

void test2(_Region* _return_region, Test2Struct n) {
    /* Line 6: Pointer dereference */
    int$ptr $deref_2 = (*((n.field).data));

    /* Line 6: Pointer dereference */
    int $deref_3 = (*($deref_2.data));

    /* Line 6: New variable declaration 'x' */
    int* x = (int*)_region_malloc(($deref_2.region), sizeof(int));
    (*x) = $deref_3;

    /* Line 8: Assignment statement */
    (*((n.field).data)) = (int$ptr){ x, ($deref_2.region) };
}

#if __cplusplus
}
#endif
