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
_Word test(_Region* _return_region, Option x);

union Option_$Union {
    int none;
    _Word value;
};

struct Option {
    int tag;
    Option_$Union data;
};

_Word test(_Region* _return_region, Option x) {
    /* Line 7: If statement */
    if (((x.tag) == 0)) { 
        return 0;
    } 

    return (((x.data).value) + 1);
}

