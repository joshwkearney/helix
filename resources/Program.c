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

Point lifetime_test_2(Pool* _pool, int a, int$ptr b, int$ptr c);

struct int$ptr {
    int* data;
    int count;
    int pool;
};

struct Point {
    int$ptr x;
    int$ptr y;
};

Point lifetime_test_2(Pool* _pool, int a, int$ptr b, int$ptr c) {
    int $A = _pool_get_index(_pool);
    int $B = (b.pool);
    int $C = (c.pool);

    /* Line 2: New 'int*' */
    int $F = 0U;
    int$ptr $G = { (&$F), 1U, $A };
    int $H = ($G.pool);

    /* Line 2: New 'int*' */
    int* $J = (int*)_pool_malloc(_pool, $A, sizeof(int));
    int$ptr $K = { $J, 1U, $A };
    int $L = ($K.pool);

    /* Line 2: New variable declaration 'd' */
    Point d = { $G, $K };

    /* Line 2: Saving lifetime 'lifetime_test_2/$block_0/d/x' */
    int $M = (d.x.pool);

    /* Line 2: Saving lifetime 'lifetime_test_2/$block_0/d/y' */
    int $N = (d.y.pool);

    /* Line 4: If statement */
    int$ptr $if_temp_0;
    if ((a < 100U)) { 
        $if_temp_0 = b;
    } 
    else {
        $if_temp_0 = c;
    }

    /* Line 4: Saving lifetime 'lifetime_test_2/$block_0/$if_temp_0' */
    int $O = ($if_temp_0.pool);

    /* Line 4: Assignment statement */
    (d.x) = $if_temp_0;

    /* Line 4: Saving lifetime 'lifetime_test_2/$block_0/d/x' */
    int $P = (x.pool);

    return d;
}

#if __cplusplus
}
#endif
