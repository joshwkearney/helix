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

Point lifetime_test_3(Pool* _pool, int$ptr a);

struct int$ptr {
    int* data;
    int count;
    int pool;
};

struct Point {
    int$ptr x;
    int$ptr y;
};

Point lifetime_test_3(Pool* _pool, int$ptr a) {
    int $A = _pool_get_index(_pool);
    int $B = (a.pool);

    /* Line 3: New variable declaration 'some' */
    Point some = (Point){ a, a };

    /* Line 3: Saving lifetime 'lifetime_test_3/$block_0/some/x' */
    int $D = (some.x.pool);

    /* Line 3: Saving lifetime 'lifetime_test_3/$block_0/some/y' */
    int $E = (some.y.pool);

    /* Line 5: New variable declaration 'i' */
    int i = 0U;

    /* Line 5: While or for loop */
    while (1U) {
        /* Line 5: Saving lifetime 'lifetime_test_3/$block_0/some/x' */
        int $F = (some.x.pool);

        /* Line 5: Saving lifetime 'lifetime_test_3/$block_0/some/y' */
        int $G = (some.y.pool);

        /* Line 5: Saving lifetime 'lifetime_test_3/a' */
        int $H = (a.pool);

        /* Line 5: If statement */
        if ((i >= 10U)) { 
            break;
        } 

        /* Line 6: Assignment statement */
        some = (Point){ a, a };

        /* Line 6: Saving lifetime 'lifetime_test_3/$block_0/some/x' */
        int $J = (some.x.pool);

        /* Line 6: Saving lifetime 'lifetime_test_3/$block_0/some/y' */
        int $K = (some.y.pool);

        /* Line 5: Assignment statement */
        i = (i + 1U);

    }

    /* Line 5: Saving lifetime 'lifetime_test_3/$block_0/some/x' */
    int $L = (some.x.pool);

    /* Line 5: Saving lifetime 'lifetime_test_3/$block_0/some/y' */
    int $M = (some.y.pool);

    /* Line 5: Saving lifetime 'lifetime_test_3/a' */
    int $N = (a.pool);

    return some;
}

#if __cplusplus
}
#endif
