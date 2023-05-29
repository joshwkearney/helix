#ifdef _MSC_VER
#define inline __inline
#endif

typedef struct _Region {
	unsigned int depth;
} _Region;

extern _Region* _region_new();
extern void* _region_malloc(_Region* region, int size);
extern void _region_delete(_Region* region);
static inline _Region* _region_min(_Region* r1, _Region* r2) { return r1->depth < r2->depth ? r1 : r2;  }

typedef struct int_$Pointer int_$Pointer;
typedef union Option_$Union Option_$Union;
typedef struct Option Option;
Option test17(_Region* _return_region);

struct int_$Pointer {
    int* data;
    _Region* region;
};

union Option_$Union {
    int none;
    int_$Pointer some;
};

struct Option {
    int tag;
    Option_$Union data;
};

Option test17(_Region* _return_region) {
    /* Line 7: New variable declaration 'x' */
    int* x = _region_malloc(_return_region, sizeof(int));
    (*x) = 45U;

    /* Line 8: New variable declaration 'y' */
    Option y = (Option){ .tag= 1U, .data= { .some= (int_$Pointer){ x, _return_region } } };

    /* Line 10: If statement */
    if (((y.tag) == 1U)) { 
        /* Line 10: Union downcast flowtyping */
        int_$Pointer* y_1 = (&((y.data).some));

        /* Line 11: New variable declaration 'z' */
        int* z = _region_malloc(_return_region, sizeof(int));
        (*z) = 10U;

        /* Line 12: Assignment statement */
        (*y_1) = (int_$Pointer){ z, _return_region };
    } 
    else {
        /* Line 10: Union downcast flowtyping */
        int* y_2 = (&((y.data).none));
    }

    return y;
}

