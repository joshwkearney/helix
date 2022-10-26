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

    int $F = $C;
    $F = (($F <= $B) ? $F : $B);

    /* Line 2: New 'int*' */
    int* $E = (int*)_pool_malloc(_pool, $F, sizeof(int));
    int$ptr $G = { $E, 1U, $F };
    int $H = ($G.pool);

    /* Line 2: New variable declaration 'c' */
    int$ptr c = $G;
    int $I = (c.pool);

    /* Line 4: If statement */
    int$ptr$ptr $J;
    if ((10U < 100U)) { 
        $J = a;
    } 
    else {
        $J = b;
    }
    int $K = ($J.pool);

    /* Line 4: New variable declaration 'target' */
    int$ptr$ptr target = $J;
    int $L = (target.pool);

    /* Line 8: Assignment statement */
    (*(target.data)) = c;

}

#if __cplusplus
}
#endif
