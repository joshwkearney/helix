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
typedef struct int$ptr$array int$ptr$array;
typedef struct int$ptr$ptr int$ptr$ptr;

int$ptr test6(int _return_region, int$ptr$array arr);

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

int$ptr test6(int _return_region, int$ptr$array arr) {
    /* Line 2: Region calculation */
    int $A = _return_region;
    $A = (($A <= (arr.region)) ? $A : (arr.region));

    /* Line 2: New variable declaration 'x' */
    int* x = (int*)_region_malloc($A, sizeof(int));
    (*x) = 45U;

    /* Line 4: Array to pointer conversion */
    int$ptr$ptr $B = (int$ptr$ptr){ ((arr.data) + 0U), (arr.region) };

    /* Line 4: Assignment statement */
    (*($B.data)) = (int$ptr){ x, $A };

    return (int$ptr){ x, $A };
}

#if __cplusplus
}
#endif
