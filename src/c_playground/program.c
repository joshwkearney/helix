#include "include/trophy.h"

typedef union UnionType_0 UnionType_0;

typedef struct $Token $Token;

typedef struct $Lexer $Lexer;

typedef struct ArrayType0 ArrayType0;

typedef union UnionType_1 UnionType_1;

typedef struct $BinaryOperator $BinaryOperator;

typedef union UnionType_2 UnionType_2;

typedef struct $ParseTree $ParseTree;

typedef struct $BinarySyntax $BinarySyntax;

typedef union UnionType_3 UnionType_3;

typedef struct $ParseResult $ParseResult;

union UnionType_0 {
    trophy_void eof;
    trophy_void plus;
    trophy_void minus;
    trophy_void multiply;
    trophy_void open_paren;
    trophy_void close_paren;
    trophy_int int_literal;
};

struct $Token {
    int tag;
    UnionType_0 data;
};

struct ArrayType0 {
    trophy_int size;
    trophy_int* data;
};

struct $Lexer {
    trophy_int* pos;
    $Token* next_tok;
    ArrayType0 chars;
};

$Token $Lexer$next(void* env, $Lexer* $lex5);

$Token $Lexer$peek(void* env, $Lexer* $lex16);

union UnionType_1 {
    trophy_void op_multiply;
    trophy_void op_add;
    trophy_void op_subtract;
};

struct $BinaryOperator {
    int tag;
    UnionType_1 data;
};

union UnionType_2 {
    trophy_int int_literal;
    $BinarySyntax* binary_expr;
};

struct $ParseTree {
    int tag;
    UnionType_2 data;
};

struct $BinarySyntax {
    $ParseTree left;
    $ParseTree right;
    $BinaryOperator op;
};

union UnionType_3 {
    trophy_void error;
    $ParseTree success;
};

struct $ParseResult {
    int tag;
    UnionType_3 data;
};

void $run(void* env);

trophy_int $eval(void* env, $ParseTree $tree34);

$ParseResult $parse(void* env, ArrayType0 $input39);

$ParseResult $add_expr(void* env, $Lexer* $lex53);

$ParseResult $mult_expr(void* env, $Lexer* $lex64);

$ParseResult $atom(void* env, $Lexer* $lex74);

$Token $Lexer$next(void* env, $Lexer* $lex5) {
    Region* heap = (Region*)env;

    // Is Expression
    $Token $is_temp_0 = (*((*$lex5).next_tok));

    // If statement
    trophy_void if_temp_0;
    if ((($is_temp_0.tag) == 0U)) {
        // Definition of variable '$member_invoke_temp0'
        $Lexer $$member_invoke_temp00 = (*$lex5);

        (&$$member_invoke_temp00);
        $Lexer$peek(heap, (&$$member_invoke_temp00));
        if_temp_0 = 0U;
    }
    else {
        if_temp_0 = 0U;
    }

    if_temp_0;
    // Definition of variable 'result'
    $Token $result4 = (*((*$lex5).next_tok));

    (&$result4);
    // New union literal for 'Token'
    $Token new_union_0;
    (new_union_0.tag) = 0U;
    ((new_union_0.data).eof) = 0U;

    // Variable store
    (*((*$lex5).next_tok)) = new_union_0;

    return $result4;
}

