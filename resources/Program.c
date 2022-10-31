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

void lifetime_test_3(Pool* _pool, int$ptr$ptr a);

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

void lifetime_test_3(Pool* _pool, int$ptr$ptr a) {
    int $A = _pool_get_index(_pool);
    int $B = (a.pool);

    /* Line 3: New 'int**' */
    int$ptr $D = 0U;
    int$ptr$ptr $E = (int$ptr$ptr){ (&$D), 1U, $A };
    int $F = ($E.pool);

    /* Line 3: New variable declaration 'target' */
    int$ptr$ptr target = $E;

    /* Line 3: Saving lifetime 'lifetime_test_3/$block_0/target' */
    int $G = (target.pool);

    /* Line 4: New 'int*' */
    int* $I = (int*)_pool_malloc(_pool, $G, sizeof(int));
    int$ptr $J = (int$ptr){ $I, 1U, $G };
    int $K = ($J.pool);

    /* Line 4: New variable declaration 'some' */
    int$ptr some = $J;

    /* Line 4: Saving lifetime 'lifetime_test_3/$block_0/some' */
    int $L = (some.pool);

    /* Line 6: New variable declaration 'i' */
    int i = 0U;

    /* Line 6: While or for loop */
    while (1U) {
        /* Line 6: If statement */
        if ((i >= 10U)) { 
            break;
        } 

        /* Line 7: New 'int*' */
        int* $N = (int*)_pool_malloc(_pool, $G, sizeof(int));
        int$ptr $O = (int$ptr){ $N, 1U, $G };
        int $P = ($O.pool);

        /* Line 7: Assignment statement */
        some = $O;

        /* Line 7: Saving lifetime 'lifetime_test_3/$block_0/some' */
        int $Q = (some.pool);

        /* Line 6: Assignment statement */
        i = (i + 1U);

    }

    /* Line 10: Saving lifetime 'lifetime_test_3/$block_0/$deref_3' */
    int $R = ((target.data).pool);

    /* Line 10: Assignment statement */
    (*(target.data)) = some;

}

#if __cplusplus
}
#endif
