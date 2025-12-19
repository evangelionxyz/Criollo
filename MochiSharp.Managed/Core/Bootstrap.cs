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

        // Create a script instance with a caller-supplied GUID key.
        // Returns 1 on success, 0 on error.
        [UnmanagedCallersOnly]
        public static int CreateInstanceGuid(IntPtr typeNamePtr, IntPtr instanceGuidPtr)
        {
            try
            {
                string typeName = Marshal.PtrToStringUTF8(typeNamePtr)!;
                string guidText = Marshal.PtrToStringUTF8(instanceGuidPtr)!;
                Guid instanceGuid = Guid.Parse(guidText);

                GetContextOrThrow().CreateInstance(instanceGuid, typeName);
                _hostHook?.Log($"Created instance {instanceGuid}: {typeName}");
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"CreateInstanceGuid failed: {ex}");
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

        [UnmanagedCallersOnly]
        public static void DestroyInstanceGuid(IntPtr instanceGuidPtr)
        {
            try
            {
                string guidText = Marshal.PtrToStringUTF8(instanceGuidPtr)!;
                Guid instanceGuid = Guid.Parse(guidText);
                GetContextOrThrow().DestroyInstance(instanceGuid);
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"DestroyInstanceGuid failed: {ex}");
            }
        }

        // Bind an instance method and return a method handle.
        [UnmanagedCallersOnly]
        public static int BindInstanceMethod(int instanceId, IntPtr methodNamePtr, int signature)
        {
            try
            {
                string methodName = Marshal.PtrToStringUTF8(methodNamePtr)!;
                int id = GetContextOrThrow().BindInstanceMethod(instanceId, methodName, signature);
                _hostHook?.Log($"Bound instance method {id}: instance {instanceId}.{methodName} (sig={signature})");
                return id;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"BindInstanceMethod failed: {ex}");
                return 0;
            }
        }

        // Bind an instance method using a caller-supplied GUID instance key.
        [UnmanagedCallersOnly]
        public static int BindInstanceMethodGuid(IntPtr instanceGuidPtr, IntPtr methodNamePtr, int signature)
        {
            try
            {
                string guidText = Marshal.PtrToStringUTF8(instanceGuidPtr)!;
                Guid instanceGuid = Guid.Parse(guidText);
                string methodName = Marshal.PtrToStringUTF8(methodNamePtr)!;

                int id = GetContextOrThrow().BindInstanceMethod(instanceGuid, methodName, signature);
                _hostHook?.Log($"Bound instance method {id}: instance {instanceGuid}.{methodName} (sig={signature})");
                return id;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"BindInstanceMethodGuid failed: {ex}");
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
                int id = GetContextOrThrow().BindStaticMethod(typeName, methodName, signature);
                _hostHook?.Log($"Bound static method {id}: {typeName}.{methodName} (sig={signature})");
                return id;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"BindStaticMethod failed: {ex}");
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        public static int RegisterSignature(int signatureId, IntPtr returnTypeNamePtr, IntPtr parameterTypeNamePtrs, int parameterCount)
        {
            try
            {
                string returnTypeName = Marshal.PtrToStringUTF8(returnTypeNamePtr)!;
                var paramNames = parameterCount == 0 ? Array.Empty<string>() : new string[parameterCount];
                for (int i = 0; i < paramNames.Length; i++)
                {
                    IntPtr p = Marshal.ReadIntPtr(parameterTypeNamePtrs, i * IntPtr.Size);
                    paramNames[i] = Marshal.PtrToStringUTF8(p)!;
                }

                GetContextOrThrow().RegisterSignature(signatureId, returnTypeName, paramNames);
                _hostHook?.Log($"Registered signature {signatureId}: {returnTypeName}({string.Join(",", paramNames)})");
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"RegisterSignature failed: {ex}");
                return 0;
            }
        }

        // Generic invoke.
        // argsPtr points to an array of IntPtr, each element points to the value for that argument.
        // - int: pointer to int32
        // - float: pointer to float32
        // - bool: pointer to int32 (0/1)
        // - struct: pointer to struct bytes (LayoutKind.Sequential)
        // returnPtr:
        // - void: can be null
        // - int/bool: pointer to int32
        // - float: pointer to float32
        // - struct: pointer to struct bytes
        [UnmanagedCallersOnly]
        public static int Invoke(int methodId, IntPtr argsPtr, int argCount, IntPtr returnPtr)
        {
            try
            {
                GetContextOrThrow().Invoke(methodId, argsPtr, argCount, returnPtr);
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"Invoke failed: {ex}");
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
