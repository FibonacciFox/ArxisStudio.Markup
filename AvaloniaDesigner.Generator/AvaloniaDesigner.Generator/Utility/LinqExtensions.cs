using System;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaDesigner.Generator.Utility // Или .Services, если вы там храните
{
    /// <summary>
    /// Предоставляет метод DistinctBy для совместимости с .NET Standard 2.0.
    /// </summary>
    public static class LinqExtensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
            this IEnumerable<TSource> source, 
            Func<TSource, TKey> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}