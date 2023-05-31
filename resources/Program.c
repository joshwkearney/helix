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

typedef struct _Word_$Pointer _Word_$Pointer;
typedef struct _Word_$Pointer_$Pointer _Word_$Pointer_$Pointer;
_Word_$Pointer test12(_Region* _return_region, _Word_$Pointer_$Pointer A);

struct _Word_$Pointer {
    _Word* data;
    _Region* region;
};

struct _Word_$Pointer_$Pointer {
    _Word_$Pointer* data;
    _Region* region;
};

_Word_$Pointer test12(_Region* _return_region, _Word_$Pointer_$Pointer A) {
    /* Line 2: New variable declaration 'a' */
    _Word a = 5;

    /* Line 3: New variable declaration 'b' */
    _Word* b = _region_malloc(_return_region, sizeof(_Word));
    (*b) = 10;

    /* Line 5: New variable declaration 'x' */
    _Word_$Pointer x = (_Word_$Pointer){ (&a), _return_region };

    /* Line 6: New variable declaration 'z' */
    _Word_$Pointer_$Pointer z = (_Word_$Pointer_$Pointer){ (&x), _return_region };

    /* Line 8: Assignment statement */
    x = (_Word_$Pointer){ b, _return_region };

    return x;
}

