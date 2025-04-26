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
typedef struct Test Test;
_Word test(_Region* _return_region, _Word limit);

struct Point {
    _Word x;
    Test y;
};

struct Test {
    _Word x;
};

_Word test(_Region* _return_region, _Word limit) {
    _Word i = 0;

    /* Line 13: Loop */
    while (1) {
        /* Line 14: If statement */
        if ((i < limit)) { 
            /* Line 15: Assignment statement */
            i = (i + 1);

            continue;
        } 

        return i;
    }

    return 0;
}

