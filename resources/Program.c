function_start:
    array_malloc    | let $2 = malloc(7 * &word)
    array_store     | 4 -> $2[0]
    array_store     | 5 -> $2[1]
    array_store     | 5 -> $2[2]
    array_store     | 3 -> $2[3]
    array_store     | 4 -> $2[4]
    array_store     | 5 -> $2[5]
    array_store     | 8 -> $2[6]
    local_create    | var x
    local_assign    | x = $2
    return          | return void

