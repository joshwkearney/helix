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
_Word test12(_Region* _return_region);

struct Point {
    _Word x;
    _Word y;
};

_Word test12(_Region* _return_region) {
    Point a = (Point){ .x= 8, .y= 9 };

    return (a.x);

    return 0;
}

