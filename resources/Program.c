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
_Word test(_Region* _return_region, int y);

struct Point {
    _Word x;
    _Word y;
};

_Word test(_Region* _return_region, int y) {
    /* Line 8: If statement */
    if (y) { 
        return 0;
    } 

    /* Line 7: New variable declaration 'x' */
    _Word x = 82;

    return 72;
}

