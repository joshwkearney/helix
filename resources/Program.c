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
typedef struct _Word_$Pointer _Word_$Pointer;
_Word test(_Region* _return_region);

struct Point {
    _Word x;
    Test y;
};

struct Test {
    _Word x;
};

struct _Word_$Pointer {
    _Word* data;
    _Region* region;
};

_Word test(_Region* _return_region) {
    _Word a = 45;

    _Word_$Pointer b = (_Word_$Pointer){ (&a) };

    return (a + 7);

    return 0;
}

