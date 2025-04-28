function_start:
    local_create    | var $1
    local_create    | var sum
    local_assign    | sum = 0
    local_create    | var i
    local_assign    | i = 1
    jump            | goto loop

loop:
    local_assign    | sum = 1
    jump            | goto loop_after

loop_after:
    local_assign    | $1 = sum
    jump            | goto function_end

function_end:
    return          | return $1

