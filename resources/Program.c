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
typedef struct Node$ptr Node$ptr;
typedef struct Node Node;

void lifetime_test_3(Pool* _pool);
void struct_test(Pool* _pool, Node x_1);

struct Point {
    int x;
    int y;
};

struct Node$ptr {
    Node* data;
    int pool;
};

struct Node {
    Node$ptr prev;
    Node$ptr next;
    int data;
};

void lifetime_test_3(Pool* _pool) {
    int $A = _pool_get_index(_pool);

    /* Line 3: New variable declaration 'x' */
    Point x = (Point){ 0U, 0U };

    /* Line 4: New variable declaration 'y' */
    int y = 9U;

    /* Line 6: Assignment statement */
    (x.x) = ((x.x) + y);

}

void struct_test(Pool* _pool, Node x_1) {
    int $A = _pool_get_index(_pool);
    int $B = (x_1.pool);
    int $C = (x_1.prev.pool);
    int $D = (x_1.next.pool);
    int $E = (x_1.data.pool);

    /* Line 10: New variable declaration 'p' */
    Node$ptr p = (x_1.prev);

    /* Line 11: New variable declaration 'n' */
    Node$ptr n = (x_1.next);

}

#if __cplusplus
}
#endif
