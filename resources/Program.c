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

_Word test(_Region* _return_region);

_Word test(_Region* _return_region) {
    _Word sum = 0;

    _Word i = 0;

    /* Line 4: Loop */
    while (1) {
        /* Line 4: If statement */
        if ((i >= 10)) { 
            break;
        } 

        /* Line 5: Assignment statement */
        sum = (sum + i);

        /* Line 4: Assignment statement */
        i = (i + 1);
    }

    return sum;
}

