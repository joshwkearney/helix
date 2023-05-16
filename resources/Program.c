#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;

extern int _region_create();
extern void* _region_malloc(int region, int size);
extern void _region_delete(int region);

typedef struct int$ptr int$ptr;
typedef struct int$ptr$ptr int$ptr$ptr;

void test1(int _return_region, int$ptr$ptr a, int$ptr b);

struct int$ptr {
    int* data;
    int region;
};

struct int$ptr$ptr {
    int$ptr* data;
    int region;
};

void test1(int _return_region, int$ptr$ptr a, int$ptr b) {
    /* Line 2: Assignment statement */
    (*(a.data)) = b;
}

#if __cplusplus
}
#endif
