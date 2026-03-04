using System;

namespace WukongMp.Api.Compat;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
internal sealed class NotNullAttribute : Attribute
{
    // empty
}