#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;

extern int _region_min;
extern int _region_create();
extern void* _region_malloc(int region, int size);
extern void _region_delete(int region);

typedef struct int$ptr int$ptr;
typedef struct int$ptr$ptr int$ptr$ptr;
typedef struct int$ptr$ptr$ptr int$ptr$ptr$ptr;

void test3(int _return_region, int$ptr$ptr$ptr a);

struct int$ptr {
    int* data;
    int region;
};

struct int$ptr$ptr {
    int$ptr* data;
    int region;
};

struct int$ptr$ptr$ptr {
    int$ptr$ptr* data;
    int region;
};

void test3(int _return_region, int$ptr$ptr$ptr a) {
    /* Line 2: New variable declaration 'b' */
    int b = 45U;

    /* Line 4: Pointer dereference */
    int$ptr$ptr $deref_3 = (*(a.data));

    /* Line 4: Assignment statement */
    (*($deref_3.data)) = (int$ptr){ (&b), _region_min };
}

#if __cplusplus
}
#endif
