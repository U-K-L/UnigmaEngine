// UnigmaNative.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "UnigmaNative.h"
#include <iostream>


// This is an example of an exported variable
DLLEXPORT UNIGMANATIVE_API int nUnigmaNative=0;

// This is an example of an exported function.
DLLEXPORT UNIGMANATIVE_API int fnUnigmaNative(void)
{
    return 0;
}

DLLEXPORT UNIGMANATIVE_API int GetSquared(int x)
{
    int val = 0;//CalledFromCSharp(1, 1);
    return x * x + finalVal + val;
}

// This is the constructor of a class that has been exported.
CUnigmaNative::CUnigmaNative()
{
    return;
}

