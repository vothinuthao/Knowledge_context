using System;

namespace VikingRaven.Core.DI
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class InjectAttribute : Attribute
    {
    }
}