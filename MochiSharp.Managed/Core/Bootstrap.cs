using System;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace MochiSharp.Managed.Core
{
    public static class Bootstrap
    {
        private static HostHook? _hostHook;
        private static GameContext? _gameContext;

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

        // Hot Reload: Load the Game Assembly
        [UnmanagedCallersOnly]
        public static int LoadGameAssembly(IntPtr assemblyPathPtr)
        {
            string path = Marshal.PtrToStringUTF8(assemblyPathPtr)!;
            if (_gameContext != null)
            {
                _gameContext.Unload();
                _gameContext = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            try
            {
                _gameContext = new GameContext(path);
                _hostHook?.Log($"Loaded Game Assembly: {path}");
                return 1;
            }
            catch (Exception ex)
            {
                _hostHook?.Log($"Failed to load assembly: {ex.Message}");
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        public static void Update()
        {
            _gameContext?.InvokeUpdate();
        }
    }
}
