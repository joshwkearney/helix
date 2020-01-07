#include "program.h"

int main() {
    Point _temp_structinit_0;
    _temp_structinit_0.x = 4;
    _temp_structinit_0.y = 7;
    
    Point p = _temp_structinit_0;
    
    int _temp_call_0 = sum(p);
    
    int c = _temp_call_0;
    
    char _temp_call_1 = mutate(&c);
    
    return 0;
}

void mutate(int* some) {
    *some = (2 * *some);
    
    return 0;
}

int sum(Point p) {
    return ((p->x) + (p->y));
}

int fib(int n) {
    int a = 0;
    
    int b = 1;
    
    int count = n;
    
    while (count) {
    
        int c = (a + b);
        
        a = b;
        
        b = c;
        
        count = (count - 1);
    }
    
    return count;
}

