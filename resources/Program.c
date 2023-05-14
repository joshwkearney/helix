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
typedef struct Point Point;
typedef struct Point$ptr Point$ptr;
typedef struct Point$ptr$array Point$ptr$array;
typedef struct int$ptr int$ptr;
typedef struct Point$ptr$ptr Point$ptr$ptr;

Point$ptr main(Pool* _pool, Point$ptr$array array, int$ptr ptr);

struct Point {
    int x;
    int y;
};

struct Point$ptr {
    Point* data;
    int pool;
};

struct Point$ptr$array {
    Point$ptr* data;
    int pool;
    int count;
};

struct int$ptr {
    int* data;
    int pool;
};

struct Point$ptr$ptr {
    Point$ptr* data;
    int pool;
};

Point$ptr main(Pool* _pool, Point$ptr$array array, int$ptr ptr) {
    int $A = _pool_get_index(_pool);
    int $B = (array.pool);
    int $C = (ptr.pool);

    int $G = $A;
    $G = (($G <= $B) ? $G : $B);

    /* Line 2: New 'Point*' */
    Point* $F = (Point*)_pool_malloc(_pool, $G, sizeof(Point));
    (*$F) = (Point){ 0U, 0U };
    Point$ptr $H = (Point$ptr){ $F, $G };
    int $I = ($H.pool);

    /* Line 2: New variable declaration 'test' */
    Point$ptr test = $H;

    /* Line 4: Array to pointer conversion */
    Point$ptr$ptr $J = (Point$ptr$ptr){ ((array.data) + 10U), (array.pool) };

    /* Line 4: Assignment statement */
    (*($J.data)) = test;

    return test;
}

#if __cplusplus
}
#endif
