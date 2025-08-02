using System;

namespace Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ReadOnlyAttribute : Attribute
    {
    }
}