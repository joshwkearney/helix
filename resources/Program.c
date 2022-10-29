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
typedef struct Point Point;
typedef struct Point$ptr Point$ptr;

int$ptr lifetime_test_3(Pool* _pool, Point$ptr c);

struct int$ptr {
    int* data;
    int count;
    int pool;
};

struct Point {
    int$ptr x;
    int$ptr y;
};

struct Point$ptr {
    Point* data;
    int count;
    int pool;
};

int$ptr lifetime_test_3(Pool* _pool, Point$ptr c) {
    int $A = _pool_get_index(_pool);
    int $B = (c.pool);

    /* Line 4: Saving lifetime 'lifetime_test_3/$block_0/$deref_2/x' */
    int $C = (((c.data)->x).pool);

    /* Line 4: Saving lifetime 'lifetime_test_3/$block_0/$deref_2/y' */
    int $D = (((c.data)->y).pool);

    /* Line 4: New variable declaration 'some' */
    int$ptr some = ((*(c.data)).x);

    /* Line 4: Saving lifetime 'lifetime_test_3/$block_0/some' */
    int $E = (some.pool);

    /* Line 6: Saving lifetime 'lifetime_test_3/$block_0/$deref_3/x' */
    int $F = (((c.data)->x).pool);

    /* Line 6: Saving lifetime 'lifetime_test_3/$block_0/$deref_3/y' */
    int $G = (((c.data)->y).pool);

    /* Line 6: New 'int*' */
    int* $I = (int*)_pool_malloc(_pool, $F, sizeof(int));
    int$ptr $J = { $I, 1U, $F };
    int $K = ($J.pool);

    /* Line 6: Assignment statement */
    ((*(c.data)).x) = $J;

    /* Line 8: Saving lifetime 'lifetime_test_3/$block_0/$deref_4/x' */
    int $L = (((c.data)->x).pool);

    /* Line 8: Saving lifetime 'lifetime_test_3/$block_0/$deref_4/y' */
    int $M = (((c.data)->y).pool);

    return ((*(c.data)).x);
}

#if __cplusplus
}
#endif
