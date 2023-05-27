#ifdef _MSC_VER
#define inline __inline
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;

typedef struct _Region {
	unsigned int depth;
} _Region;

extern _Region* _region_new();
extern void* _region_malloc(_Region* region, int size);
extern void _region_delete(_Region* region);
static inline _Region* _region_min(_Region* r1, _Region* r2) { return r1->depth < r2->depth ? r1 : r2;  }

typedef struct int_$Pointer int_$Pointer;
typedef struct int_$Pointer_$Pointer int_$Pointer_$Pointer;
typedef struct Test2Struct Test2Struct;
void test2(_Region* _return_region, Test2Struct n);

struct int_$Pointer {
    int* data;
    _Region* region;
};

struct int_$Pointer_$Pointer {
    int_$Pointer* data;
    _Region* region;
};

struct Test2Struct {
    int_$Pointer_$Pointer field;
};

void test2(_Region* _return_region, Test2Struct n) {
    /* Line 6: Pointer dereference */
    int_$Pointer $deref0 = *(n.field.data);

    /* Line 6: Pointer dereference */
    int $deref1 = *($deref0.data);

    /* Line 6: New variable declaration 'x' */
    int* x = _region_malloc($deref0.region, sizeof(int));
    *(x) = $deref1;

    /* Line 8: Assignment statement */
    *(n.field.data) = (int_$Pointer){ x, $deref0.region };
}

