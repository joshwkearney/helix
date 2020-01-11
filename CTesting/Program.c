#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>

typedef short selection_sort;

typedef short fib_linear;

typedef short fib_recursive;

typedef struct $array {
    int64_t size;
    uintptr_t data;
} $array;

uint16_t $func$main();

uint16_t $func$selection_sort($array array);

int64_t $func$fib_linear(int64_t n);

int64_t $func$fib_recursive(int64_t n);

inline void $destructor_array_int($array obj) {
    if ((obj.data & 1) == 1) {
        free((int64_t*)(obj.data & ~1));
    }
}

int main() {
    $func$main();
    return 0;
}

uint16_t $func$main() {
    // Array initialization
    $array $array_init_0;
    $array_init_0.size = 7LL;
    $array_init_0.data = (uintptr_t)malloc(7LL, sizeof(int64_t));
    ((int64_t*)($array_init_0.data))[0] = 5LL;
    ((int64_t*)($array_init_0.data))[1] = 8LL;
    ((int64_t*)($array_init_0.data))[2] = 6LL;
    ((int64_t*)($array_init_0.data))[3] = 2LL;
    ((int64_t*)($array_init_0.data))[4] = 7LL;
    ((int64_t*)($array_init_0.data))[5] = 3LL;
    ((int64_t*)($array_init_0.data))[6] = 9LL;
    $array_init_0.data |= 1;

    // Variable initalization
    $array array = $array_init_0;

    // Array copy
    $array $array_copy_0 = array;
    $array_copy_0.data &= ~1;

    // Function invoke
    uint16_t $invoke_result_0 = $func$selection_sort($array_copy_0);

    // Debuggins
    int64_t* a = (int64_t*)(array.data & ~1);

    for (int64_t i = 0; i < array.size; i++) {
        printf("%d\n", a[i]);
    }

    // Block cleanup
    uint16_t $block_return_0 = 0;
    $destructor_array_int($array_copy_0);
    $destructor_array_int(array);

    // Function cleanup
    uint16_t $func_return = $block_return_0;
    return $func_return;
}

uint16_t $func$selection_sort($array array) {
    // Variable initalization
    int64_t i = 0LL;

    // While loop
    while (((i + 1LL) < (array.size))) {
        // Variable initalization
        int64_t j = i;

        // Variable initalization
        int64_t smallest = j;

        // While loop
        while (((j + 1LL) < (array.size))) {
            // Variable store
            j = (j + 1LL);

            // If statement
            if ((((int64_t*)(array.data & ~1))[j] < ((int64_t*)(array.data & ~1))[smallest])) {
                // Variable store
                smallest = j;

                // Block cleanup
                uint16_t $block_return_1 = 0;
            }

            // Block cleanup
            uint16_t $block_return_2 = 0;
        }

        // Variable initalization
        int64_t temp = ((int64_t*)(array.data & ~1))[i];

        // Array store
        ((int64_t*)(array.data & ~1))[i] = ((int64_t*)(array.data & ~1))[smallest];

        // Array store
        ((int64_t*)(array.data & ~1))[smallest] = temp;

        // Variable store
        i = (i + 1LL);

        // Block cleanup
        uint16_t $block_return_3 = 0;
    }

    // Block cleanup
    uint16_t $block_return_4 = 0;

    // Function cleanup
    uint16_t $func_return = $block_return_4;
    $destructor_array_int(array);
    return $func_return;
}