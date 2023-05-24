#if __cplusplus
extern "C" {
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

void test14(_Region* _return_region);

void test14(_Region* _return_region) {
    /* Line 2: New variable declaration 'x' */
    int x = (4U + 9U);

    /* Line 4: Assignment statement */
    x = (x + 7U);
}

#if __cplusplus
}
#endif
