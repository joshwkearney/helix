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

typedef struct Test4Struct_$Pointer Test4Struct_$Pointer;
typedef struct Test4Struct Test4Struct;
int test4(_Region* _return_region, Test4Struct_$Pointer a);

struct Test4Struct_$Pointer {
    Test4Struct* data;
    _Region* region;
};

struct Test4Struct {
    Test4Struct_$Pointer next;
    int data;
};

int test4(_Region* _return_region, Test4Struct_$Pointer a) {
    /* Line 7: New variable declaration 'A' */
    Test4Struct A = (Test4Struct){ .next= a, .data= 0U };

    /* Line 8: New variable declaration 'B' */
    A B = (A){ (&A), _return_region };

    /* Line 10: Pointer dereference */
    Test4Struct $deref0 = (*(B.data));

    /* Line 10: Pointer dereference */
    Test4Struct $deref1 = (*(($deref0.next).data));

    /* Line 10: Pointer dereference */
    Test4Struct $deref2 = (*(($deref1.next).data));

    return ($deref2.data);
}

