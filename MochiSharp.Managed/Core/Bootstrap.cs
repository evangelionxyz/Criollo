using System;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace MochiSharp.Managed.Core
{
    public static class Bootstrap
    {
        private static HostHook? _hostHook;
        private static ScriptContext? _scriptContext;

        private static int LoadAssemblyCore(string path)
        {
            if (_scriptContext != null)
            {
                _scriptContext.Unload();
                _scriptContext = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            try
            {
                string fullPath = System.IO.Path.GetFullPath(path);
                _scriptContext = new ScriptContext(fullPath);
                _hostHook?.Log($"Loaded Script Assembly: {fullPath}");
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"Failed to load script assembly: {ex}");
                return 0;
            }
        }

        private static ScriptContext GetContextOrThrow()
        {
            if (_scriptContext == null)
            {
                throw new InvalidOperationException("No ScriptContext loaded. Call LoadAssembly first.");
            }

            return _scriptContext;
        }

        // Structure to hold C++ function pointers (Engine API)
        [StructLayout(LayoutKind.Sequential)]
        public struct EngineInterface
        {
            public IntPtr LogMessage;
        }

        // Entry point called by C++
        // UnmanagedCallersOnly is crucial for preformant revers-P/Invoke
        [UnmanagedCallersOnly]
        public static int Initialize(IntPtr engineArgs)
        {
            var engineApi = Marshal.PtrToStructure<EngineInterface>(engineArgs);

            _hostHook = new HostHook(engineApi);
            _hostHook.Log("C# Managed Core Initialized successfully");

            return 0;
        }

        // Load/Reload a plugin assembly into a collectible context.
        [UnmanagedCallersOnly]
        public static int LoadAssembly(IntPtr assemblyPathPtr)
        {
            string path = Marshal.PtrToStringUTF8(assemblyPathPtr)!;
            return LoadAssemblyCore(path);
        }

        // Create a script instance; returns a positive handle, 0 on error.
        [UnmanagedCallersOnly]
        public static int CreateInstance(IntPtr typeNamePtr)
        {
            try
            {
                string typeName = Marshal.PtrToStringUTF8(typeNamePtr)!;
                int id = GetContextOrThrow().CreateInstance(typeName);
                _hostHook?.Log($"Created instance {id}: {typeName}");
                return id;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"CreateInstance failed: {ex}");
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        public static void DestroyInstance(int instanceId)
        {
            try
            {
                GetContextOrThrow().DestroyInstance(instanceId);
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"DestroyInstance failed: {ex}");
            }
        }

        // Bind an instance method and return a method handle.
        // signature: see ScriptMethodSignature enum.
        [UnmanagedCallersOnly]
        public static int BindInstanceMethod(int instanceId, IntPtr methodNamePtr, int signature)
        {
            try
            {
                string methodName = Marshal.PtrToStringUTF8(methodNamePtr)!;
                var sig = (ScriptMethodSignature)signature;
                int id = GetContextOrThrow().BindInstanceMethod(instanceId, methodName, sig);
                _hostHook?.Log($"Bound instance method {id}: instance {instanceId}.{methodName} ({sig})");
                return id;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"BindInstanceMethod failed: {ex}");
                return 0;
            }
        }

        // Bind a static method and return a method handle.
        [UnmanagedCallersOnly]
        public static int BindStaticMethod(IntPtr typeNamePtr, IntPtr methodNamePtr, int signature)
        {
            try
            {
                string typeName = Marshal.PtrToStringUTF8(typeNamePtr)!;
                string methodName = Marshal.PtrToStringUTF8(methodNamePtr)!;
                var sig = (ScriptMethodSignature)signature;
                int id = GetContextOrThrow().BindStaticMethod(typeName, methodName, sig);
                _hostHook?.Log($"Bound static method {id}: {typeName}.{methodName} ({sig})");
                return id;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"BindStaticMethod failed: {ex}");
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        public static int InvokeVoid(int methodId)
        {
            try
            {
                GetContextOrThrow().InvokeVoid(methodId);
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"InvokeVoid failed: {ex}");
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        public static int InvokeFloat(int methodId, float arg0)
        {
            try
            {
                GetContextOrThrow().InvokeFloat(methodId, arg0);
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"InvokeFloat failed: {ex}");
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        public static int InvokeInt(int methodId, int arg0)
        {
            try
            {
                GetContextOrThrow().InvokeInt(methodId, arg0);
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"InvokeInt failed: {ex}");
                return 0;
            }
        }

        // bool marshaled as int (0/1) for a stable native ABI.
        [UnmanagedCallersOnly]
        public static int InvokeBool(int methodId, int arg0)
        {
            try
            {
                GetContextOrThrow().InvokeBool(methodId, arg0);
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"InvokeBool failed: {ex}");
                return 0;
            }
        }

        // Writes the int result to outResultPtr.
        [UnmanagedCallersOnly]
        public static int InvokeInt2(int methodId, int a, int b, IntPtr outResultPtr)
        {
            try
            {
                int result = GetContextOrThrow().InvokeInt2(methodId, a, b);
                Marshal.WriteInt32(outResultPtr, result);
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"InvokeInt2 failed: {ex}");
                return 0;
            }
        }

        // Reads two Vector3 structs from pointers and writes the result to outResultPtr.
        [UnmanagedCallersOnly]
        public static int InvokeVector3(int methodId, IntPtr aPtr, IntPtr bPtr, IntPtr outResultPtr)
        {
            try
            {
                Vector3 a = Marshal.PtrToStructure<Vector3>(aPtr);
                Vector3 b = Marshal.PtrToStructure<Vector3>(bPtr);
                Vector3 result = GetContextOrThrow().InvokeVector3(methodId, a, b);
                Marshal.StructureToPtr(result, outResultPtr, fDeleteOld: false);
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"InvokeVector3 failed: {ex}");
                return 0;
            }
        }

        // Reads a Transform struct from pointer and invokes a void(Transform) method.
        [UnmanagedCallersOnly]
        public static int InvokeTransformIn(int methodId, IntPtr transformPtr)
        {
            try
            {
                Transform transform = Marshal.PtrToStructure<Transform>(transformPtr);
                GetContextOrThrow().InvokeTransformIn(methodId, transform);
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"InvokeTransformIn failed: {ex}");
                return 0;
            }
        }

        // Invokes a Transform() method and writes the Transform result to outTransformPtr.
        [UnmanagedCallersOnly]
        public static int InvokeTransformOut(int methodId, IntPtr outTransformPtr)
        {
            try
            {
                Transform transform = GetContextOrThrow().InvokeTransformOut(methodId);
                Marshal.StructureToPtr(transform, outTransformPtr, fDeleteOld: false);
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"InvokeTransformOut failed: {ex}");
                return 0;
            }
        }

        // Back-compat: previous API used by older native hosts.
        [UnmanagedCallersOnly]
        public static int LoadGameAssembly(IntPtr assemblyPathPtr)
        {
            string path = Marshal.PtrToStringUTF8(assemblyPathPtr)!;
            return LoadAssemblyCore(path);
        }

        // Back-compat: previously loaded and bound one method in managed.
        [UnmanagedCallersOnly]
        public static int LoadGameAssemblyEx(IntPtr assemblyPathPtr, IntPtr entryTypeNamePtr, IntPtr updateMethodNamePtr)
        {
            string path = Marshal.PtrToStringUTF8(assemblyPathPtr)!;
            _ = Marshal.PtrToStringUTF8(entryTypeNamePtr)!;
            _ = Marshal.PtrToStringUTF8(updateMethodNamePtr)!;
            // New flow: LoadAssembly + CreateInstance + Bind* + Invoke*.
            return LoadAssemblyCore(path);
        }

        [UnmanagedCallersOnly]
        public static void Update()
        {
            // Intentionally empty: the engine should invoke whatever methods it wants
            // using Invoke* APIs. Kept for older hosts that call Update().
        }
    }
}
