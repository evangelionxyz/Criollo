using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace MochiSharp.Managed.Core
{
    public enum ScriptMethodSignature : int
    {
        Void = 0,
        Void_Float = 1,
        Void_Int = 2,
        Void_Bool = 3,

        Int_IntInt = 10,
        Vector3_Vector3Vector3 = 11,
        Void_Transform = 12,
        Transform = 13,
    }

    public sealed class ScriptContext : AssemblyLoadContext
    {
        private readonly string _pluginPath;
        private readonly string _pluginDirectory;
        private readonly Assembly _assembly;
        private readonly AssemblyDependencyResolver _resolver;

        private readonly Dictionary<int, object?> _instances = new();
        private readonly Dictionary<int, BoundMethod> _methods = new();
        private int _nextInstanceId = 1;
        private int _nextMethodId = 1;

        private readonly struct BoundMethod
        {
            public BoundMethod(MethodInfo method, object? target, ScriptMethodSignature signature)
            {
                Method = method;
                Target = target;
                Signature = signature;
            }

            public MethodInfo Method { get; }
            public object? Target { get; }
            public ScriptMethodSignature Signature { get; }
        }

        public ScriptContext(string pluginPath)
            : base(isCollectible: true)
        {
            // Important: native often passes a relative path (e.g. "Example.Managed.dll").
            // Normalize it so dependency probing has a real directory.
            _pluginPath = Path.GetFullPath(pluginPath);
            _pluginDirectory = Path.GetDirectoryName(_pluginPath) ?? AppContext.BaseDirectory;
            _resolver = new AssemblyDependencyResolver(_pluginPath);

            using var fs = new FileStream(_pluginPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _assembly = LoadFromStream(fs);
        }

        public int CreateInstance(string typeName)
        {
            var type = _assembly.GetType(typeName, throwOnError: true, ignoreCase: false)
                ?? throw new TypeLoadException($"Type '{typeName}' not found in '{_pluginPath}'.");

            object? instance = Activator.CreateInstance(type);
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to create instance of '{type.FullName}'. Ensure it has a parameterless constructor.");
            }

            int id = _nextInstanceId++;
            _instances.Add(id, instance);
            return id;
        }

        public void DestroyInstance(int instanceId)
        {
            _instances.Remove(instanceId);
        }

        public int BindInstanceMethod(int instanceId, string methodName, ScriptMethodSignature signature)
        {
            if (!_instances.TryGetValue(instanceId, out object? instance) || instance == null)
            {
                throw new KeyNotFoundException($"Instance '{instanceId}' not found.");
            }

            var type = instance.GetType();
            MethodInfo method = ResolveMethod(type, methodName, signature);

            int id = _nextMethodId++;
            _methods.Add(id, new BoundMethod(method, method.IsStatic ? null : instance, signature));
            return id;
        }

        public int BindStaticMethod(string typeName, string methodName, ScriptMethodSignature signature)
        {
            var type = _assembly.GetType(typeName, throwOnError: true, ignoreCase: false)
                ?? throw new TypeLoadException($"Type '{typeName}' not found in '{_pluginPath}'.");

            MethodInfo method = ResolveMethod(type, methodName, signature);
            if (!method.IsStatic)
            {
                throw new InvalidOperationException($"Method '{type.FullName}.{methodName}' must be static for BindStaticMethod.");
            }

            int id = _nextMethodId++;
            _methods.Add(id, new BoundMethod(method, null, signature));
            return id;
        }

        public void UnbindMethod(int methodId)
        {
            _methods.Remove(methodId);
        }

        public void InvokeVoid(int methodId)
        {
            var bound = GetBoundMethod(methodId, ScriptMethodSignature.Void);
            bound.Method.Invoke(bound.Target, null);
        }

        public void InvokeFloat(int methodId, float arg0)
        {
            var bound = GetBoundMethod(methodId, ScriptMethodSignature.Void_Float);
            bound.Method.Invoke(bound.Target, new object?[] { arg0 });
        }

        public void InvokeInt(int methodId, int arg0)
        {
            var bound = GetBoundMethod(methodId, ScriptMethodSignature.Void_Int);
            bound.Method.Invoke(bound.Target, new object?[] { arg0 });
        }

        public void InvokeBool(int methodId, int arg0AsInt)
        {
            var bound = GetBoundMethod(methodId, ScriptMethodSignature.Void_Bool);
            bool value = arg0AsInt != 0;
            bound.Method.Invoke(bound.Target, new object?[] { value });
        }

        public int InvokeInt2(int methodId, int a, int b)
        {
            var bound = GetBoundMethod(methodId, ScriptMethodSignature.Int_IntInt);
            object? result = bound.Method.Invoke(bound.Target, new object?[] { a, b });
            return result is int value
                ? value
                : throw new InvalidOperationException($"Method '{methodId}' did not return an int.");
        }

        public Vector3 InvokeVector3(int methodId, Vector3 a, Vector3 b)
        {
            var bound = GetBoundMethod(methodId, ScriptMethodSignature.Vector3_Vector3Vector3);
            object? result = bound.Method.Invoke(bound.Target, new object?[] { a, b });
            return result is Vector3 value
                ? value
                : throw new InvalidOperationException($"Method '{methodId}' did not return a Vector3.");
        }

        public void InvokeTransformIn(int methodId, Transform transform)
        {
            var bound = GetBoundMethod(methodId, ScriptMethodSignature.Void_Transform);
            bound.Method.Invoke(bound.Target, new object?[] { transform });
        }

        public Transform InvokeTransformOut(int methodId)
        {
            var bound = GetBoundMethod(methodId, ScriptMethodSignature.Transform);
            object? result = bound.Method.Invoke(bound.Target, null);
            return result is Transform value
                ? value
                : throw new InvalidOperationException($"Method '{methodId}' did not return a Transform.");
        }

        private BoundMethod GetBoundMethod(int methodId, ScriptMethodSignature expected)
        {
            if (!_methods.TryGetValue(methodId, out BoundMethod bound))
            {
                throw new KeyNotFoundException($"Method '{methodId}' not found.");
            }

            if (bound.Signature != expected)
            {
                throw new InvalidOperationException($"Method '{methodId}' was bound as '{bound.Signature}' but invoked as '{expected}'.");
            }

            return bound;
        }

        private static MethodInfo ResolveMethod(Type type, string methodName, ScriptMethodSignature signature)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            (Type[] parameterTypes, Type returnType) = signature switch
            {
                ScriptMethodSignature.Void => (Type.EmptyTypes, typeof(void)),
                ScriptMethodSignature.Void_Float => (new[] { typeof(float) }, typeof(void)),
                ScriptMethodSignature.Void_Int => (new[] { typeof(int) }, typeof(void)),
                ScriptMethodSignature.Void_Bool => (new[] { typeof(bool) }, typeof(void)),

                ScriptMethodSignature.Int_IntInt => (new[] { typeof(int), typeof(int) }, typeof(int)),
                ScriptMethodSignature.Vector3_Vector3Vector3 => (new[] { typeof(Vector3), typeof(Vector3) }, typeof(Vector3)),
                ScriptMethodSignature.Void_Transform => (new[] { typeof(Transform) }, typeof(void)),
                ScriptMethodSignature.Transform => (Type.EmptyTypes, typeof(Transform)),

                _ => throw new ArgumentOutOfRangeException(nameof(signature), signature, "Unsupported signature"),
            };

            var method = type.GetMethod(methodName, flags, binder: null, types: parameterTypes, modifiers: null);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, methodName);
            }

            if (method.ReturnType != returnType)
            {
                throw new InvalidOperationException($"Method '{type.FullName}.{methodName}' must return '{returnType.Name}'.");
            }

            return method;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName.Name))
            {
                return null!;
            }

            // Always share MochiSharp.Managed with scripts.
            // If scripts end up loading a second copy of MochiSharp.Managed into the collectible context,
            // types like Vector3/Transform become non-identical and reflection binding/invocation breaks.
            var coreName = typeof(ScriptContext).Assembly.GetName().Name;
            if (coreName != null && string.Equals(coreName, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
            {
                return typeof(ScriptContext).Assembly;
            }

            // Prefer already-loaded assemblies from the default context.
            // This fixes cases like Example.Managed referencing MochiSharp.Managed
            // (which is already loaded as the host's managed core).
            foreach (var asm in AssemblyLoadContext.Default.Assemblies)
            {
                var name = asm.GetName().Name;
                if (name != null && string.Equals(name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return asm;
                }
            }

            // Use deps.json-based resolution when available.
            string? resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (!string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath))
            {
                using var resolvedStream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return LoadFromStream(resolvedStream);
            }

            // Fallback: probe next to the plugin DLL, then the host base directory.
            string candidatePath = Path.Combine(_pluginDirectory, assemblyName.Name + ".dll");
            if (!File.Exists(candidatePath))
            {
                candidatePath = Path.Combine(AppContext.BaseDirectory, assemblyName.Name + ".dll");
                if (!File.Exists(candidatePath))
                {
                    return null!;
                }
            }

            using var candidateStream = new FileStream(candidatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return LoadFromStream(candidateStream);
        }
    }
}
