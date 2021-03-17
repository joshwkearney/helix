using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Features.Meta {
    public class ArrayTypeSyntaxA : ISyntaxA {
        private readonly ISyntaxA elemTypeSyntax;
        private readonly bool isReadonly;

        public TokenLocation Location { get; }

        public ArrayTypeSyntaxA(TokenLocation loc, ISyntaxA elemType, bool isReadonly) {
            this.Location = loc;
            this.elemTypeSyntax = elemType;
            this.isReadonly = isReadonly;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var elemType = this.elemTypeSyntax.CheckNames(names);

            return new ArrayTypeSyntaxB(this.Location, elemType, this.isReadonly);
        }

        public IOption<ITrophyType> ResolveToType(INameRecorder names) {
            return this.elemTypeSyntax.ResolveToType(names).Select(x => new ArrayType(x, this.isReadonly));
        }
    }

    public class ArrayTypeSyntaxB : ISyntaxB {
        private readonly ISyntaxB elemTypeSyntax;
        private readonly bool isReadonly;

        public TokenLocation Location { get; }

        public ArrayTypeSyntaxB(TokenLocation loc, ISyntaxB elemType, bool isReadonly) {
            this.Location = loc;
            this.elemTypeSyntax = elemType;
            this.isReadonly = isReadonly;
        }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage => this.elemTypeSyntax.VariableUsage;

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var check = this.elemTypeSyntax.CheckTypes(types);
            var returnOp = check.ReturnType
                .AsMetaType()
                .Select(x => x.PayloadType)
                .Select(x => new ArrayType(x, this.isReadonly))
                .Select(x => new MetaType(x));

            if (!returnOp.TryGetValue(out var returnType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.elemTypeSyntax.Location);
            }

            return new TypeSyntaxC(returnType, check.Lifetimes);
        }
    }
}