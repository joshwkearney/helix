#include "include/trophy.h"

typedef struct ArrayType0 ArrayType0;

typedef struct ClosureEnvironment0 ClosureEnvironment0;

typedef struct ClosureType_2 ClosureType_2;

struct ArrayType0 {
    trophy_int size;
    trophy_int* data;
};

void $parallel_mergesort(void* env, ArrayType0 $arr0);

struct ClosureEnvironment0 {
    trophy_int* $tasks17;
    ArrayType0* $part15;
};

static void $parallel_mergesort_helper$$block1$$block11$$block13$$lambda15(void* environment);

typedef void (*FuncType_1)(void* environment);

struct ClosureType_2 {
    void* environment;
    FuncType_1 function;
};

void $parallel_mergesort_helper(void* env, ArrayType0 $arr16, trophy_int $tasks17);

void $merge(void* env, ArrayType0 $arr33, trophy_int $mid34);

trophy_int $binary_search(void* env, ArrayType0 $arr47, trophy_int $to_find48);

void $selection_sort(void* env, ArrayType0 $arr67);

void $parallel_mergesort(void* env, ArrayType0 $arr0) {
    Region* heap = (Region*)env;

    $parallel_mergesort_helper(heap, $arr0, 16U);
}

static void $parallel_mergesort_helper$$block1$$block11$$block13$$lambda15(void* environment) {
    ClosureEnvironment0* environment_temp1 = (ClosureEnvironment0*)environment;

    // Unpack the closure environment
    trophy_int* $tasks17 = ((*environment_temp1).$tasks17);
    ArrayType0* $part15 = ((*environment_temp1).$part15);

    // Create new region
    Region* $async_region14 = 0U;
    jmp_buf async_jump_buffer_0;
    if (HEDLEY_UNLIKELY((0U != setjmp(async_jump_buffer_0)))) {
        region_delete($async_region14);
        return;
    }

    $async_region14 = region_create((&async_jump_buffer_0));

    $parallel_mergesort_helper($async_region14, (*$part15), ((*$tasks17) / 2U));
    region_delete($async_region14);
}

void $parallel_mergesort_helper(void* env, ArrayType0 $arr16, trophy_int $tasks17) {
    Region* heap = (Region*)env;

    // If statement
    trophy_void if_temp_0;
    if ((($arr16.size) <= 15U)) {
        $selection_sort(heap, $arr16);
        return;
        if_temp_0 = 0U;
    }
    else {
        if_temp_0 = 0U;
    }

    if_temp_0;
    // Definition of variable 'mid'
    trophy_int $mid4 = (($arr16.size) / 2U);

    (&$mid4);
    // Array slice bounds check
    if (HEDLEY_UNLIKELY(((0U < 0U) | (($mid4 < 0U) | ($mid4 > ($arr16.size)))))) {
        region_panic(heap, "Panic! Array slice bounds \"0U\" and \"$mid4\" are outside the bounds of the array \"$arr16\"");
    }

    // Array slice
    ArrayType0 array_slice_0;
    (array_slice_0.data) = (($arr16.data) + 0U);
    (array_slice_0.size) = ($mid4 - 0U);

    // Definition of variable 'part1'
    ArrayType0 $part15 = array_slice_0;

    (&$part15);
    // Definition of variable '$slice_temp6'
    ArrayType0 $$slice_temp68 = $arr16;

    (&$$slice_temp68);
    // Array slice bounds check
    if (HEDLEY_UNLIKELY((($mid4 < 0U) | ((($$slice_temp68.size) < $mid4) | (($$slice_temp68.size) > ($$slice_temp68.size)))))) {
        region_panic(heap, "Panic! Array slice bounds \"$mid4\" and \"($$slice_temp68.size)\" are outside the bounds of the array \"$$slice_temp68\"");
    }

    // Array slice
    ArrayType0 array_slice_1;
    (array_slice_1.data) = (($$slice_temp68.data) + $mid4);
    (array_slice_1.size) = (($$slice_temp68.size) - $mid4);

    // Definition of variable 'part2'
    ArrayType0 $part29 = array_slice_1;

    (&$part29);
    // If statement
    trophy_void if_temp_1;
    if (1) {
        $parallel_mergesort_helper(heap, $part15, 1U);
        $parallel_mergesort_helper(heap, $part29, 1U);
        if_temp_1 = 0U;
    }
    else {
        // Create new region
        Region* $anon_region_12 = 0U;
        jmp_buf jump_buffer_0;
        if (HEDLEY_UNLIKELY((0U != setjmp(jump_buffer_0)))) {
            region_delete($anon_region_12);
            region_panic(heap, "at region $anon_region_12");
        }

        $anon_region_12 = region_create((&jump_buffer_0));

        Region* $async_region14 = $anon_region_12;
        // Pack the lambda environment
        ClosureEnvironment0* closure_environment2 = (ClosureEnvironment0*)region_alloc($async_region14, sizeof(ClosureEnvironment0));
        ((*closure_environment2).$tasks17) = (&$tasks17);
        ((*closure_environment2).$part15) = (&$part15);

        // Lambda expression literal
        ClosureType_2 closure_temp3;
        (closure_temp3.environment) = closure_environment2;
        (closure_temp3.function) = (&$parallel_mergesort_helper$$block1$$block11$$block13$$lambda15);

        region_async($anon_region_12, (closure_temp3.function), (closure_temp3.environment));

        $parallel_mergesort_helper($anon_region_12, $part29, ($tasks17 / 2U));
        region_delete($anon_region_12);
        if_temp_1 = 0U;
    }

    if_temp_1;
    $merge(heap, $arr16, $mid4);
}

