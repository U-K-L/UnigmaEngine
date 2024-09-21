// UnigmaNative.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "UnigmaNative.h"
#include <iostream>


// This is an example of an exported variable
UNIGMANATIVE_API int nUnigmaNative=0;

// This is an example of an exported function.
UNIGMANATIVE_API int fnUnigmaNative(void)
{
    return 0;
}

UNIGMANATIVE_API int GetSquared(int x)
{

    return x * x + finalVal;
}

// This is the constructor of a class that has been exported.
CUnigmaNative::CUnigmaNative()
{
    return;
}

