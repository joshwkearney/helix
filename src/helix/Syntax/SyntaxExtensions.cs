namespace Helix.Syntax {
    public static class SyntaxExtensions {
        public static IEnumerable<ISyntaxTree> GetAllChildren(this ISyntaxTree syntax) {
            var stack = new Queue<ISyntaxTree>(syntax.Children);

            while (stack.Count > 0) {
                var item = stack.Dequeue();

                foreach (var child in item.Children) {
                    stack.Enqueue(child);
                }

                yield return item;
            }
        }
    }
}
