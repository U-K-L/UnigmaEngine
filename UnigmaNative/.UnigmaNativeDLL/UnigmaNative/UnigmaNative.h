// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the UNIGMANATIVE_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// UNIGMANATIVE_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
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

// This class is exported from the dll
class UNIGMANATIVE_API CUnigmaNative {
public:
	CUnigmaNative(void);
	// TODO: add your methods here.
};

extern UNIGMANATIVE_API int nUnigmaNative;

UNIGMANATIVE_API int fnUnigmaNative(void);

extern "C" {

	DLLEXPORT UNIGMANATIVE_API int GetSquared(int x);

	int finalVal = 15;

	int(*CalledFromCSharp)(int a, int b);
	
	
	DLLEXPORT UNIGMANATIVE_API int Init(int(*calledFromCSharp)(int a, int b))
	{
		CalledFromCSharp = calledFromCSharp;
	}


}
