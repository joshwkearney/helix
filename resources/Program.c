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

Point lifetime_test_3(Pool* _pool, Point a, Point b);

struct int$ptr {
    int* data;
    int count;
    int pool;
};

struct Point {
    int$ptr x;
    int$ptr y;
};

Point lifetime_test_3(Pool* _pool, Point a, Point b) {
    int $A = _pool_get_index(_pool);
    int $B = (a.x.pool);
    int $C = (a.y.pool);
    int $D = (b.x.pool);
    int $E = (b.y.pool);

    /* Line 4: New variable declaration 'some' */
    Point some = a;

    /* Line 4: Saving lifetime 'lifetime_test_3/$block_0/some/x' */
    int $F = (some.x.pool);

    /* Line 4: Saving lifetime 'lifetime_test_3/$block_0/some/y' */
    int $G = (some.y.pool);

    /* Line 6: If statement */
    if ((10U < 100U)) { 
        /* Line 7: New 'int*' */
        int* $J = (int*)_pool_malloc(_pool, $A, sizeof(int));
        int$ptr $K = (int$ptr){ $J, 1U, $A };
        int $L = ($K.pool);

        /* Line 7: New 'int*' */
        int* $N = (int*)_pool_malloc(_pool, $A, sizeof(int));
        int$ptr $O = (int$ptr){ $N, 1U, $A };
        int $P = ($O.pool);

        /* Line 7: Assignment statement */
        some = (Point){ $K, $O };

        /* Line 7: Saving lifetime 'lifetime_test_3/$block_0/some/x' */
        int $Q = (some.x.pool);

        /* Line 7: Saving lifetime 'lifetime_test_3/$block_0/some/y' */
        int $R = (some.y.pool);

    } 
    else {
        /* Line 10: Assignment statement */
        some = b;

        /* Line 10: Saving lifetime 'lifetime_test_3/$block_0/some/x' */
        int $S = (some.x.pool);

        /* Line 10: Saving lifetime 'lifetime_test_3/$block_0/some/y' */
        int $T = (some.y.pool);

    }

    /* Line 6: Saving lifetime 'lifetime_test_3/$block_0/some/x' */
    int $U = (some.x.pool);

    /* Line 6: Saving lifetime 'lifetime_test_3/$block_0/some/y' */
    int $V = (some.y.pool);

    return some;
}

#if __cplusplus
}
#endif
