function_start:
    local_create    | var $2 as Point
    member_assign   | $2.x = 4
    member_assign   | $2.y = 8
    local_create    | var a as StructType { Members = System.Collections.Generic.List`1[Helix.Types.StructMember] }
    local_assign    | a = $2
    return          | return void