$Token $Lexer$peek(void* env, $Lexer* $lex16) {
    Region* heap = (Region*)env;

    // Is Expression
    $Token $is_temp_1 = (*((*$lex16).next_tok));

    // If statement
    trophy_void if_temp_1;
    if ((1U ^ (($is_temp_1.tag) == 0U))) {
        return (*((*$lex16).next_tok));
        if_temp_1 = 0U;
    }
    else {
        if_temp_1 = 0U;
    }

    if_temp_1;
    // If statement
    trophy_void if_temp_2;
    if (((*((*$lex16).pos)) >= (((*$lex16).chars).size))) {
        // New union literal for 'Token'
        $Token new_union_1;
        (new_union_1.tag) = 0U;
        ((new_union_1.data).eof) = 0U;

        return new_union_1;
        if_temp_2 = 0U;
    }
    else {
        if_temp_2 = 0U;
    }

    if_temp_2;
    // Array access bounds check
    if (HEDLEY_UNLIKELY((((*((*$lex16).pos)) < 0U) | ((*((*$lex16).pos)) >= (((*$lex16).chars).size))))) {
        region_panic(heap, "Panic! Expression \"(*((*$lex16).pos))\" is outside the bounds of the array \"((*$lex16).chars)\"");
    }

    // Definition of variable 'c'
    trophy_int $c12 = (*((((*$lex16).chars).data) + (*((*$lex16).pos))));

    (&$c12);
    // If statement
    $Token if_temp_8;
    if (($c12 == 40U)) {
        // New union literal for 'Token'
        $Token new_union_2;
        (new_union_2.tag) = 4U;
        ((new_union_2.data).open_paren) = 0U;

        if_temp_8 = new_union_2;
    }
    else {
        // If statement
        $Token if_temp_7;
        if (($c12 == 41U)) {
            // New union literal for 'Token'
            $Token new_union_3;
            (new_union_3.tag) = 5U;
            ((new_union_3.data).close_paren) = 0U;

            if_temp_7 = new_union_3;
        }
        else {
            // If statement
            $Token if_temp_6;
            if (($c12 == 43U)) {
                // New union literal for 'Token'
                $Token new_union_4;
                (new_union_4.tag) = 1U;
                ((new_union_4.data).plus) = 0U;

                if_temp_6 = new_union_4;
            }
            else {
                // If statement
                $Token if_temp_5;
                if (($c12 == 45U)) {
                    // New union literal for 'Token'
                    $Token new_union_5;
                    (new_union_5.tag) = 2U;
                    ((new_union_5.data).minus) = 0U;

                    if_temp_5 = new_union_5;
                }
                else {
                    // If statement
                    $Token if_temp_4;
                    if (($c12 == 42U)) {
                        // New union literal for 'Token'
                        $Token new_union_6;
                        (new_union_6.tag) = 3U;
                        ((new_union_6.data).multiply) = 0U;

                        if_temp_4 = new_union_6;
                    }
                    else {
                        // If statement
                        $Token if_temp_3;
                        if ((($c12 >= 48U) & ($c12 <= 57U))) {
                            // Definition of variable 'value'
                            trophy_int $value14 = 0U;

                            (&$value14);
                            // While loop
                            while (1U) {
                                if ((!(($c12 >= 48U) & ($c12 <= 57U)))) {
                                    break;
                                }

                                // Variable store
                                $value14 = (($value14 * 10U) + ($c12 - 48U));

                                // Variable store
                                (*((*$lex16).pos)) = ((*((*$lex16).pos)) + 1U);

                                // Array access bounds check
                                if (HEDLEY_UNLIKELY((((*((*$lex16).pos)) < 0U) | ((*((*$lex16).pos)) >= (((*$lex16).chars).size))))) {
                                    region_panic(heap, "Panic! Expression \"(*((*$lex16).pos))\" is outside the bounds of the array \"((*$lex16).chars)\"");
                                }

                                // Variable store
                                $c12 = (*((((*$lex16).chars).data) + (*((*$lex16).pos))));

                            }

                            // Variable store
                            (*((*$lex16).pos)) = ((*((*$lex16).pos)) - 1U);

                            // New union literal for 'Token'
                            $Token new_union_7;
                            (new_union_7.tag) = 6U;
                            ((new_union_7.data).int_literal) = $value14;

                            if_temp_3 = new_union_7;
                        }
                        else {
                            // New union literal for 'Token'
                            $Token new_union_8;
                            (new_union_8.tag) = 0U;
                            ((new_union_8.data).eof) = 0U;

                            if_temp_3 = new_union_8;
                        }

                        if_temp_4 = if_temp_3;
                    }

                    if_temp_5 = if_temp_4;
                }

                if_temp_6 = if_temp_5;
            }

            if_temp_7 = if_temp_6;
        }

        if_temp_8 = if_temp_7;
    }

    // Variable store
    (*((*$lex16).next_tok)) = if_temp_8;

    // Variable store
    (*((*$lex16).pos)) = ((*((*$lex16).pos)) + 1U);

    return (*((*$lex16).next_tok));
}