void $merge(void* env, ArrayType0 $arr33, trophy_int $mid34) {
    Region* heap = (Region*)env;

    // Fixed array literal
    ArrayType0 fixed_array_0;
    (fixed_array_0.data) = (trophy_int*)region_alloc(heap, (10U * sizeof(trophy_int)));
    (fixed_array_0.size) = 10U;
    memset((fixed_array_0.data), 0U, (10U * sizeof(trophy_int)));

    // Definition of variable 'copy'
    ArrayType0 $copy19 = fixed_array_0;

    (&$copy19);
    // Definition of variable '$for_counter_20'
    trophy_int $$for_counter_2022 = 0U;

    (&$$for_counter_2022);
    // While loop
    while (1U) {
        if ((!($$for_counter_2022 <= ($mid34 - 1U)))) {
            break;
        }

        // Definition of variable 'i'
        trophy_int $i24 = $$for_counter_2022;

        (&$i24);
        // Array access bounds check
        if (HEDLEY_UNLIKELY((($i24 < 0U) | ($i24 >= ($copy19.size))))) {
            region_panic(heap, "Panic! Expression \"$i24\" is outside the bounds of the array \"$copy19\"");
        }

        // Array access bounds check
        if (HEDLEY_UNLIKELY((($i24 < 0U) | ($i24 >= ($arr33.size))))) {
            region_panic(heap, "Panic! Expression \"$i24\" is outside the bounds of the array \"$arr33\"");
        }

        // Variable store
        (*(($copy19.data) + $i24)) = (*(($arr33.data) + $i24));

        // Variable store
        $$for_counter_2022 = ($$for_counter_2022 + 1U);

    }

    // Definition of variable 'i'
    trophy_int $i26 = 0U;

    (&$i26);
    // Definition of variable 'j'
    trophy_int $j27 = $mid34;

    (&$j27);
    // Definition of variable 'k'
    trophy_int $k28 = 0U;

    (&$k28);
    // While loop
    while (1U) {
        if ((!(($i26 < ($copy19.size)) & ($j27 < ($arr33.size))))) {
            break;
        }

        // Array access bounds check
        if (HEDLEY_UNLIKELY((($i26 < 0U) | ($i26 >= ($copy19.size))))) {
            region_panic(heap, "Panic! Expression \"$i26\" is outside the bounds of the array \"$copy19\"");
        }

        // Array access bounds check
        if (HEDLEY_UNLIKELY((($j27 < 0U) | ($j27 >= ($arr33.size))))) {
            region_panic(heap, "Panic! Expression \"$j27\" is outside the bounds of the array \"$arr33\"");
        }

        // If statement
        trophy_void if_temp_2;
        if (((*(($copy19.data) + $i26)) < (*(($arr33.data) + $j27)))) {
            // Array access bounds check
            if (HEDLEY_UNLIKELY((($k28 < 0U) | ($k28 >= ($arr33.size))))) {
                region_panic(heap, "Panic! Expression \"$k28\" is outside the bounds of the array \"$arr33\"");
            }

            // Array access bounds check
            if (HEDLEY_UNLIKELY((($i26 < 0U) | ($i26 >= ($copy19.size))))) {
                region_panic(heap, "Panic! Expression \"$i26\" is outside the bounds of the array \"$copy19\"");
            }

            // Variable store
            (*(($arr33.data) + $k28)) = (*(($copy19.data) + $i26));

            // Variable store
            $i26 = ($i26 + 1U);

            if_temp_2 = 0U;
        }
        else {
            // Array access bounds check
            if (HEDLEY_UNLIKELY((($k28 < 0U) | ($k28 >= ($arr33.size))))) {
                region_panic(heap, "Panic! Expression \"$k28\" is outside the bounds of the array \"$arr33\"");
            }

            // Array access bounds check
            if (HEDLEY_UNLIKELY((($j27 < 0U) | ($j27 >= ($arr33.size))))) {
                region_panic(heap, "Panic! Expression \"$j27\" is outside the bounds of the array \"$arr33\"");
            }

            // Variable store
            (*(($arr33.data) + $k28)) = (*(($arr33.data) + $j27));

            // Variable store
            $j27 = ($j27 + 1U);

            if_temp_2 = 0U;
        }

        if_temp_2;
        // Variable store
        $k28 = ($k28 + 1U);

    }

    // While loop
    while (1U) {
        if ((!($i26 < ($copy19.size)))) {
            break;
        }

        // Array access bounds check
        if (HEDLEY_UNLIKELY((($k28 < 0U) | ($k28 >= ($arr33.size))))) {
            region_panic(heap, "Panic! Expression \"$k28\" is outside the bounds of the array \"$arr33\"");
        }

        // Array access bounds check
        if (HEDLEY_UNLIKELY((($i26 < 0U) | ($i26 >= ($copy19.size))))) {
            region_panic(heap, "Panic! Expression \"$i26\" is outside the bounds of the array \"$copy19\"");
        }

        // Variable store
        (*(($arr33.data) + $k28)) = (*(($copy19.data) + $i26));

        // Variable store
        $i26 = ($i26 + 1U);

        // Variable store
        $k28 = ($k28 + 1U);

    }

}

