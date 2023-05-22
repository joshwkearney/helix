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

int$ptr test12(int _return_region, int$ptr$ptr A);

struct int$ptr {
    int* data;
    int region;
};

struct int$ptr$ptr {
    int$ptr* data;
    int region;
};

int$ptr test12(int _return_region, int$ptr$ptr A) {
    /* Line 2: New variable declaration 'a' */
    int a = 5U;

    /* Line 3: New variable declaration 'b' */
    int* b = (int*)_region_malloc(_return_region, sizeof(int));
    (*b) = 10U;

    /* Line 5: New variable declaration 'x' */
    int$ptr x = (int$ptr){ (&a), _region_min };

    /* Line 6: New variable declaration 'z' */
    int$ptr$ptr z = (int$ptr$ptr){ (&x), _region_min };

    /* Line 8: Assignment statement */
    (*(z.data)) = (int$ptr){ b, _return_region };

    return x;
}

#if __cplusplus
}
#endif