void $run(void* env) {
    Region* heap = (Region*)env;

    // Array literal on region 'stack'
    ArrayType0 array_0;
    trophy_int array_temp_1[4U];
    (array_0.data) = array_temp_1;
    (array_0.size) = 4U;
    (array_0.data)[0U] = 50U;
    (array_0.data)[1U] = 43U;
    (array_0.data)[2U] = 52U;
    (array_0.data)[3U] = 0U;

    // Definition of variable 'input'
    ArrayType0 $input18 = array_0;

    (&$input18);
    // Create new region
    Region* $anon_region_19 = 0U;
    jmp_buf jump_buffer_0;
    if (HEDLEY_UNLIKELY((0U != setjmp(jump_buffer_0)))) {
        region_delete($anon_region_19);
        region_panic(heap, "at region $anon_region_19");
    }

    $anon_region_19 = region_create((&jump_buffer_0));

    // New union literal for 'ParseTree'
    $ParseTree new_union_9;
    (new_union_9.tag) = 0U;
    ((new_union_9.data).int_literal) = 0U;

    // Is Expression
    $ParseResult $is_temp_2 = $parse($anon_region_19, $input18);
    $ParseTree $tree21 = new_union_9;
    if ((($is_temp_2.tag) == 1U)) {
        $tree21 = (($is_temp_2.data).success);
    }

    // If statement
    trophy_void if_temp_9;
    if ((($is_temp_2.tag) == 1U)) {
        // Definition of variable 'result'
        trophy_int $result24 = $eval($anon_region_19, $tree21);

        (&$result24);
        if_temp_9 = 0U;
    }
    else {
        if_temp_9 = 0U;
    }

    region_delete($anon_region_19);
    if_temp_9;
}

trophy_int $eval(void* env, $ParseTree $tree34) {
    Region* heap = (Region*)env;

    $ParseTree $switch_arg_temp_0 = $tree34;
    trophy_int $switch_temp_1;
    switch (($switch_arg_temp_0.tag)) {
    case 0U: {
        trophy_int $i26 = (($switch_arg_temp_0.data).int_literal);

        $switch_temp_1 = $i26;
        break;
    }
    case 1U: {
        $BinarySyntax* $b33 = (($switch_arg_temp_0.data).binary_expr);

        // Definition of variable 'left'
        trophy_int $left28 = $eval(heap, ((*$b33).left));

        (&$left28);
        // Definition of variable 'right'
        trophy_int $right29 = $eval(heap, ((*$b33).right));

        (&$right29);
        $BinaryOperator $switch_arg_temp_2 = ((*$b33).op);
        trophy_int $switch_temp_3;
        switch (($switch_arg_temp_2.tag)) {
        case 1U: {
            $switch_temp_3 = ($left28 + $right29);
            break;
        }
        case 2U: {
            $switch_temp_3 = ($left28 - $right29);
            break;
        }
        case 0U: {
            $switch_temp_3 = ($left28 * $right29);
            break;
        }
        }

        $switch_temp_1 = $switch_temp_3;
        break;
    }
    }

    return $switch_temp_1;
}

