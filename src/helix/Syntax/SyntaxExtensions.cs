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

        public static DecoratedSyntaxTree Decorate(this ISyntaxTree syntax, IEnumerable<ISyntaxDecorator> decos) {
            if (syntax is DecoratedSyntaxTree decoSyntax) {
                return new DecoratedSyntaxTree(
                    decoSyntax.WrappedSyntax, 
                    decoSyntax.Decorators.Concat(decos));
            }
            else {
                return new DecoratedSyntaxTree(syntax, decos);
            }
        }

        public static DecoratedSyntaxTree Decorate(this ISyntaxTree syntax, ISyntaxDecorator deco) {
            return syntax.Decorate(new[] { deco });
        }
    }
}
