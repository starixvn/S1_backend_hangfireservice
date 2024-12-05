using System;
using System.Reflection;
using HangfireService.Core.Attributes;

namespace HangfireService.Core.Extensions;

public static class EnumExtensions
{
     public static bool IsEnabled(this Enum value) {
        FieldInfo field = value.GetType().GetField(value.ToString());
        return Attribute.IsDefined(field, typeof(EnabledAttribute));
     }

     public static string GetName<TEnum>(this int value, string defaultValue = "Unknown") where TEnum: Enum {
         if (Enum.IsDefined(typeof(TEnum), value)) return Enum.GetName(typeof(TEnum), value);
         return defaultValue;
     }
}
