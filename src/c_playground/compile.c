#include "include\trophy.h"
#include <stdio.h>

typedef struct ArrayType0 ArrayType0;

typedef struct ClosureEnvironment0 ClosureEnvironment0;

typedef struct ClosureType_2 ClosureType_2;

struct ArrayType0 {
    trophy_int size;
    trophy_int* data;
};

trophy_int $test(void* env);

struct ClosureEnvironment0 {
    ArrayType0* $part14;
};

static void $parallel_mergesort$$block2$$block10$$lambda12(void* environment);

typedef void (*FuncType_1)(void* environment);

struct ClosureType_2 {
    void* environment;
    FuncType_1 function;
};

void $parallel_mergesort(void* env, ArrayType0 $arr13);

trophy_int $binary_search(void* env, ArrayType0 $arr24, trophy_int $to_find25);

void $selection_sort(void* env, ArrayType0 $arr44);

trophy_int $test(void* env) {
    Region* heap = (Region*)env;

    // Array literal on region 'stack'
    ArrayType0 array_0;
    trophy_int array_temp_1[9U];
    (array_0.data) = array_temp_1;
    (array_0.size) = 9U;
    (array_0.data)[0U] = 5U;
    (array_0.data)[1U] = 6U;
    (array_0.data)[2U] = 3U;
    (array_0.data)[3U] = 2U;
    (array_0.data)[4U] = 1U;
    (array_0.data)[5U] = 7U;
    (array_0.data)[6U] = 5U;
    (array_0.data)[7U] = 2U;
    (array_0.data)[8U] = 9U;

    // Definition of variable 'arr'
    ArrayType0 $arr1 = array_0;

    (&$arr1);
    //$selection_sort(heap, $arr1);
    $parallel_mergesort(heap, $arr1);

    for (int i = 0; i < array_0.size; i++) {
        printf("%d ", array_0.data[i]);
    }

    return 0U;
}

static void $parallel_mergesort$$block2$$block10$$lambda12(void* environment) {
    ClosureEnvironment0* environment_temp1 = (ClosureEnvironment0*)environment;

    // Unpack the closure environment
    ArrayType0* $part14 = ((*environment_temp1).$part14);

    // Create new region
    Region* $async_region11 = 0U;
    jmp_buf async_jump_buffer_0;
    if (HEDLEY_UNLIKELY((0U != setjmp(async_jump_buffer_0)))) {
        region_delete($async_region11);
        return;
    }

    $async_region11 = region_create((&async_jump_buffer_0));

    $selection_sort($async_region11, (*$part14));
    region_delete($async_region11);
}

void $parallel_mergesort(void* env, ArrayType0 $arr13) {
    Region* heap = (Region*)env;

    // Definition of variable 'mid'
    trophy_int $mid3 = (($arr13.size) / 2U);

    (&$mid3);
    // Array slice bounds check
    if (HEDLEY_UNLIKELY(((0U < 0U) | (($mid3 <= 0U) | ($mid3 > ($arr13.size)))))) {
        region_panic(heap);
    }

    // Array slice
    ArrayType0 array_slice_0;
    (array_slice_0.data) = (($arr13.data) + 0U);
    (array_slice_0.size) = ($mid3 - 0U);

    // Definition of variable 'part1'
    ArrayType0 $part14 = array_slice_0;

    (&$part14);
    // Definition of variable '$slice_temp5'
    ArrayType0 $$slice_temp57 = $arr13;

    (&$$slice_temp57);
    // Array slice bounds check
    if (HEDLEY_UNLIKELY((($mid3 < 0U) | ((($$slice_temp57.size) <= $mid3) | (($$slice_temp57.size) > ($$slice_temp57.size)))))) {
        region_panic(heap);
    }

    // Array slice
    ArrayType0 array_slice_1;
    (array_slice_1.data) = (($$slice_temp57.data) + $mid3);
    (array_slice_1.size) = (($$slice_temp57.size) - $mid3);

    // Definition of variable 'part2'
    ArrayType0 $part28 = array_slice_1;

    (&$part28);
    // Create new region
    Region* $anon_region_9 = 0U;
    jmp_buf jump_buffer_0;
    if (HEDLEY_UNLIKELY((0U != setjmp(jump_buffer_0)))) {
        region_delete($anon_region_9);
        region_panic(heap);
    }

    $anon_region_9 = region_create((&jump_buffer_0));

    Region* $async_region11 = $anon_region_9;
    // Pack the lambda environment
    ClosureEnvironment0* closure_environment2 = (ClosureEnvironment0*)region_alloc($async_region11, sizeof(ClosureEnvironment0));
    ((*closure_environment2).$part14) = (&$part14);

    // Lambda expression literal
    ClosureType_2 closure_temp3;
    (closure_temp3.environment) = closure_environment2;
    (closure_temp3.function) = (&$parallel_mergesort$$block2$$block10$$lambda12);

    region_async($anon_region_9, (closure_temp3.function), (closure_temp3.environment));

    $selection_sort($anon_region_9, $part28);
    region_delete($anon_region_9);
}

