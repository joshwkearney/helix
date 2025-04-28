namespace Helix {
    public static class Extensions {
        public static int IndexOf<T>(this IEnumerable<T> list, Func<T, bool> selector) {
            int i = 0;

            foreach (var item in list) { 
                if (selector(item)) {
                    return i;
                }

                i++;
            }

            return -1;
        }

        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> items) {
            foreach (var item in items) {
                queue.Enqueue(item);
            }
        }
    }
}
