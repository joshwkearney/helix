#ifdef _MSC_VER
#define inline __inline
#endif

typedef struct _Region {
	unsigned int depth;
} _Region;

extern _Region* _region_new();
extern void* _region_malloc(_Region* region, int size);
extern void _region_delete(_Region* region);
static inline _Region* _region_min(_Region* r1, _Region* r2) { return r1->depth < r2->depth ? r1 : r2;  }

typedef struct int_$Pointer int_$Pointer;
typedef struct int_$Pointer_$Pointer int_$Pointer_$Pointer;
int_$Pointer test12(_Region* _return_region, int_$Pointer_$Pointer A);

int test(_Region* _return_region, int a);

int test(_Region* _return_region, int a) {
    /* Line 2: New variable declaration 'x' */
    int x = 45U;
    
    return x;
}

