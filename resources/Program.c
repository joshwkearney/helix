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

int$ptr test1(int _return_region);

struct int$ptr {
    int* data;
    int region;
};

int$ptr test1(int _return_region) {
    /* Line 2: New variable declaration 'x' */
    int* x = (int*)_region_malloc((_return_region.region), sizeof(int));
    (*x) = 45U;

    return (int$ptr){ x, (_return_region.region) };
}

#if __cplusplus
}
#endif
