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

void loop_test_1(Pool* _pool, int$ptr$ptr a, int$ptr$ptr b);

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

void loop_test_1(Pool* _pool, int$ptr$ptr a, int$ptr$ptr b) {
    int $A = _pool_get_index(_pool);
    int $B = (a.pool);
    int $C = (b.pool);

    /* Line 2: If statement */
    int$ptr$ptr $D;
    if ((10U < 100U)) { 
        $D = a;
    } 
    else {
        $D = b;
    }
    int $E = ($D.pool);

    /* Line 2: New variable declaration 'target' */
    int$ptr$ptr target = $D;
    int $F = (target.pool);

    /* Line 6: New 'int*' */
    int* $H = (int*)_pool_malloc(_pool, $F, sizeof(int));
    int$ptr $I = { $H, 1U, $F };
    int $J = ($I.pool);

    /* Line 6: New variable declaration 'c' */
    int$ptr c = $I;
    int $K = (c.pool);

    /* Line 7: Assignment statement */
    (*(target.data)) = c;

}

#if __cplusplus
}
#endif
