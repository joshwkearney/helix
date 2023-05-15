#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;

extern int _region_create();
extern void* _region_malloc(int region, int size);
extern void _region_delete(int region);

typedef struct Point Point;

Point test(int _return_region, Point x);

struct Point {
    int x;
    int y;
};

Point test(int _return_region, Point x) {
    return x;
}

#if __cplusplus
}
#endif
