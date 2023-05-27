#ifdef _MSC_VER
#define inline __inline
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
static inline _Region* _region_min(_Region* r1, _Region* r2) { return r1->depth < r2->depth ? r1 : r2;  }

typedef struct int_$Pointer int_$Pointer;
typedef union Option_$Union Option_$Union;
typedef struct Option Option;
Option test3(_Region* _return_region, int_$Pointer Z);

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

Option test3(_Region* _return_region, int_$Pointer Z) {
    /* Line 7: New variable declaration 'x' */
    int* x = _region_malloc(_return_region, sizeof(int));
    (*x) = 45U;

    /* Line 8: New variable declaration 'p' */
    Option p = (Option){ .tag= 1U, .data= { .some= (int_$Pointer){ x, _return_region } } };

    /* Line 10: If statement */
    if (((p.tag)==1U)) { 
        /* Line 10: Union downcast flowtyping */
        int_$Pointer* p_1 = (&((p.data).some));

        /* Line 11: Assignment statement */
        (*p_1) = Z;
    } 
    else {
        /* Line 10: Union downcast flowtyping */
        int* p_2 = (&((p.data).none));
    }

    return p;
}

