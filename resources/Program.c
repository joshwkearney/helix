function_start:
    local_create    | var $1
    local_create    | var sum
    local_assign    | sum = 0
    local_create    | var i
    local_assign    | i = 1
    jump            | goto loop

loop:
    binary_op       | let $2 = i > 10
    jump_cond       | goto loop_after if $2 else goto if_after

if_after:
    binary_op       | let $4 = sum + i
    local_assign    | sum = $4
    binary_op       | let $5 = i + 1
    local_assign    | i = $5
    jump            | goto loop

loop_after:
    local_assign    | $1 = sum
    return          | return $1

