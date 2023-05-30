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
typedef struct int_$Pointer_$Pointer int_$Pointer_$Pointer;
typedef struct int_$Pointer_$Pointer_$Pointer int_$Pointer_$Pointer_$Pointer;
void test16(_Region* _return_region, int_$Pointer_$Pointer a);

struct int_$Pointer {
    int* data;
    _Region* region;
};

struct int_$Pointer_$Pointer {
    int_$Pointer* data;
    _Region* region;
};

struct int_$Pointer_$Pointer_$Pointer {
    int_$Pointer_$Pointer* data;
    _Region* region;
};

void test16(_Region* _return_region, int_$Pointer_$Pointer a) {
    /* Line 2: New variable declaration 'x' */
    int_$Pointer_$Pointer_$Pointer x = (int_$Pointer_$Pointer_$Pointer){ (&a), _return_region };

    /* Line 4: New variable declaration 'i' */
    int i = 0U;

    /* Line 4: While or for loop */
    while (1U) {
        /* Line 4: If statement */
        if ((i > 10U)) { 
            break;
        } 

        /* Line 5: New variable declaration 'b' */
        int* b = _region_malloc((a.region), sizeof(int));
        (*b) = 10U;

        /* Line 6: New variable declaration 'c' */
        int_$Pointer* c = _region_malloc((a.region), sizeof(int_$Pointer));
        (*c) = (int_$Pointer){ b, (a.region) };

        /* Line 8: Assignment statement */
        (*(x.data)) = (int_$Pointer_$Pointer){ c, (a.region) };

        /* Line 4: Assignment statement */
        i = (i + 1U);
    }
}

