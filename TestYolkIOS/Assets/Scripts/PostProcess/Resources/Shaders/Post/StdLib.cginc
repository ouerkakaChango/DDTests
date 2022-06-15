#ifndef __DD_POST_STDLIB__
#define __DD_POST_STDLIB__

#define PI              3.14159265359
#define HALF_MAX        65504.0 // (2 - 2^-10) * 2^15
#define EPSILON         1.0e-4
#define FLT_MAX         3.402823466e+38 // Maximum representable floating-point number

float3 FastSign(float3 x)
{
    return saturate(x * FLT_MAX + 0.5) * 2.0 - 1.0;
}

half3 Min3(half3 a, half3 b, half3 c)
{
    return min(min(a, b), c);
}

half3 Max3(half3 a, half3 b, half3 c)
{
    return max(max(a, b), c);
}

#endif // __DD_POST_STDLIB__
