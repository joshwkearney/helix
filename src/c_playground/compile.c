
#include "include/trophy.h"
#include <stdlib.h>
#include <stdio.h>
#include <time.h>
#include <sys/timeb.h>

typedef struct IntArray {
    trophy_int size;
    trophy_int* data;
} IntArray;

uint64_t system_current_time_millis()
{
#if defined(_WIN32) || defined(_WIN64)
    struct _timeb timebuffer;
    _ftime_s(&timebuffer);
    return (uint64_t)(((timebuffer.time * 1000) + timebuffer.millitm));
#else
    struct timeb timebuffer;
    ftime(&timebuffer);
    return (uint64_t)(((timebuffer.time * 1000) + timebuffer.millitm));
#endif
}

void $parallel_mergesort(void* env, IntArray $arr17);

void $selection_sort(void* env, IntArray $arr67);

int qsort_compare(const void* a, const void* b)
{
    trophy_int int_a = *((trophy_int*)a);
    trophy_int int_b = *((trophy_int*)b);

    // an easy expression for comparing
    return (int_a > int_b) - (int_a < int_b);
}

int main() {
    // Create new region
    Region* $anon_region_13 = 0U;
    jmp_buf jump_buffer_1;
    if (HEDLEY_UNLIKELY((0U != setjmp(jump_buffer_1)))) {
        region_delete($anon_region_13);
        exit(-1);
    }

    $anon_region_13 = region_create((&jump_buffer_1));


    srand(time(NULL));

    size_t len = 10000000;

    trophy_int* data = calloc(len, sizeof(trophy_int));
    for (int i = 0; i < len; i++) {
        data[i] = rand();
    }

    IntArray array;
    array.size = len;
    array.data = data;

    //for (int i = 0; i < len; i++) {
        //printf("%lu ", array.data[i]);
   // }

    printf("\n\n");

    uint64_t start = system_current_time_millis();

    //qsort(array.data, array.size, sizeof(trophy_int), qsort_compare);

    $parallel_mergesort($anon_region_13, array);
    //$selection_sort($anon_region_13, array);

    uint64_t end = system_current_time_millis();

   // for (int i = 0; i < len; i++) {
    //    printf("%lu ", array.data[i]);
    //}

    printf("\n\n");

    printf("Sorting took %d ms", end - start);
}