trophy_int $binary_search(void* env, ArrayType0 $arr24, trophy_int $to_find25) {
    Region* heap = (Region*)env;

    // Definition of variable 'mid'
    trophy_int $mid15 = (($arr24.size) / 2U);

    (&$mid15);
    // Array access bounds check
    if (HEDLEY_UNLIKELY((($mid15 < 0U) | ($mid15 >= ($arr24.size))))) {
        region_panic(heap);
    }

    // Definition of variable 'mid_value'
    trophy_int $mid_value16 = (*(($arr24.data) + $mid15));

    (&$mid_value16);
    // While loop
    while (1U) {
        if (HEDLEY_UNLIKELY((!($mid_value16 != $to_find25)))) {
            break;
        }

        // If statement
        trophy_void if_temp_2;
        if (($to_find25 < $mid_value16)) {
            // If statement
            trophy_void if_temp_0;
            if (($mid15 == 0U)) {
                return (0U - 1U);
                if_temp_0 = 0U;
            }
            else {
                if_temp_0 = 0U;
            }

            if_temp_0;
            // Variable store
            $mid15 = ($mid15 / 2U);

            if_temp_2 = 0U;
        }
        else {
            // If statement
            trophy_void if_temp_1;
            if (($mid15 == (($arr24.size) - 1U))) {
                return (0U - 1U);
                if_temp_1 = 0U;
            }
            else {
                if_temp_1 = 0U;
            }

            if_temp_1;
            // Variable store
            $mid15 = ($mid15 + ((($arr24.size) - $mid15) / 2U));

            if_temp_2 = 0U;
        }

        if_temp_2;
        // Array access bounds check
        if (HEDLEY_UNLIKELY((($mid15 < 0U) | ($mid15 >= ($arr24.size))))) {
            region_panic(heap);
        }

        // Variable store
        $mid_value16 = (*(($arr24.data) + $mid15));

    }

    return $mid15;
}

void $selection_sort(void* env, ArrayType0 $arr44) {
    Region* heap = (Region*)env;

    // Definition of variable '$for_counter_27'
    trophy_int $$for_counter_2729 = 0U;

    (&$$for_counter_2729);
    // While loop
    while (1U) {
        if (HEDLEY_UNLIKELY((!($$for_counter_2729 <= (($arr44.size) - 1U))))) {
            break;
        }

        // Definition of variable 'i'
        trophy_int $i31 = $$for_counter_2729;

        (&$i31);
        // Array access bounds check
        if (HEDLEY_UNLIKELY((($i31 < 0U) | ($i31 >= ($arr44.size))))) {
            region_panic(heap);
        }

        // Definition of variable 'smallest'
        trophy_int $smallest33 = (*(($arr44.data) + $i31));

        (&$smallest33);
        // Definition of variable 'smallest_index'
        trophy_int $smallest_index34 = $i31;

        (&$smallest_index34);
        // Definition of variable '$for_counter_35'
        trophy_int $$for_counter_3537 = ($i31 + 1U);

        (&$$for_counter_3537);
        // While loop
        while (1U) {
            if (HEDLEY_UNLIKELY((!($$for_counter_3537 <= (($arr44.size) - 1U))))) {
                break;
            }

            // Definition of variable 'j'
            trophy_int $j39 = $$for_counter_3537;

            (&$j39);
            // Array access bounds check
            if (HEDLEY_UNLIKELY((($j39 < 0U) | ($j39 >= ($arr44.size))))) {
                region_panic(heap);
            }

            // If statement
            trophy_void if_temp_3;
            if (((*(($arr44.data) + $j39)) < $smallest33)) {
                // Variable store
                $smallest_index34 = $j39;

                // Array access bounds check
                if (HEDLEY_UNLIKELY((($j39 < 0U) | ($j39 >= ($arr44.size))))) {
                    region_panic(heap);
                }

                // Variable store
                $smallest33 = (*(($arr44.data) + $j39));

                if_temp_3 = 0U;
            }
            else {
                if_temp_3 = 0U;
            }

            if_temp_3;
            // Variable store
            $$for_counter_3537 = ($$for_counter_3537 + 1U);

        }

        // Array access bounds check
        if (HEDLEY_UNLIKELY((($i31 < 0U) | ($i31 >= ($arr44.size))))) {
            region_panic(heap);
        }

        // Definition of variable 'swap'
        trophy_int $swap43 = (*(($arr44.data) + $i31));

        (&$swap43);
        // Array access bounds check
        if (HEDLEY_UNLIKELY((($i31 < 0U) | ($i31 >= ($arr44.size))))) {
            region_panic(heap);
        }

        // Variable store
        (*(($arr44.data) + $i31)) = $smallest33;

        // Array access bounds check
        if (HEDLEY_UNLIKELY((($smallest_index34 < 0U) | ($smallest_index34 >= ($arr44.size))))) {
            region_panic(heap);
        }

        // Variable store
        (*(($arr44.data) + $smallest_index34)) = $swap43;

        // Variable store
        $$for_counter_2729 = ($$for_counter_2729 + 1U);

    }

}