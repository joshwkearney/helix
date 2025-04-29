function_start:
    local_create    | var $1
    binary_op       | let $2 = x < 15
    local_create    | var $3
    jump_cond       | goto if_true or if_false if $2

if_true:
    local_assign    | $3 = 89
    jump            | goto if_after

if_false:
    local_assign    | $3 = 15
    jump            | goto if_after

if_after:
    local_assign    | $1 = $3
    return          | return $1

