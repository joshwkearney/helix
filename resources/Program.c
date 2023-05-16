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
typedef struct int$ptr$array int$ptr$array;
typedef struct int$ptr$ptr int$ptr$ptr;

void test7(int _return_region, int$ptr$array arr);

struct int$ptr {
    int* data;
    int region;
};

struct int$ptr$array {
    int$ptr* data;
    int region;
    int count;
};

struct int$ptr$ptr {
    int$ptr* data;
    int region;
};

void test7(int _return_region, int$ptr$array arr) {
    /* Line 2: Array to pointer conversion */
    int$ptr$ptr $A = (int$ptr$ptr){ ((arr.data) + 0U), (arr.region) };

    /* Line 2: Array to pointer conversion */
    int$ptr$ptr $B = (int$ptr$ptr){ ((arr.data) + 1U), (arr.region) };

    /* Line 2: Pointer dereference */
    int$ptr $array_index_1 = (*($B.data));

    /* Line 2: Assignment statement */
    (*($A.data)) = $array_index_1;
}

#if __cplusplus
}
#endif
