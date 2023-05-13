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
typedef struct int$ptr int$ptr;

int$ptr main(Pool* _pool, int$ptr ptr);

struct int$ptr {
    int* data;
    int pool;
};

int$ptr main(Pool* _pool, int$ptr ptr) {
    int $A = _pool_get_index(_pool);
    int $B = (ptr.pool);

    /* Line 2: New 'int*' */
    int* $D = (int*)_pool_malloc(_pool, $A, sizeof(int));
    (*$D) = 10U;
    int$ptr $E = (int$ptr){ $D, $A };
    int $F = ($E.pool);

    /* Line 2: New variable declaration 'x' */
    int$ptr x = $E;

    return x;
}

#if __cplusplus
}
#endif
