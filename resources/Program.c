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
typedef struct Point Point;

int$ptr lifetime_test_3(Pool* _pool, int$ptr$ptr a, int$ptr$ptr b, Point c);

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

struct Point {
    int$ptr x;
    int$ptr y;
};

int$ptr lifetime_test_3(Pool* _pool, int$ptr$ptr a, int$ptr$ptr b, Point c) {
    int $A = _pool_get_index(_pool);
    int $B = (a.pool);
    int $C = (b.pool);
    int $D = (c.x.pool);
    int $E = (c.y.pool);

    int $H = $C;
    $H = (($H <= $A) ? $H : $A);

    /* Line 4: New 'int*' */
    int* $G = (int*)_pool_malloc(_pool, $H, sizeof(int));
    int$ptr $I = (int$ptr){ $G, 1U, $H };
    int $J = ($I.pool);

    /* Line 4: New variable declaration 'some' */
    int$ptr some = $I;

    /* Line 4: Saving lifetime 'lifetime_test_3/$block_0/some' */
    int $K = (some.pool);

    /* Line 5: New variable declaration 'target' */
    int$ptr$ptr target = a;

    /* Line 5: Saving lifetime 'lifetime_test_3/$block_0/target' */
    int $L = (target.pool);

    /* Line 7: If statement */
    if ((10U < 100U)) { 
        /* Line 8: Assignment statement */
        target = b;

        /* Line 8: Saving lifetime 'lifetime_test_3/$block_0/target' */
        int $M = (target.pool);

    } 
    else {
        /* Line 11: Assignment statement */
        target = b;

        /* Line 11: Saving lifetime 'lifetime_test_3/$block_0/target' */
        int $N = (target.pool);

    }

    /* Line 7: Saving lifetime 'lifetime_test_3/$block_0/target' */
    int $O = (target.pool);

    /* Line 14: Saving lifetime 'lifetime_test_3/$block_0/$deref_5' */
    int $P = ((target.data).pool);

    /* Line 14: Assignment statement */
    (*(target.data)) = some;

    return some;
}

#if __cplusplus
}
#endif
