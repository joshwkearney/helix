using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        public ISyntaxB CheckNames(INamesRecorder names) {
            return new TypeAccessSyntaxB(this.Location, this.type);
        }

        public IOption<ITrophyType> ResolveToType(INamesRecorder names) {
            // If our type is a named type, make sure it points to something
            if (this.type.AsNamedType().TryGetValue(out var path)) {
                var target = NameTarget.Reserved;
                var totalPath = path;

                if (path.Segments.Count == 1) {
                    if (!names.TryFindName(path.Segments.First(), out target, out totalPath)) {
                        return Option.None<ITrophyType>();
                    }
                }
                else {
                    if (!names.TryGetName(path, out target)) {
                        return Option.None<ITrophyType>();
                    }
                }

                if (target != NameTarget.Struct && target != NameTarget.Union && target != NameTarget.Function) {
                    return Option.None<ITrophyType>();
                }

                return Option.Some(new NamedType(totalPath));
            }

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

        public ISyntaxC CheckTypes(ITypesRecorder types) {
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