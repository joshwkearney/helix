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
        private readonly IOption<int> size;

        public TokenLocation Location { get; }

        public ArrayTypeSyntaxA(TokenLocation loc, ISyntaxA elemType, bool isReadonly) {
            this.Location = loc;
            this.elemTypeSyntax = elemType;
            this.isReadonly = isReadonly;
            this.size = Option.None<int>();
        }

        public ArrayTypeSyntaxA(TokenLocation loc, ISyntaxA elemType, bool isReadonly, int size) {
            this.Location = loc;
            this.elemTypeSyntax = elemType;
            this.isReadonly = isReadonly;
            this.size = Option.Some(size);
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var elemType = this.elemTypeSyntax.CheckNames(names);

            return new ArrayTypeSyntaxB(this.Location, elemType, this.isReadonly, this.size);
        }

        public IOption<ITrophyType> ResolveToType(INamesRecorder names) {
            return this.elemTypeSyntax.ResolveToType(names).Select(x => new ArrayType(x, this.isReadonly));
        }
    }

    public class ArrayTypeSyntaxB : ISyntaxB {
        private readonly ISyntaxB elemTypeSyntax;
        private readonly bool isReadonly;
        private readonly IOption<int> size;

        public TokenLocation Location { get; }

        public ArrayTypeSyntaxB(TokenLocation loc, ISyntaxB elemType, bool isReadonly, IOption<int> size) {
            this.Location = loc;
            this.elemTypeSyntax = elemType;
            this.isReadonly = isReadonly;
            this.size = size;
        }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage => this.elemTypeSyntax.VariableUsage;

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var check = this.elemTypeSyntax.CheckTypes(types);
            var returnOp = check.ReturnType
                .AsMetaType()
                .Select(x => x.PayloadType)
                .Select(x => { 
                    if (this.size.TryGetValue(out var size)) {
                        return (ITrophyType)new FixedArrayType(x, size, this.isReadonly);
                    }
                    else {
                        return new ArrayType(x, this.isReadonly);
                    }
                })
                .Select(x => new MetaType(x));

            if (!returnOp.TryGetValue(out var returnType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.elemTypeSyntax.Location);
            }

            return new TypeSyntaxC(returnType, check.Lifetimes);
        }
    }
}