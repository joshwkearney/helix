using Helix;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using System;

namespace helix.Syntax {
    public interface ISyntaxTree {
        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children { get; }

        public bool IsPure { get; }

        public Option<HelixType> AsType(EvalFrame types) => Option.None;

        public ISyntaxTree CheckTypes(EvalFrame types);

        // Mixins
        public void AnalyzeFlow(FlowFrame flow) {
            throw new Exception("Compiler bug");
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            throw new Exception("Compiler bug");
        }

        public ISyntaxTree ToRValue(EvalFrame types) {
            throw TypeCheckingErrors.RValueRequired(Location);
        }

        /// <summary>
        /// An LValue is a special type of syntax tree that is used to represent
        /// a location where values can be stored. LValues return and generate 
        /// pointer types but have lifetimes that match the inner type of the
        /// pointer. This is done so as to not rely on C's lvalue semantics. The
        /// lifetimes of an lvalue represent the region where the memory storage
        /// has been allocated, which any assigned values must outlive
        /// </summary>
        public ISyntaxTree ToLValue(EvalFrame types) {
            throw TypeCheckingErrors.LValueRequired(Location);
        }
    }

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

    public interface IDeclaration {
        public TokenLocation Location { get; }

        public void DeclareNames(EvalFrame names);

        public void DeclareTypes(EvalFrame types);

        public IDeclaration CheckTypes(EvalFrame types);

        public void AnalyzeFlow(FlowFrame flow) {
            throw new InvalidOperationException();
        }

        public void GenerateCode(FlowFrame types, ICWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
