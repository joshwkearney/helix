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

void lifetime_test_2(Pool* _pool, int$ptr$ptr a, int$ptr$ptr b);

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

void lifetime_test_2(Pool* _pool, int$ptr$ptr a, int$ptr$ptr b) {
    int $A = _pool_get_index(_pool);
    int $B = (a.pool);
    int $C = (b.pool);

    /* Line 4: If statement */
    int$ptr$ptr $if_temp_0;
    if ((10U < 100U)) { 
        $if_temp_0 = a;
    } 
    else {
        $if_temp_0 = b;
    }

    /* Line 4: Saving lifetime 'lifetime_test_2/$block_0/$if_temp_0' */
    int $D = ($if_temp_0.pool);

    /* Line 4: New variable declaration 'target' */
    int$ptr$ptr target = $if_temp_0;

    /* Line 4: Saving lifetime 'lifetime_test_2/$block_0/target' */
    int $E = (target.pool);

    /* Line 8: New 'int*' */
    int* $G = (int*)_pool_malloc(_pool, $E, sizeof(int));
    int$ptr $H = { $G, 1U, $E };
    int $I = ($H.pool);

    /* Line 8: New variable declaration 'c' */
    int$ptr c = $H;

    /* Line 8: Saving lifetime 'lifetime_test_2/$block_0/c' */
    int $J = (c.pool);

    /* Line 9: Assignment statement */
    (*(target.data)) = c;

}

#if __cplusplus
}
#endif
