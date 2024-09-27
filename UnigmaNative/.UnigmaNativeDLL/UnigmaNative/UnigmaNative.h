// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the UNIGMANATIVE_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// UNIGMANATIVE_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#pragma once
#ifndef C_UNIGMA_NATIVE_H
#define C_UNIGMA_NATIVE_H

#ifdef UNIGMANATIVE_EXPORTS
#define UNIGMANATIVE_API __declspec(dllexport)
#else
#define UNIGMANATIVE_API __declspec(dllimport)
#endif

#ifdef _WIN32
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT 
#endif

#include "Vector.h"
#include <mutex> 

// This class is exported from the dll
class UNIGMANATIVE_API CUnigmaNative {
public:
	CUnigmaNative(void);
	// TODO: add your methods here.
};

extern UNIGMANATIVE_API int nUnigmaNative;

extern bool programRunning;

UNIGMANATIVE_API int fnUnigmaNative(void);

extern "C" {


	extern DLLEXPORT UNIGMANATIVE_API Vector3 GetSquared(void* x);

	UNIGMANATIVE_API void EndProgram();


	/*
	extern int finalVal = 15;

	extern int(*CalledFromCSharp)(int a, int b);
	
	
	extern DLLEXPORT UNIGMANATIVE_API int Init(int(*calledFromCSharp)(int a, int b))
	{
		CalledFromCSharp = calledFromCSharp;
		return 3;
	}
	*/
}

#endif  // C_UNIGMA_NATIVE_H