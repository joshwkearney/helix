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
typedef struct int$ptr$ptr int$ptr$ptr;

int$ptr loop_test_1(Pool* _pool, int$ptr$ptr a);

struct int$ptr {
    int* data;
    int count;
    int pool;
};

struct int$ptr$ptr {
    int$ptr* data;
    int count;
    int pool;
};

int$ptr loop_test_1(Pool* _pool, int$ptr$ptr a) {
    int $A = _pool_get_index(_pool);
    int $B = (a.pool);

    int $E = $A;
    $E = (($E <= $B) ? $E : $B);

    /* Line 2: New 'int*' */
    int* $D = (int*)_pool_malloc(_pool, $E, sizeof(int));
    int$ptr $F = { $D, 1U, $E };
    int $G = ($F.pool);

    /* Line 2: New variable declaration 'b' */
    int$ptr b = $F;

    /* Line 4: Assignment statement */
    (*(a.data)) = b;

    return b;
}

#if __cplusplus
}
#endif
