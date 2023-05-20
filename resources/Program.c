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

int$ptr test2(int _return_region, int$ptr a);

struct int$ptr {
    int* data;
    int region;
};

int$ptr test2(int _return_region, int$ptr a) {
    return a;
}

#if __cplusplus
}
#endif