trophy_int $binary_search(void* env, ArrayType0 $arr47, trophy_int $to_find48) {
    Region* heap = (Region*)env;

    // Definition of variable 'start'
    trophy_int $start36 = 0U;

    (&$start36);
    // Definition of variable 'slice'
    ArrayType0 $slice37 = $arr47;

    (&$slice37);
    // While loop
    while (1U) {
        if ((!(($slice37.size) > 0U))) {
            break;
        }

        // Definition of variable 'mid'
        trophy_int $mid39 = (($slice37.size) / 2U);

        (&$mid39);
        // Array access bounds check
        if (HEDLEY_UNLIKELY((($mid39 < 0U) | ($mid39 >= ($slice37.size))))) {
            region_panic(heap, "Panic! Expression \"$mid39\" is outside the bounds of the array \"$slice37\"");
        }

        // Definition of variable 'mid_value'
        trophy_int $mid_value40 = (*(($slice37.data) + $mid39));

        (&$mid_value40);
        // If statement
        trophy_void if_temp_4;
        if (($to_find48 == $mid_value40)) {
            return ($start36 + $mid39);
            if_temp_4 = 0U;
        }
        else {
            // If statement
            trophy_void if_temp_3;
            if (($to_find48 < $mid_value40)) {
                // Array slice bounds check
                if (HEDLEY_UNLIKELY(((0U < 0U) | (($mid39 < 0U) | ($mid39 > ($slice37.size)))))) {
                    region_panic(heap, "Panic! Array slice bounds \"0U\" and \"$mid39\" are outside the bounds of the array \"$slice37\"");
                }

                // Array slice
                ArrayType0 array_slice_2;
                (array_slice_2.data) = (($slice37.data) + 0U);
                (array_slice_2.size) = ($mid39 - 0U);

                // Variable store
                $slice37 = array_slice_2;

                if_temp_3 = 0U;
            }
            else {
                // Definition of variable '$slice_temp44'
                ArrayType0 $$slice_temp4446 = $slice37;

                (&$$slice_temp4446);
                // Array slice bounds check
                if (HEDLEY_UNLIKELY((($mid39 < 0U) | ((($$slice_temp4446.size) < $mid39) | (($$slice_temp4446.size) > ($$slice_temp4446.size)))))) {
                    region_panic(heap, "Panic! Array slice bounds \"$mid39\" and \"($$slice_temp4446.size)\" are outside the bounds of the array \"$$slice_temp4446\"");
                }

                // Array slice
                ArrayType0 array_slice_3;
                (array_slice_3.data) = (($$slice_temp4446.data) + $mid39);
                (array_slice_3.size) = (($$slice_temp4446.size) - $mid39);

                // Variable store
                $slice37 = array_slice_3;

                // Variable store
                $start36 = ($start36 + $mid39);

                if_temp_3 = 0U;
            }

            if_temp_4 = if_temp_3;
        }

    }

    return (0U - 1U);
}