$ParseResult $parse(void* env, ArrayType0 $input39) {
    Region* heap = (Region*)env;

    // Definition of variable 'pos'
    trophy_int $pos36 = 0U;

    (&$pos36);
    // New union literal for 'Token'
    $Token new_union_10;
    (new_union_10.tag) = 0U;
    ((new_union_10.data).eof) = 0U;

    // Definition of variable 'next_tok'
    $Token $next_tok37 = new_union_10;

    (&$next_tok37);
    // New struct literal for 'Lexer'
    $Lexer new_struct_0;
    (new_struct_0.pos) = (&$pos36);
    (new_struct_0.next_tok) = (&$next_tok37);
    (new_struct_0.chars) = $input39;

    // Definition of variable 'lex'
    $Lexer $lex38 = new_struct_0;

    (&$lex38);
    return $add_expr(heap, (&$lex38));
}

$ParseResult $add_expr(void* env, $Lexer* $lex53) {
    Region* heap = (Region*)env;

    // New union literal for 'ParseTree'
    $ParseTree new_union_12;
    (new_union_12.tag) = 0U;
    ((new_union_12.data).int_literal) = 0U;

    // Is Expression
    $ParseResult $is_temp_3 = $mult_expr(heap, $lex53);
    $ParseTree $first41 = new_union_12;
    if ((($is_temp_3.tag) == 1U)) {
        $first41 = (($is_temp_3.data).success);
    }

    // If statement
    trophy_void if_temp_10;
    if ((1U ^ (($is_temp_3.tag) == 1U))) {
        // New union literal for 'ParseResult'
        $ParseResult new_union_13;
        (new_union_13.tag) = 0U;
        ((new_union_13.data).error) = 0U;

        return new_union_13;
        if_temp_10 = 0U;
    }
    else {
        if_temp_10 = 0U;
    }

    if_temp_10;
    // While loop
    while (1U) {
        // Definition of variable '$member_invoke_temp1'
        $Lexer $$member_invoke_temp11 = (*$lex53);

        (&$$member_invoke_temp11);
        // Is Expression
        $Token $is_temp_4 = $Lexer$peek(heap, (&$$member_invoke_temp11));

        // Definition of variable '$member_invoke_temp2'
        $Lexer $$member_invoke_temp22 = (*$lex53);

        (&$$member_invoke_temp22);
        // Is Expression
        $Token $is_temp_5 = $Lexer$peek(heap, (&$$member_invoke_temp22));

        if ((!((($is_temp_4.tag) == 1U) | (($is_temp_5.tag) == 2U)))) {
            break;
        }

        // Definition of variable '$member_invoke_temp3'
        $Lexer $$member_invoke_temp33 = (*$lex53);

        (&$$member_invoke_temp33);
        $Token $switch_arg_temp_4 = $Lexer$next(heap, (&$$member_invoke_temp33));
        $BinaryOperator $switch_temp_5;
        switch (($switch_arg_temp_4.tag)) {
        case 1U: {
            // New union literal for 'BinaryOperator'
            $BinaryOperator new_union_14;
            (new_union_14.tag) = 1U;
            ((new_union_14.data).op_add) = 0U;

            $switch_temp_5 = new_union_14;
            break;
        }
        default: {
            // New union literal for 'BinaryOperator'
            $BinaryOperator new_union_15;
            (new_union_15.tag) = 2U;
            ((new_union_15.data).op_subtract) = 0U;

            $switch_temp_5 = new_union_15;
            break;
        }
        }

        // Definition of variable 'op'
        $BinaryOperator $op48 = $switch_temp_5;

        (&$op48);
        // New union literal for 'ParseTree'
        $ParseTree new_union_16;
        (new_union_16.tag) = 0U;
        ((new_union_16.data).int_literal) = 0U;

        // Is Expression
        $ParseResult $is_temp_6 = $mult_expr(heap, $lex53);
        $ParseTree $next49 = new_union_16;
        if ((($is_temp_6.tag) == 1U)) {
            $next49 = (($is_temp_6.data).success);
        }

        // If statement
        trophy_void if_temp_11;
        if ((1U ^ (($is_temp_6.tag) == 1U))) {
            // New union literal for 'ParseResult'
            $ParseResult new_union_17;
            (new_union_17.tag) = 0U;
            ((new_union_17.data).error) = 0U;

            return new_union_17;
            if_temp_11 = 0U;
        }
        else {
            if_temp_11 = 0U;
        }

        if_temp_11;
        // New struct literal for 'BinarySyntax'
        $BinarySyntax new_struct_1;
        (new_struct_1.left) = $first41;
        (new_struct_1.right) = $next49;
        (new_struct_1.op) = $op48;

        // Definition of variable 'node'
        $BinarySyntax* $node52 = region_alloc(heap, sizeof($BinarySyntax));
        (*$node52) = new_struct_1;

        (&$node52);
        // New union literal for 'ParseTree'
        $ParseTree new_union_18;
        (new_union_18.tag) = 1U;
        ((new_union_18.data).binary_expr) = $node52;

        // Variable store
        $first41 = new_union_18;

    }

    // New union literal for 'ParseResult'
    $ParseResult new_union_11;
    (new_union_11.tag) = 1U;
    ((new_union_11.data).success) = $first41;

    return new_union_11;
}

