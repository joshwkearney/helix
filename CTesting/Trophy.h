#pragma once

#include <math.h>
#include <inttypes.h>
#include <stdlib.h>

int64_t int_pow(int64_t base, int64_t exp) {
	int64_t result = 1;
	while (exp) {
		if (exp & 1) {
			result *= base;
		}

		exp /= 2;
		base *= base;
	}

	return result;
}