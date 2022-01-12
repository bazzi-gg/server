using System.Collections.Generic;

namespace Server.Extensions
{

    public static class ListExtension
    {
        public static void AddRangeAndLimit<T>(this List<T> list, IEnumerable<T> collection, int limit)
        {
            foreach (T value in collection)
            {
                if (limit <= list.Count)
                {
                    return;
                }
                list.Add(value);
            }
        }

    }
}
