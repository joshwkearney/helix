#include "program.h"

int main() {
	int x = 4;
	int* ptr = &x;



    Point _temp_structinit_0;
    _temp_structinit_0.x = 0;
    _temp_structinit_0.y = 1;
    
    Point _temp_call_0 = f(_temp_structinit_0);
    
    Point ret = _temp_call_0;
    
    return (ret.x);
}

Point f(Point p) {
    return p;
}

