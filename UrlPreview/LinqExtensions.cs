using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoSmart.UrlPreview
{
    static class LinqExtensions
    {
        public static T? FirstOrNull<T>(this IEnumerable<T> container, Func<T, bool> predicate) where T : struct
        {
            if (container == null || !container.Any())
            {
                return null;
            }

            var results = container.Where(predicate);
            foreach (var r in results)
            {
                return r;
            }

            return null;
        }
    }
}
