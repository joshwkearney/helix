#ifdef _MSC_VER
#define inline __inline
#endif

typedef struct _Region {
	unsigned int depth;
} _Region;

extern _Region* _region_new();
extern void* _region_malloc(_Region* region, int size);
extern void _region_delete(_Region* region);
static inline _Region* _region_min(_Region* r1, _Region* r2) { return r1->depth < r2->depth ? r1 : r2;  }
typedef struct int_$Pointer int_$Pointer;
int_$Pointer test3(_Region* _return_region, int x);

struct int_$Pointer {
    int* data;
    _Region* region;
};

int_$Pointer test3(_Region* _return_region, int x) {
    /* Line 2: New variable declaration 'a' */
    int* a = _region_malloc(_return_region, sizeof(int));
    (*a) = 10U;

    /* Line 3: New variable declaration 'b' */
    int* b = _region_malloc(_return_region, sizeof(int));
    (*b) = 15U;

    /* Line 5: New variable declaration 'aa' */
    int_$Pointer aa = (int_$Pointer){ a, _return_region };

    /* Line 6: New variable declaration 'bb' */
    int_$Pointer bb = (int_$Pointer){ b, _return_region };

    /* Line 7: New variable declaration 'cc' */
    int_$Pointer cc = (int_$Pointer){ a, _return_region };

    /* Line 9: While or for loop */
    while (1U) {
        /* Line 9: If statement */
        if (!x) { 
            break;
        } 

        /* Line 10: Assignment statement */
        cc = aa;

        /* Line 11: Assignment statement */
        aa = bb;
    }

    return cc;
}

