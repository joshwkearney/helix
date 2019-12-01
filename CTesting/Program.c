
typedef struct Point Point;

typedef struct A A;

typedef struct B B;

int main();

int f(int* x);

struct Point {
	int x;
	int y;
};

struct A {
	B* b;
};

struct B {
	A a;
};

int main() {
	return 0;
}

