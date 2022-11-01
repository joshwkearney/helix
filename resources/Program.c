#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;

typedef struct Pool Pool;

extern Pool* _pool_create();
extern void* _pool_malloc(Pool* pool, int pool_index, int size);
extern int _pool_get_index(Pool* pool);
extern void _pool_delete();
typedef struct int$array int$array;
typedef struct int$ptr int$ptr;

void lifetime_test_3(Pool* _pool, int$array a);

struct int$array {
    int* data;
    int pool;
    int count;
};

struct int$ptr {
    int* data;
    int pool;
};

void lifetime_test_3(Pool* _pool, int$array a) {
    int $A = _pool_get_index(_pool);
    int $B = (a.pool);

    /* Line 3: Array to pointer conversion */
    int$ptr $C = (int$ptr){ ((a.data) + 45U), (a.pool) };

    /* Line 3: Array to pointer conversion */
    int$ptr $D = (int$ptr){ ((a.data) + 0U), (a.pool) };

    /* Line 3: Assignment statement */
    (*($C.data)) = (*($D.data));

}

#if __cplusplus
}
#endif
