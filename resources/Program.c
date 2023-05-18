#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;


extern int _region_min();
extern int _region_create();
extern void* _region_malloc(int region, int size);
extern void _region_delete(int region);

typedef struct Test4Struct$ptr Test4Struct$ptr;
typedef struct Test4Struct Test4Struct;

int test4(int _return_region, Test4Struct$ptr a);

struct Test4Struct$ptr {
    Test4Struct* data;
    int region;
};

struct Test4Struct {
    Test4Struct$ptr next;
    int data;
};

int test4(int _return_region, Test4Struct$ptr a) {
    /* Line 7: New variable declaration 'A' */
    Test4Struct A = (Test4Struct){ a, 0U };

    /* Line 8: New variable declaration 'B' */
    Test4Struct$ptr B = (Test4Struct$ptr){ (&A), _region_min() };

    /* Line 10: Pointer dereference */
    Test4Struct $deref_2 = (*(B.data));

    /* Line 10: Pointer dereference */
    Test4Struct $deref_3 = (*(($deref_2.next).data));

    /* Line 10: Pointer dereference */
    Test4Struct $deref_4 = (*(($deref_3.next).data));

    return ($deref_4.data);
}

#if __cplusplus
}
#endif
