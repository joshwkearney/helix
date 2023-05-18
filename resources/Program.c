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

typedef struct Test3Struct Test3Struct;

int test3(int _return_region, Test3Struct arg);

struct Test3Struct {
    int field;
};

int test3(int _return_region, Test3Struct arg) {
    return (arg.field);
}

#if __cplusplus
}
#endif
