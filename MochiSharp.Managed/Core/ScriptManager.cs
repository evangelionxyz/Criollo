using System;
using System.Reflection;

namespace MochiSharp.Managed.Core;

/// <summary>
/// Manages script assembly loading for hot-reload scenarios.
/// Called from C++ to explicitly load/unload/reload game scripts.
/// </summary>
public static class ScriptManager
{
    private static ScriptAssemblyManager? _manager;
    private static Assembly? _currentAssembly;

    /// <summary>
    /// Load a script assembly in an isolated context.
    /// </summary>
    public static bool LoadScripts(string assemblyPath)
    {
        try
        {
            Debug.Log($"[ScriptManager] Loading scripts from: {assemblyPath}");
            
            _manager = new ScriptAssemblyManager();
            _currentAssembly = _manager.LoadScriptAssembly(assemblyPath);
            
            if (_currentAssembly != null)
            {
                Debug.Log($"[ScriptManager] Successfully loaded: {_currentAssembly.FullName}");
                Debug.Log($"[ScriptManager] Assembly types count: {_currentAssembly.GetTypes().Length}");
                
                // Log available types for debugging
                foreach (var type in _currentAssembly.GetTypes())
                {
                    if (type.IsPublic && !type.IsAbstract)
                    {
                        Debug.Log($"[ScriptManager] Available type: {type.FullName}");
                    }
                }
                
                return true;
            }
            
            Debug.LogError("[ScriptManager] Failed to load script assembly");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ScriptManager] Exception loading scripts: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Unload the current script assembly.
    /// </summary>
    public static void UnloadScripts()
    {
        try
        {
            Debug.Log("[ScriptManager] Unloading scripts...");
            
            _currentAssembly = null;
            _manager?.UnloadScriptAssembly();
            _manager = null;
            
            // Force GC to clean up
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            Debug.Log("[ScriptManager] Scripts unloaded");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ScriptManager] Exception unloading scripts: {ex.Message}");
        }
    }

    /// <summary>
    /// Reload the script assembly (unload + load).
    /// </summary>
    public static bool ReloadScripts(string assemblyPath)
    {
        Debug.Log("[ScriptManager] Reloading scripts...");
        UnloadScripts();
        return LoadScripts(assemblyPath);
    }

    /// <summary>
    /// Check if scripts are currently loaded.
    /// </summary>
    public static bool IsLoaded()
    {
        return _manager?.IsLoaded ?? false;
    }

    /// <summary>
    /// Get the currently loaded script assembly.
    /// </summary>
    public static Assembly? GetCurrentAssembly()
    {
        return _currentAssembly;
    }
    
    /// <summary>
    /// Find a type by name in the currently loaded script assembly.
    /// </summary>
    public static Type? FindType(string typeName)
    {
        if (_currentAssembly == null)
        {
            Debug.LogWarning($"[ScriptManager] No assembly loaded, cannot find type: {typeName}");
            return null;
        }
        
        // Try exact match first
        var type = _currentAssembly.GetType(typeName);
        if (type != null)
        {
            return type;
        }
        
        // Try searching all types (in case namespace is missing)
        foreach (var t in _currentAssembly.GetTypes())
        {
            if (t.Name == typeName || t.FullName == typeName)
            {
                return t;
            }
        }
        
        Debug.LogWarning($"[ScriptManager] Type '{typeName}' not found in loaded assembly");
        return null;
    }
}