function_start:
    local_create    | var $2 as Point
    member_assign   | $2.x = 1
    member_assign   | $2.y = 2
    local_create    | var p as Point
    local_assign    | p = $2
    return          | return void

