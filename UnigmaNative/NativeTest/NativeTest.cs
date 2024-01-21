using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class NativeTest : MonoBehaviour
{

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
    delegate int GetSquaredFunction(int x);
    GetSquaredFunction GetSquared;
    IntPtr symbol;

    delegate int InitFunction(IntPtr callFromCPP);
    static InitFunction Init;
    //static extern int GetSquared(int x);

    //C++ Calls CSHARP
    static int CalledFromCSharp(int a, int b)
    {
        return 100;
    }

    static int InitializeFunctionPointers()
    {
        // Make a delegate out of the C# function to expose
        Func<int, int, int> del = new Func<int, int, int>(CalledFromCSharp);

        // Get a function pointer for the delegate
        IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(del);

        // Call C++ and pass the function pointer so it can initialize
        return Init(funcPtr);

    }
    
    // Start is called before the first frame update
    void Start()
    {
        GetMemoryAddressOfFunctions();
    }

    void GetMemoryAddressOfFunctions()
    {
        symbol = GetProcAddress(OpenLibrary(Application.streamingAssetsPath + "/UnigmaDLLs/UnigmaNative.dll"), "GetSquared");
        GetSquared = Marshal.GetDelegateForFunctionPointer(symbol, typeof(GetSquaredFunction)) as GetSquaredFunction;
        Init = Marshal.GetDelegateForFunctionPointer(symbol, typeof(InitFunction)) as InitFunction;

        //Memory is set, now initialize.
        InitializeFunctionPointers();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Coming from native devices: " + GetSquared(100));
    }

    void OnApplicationQuit()
    {
        
        bool result = CloseLibrary(symbol);
        symbol = IntPtr.Zero;
        Debug.Log("Closed DLL is: " + result);
        
    }
}
