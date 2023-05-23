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

typedef struct int$ptr int$ptr;
typedef struct int$ptr$ptr int$ptr$ptr;

void test14(_Region* _return_region, int$ptr a);

struct int$ptr {
    int* data;
    _Region* region;
};

struct int$ptr$ptr {
    int$ptr* data;
    _Region* region;
};

void test14(_Region* _return_region, int$ptr a) {
    /* Line 7: New variable declaration 'x' */
    int$ptr$ptr x = (int$ptr$ptr){ (&a), _return_region };

    /* Line 9: New variable declaration 'i' */
    int i = 0U;

    /* Line 9: While or for loop */
    while (1U) {
        /* Line 9: If statement */
        if ((i > 10U)) { 
            break;
        } 

        /* Line 10: New variable declaration 'b' */
        int* b = (int*)_region_malloc((x.region), sizeof(int));
        (*b) = 10U;

        /* Line 11: New variable declaration 'c' */
        int$ptr* c = (int$ptr*)_region_malloc((x.region), sizeof(int$ptr));
        (*c) = (int$ptr){ b, (x.region) };

        /* Line 13: Assignment statement */
        x = (int$ptr$ptr){ c, (x.region) };

        /* Line 9: Assignment statement */
        i = (i + 1U);
    }
}

#if __cplusplus
}
#endif
