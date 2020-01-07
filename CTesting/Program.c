typedef short main;

typedef short some;

typedef short a;

typedef short b;

short $func$main();

long long $func$some(long long x);

b $func$a();

a $func$b();

short $func$main() {
    long long foo = 1LL;

    long long $invoke_result_0 = $func$some(5LL);

    long long test = $invoke_result_0;

    test = (3LL + 2LL);

    return 0;
}

long long $func$some(long long x) {
    long long $if_result_0;

    if (x) {
        $if_result_0 = 4LL;
    }
    else {
        $if_result_0 = 2LL;
    }

    return $if_result_0;
}

b $func$a() {
    return 0;
}

a $func$b() {
    return 0;
}


