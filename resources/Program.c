#ifdef _MSC_VER
#define inline __inline
#endif

typedef signed long long _Word;

typedef struct _Region {
	unsigned int depth;
} _Region;

extern _Region* _region_create();
extern void _region_destroy(_Region* region);
extern void* _region_malloc(_Region* region, int size);
static inline _Region* _region_min(_Region* r1, _Region* r2) { return r1->depth < r2->depth ? r1 : r2;  }

typedef union Option_$Union Option_$Union;
typedef struct Option Option;
_Word test2(_Region* _return_region, Option opt);
_Word test3(_Region* _return_region, Option opt_2);

union Option_$Union {
    int first;
    int second;
    _Word third;
};

struct Option {
    int tag;
    Option_$Union data;
};

_Word test2(_Region* _return_region, Option opt) {
    /* Line 8: If statement */
    if (((opt.tag) == 2)) { 
        /* Line 9: Union downcast flowtyping */
        _Word* opt_1 = (&((opt.data).third));

        return ((*opt_1) + 10);
    } 

    return 45;
}

_Word test3(_Region* _return_region, Option opt_2) {
    /* Line 16: If statement */
    _Word $A;
    if ((((opt_2.tag) == 0) | ((opt_2.tag) == 1))) { 
        $A = 10;
    } 
    else {
        /* Line 18: Union downcast flowtyping */
        _Word* opt_3 = (&((opt_2.data).third));

        $A = (*opt_3);
    }

    return $A;
}

