using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnigmaEngine
{
    public class UnigmaNativeManager : MonoBehaviour
    {
        //Native Functions
        unsafe delegate void EndProgram();
        EndProgram endProgramFunc;
        IntPtr symbol;

        private void Awake()
        {
            GetMemoryAddressOfDLL();

            //Get Functions...
            GetFunctions();
        }
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

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

        //Get DLL
        public static IntPtr libraryHandle;

        void GetMemoryAddressOfDLL()
        {
            libraryHandle = OpenLibrary(Application.streamingAssetsPath + "/UnigmaDLLs/UnigmaNative.dll");
        }

        void GetFunctions()
        {
            endProgramFunc = GetNativeFunction<EndProgram>(ref symbol, "EndProgram");
        }

        public static unsafe T GetNativeFunction<T>(ref IntPtr symbol, string funcName) where T : Delegate
        {
            if (libraryHandle == null)
                return default;
            symbol = GetProcAddress(libraryHandle, funcName);
            return Marshal.GetDelegateForFunctionPointer<T>(symbol);
        }

        void OnApplicationQuit()
        {
            endProgramFunc();
            UnigmaPhysicsManager.wakePhysicsThread();
            bool result = CloseLibrary(libraryHandle);
            libraryHandle = IntPtr.Zero;
            symbol = IntPtr.Zero;
            Debug.Log("Closed DLL is: " + result);

        }
    }
}
