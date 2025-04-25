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

typedef struct Point Point;
typedef struct _Word_$Array _Word_$Array;
typedef struct _Word_$Pointer _Word_$Pointer;
_Word test12(_Region* _return_region);

struct Point {
    _Word x;
    _Word y;
};

struct _Word_$Array {
    _Word* data;
    _Region* region;
    _Word count;
};

struct _Word_$Pointer {
    _Word* data;
    _Region* region;
};

_Word test12(_Region* _return_region) {
    /* Line 7: Array literal */
    _Word $A[] = { 1, 2, 3, 4 };
    _Word_$Array $B = (_Word_$Array){ $A, _return_region };

    _Word_$Array a = $B;

    /* Line 9: Array to pointer conversion */
    _Word_$Pointer $C = (_Word_$Pointer){ ((a.data) + 1), (a.region) };

    /* Line 9: Pointer dereference */
    _Word $D = (*($C.data));

    return $D;

    return 0;
}

