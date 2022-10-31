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

void lifetime_test_3(Pool* _pool, int$ptr a);

struct int$ptr {
    int* data;
    int pool;
};

void lifetime_test_3(Pool* _pool, int$ptr a) {
    int $A = _pool_get_index(_pool);
    int $B = (a.pool);

    /* Line 3: New variable declaration 'x' */
    int x = 45U;

    /* Line 5: New '0*' */
    int $D = 0U;
    int$ptr $E = (int$ptr){ (&$D), $A };
    int $F = ($E.pool);
    (*($E.data)) = 0U;

    /* Line 5: New variable declaration 'some' */
    int$ptr some = $E;

    /* Line 5: Saving lifetime 'lifetime_test_3/$block_0/some' */
    int $G = (some.pool);

}

#if __cplusplus
}
#endif
