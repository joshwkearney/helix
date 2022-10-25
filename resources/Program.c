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

    /* Line 2: New 'int*' */
    int $D = 0U;
    int$ptr $E = { (&$D), 1U, 32767U };
    int $F = ($E.pool);

    /* Line 2: New variable declaration 'b' */
    int$ptr b = $E;

    int $I = $A;
    $I = (($I <= $B) ? $I : $B);

    /* Line 5: New 'int*' */
    int* $H = (int*)_pool_malloc(_pool, $I, sizeof(int));
    int$ptr $J = { $H, 1U, $I };
    int $K = ($J.pool);

    /* Line 5: Assignment statement */
    b = $J;
    int $L = ($J.pool);

    /* Line 7: Assignment statement */
    (*(a.data)) = b;

    return b;
}

#if __cplusplus
}
#endif
