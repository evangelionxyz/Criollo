using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace MochiSharp.Managed.Core;

/// <summary>
/// Custom AssemblyLoadContext for loading script assemblies in isolation.
/// This allows unloading and reloading of script assemblies without affecting the core engine.
/// </summary>
public class ScriptLoadContext : AssemblyLoadContext
{
    private readonly string _assemblyDirectory;

    public ScriptLoadContext(string assemblyPath) : base(isCollectible: true)
    {
        _assemblyDirectory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Allow MochiSharp.Managed and system assemblies to be shared between contexts
        if (assemblyName.Name == "MochiSharp.Managed" ||
            assemblyName.Name?.StartsWith("System") == true ||
            assemblyName.Name?.StartsWith("Microsoft") == true ||
            assemblyName.Name == "netstandard" ||
            assemblyName.Name == "mscorlib")
        {
            return null; // Use default context's version
        }

        // Try to find the assembly in the script directory
        string assemblyPath = Path.Combine(_assemblyDirectory, assemblyName.Name + ".dll");
        if (File.Exists(assemblyPath))
        {
            Debug.Log($"[ScriptLoadContext] Loading dependency: {assemblyName.Name}");
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Let default context handle it
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        // Try to find the unmanaged DLL in the script directory
        string libraryPath = Path.Combine(_assemblyDirectory, unmanagedDllName);
        if (File.Exists(libraryPath))
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}

/// <summary>
/// Manages loading and unloading of script assemblies using AssemblyLoadContext.
/// </summary>
public class ScriptAssemblyManager
{
    private ScriptLoadContext? _loadContext;
    private Assembly? _scriptAssembly;

    /// <summary>
    /// Loads a script assembly into an isolated context.
    /// </summary>
    /// <param name="assemblyPath">Full path to the script assembly DLL</param>
    /// <returns>The loaded assembly, or null if loading failed</returns>
    public Assembly? LoadScriptAssembly(string assemblyPath)
    {
        try
        {
            Debug.Log($"[ScriptAssemblyManager] Loading script assembly from: {assemblyPath}");

            // Validate path
            if (!System.IO.File.Exists(assemblyPath))
            {
                Debug.LogError($"[ScriptAssemblyManager] Assembly file not found: {assemblyPath}");
                return null;
            }

            // Get absolute path
            assemblyPath = System.IO.Path.GetFullPath(assemblyPath);
            Debug.Log($"[ScriptAssemblyManager] Absolute path: {assemblyPath}");

            // Create a new load context for this assembly
            _loadContext = new ScriptLoadContext(assemblyPath);
            _scriptAssembly = _loadContext.LoadFromAssemblyPath(assemblyPath);

            Debug.Log($"[ScriptAssemblyManager] Successfully loaded: {_scriptAssembly.FullName}");
            return _scriptAssembly;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ScriptAssemblyManager] Failed to load script assembly: {ex.Message}");
            Debug.LogError($"[ScriptAssemblyManager] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"[ScriptAssemblyManager] Inner exception: {ex.InnerException.Message}");
            }
            return null;
        }
    }

    /// <summary>
    /// Unloads the currently loaded script assembly and its context.
    /// </summary>
    public void UnloadScriptAssembly()
    {
        if (_loadContext != null)
        {
            Debug.Log("[ScriptAssemblyManager] Unloading script assembly context...");
            
            _scriptAssembly = null;
            
            // Create a weak reference to track when the context is actually collected
            var weakRef = new WeakReference(_loadContext);
            _loadContext.Unload();
            _loadContext = null;

            // Force garbage collection to ensure context is cleaned up
            for (int i = 0; i < 3 && weakRef.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            
            if (weakRef.IsAlive)
            {
                Debug.LogWarning("[ScriptAssemblyManager] Script context may still be alive after unload");
            }
            else
            {
                Debug.Log("[ScriptAssemblyManager] Script assembly context unloaded successfully");
            }
        }
    }

    /// <summary>
    /// Gets the currently loaded script assembly.
    /// </summary>
    public Assembly? ScriptAssembly => _scriptAssembly;

    /// <summary>
    /// Gets whether a script assembly is currently loaded.
    /// </summary>
    public bool IsLoaded => _scriptAssembly != null && _loadContext != null;
}
