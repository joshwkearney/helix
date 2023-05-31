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
    /* Line 7: New variable declaration '$t_0' */
    Point $t_0 = (Point){ .x= 8, .y= 9 };

    /* Line 7: New variable declaration 'a' */
    _Word a = ($t_0.x);

    /* Line 7: New variable declaration 'b' */
    _Word b = ($t_0.y);

    return a;

    return 0;
}

