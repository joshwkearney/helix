function_start:
    local_create    | var $1
    local_create    | var sum
    local_assign    | sum = 1
    local_create    | var i
    local_assign    | i = 0
    jump            | goto loop

loop:
    binary_op       | let $2 = i + 1
    local_assign    | i = $2
    binary_op       | let $3 = i > 10
    jump_cond       | goto loop_after or if_after if $3

if_after:
    binary_op       | let $4 = sum + i
    local_assign    | sum = $4
    jump            | goto loop

loop_after:
    local_assign    | $1 = sum
    return          | return $1

