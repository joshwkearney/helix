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
int_$Pointer test11(_Region* _return_region);

struct int_$Pointer {
    int* data;
    _Region* region;
};

struct int_$Pointer_$Pointer {
    int_$Pointer* data;
    _Region* region;
};

int_$Pointer test11(_Region* _return_region) {
    /* Line 2: New variable declaration 'a' */
    int* a = _region_malloc(_return_region, sizeof(int));
    (*a) = 10U;

    /* Line 3: New variable declaration 'b' */
    int_$Pointer b = (int_$Pointer){ a, _return_region };

    /* Line 4: New variable declaration 'c' */
    int_$Pointer_$Pointer c = (int_$Pointer_$Pointer){ (&b), _return_region };

    return b;
}

