#if __cplusplus
extern "C" {
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

typedef struct int$array int$array;
typedef struct int$array$ptr int$array$ptr;

void helix_main(_Region* _return_region, int$array$ptr A);

struct int$array {
    int* data;
    _Region* region;
    int count;
};

struct int$array$ptr {
    int$array* data;
    _Region* region;
};

void helix_main(_Region* _return_region, int$array$ptr A) {
    /* Line 2: Array literal */
    int $A[] = { 10U, 9U, 8U, 7U, 6U };
    int$array $array_literal_0 = (int$array){ $A, _return_region };

    /* Line 2: New variable declaration 'array' */
    int$array array = $array_literal_0;
}

#if __cplusplus
}
#endif
