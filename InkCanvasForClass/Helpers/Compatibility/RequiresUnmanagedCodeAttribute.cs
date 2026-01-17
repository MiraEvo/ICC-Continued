using System;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Interface, Inherited = false)]
    internal sealed class RequiresUnmanagedCodeAttribute : Attribute
    {
        public RequiresUnmanagedCodeAttribute() { }

        public RequiresUnmanagedCodeAttribute(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
