using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Meta {
    public class TypeAccessSyntaxA : ISyntaxA {
        private readonly ITrophyType type;

        public TokenLocation Location { get; }

        public TypeAccessSyntaxA(TokenLocation loc, ITrophyType type) {
            this.type = type;
            this.Location = loc;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            return new TypeAccessSyntaxB(this.Location, this.type);
        }

        public IOption<ITrophyType> ResolveToType(INameRecorder names) {
            return Option.Some(this.type);
        }
    }

    public class TypeAccessSyntaxB : ISyntaxB {
        private readonly ITrophyType type;

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage => ImmutableDictionary.Create<IdentifierPath, VariableUsageKind>();

        public TypeAccessSyntaxB(TokenLocation loc, ITrophyType type) {
            this.Location = loc;
            this.type = type;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            return new TypeAccessSyntaxC(new MetaType(this.type));
        }
    }

    public class TypeAccessSyntaxC : ISyntaxC {
        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public TypeAccessSyntaxC(ITrophyType returnType) {
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }
    }
}