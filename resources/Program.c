#ifdef _MSC_VER
#define inline __inline
#endif

typedef signed long long WORD;
typedef struct Point Point;
typedef struct WORD_$Array WORD_$Array;
void test();

struct Point {
    WORD x;
    WORD y;
};

struct WORD_$Array {
    WORD* data;
    _Region* region;
    WORD count;
};

void test() {
    /* Line 7: Array literal */
    WORD $A[] = { 0, 1, 2 };
    WORD_$Array $B = (WORD_$Array){ $A, _return_region };

    WORD_$Array p = $B;

    /* Line 9: Assignment statement */
    (*(p + 2)) = 7;
}

