using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;
using System;

namespace Unigma
{

    public sealed class QTDoughApplication
    {
        public static QTDoughApplication QTDoughInstance = null;

        QTDoughApplication()
        {
            GetMemoryAddressOfFunctions();
        }

        public static QTDoughApplication Instance
        {
            get
            {
                if (QTDoughInstance == null)
                    QTDoughInstance = new QTDoughApplication();
                return QTDoughInstance;
            }
        }

        //Csharp calls C++ function
        //Initialize and load DLL Library.
        [DllImport("kernel32")]
        public static extern IntPtr LoadLibrary(
            string path);

        static IntPtr OpenLibrary(string path)
        {
            IntPtr handle = LoadLibrary(path);
            if (handle == IntPtr.Zero)
            {
                throw new Exception("Couldn't open native library: " + path);
            }

            return handle;
        }

        static bool CloseLibrary(IntPtr libraryHandle)
        {
            bool result = FreeLibrary(libraryHandle);
            return result;
        }

        //Get the function.
        [DllImport("kernel32")]
        public static extern IntPtr GetProcAddress(
        IntPtr libraryHandle,
            string symbolName);

        //Free the library.
        [DllImport("kernel32")]
        public static extern bool FreeLibrary(
            IntPtr libraryHandle);



        //Functions that are called from the DLL.
        IntPtr symbol;
        public delegate int GetRandomNumberFN();
        public GetRandomNumberFN GetRandomNumber;
        public delegate int GetFooFN();
        public GetFooFN GetFoo;


        //Functions the DLL Calls from Unity / C#.

        delegate int InitFunction(IntPtr CalledFromCSharp);
        static InitFunction Init;
        //static extern int GetSquared(int x);

        //C++ Calls CSHARP
        static int CalledFromCSharp(int a, float b)
        {
            return Mathf.CeilToInt(100 * b * a);
        }

        static int InitializeFunctionPointers()
        {
            // Make a delegate out of the C# function to expose
            Func<int, float, int> del = new Func<int, float, int>(CalledFromCSharp);

            // Get a function pointer for the delegate
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(del);

            // Call C++ and pass the function pointer so it can initialize
            return Init(funcPtr);

        }

        //Initialization of these functions. Find memory mappings.
        void GetMemoryAddressOfFunctions()
        {
            symbol = GetProcAddress(OpenLibrary(Application.streamingAssetsPath + "/UnigmaDLLs/QTDoughDLL.dll"), "getRandomNumber");
            GetRandomNumber = Marshal.GetDelegateForFunctionPointer(symbol, typeof(GetRandomNumberFN)) as GetRandomNumberFN;
            symbol = GetProcAddress(OpenLibrary(Application.streamingAssetsPath + "/UnigmaDLLs/QTDoughDLL.dll"), "Foo");
            GetFoo = Marshal.GetDelegateForFunctionPointer(symbol, typeof(GetFooFN)) as GetFooFN;
            symbol = GetProcAddress(OpenLibrary(Application.streamingAssetsPath + "/UnigmaDLLs/QTDoughDLL.dll"), "Init");
            Init = Marshal.GetDelegateForFunctionPointer(symbol, typeof(InitFunction)) as InitFunction;

            //Memory is set, now initialize.
            InitializeFunctionPointers();
        }

        public void OnApplicationQuit()
        {

            bool result = CloseLibrary(symbol);
            symbol = IntPtr.Zero;
            Debug.Log("Closed DLL is: " + result);

        }
    }
}