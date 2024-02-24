func test(...) as ... {
    var %0 = !b;
    var x = %0;
    var %1;
    if b then {
        %1 = 1;
    }
    else {
        var %2;
        if x then {
            %2 = 2;
        }
        else {
            %2 = 3;
        };
        %1 = %2;
    };
    return %1;
};