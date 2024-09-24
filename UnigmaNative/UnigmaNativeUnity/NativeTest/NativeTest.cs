using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using Unity.Collections;
using static UnigmaEngine.UnigmaSpaceTime;
using Unity.Collections.LowLevel.Unsafe;

public class NativeTest : MonoBehaviour
{

    struct float3
    {
        public float x;
        public float y;
        public float z;

        public float3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    NativeArray<float3> vecs;

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
    public IntPtr libraryHandle;

    //Get the function from the DLL.

    //Convert the function to a delegate.
    unsafe delegate Vector3 GetSquaredFunction(void* x);
    GetSquaredFunction GetSquared;
    IntPtr symbol;
    IntPtr initSymbol;

    delegate int InitFunction(IntPtr calledFromCSharp);
    static InitFunction Init;
    //static extern int GetSquared(int x);

    //C++ Calls CSHARP
    static int CalledFromCSharp(int a, int b)
    {
        Debug.Log("Unigma Native Called from C++");
        return a*b;
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
        vecs = new NativeArray<float3>(4, Allocator.Persistent);
        vecs[2] = new float3(4, 5, 6);
        GetMemoryAddressOfFunctions();
    }

    void GetMemoryAddressOfFunctions()
    {
        libraryHandle = OpenLibrary(Application.streamingAssetsPath + "/UnigmaDLLs/UnigmaNative.dll");
        symbol = GetProcAddress(libraryHandle, "GetSquared");
        GetSquared = Marshal.GetDelegateForFunctionPointer(symbol, typeof(GetSquaredFunction)) as GetSquaredFunction;
        initSymbol = GetProcAddress(libraryHandle, "Init");
        Init = Marshal.GetDelegateForFunctionPointer(initSymbol, typeof(InitFunction)) as InitFunction;
        //Memory is set, now initialize.
        Debug.Log("Initializer Native DLL: " + InitializeFunctionPointers());
    }

    // Update is called once per frame
    void Update()
    {
        DebugStructs();
    }

    unsafe void DebugStructs()
    {

        void* ptr = NativeArrayUnsafeUtility.GetUnsafePtr(vecs);
        Debug.Log("Vector is: " + GetSquared(ptr).z + " | Size of Vector3 is " + sizeof(float3));
    }

    void OnApplicationQuit()
    {
        
        bool result = CloseLibrary(libraryHandle);
        libraryHandle = IntPtr.Zero;
        Debug.Log("Closed DLL is: " + result);
        
    }
}
