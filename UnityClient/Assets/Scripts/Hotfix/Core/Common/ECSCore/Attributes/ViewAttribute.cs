using ECSCore;
using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ViewAttribute : ContextAttribute
{
    public ViewAttribute() : base("View")
    {
    }
}
