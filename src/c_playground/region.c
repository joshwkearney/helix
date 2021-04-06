#include "include\trophy.h"
#include <stdio.h>

typedef struct RegionFrame RegionFrame;

struct RegionFrame {
    RegionFrame* next;
    char* data;
    int64_t total_size;
    int64_t current_size;
};

struct Region {
    RegionFrame* frame;

    int num_thread_handles;
    int max_thread_handles;
    pthread_t* thread_handles;

    jmp_buf* panic_buffer;
};

static RegionFrame* frame_alloc(Region* region, int64_t bytes, RegionFrame* next) {
    RegionFrame* frame = malloc(sizeof(RegionFrame));

    // If the allocation failed, panic the region
    if (HEDLEY_UNLIKELY(frame == 0)) {
        region_panic(region);
    }

    frame->total_size = bytes;
    frame->current_size = 0;
    frame->next = next;
    frame->data = malloc(bytes);

    if (frame->data == 0) {
        region_panic(region);
    }

    return frame;
}

void* region_alloc(Region* region, int bytes) {
    // If this is a new region, allocate an initial frame
    if (region->frame == 0) {
        region->frame = frame_alloc(region, 8 * 1024, 0);
    }

    // If the current frame is too small, make a new frame
    if (region->frame->total_size - region->frame->current_size < bytes) {
        int64_t size = 2 * region->frame->total_size;
        while (size < bytes) {
            size *= 2;
        }

        RegionFrame* newFrame = frame_alloc(region, size, region->frame);
        region->frame = newFrame;
    }

    // Otherwise, allocate from the current frame
    void* data = region->frame->data + region->frame->current_size;

    region->frame->current_size += bytes;

    return data;
}

HEDLEY_NO_RETURN
void region_panic(Region* region) {
    longjmp(*region->panic_buffer, 1);
}

Region* region_create(jmp_buf* panic_buffer) {
    Region* region = malloc(sizeof(Region));

    // Make sure the malloc didn't fail
    if (HEDLEY_UNLIKELY(region == 0)) {
        longjmp(*panic_buffer, 1);
    }

    // The frame will be allocated later
    region->frame = 0;
    region->panic_buffer = panic_buffer;
    region->thread_handles = malloc(sizeof(pthread_t) * 0);
    region->num_thread_handles = 0;
    region->max_thread_handles = 0;

    return region;
}

void region_delete(Region* region) {
    if (region == 0) {
        return;
    }

    // Try to join all threads
    int join_failure = 0;
    for (int i = 0; i < region->num_thread_handles; i++) {
        void* result;
        int code = pthread_join(region->thread_handles[i], &result);

        if (0 != code) {
            char buff[1001];
            buff[1000] = '\0';

            strerror_s(buff, 1000, code);
            printf("%s", buff);

            join_failure = 1;
        }

        if (result == PTHREAD_CANCELED || result < 0) {
            join_failure = 1;
        }
    }

    // Clean up threading stuff
    free(region->thread_handles);
    region->max_thread_handles = 0;
    region->num_thread_handles = 0;
    region->thread_handles = 0;

    // If a thread failed to join, panic. The panic will finish cleaning up later.
    if (join_failure) {
        region_panic(region);
    }

    // Free memory frames
    RegionFrame* frame = region->frame;
    while (frame != 0) {
        RegionFrame* next = frame->next;

        free(frame->data);
        free(frame);

        frame = next;
    }

    free(region);
}

void region_async(Region* region, void* func(void*), void* arg) {
    if (region->max_thread_handles < region->num_thread_handles + 1) {
        void* new_mem = realloc(region->thread_handles, sizeof(pthread_t) * (region->max_thread_handles * 2 + 1));

        // Make sure realloc didn't fail
        if (new_mem == 0) {
            region_panic(region);
        }

        region->thread_handles = new_mem;
        region->max_thread_handles = region->max_thread_handles * 2 + 1;
    }

    pthread_create(&region->thread_handles[region->num_thread_handles], 0, func, arg);
    region->num_thread_handles++;
}