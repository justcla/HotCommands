using System.Collections.Generic;

namespace HotCommands
{
    internal static class CSharpExtensions
    {
        internal static bool NotEquals<T1, T2> (this T1 obj1, T2 obj2)
        {
            return !obj1.Equals(obj2);
        }

        internal static string Join (this IEnumerable<string> items, string separator)
        {          
            return string.Join(separator, items);
        }
    }
}
