using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Features.Meta {
    public class VarRefTypeSyntaxA : ISyntaxA {
        private readonly ISyntaxA innerTypeSyntax;
        private readonly bool isReadonly;

        public TokenLocation Location { get; }

        public VarRefTypeSyntaxA(TokenLocation loc, ISyntaxA innerTypeSyntax, bool isReadonly) {
            this.Location = loc;
            this.innerTypeSyntax = innerTypeSyntax;
            this.isReadonly = isReadonly;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var elemType = this.innerTypeSyntax.CheckNames(names);

            return new VarRefTypeSyntaxB(this.Location, elemType, this.isReadonly);
        }

        public IOption<ITrophyType> ResolveToType(INamesRecorder names) {
            return this.innerTypeSyntax.ResolveToType(names).Select(x => new VarRefType(x, this.isReadonly));
        }
    }

    public class VarRefTypeSyntaxB : ISyntaxB {
        private readonly ISyntaxB elemTypeSyntax;
        private readonly bool isReadonly;

        public TokenLocation Location { get; }

        public VarRefTypeSyntaxB(TokenLocation loc, ISyntaxB elemType, bool isReadonly) {
            this.Location = loc;
            this.elemTypeSyntax = elemType;
            this.isReadonly = isReadonly;
        }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage => this.elemTypeSyntax.VariableUsage;

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var check = this.elemTypeSyntax.CheckTypes(types);
            var returnOp = check.ReturnType
                .AsMetaType()
                .Select(x => x.PayloadType)
                .Select(x => new VarRefType(x, this.isReadonly))
                .Select(x => new MetaType(x));

            if (!returnOp.TryGetValue(out var returnType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.elemTypeSyntax.Location);
            }

            return new TypeSyntaxC(returnType, check.Lifetimes);
        }
    }
}