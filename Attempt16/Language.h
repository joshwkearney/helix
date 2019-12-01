#ifndef LANGUAGE_H
#define LANGUAGE_H

#include <stdlib.h>
#include <stdint.h>

typedef union {
	void* ptr;
	uint8_t flag;
} MetaPtr;

#endif