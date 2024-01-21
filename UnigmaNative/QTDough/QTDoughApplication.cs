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


        //Get the function from the DLL.

        //Convert the function to a delegate.
        //List of functions called from C++
        public delegate int GetRandomNumberFN();
        public GetRandomNumberFN GetRandomNumber;
        IntPtr symbol;


        void GetMemoryAddressOfFunctions()
        {
            symbol = GetProcAddress(OpenLibrary(Application.streamingAssetsPath + "/UnigmaDLLs/QTDoughDLL.dll"), "getRandomNumber");
            GetRandomNumber = Marshal.GetDelegateForFunctionPointer(symbol, typeof(GetRandomNumberFN)) as GetRandomNumberFN;

        }

        public void OnApplicationQuit()
        {

            bool result = CloseLibrary(symbol);
            symbol = IntPtr.Zero;
            Debug.Log("Closed DLL is: " + result);

        }
    }
}