$ParseResult $mult_expr(void* env, $Lexer* $lex64) {
    Region* heap = (Region*)env;

    // New union literal for 'ParseTree'
    $ParseTree new_union_20;
    (new_union_20.tag) = 0U;
    ((new_union_20.data).int_literal) = 0U;

    // Is Expression
    $ParseResult $is_temp_7 = $atom(heap, $lex64);
    $ParseTree $first55 = new_union_20;
    if ((($is_temp_7.tag) == 1U)) {
        $first55 = (($is_temp_7.data).success);
    }

    // If statement
    trophy_void if_temp_12;
    if ((1U ^ (($is_temp_7.tag) == 1U))) {
        // New union literal for 'ParseResult'
        $ParseResult new_union_21;
        (new_union_21.tag) = 0U;
        ((new_union_21.data).error) = 0U;

        return new_union_21;
        if_temp_12 = 0U;
    }
    else {
        if_temp_12 = 0U;
    }

    if_temp_12;
    // While loop
    while (1U) {
        // Definition of variable '$member_invoke_temp4'
        $Lexer $$member_invoke_temp44 = (*$lex64);

        (&$$member_invoke_temp44);
        // Is Expression
        $Token $is_temp_8 = $Lexer$peek(heap, (&$$member_invoke_temp44));

        if ((!(($is_temp_8.tag) == 3U))) {
            break;
        }

        // Definition of variable '$member_invoke_temp5'
        $Lexer $$member_invoke_temp55 = (*$lex64);

        (&$$member_invoke_temp55);
        $Lexer$next(heap, (&$$member_invoke_temp55));
        // New union literal for 'ParseTree'
        $ParseTree new_union_22;
        (new_union_22.tag) = 0U;
        ((new_union_22.data).int_literal) = 0U;

        // Is Expression
        $ParseResult $is_temp_9 = $atom(heap, $lex64);
        $ParseTree $next60 = new_union_22;
        if ((($is_temp_9.tag) == 1U)) {
            $next60 = (($is_temp_9.data).success);
        }

        // If statement
        trophy_void if_temp_13;
        if ((1U ^ (($is_temp_9.tag) == 1U))) {
            // New union literal for 'ParseResult'
            $ParseResult new_union_23;
            (new_union_23.tag) = 0U;
            ((new_union_23.data).error) = 0U;

            return new_union_23;
            if_temp_13 = 0U;
        }
        else {
            if_temp_13 = 0U;
        }

        if_temp_13;
        // New union literal for 'BinaryOperator'
        $BinaryOperator new_union_24;
        (new_union_24.tag) = 0U;
        ((new_union_24.data).op_multiply) = 0U;

        // New struct literal for 'BinarySyntax'
        $BinarySyntax new_struct_2;
        (new_struct_2.left) = $first55;
        (new_struct_2.right) = $next60;
        (new_struct_2.op) = new_union_24;

        // Definition of variable 'node'
        $BinarySyntax* $node63 = region_alloc(heap, sizeof($BinarySyntax));
        (*$node63) = new_struct_2;

        (&$node63);
        // New union literal for 'ParseTree'
        $ParseTree new_union_25;
        (new_union_25.tag) = 1U;
        ((new_union_25.data).binary_expr) = $node63;

        // Variable store
        $first55 = new_union_25;

    }

    // New union literal for 'ParseResult'
    $ParseResult new_union_19;
    (new_union_19.tag) = 1U;
    ((new_union_19.data).success) = $first55;

    return new_union_19;
}

