using System;
using System.Collections.Generic;

namespace MochiSharp.Managed.Core
{
    /// <summary>
    /// Represents a type interface in the MochiSharp type system.
    /// </summary>
    public interface TypeInterface
    {
        /// <summary>
        /// Gets the name of the type interface.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Gets the methods defined in the type interface.
        /// </summary>
        IReadOnlyList<MethodSignature> Methods { get; }
    }
    /// <summary>
    /// Represents a method signature in a type interface.
    /// </summary>
    public class MethodSignature
    {
        /// <summary>
        /// Gets the name of the method.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the return type of the method.
        /// </summary>
        public Type ReturnType { get; }
        /// <summary>
        /// Gets the parameters of the method.
        /// </summary>
        public IReadOnlyList<Parameter> Parameters { get; }
        public MethodSignature(string name, Type returnType, IReadOnlyList<Parameter> parameters)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
        }
    }
    /// <summary>
    /// Represents a parameter in a method signature.
    /// </summary>
    public class Parameter
    {
        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        public Type Type { get; }
        public Parameter(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}