void $selection_sort(void* env, ArrayType0 $arr67) {
    Region* heap = (Region*)env;

    // Definition of variable '$for_counter_50'
    trophy_int $$for_counter_5052 = 0U;

    (&$$for_counter_5052);
    // While loop
    while (1U) {
        if ((!($$for_counter_5052 <= (($arr67.size) - 1U)))) {
            break;
        }

        // Definition of variable 'i'
        trophy_int $i54 = $$for_counter_5052;

        (&$i54);
        // Array access bounds check
        if (HEDLEY_UNLIKELY((($i54 < 0U) | ($i54 >= ($arr67.size))))) {
            region_panic(heap, "Panic! Expression \"$i54\" is outside the bounds of the array \"$arr67\"");
        }

        // Definition of variable 'smallest'
        trophy_int $smallest56 = (*(($arr67.data) + $i54));

        (&$smallest56);
        // Definition of variable 'smallest_index'
        trophy_int $smallest_index57 = $i54;

        (&$smallest_index57);
        // Definition of variable '$for_counter_58'
        trophy_int $$for_counter_5860 = ($i54 + 1U);

        (&$$for_counter_5860);
        // While loop
        while (1U) {
            if ((!($$for_counter_5860 <= (($arr67.size) - 1U)))) {
                break;
            }

            // Definition of variable 'j'
            trophy_int $j62 = $$for_counter_5860;

            (&$j62);
            // Array access bounds check
            if (HEDLEY_UNLIKELY((($j62 < 0U) | ($j62 >= ($arr67.size))))) {
                region_panic(heap, "Panic! Expression \"$j62\" is outside the bounds of the array \"$arr67\"");
            }

            // If statement
            trophy_void if_temp_5;
            if (((*(($arr67.data) + $j62)) < $smallest56)) {
                // Variable store
                $smallest_index57 = $j62;

                // Array access bounds check
                if (HEDLEY_UNLIKELY((($j62 < 0U) | ($j62 >= ($arr67.size))))) {
                    region_panic(heap, "Panic! Expression \"$j62\" is outside the bounds of the array \"$arr67\"");
                }

                // Variable store
                $smallest56 = (*(($arr67.data) + $j62));

                if_temp_5 = 0U;
            }
            else {
                if_temp_5 = 0U;
            }

            if_temp_5;
            // Variable store
            $$for_counter_5860 = ($$for_counter_5860 + 1U);

        }

        // Array access bounds check
        if (HEDLEY_UNLIKELY((($i54 < 0U) | ($i54 >= ($arr67.size))))) {
            region_panic(heap, "Panic! Expression \"$i54\" is outside the bounds of the array \"$arr67\"");
        }

        // Definition of variable 'swap'
        trophy_int $swap66 = (*(($arr67.data) + $i54));

        (&$swap66);
        // Array access bounds check
        if (HEDLEY_UNLIKELY((($i54 < 0U) | ($i54 >= ($arr67.size))))) {
            region_panic(heap, "Panic! Expression \"$i54\" is outside the bounds of the array \"$arr67\"");
        }

        // Variable store
        (*(($arr67.data) + $i54)) = $smallest56;

        // Array access bounds check
        if (HEDLEY_UNLIKELY((($smallest_index57 < 0U) | ($smallest_index57 >= ($arr67.size))))) {
            region_panic(heap, "Panic! Expression \"$smallest_index57\" is outside the bounds of the array \"$arr67\"");
        }

        // Variable store
        (*(($arr67.data) + $smallest_index57)) = $swap66;

        // Variable store
        $$for_counter_5052 = ($$for_counter_5052 + 1U);

    }

}