$ParseResult $atom(void* env, $Lexer* $lex74) {
    Region* heap = (Region*)env;

    // Definition of variable '$member_invoke_temp6'
    $Lexer $$member_invoke_temp66 = (*$lex74);

    (&$$member_invoke_temp66);
    // Is Expression
    $Token $is_temp_10 = $Lexer$peek(heap, (&$$member_invoke_temp66));

    // If statement
    $ParseResult if_temp_16;
    if ((($is_temp_10.tag) == 4U)) {
        // Definition of variable '$member_invoke_temp7'
        $Lexer $$member_invoke_temp77 = (*$lex74);

        (&$$member_invoke_temp77);
        $Lexer$next(heap, (&$$member_invoke_temp77));
        // Definition of variable 'result'
        $ParseResult $result68 = $add_expr(heap, $lex74);

        (&$result68);
        // Definition of variable '$member_invoke_temp8'
        $Lexer $$member_invoke_temp88 = (*$lex74);

        (&$$member_invoke_temp88);
        // Is Expression
        $Token $is_temp_11 = $Lexer$next(heap, (&$$member_invoke_temp88));

        // If statement
        trophy_void if_temp_14;
        if ((1U ^ (($is_temp_11.tag) == 5U))) {
            // New union literal for 'ParseResult'
            $ParseResult new_union_26;
            (new_union_26.tag) = 0U;
            ((new_union_26.data).error) = 0U;

            return new_union_26;
            if_temp_14 = 0U;
        }
        else {
            if_temp_14 = 0U;
        }

        if_temp_14;
        if_temp_16 = $result68;
    }
    else {
        // Definition of variable '$member_invoke_temp9'
        $Lexer $$member_invoke_temp99 = (*$lex74);

        (&$$member_invoke_temp99);
        // Is Expression
        $Token $is_temp_12 = $Lexer$peek(heap, (&$$member_invoke_temp99));
        trophy_int $i72 = 0U;
        if ((($is_temp_12.tag) == 6U)) {
            $i72 = (($is_temp_12.data).int_literal);
        }

        // If statement
        $ParseTree if_temp_15;
        if ((($is_temp_12.tag) == 6U)) {
            // Definition of variable '$member_invoke_temp10'
            $Lexer $$member_invoke_temp1010 = (*$lex74);

            (&$$member_invoke_temp1010);
            $Lexer$next(heap, (&$$member_invoke_temp1010));
            // New union literal for 'ParseTree'
            $ParseTree new_union_28;
            (new_union_28.tag) = 0U;
            ((new_union_28.data).int_literal) = $i72;

            if_temp_15 = new_union_28;
        }
        else {
            // New union literal for 'ParseTree'
            $ParseTree new_union_29;
            (new_union_29.tag) = 0U;
            ((new_union_29.data).int_literal) = 0U;

            if_temp_15 = new_union_29;
        }

        // New union literal for 'ParseResult'
        $ParseResult new_union_27;
        (new_union_27.tag) = 1U;
        ((new_union_27.data).success) = if_temp_15;

        if_temp_16 = new_union_27;
    }

    return if_temp_16;
}