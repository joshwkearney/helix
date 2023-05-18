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
typedef struct int$ptr$ptr int$ptr$ptr;

void test9(int _return_region, int$ptr$ptr a, int$ptr$ptr b);

struct int$ptr {
    int* data;
    int region;
};

struct int$ptr$ptr {
    int$ptr* data;
    int region;
};

void test9(int _return_region, int$ptr$ptr a, int$ptr$ptr b) {
    /* Line 2: New variable declaration 'c' */
    int$ptr$ptr c = a;

    /* Line 3: New variable declaration 'd' */
    int* d = (int*)_region_malloc((a.region), sizeof(int));
    (*d) = 0U;

    /* Line 5: Assignment statement */
    (*(c.data)) = (int$ptr){ d, (a.region) };
}

#if __cplusplus
}
#endif
