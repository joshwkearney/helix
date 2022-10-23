#if __cplusplus
extern "C" {
#endif

typedef unsigned int _helix_bool;
typedef unsigned int _helix_void;
typedef unsigned int _helix_int;
_helix_int* loops(_helix_int p, _helix_int* z);

_helix_int* loops(_helix_int p, _helix_int* z) {
    _helix_int x = 45U;
    _helix_int* w = z;

    /* Line 7: If statement */
    if ((p > 100U)) { 
        w = &x;
    } 

    return w;
}

#if __cplusplus
}
#endif
