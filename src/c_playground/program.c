// TrophyTesting.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include "include\trophy.h"

typedef struct Region Region;
typedef struct Result Result;
typedef struct IntArray IntArray;
typedef union UnionType_1 UnionType_1;

struct Result {
    int tag;
    union {
        int error;
        int success;
    } data;
};

struct IntArray {
    int size;
    int* data;
};

trophy_int $test(void* env);

int main(int argc, char** argv) {
    int total_length = 0;
    for (int i = 1; i < argc; i++) {
        total_length += strlen(argv[i]);
    }

    int pos = 0;
    int* input = malloc(total_length * sizeof(int));

    for (int i = 1; i < argc; i++) {
        for (int j = 0; j < strlen(argv[i]); j++) {
            input[pos] = argv[i][j];
            pos++;
        }
    }

    jmp_buf buffer;
    Region* region = 0;

    if (0 != setjmp(buffer)) {
        goto cleanup;
    }

    region = region_create(&buffer);

    $test(region);

cleanup:
    region_delete(region);

    /*if (result.tag == 0) {
        printf("Error!");
    }
    else {
        printf("%d", result.data.success);
    }*/
}