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

typedef union IntOption_$Union IntOption_$Union;
typedef struct IntOption IntOption;
typedef struct Point Point;
IntOption test(_Region* _return_region);

union IntOption_$Union {
    int none;
    int some;
    int other;
};

struct IntOption {
    int tag;
    IntOption_$Union data;
};

struct Point {
    int x;
    int y;
};

IntOption test(_Region* _return_region) {
    /* Line 13: New variable declaration 'x' */
    IntOption x = (IntOption){ .tag= 1U, .data= { .some= 45U } };

    /* Line 15: If statement */
    if (((x.tag) == (2U)) | ((x.tag) == (0U))) {     } 
    else {
        /* Line 15: Union downcast flowtyping */
        int* x_1 = &(x.data.some);

        /* Line 20: Assignment statement */
        *(x_1) = (*(x_1)) + (45U);
    }

    return x;
}

