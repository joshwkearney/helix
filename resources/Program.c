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
typedef struct Point$ptr Point$ptr;

void lifetime_test_3(Pool* _pool, int$ptr a);

struct int$ptr {
    int* data;
    int count;
    int pool;
};

struct Point {
    int$ptr x;
    int$ptr y;
};

struct Point$ptr {
    Point* data;
    int count;
    int pool;
};

void lifetime_test_3(Pool* _pool, int$ptr a) {
    int $A = _pool_get_index(_pool);
    int $B = (a.pool);

    /* Line 3: New variable declaration 'x' */
    int x = 45U;

    /* Line 5: New 'Point*' */
    Point $E = 0U;
    Point$ptr $F = (Point$ptr){ (&$E), 1U, $A };
    int $G = ($F.pool);
    (*($F.data)) = (Point){ a, a };

    /* Line 5: New variable declaration 'some' */
    Point$ptr some = $F;

    /* Line 5: Saving lifetime 'lifetime_test_3/$block_0/some' */
    int $H = (some.pool);

}

#if __cplusplus
}
#endif
