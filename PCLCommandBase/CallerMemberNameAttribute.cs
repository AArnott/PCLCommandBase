namespace PCLCommandBase
{
    using System;

    /// <summary>
    /// A Silverlight compatible version of the 
    /// System.Runtime.CompilerServices.CallerMemberNameAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class CallerMemberNameAttribute : Attribute
    {
    }
}
