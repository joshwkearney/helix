using System;
using System.Collections.Immutable;
using Attempt17.Types;

namespace Attempt17.Features.Containers.Unions {
    public class UnionDeclarationSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public CompositeInfo UnionInfo { get; }

        public ImmutableList<FunctionSignature> Methods { get; }

        public
        ImmutableDictionary<(Parameter, FunctionSignature), FunctionInfo> ParameterMethods
            { get; }

        public UnionDeclarationSyntax(T tag, CompositeInfo info,
            ImmutableList<FunctionSignature> methods,
            ImmutableDictionary<(Parameter, FunctionSignature), FunctionInfo> parMethods) {

            this.Tag = tag;
            this.UnionInfo = info;
            this.Methods = methods;
            this.ParameterMethods = parMethods;
        }

        public T1 Accept<T1, TContext>(ISyntaxVisitor<T1, T, TContext> visitor, TContext context) {
            return visitor
                .ContainersVisitor
                .UnionVisitor
                .VisitUnionDeclaration(this, visitor, context);
        }
    }
}