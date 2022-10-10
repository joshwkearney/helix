
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

void $run(void* env);

int main() {
    // Create new region
    Region* heap = 0U;
    jmp_buf jump_buffer_1;
    if (HEDLEY_UNLIKELY((0U != setjmp(jump_buffer_1)))) {
        region_delete(heap);
        exit(-1);
    }

    heap = region_create((&jump_buffer_1));
    
    uint64_t start = system_current_time_millis();
    $run(heap);
    uint64_t end = system_current_time_millis();

    printf("Sorting took %llu ms", end - start);
}