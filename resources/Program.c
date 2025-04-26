#ifdef _MSC_VER
#define inline __inline
#endif

typedef signed long long _Word;

typedef struct _Region {
	unsigned int depth;
} _Region;

extern _Region* _region_create();
extern void _region_destroy(_Region* region);
extern void* _region_malloc(_Region* region, int size);
static inline _Region* _region_min(_Region* r1, _Region* r2) { return r1->depth < r2->depth ? r1 : r2;  }

typedef struct Point Point;
typedef struct Test Test;
typedef struct _Word_$Pointer _Word_$Pointer;
_Word fib(_Region* _return_region, _Word x);
_Word fib2(_Region* _return_region, _Word x_1);
void test2(_Region* _return_region, _Word_$Pointer z);
_Word test(_Region* _return_region, _Word limit);

struct Point {
    _Word x;
    Test y;
};

struct Test {
    _Word x;
};

struct _Word_$Pointer {
    _Word* data;
    _Region* region;
};

_Word fib(_Region* _return_region, _Word x) {
    /* Line 11: If statement */
    _Word $C;
    if ((x <= 1)) { 
        $C = x;
    } 
    else {
        /* Line 13: Function call */
        _Word $A = fib((x - 1));

        /* Line 13: Function call */
        _Word $B = fib((x - 2));

        $C = ($A + $B);
    }

    return $C;
}

_Word fib2(_Region* _return_region, _Word x_1) {
    /* Line 17: If statement */
    if ((x_1 <= 1)) { 
        return x_1;
    } 

    /* Line 21: Function call */
    _Word $B = fib((x_1 - 1));

    /* Line 21: Function call */
    _Word $C = fib((x_1 - 2));

    return ($B + $C);
}

void test2(_Region* _return_region, _Word_$Pointer z) {
    _Word x_2 = 45;

    _Word_$Pointer y = (_Word_$Pointer){ (&x_2) };

    /* Line 28: Assignment statement */
    y = z;

    /* Line 30: Pointer dereference */
    _Word $A = (*(y.data));

    /* Line 30: Assignment statement */
    (*(y.data)) = (7 * $A);
}

_Word test(_Region* _return_region, _Word limit) {
    _Word i = 0;

    /* Line 36: Loop */
    while (1) {
        /* Line 37: If statement */
        if ((i < limit)) { 
            /* Line 38: Assignment statement */
            i = (i + 1);

            continue;
        } 

        /* Line 42: If statement */
        _Word $B;
        if ((i < 5)) { 
            $B = 0;
        } 
        else {
            $B = 8;
        }

        return $B;
    }
}

