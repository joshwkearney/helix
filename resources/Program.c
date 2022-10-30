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

void lifetime_test_3(Pool* _pool, int$ptr$ptr a, int$ptr$ptr b);

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

void lifetime_test_3(Pool* _pool, int$ptr$ptr a, int$ptr$ptr b) {
    int $A = _pool_get_index(_pool);
    int $B = (a.pool);
    int $C = (b.pool);

    /* Line 4: New 'int*' */
    int* $E = (int*)_pool_malloc(_pool, $C, sizeof(int));
    int$ptr $F = (int$ptr){ $E, 1U, $C };
    int $G = ($F.pool);

    /* Line 4: New variable declaration 'some' */
    int$ptr some = $F;

    /* Line 4: Saving lifetime 'lifetime_test_3/$block_0/some' */
    int $H = (some.pool);

    /* Line 5: New variable declaration 'target' */
    int$ptr$ptr target = a;

    /* Line 5: Saving lifetime 'lifetime_test_3/$block_0/target' */
    int $I = (target.pool);

    /* Line 7: If statement */
    if ((10U < 100U)) { 
        /* Line 8: Assignment statement */
        target = b;

        /* Line 8: Saving lifetime 'lifetime_test_3/$block_0/target' */
        int $J = (target.pool);

    } 
    else {
        /* Line 11: Assignment statement */
        target = b;

        /* Line 11: Saving lifetime 'lifetime_test_3/$block_0/target' */
        int $K = (target.pool);

    }

    /* Line 7: Saving lifetime 'lifetime_test_3/$block_0/target' */
    int $L = (target.pool);

    /* Line 14: Saving lifetime 'lifetime_test_3/$block_0/$deref_4' */
    int $M = ((target.data).pool);

    /* Line 14: Assignment statement */
    (*(target.data)) = some;

}

#if __cplusplus
}
#endif
