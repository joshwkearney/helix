#pragma once

#ifndef TROPHY_H
#define TROPHY_H

#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <setjmp.h>

#define HAVE_STRUCT_TIMESPEC
#include "include/pthread.h"

#include "include/hedley.h"

typedef int trophy_bool;
typedef int trophy_void;
typedef uint64_t trophy_int;
typedef struct Region Region;

#if defined(__cplusplus)
extern "C" {
#endif

	HEDLEY_MALLOC
	void* region_alloc(Region* region, int bytes);

	Region* region_create(jmp_buf* panic_buffer);

	void region_delete(Region* region);

	void region_async(Region* region, void* func(void* arg), void* arg);

	HEDLEY_NO_RETURN
	void region_panic(Region* region);

#if defined(__cplusplus)
}
#endif

#endif