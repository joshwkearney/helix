ref_alloc  | var x = allocate(&word)
binary_op  | var %1 = 7 + thing
ref_store  | %1 -> x
alias      | var z = x
ref_load   | var %2 <- x
ref_store  | %2 -> z