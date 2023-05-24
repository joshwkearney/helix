#if __cplusplus
extern "C" {
#endif

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

typedef struct int$ptr int$ptr;
typedef union IntOption$union IntOption$union;
typedef struct IntOption IntOption;

void helix_main(_Region* _return_region, int$ptr b);

struct int$ptr {
    int* data;
    _Region* region;
};


union IntOption$union {
    int none;
    int$ptr some;
};

struct IntOption {
    int tag;
    IntOption$union data;
};

void helix_main(_Region* _return_region, int$ptr b) {
    /* Line 2: Union literal */
    IntOption$union $new_union_0;
    ($new_union_0.some) = b;

    /* Line 2: New variable declaration 'a' */
    IntOption a = (IntOption){ 1U, $new_union_0 };
}

#if __cplusplus
}
#endif
