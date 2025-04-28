function_start:
    local_create    | var $1
    local_create    | var sum
    local_assign    | sum = 0
    local_create    | var i
    local_assign    | i = 0
    local_assign    | i = 1
    local_assign    | sum = 1
    local_assign    | $1 = sum
    return          | return $1

