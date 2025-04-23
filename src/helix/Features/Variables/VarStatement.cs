using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Structs;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree VarExpression() {
            var startLok = this.Advance(TokenKind.VarKeyword).Location;
            var names = new List<string>();
            var types = new List<Option<ISyntaxTree>>();

            while (true) {
                var name = this.Advance(TokenKind.Identifier).Value;
                names.Add(name);

                if (this.TryAdvance(TokenKind.AsKeyword)) {
                    types.Add(Option.Some(this.TopExpression()));
                }
                else {
                    types.Add(Option.None);
                }

                if (this.TryAdvance(TokenKind.Assignment)) {
                    break;
                }
                else {
                    this.Advance(TokenKind.Comma);
                }
            }

            var assign = this.TopExpression();
            var loc = startLok.Span(assign.Location);

            return new VarParseStatement(loc, names, types, assign);
        }
    }
}

namespace Helix {
    public record VarParseStatement : ISyntaxTree {
        private readonly IReadOnlyList<string> names; 
        private readonly IReadOnlyList<Option<ISyntaxTree>> types;
        private readonly ISyntaxTree assign;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.assign };

        public bool IsPure => false;

        public VarParseStatement(TokenLocation loc, IReadOnlyList<string> names, 
                                 IReadOnlyList<Option<ISyntaxTree>> types, ISyntaxTree assign) {
            this.Location = loc;
            this.names = names;
            this.assign = assign;
            this.types = types;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            // Type check the assignment value
            var assign = this.assign.CheckTypes(types).ToRValue(types);

            // If this is a compound assignment, check if we have the right
            // number of names and then recurse
            var assignType = assign.GetReturnType(types);
            if (this.names.Count > 1) {
                return this.Destructure(assignType, types);
            }

            // Make sure assign can unify with our type expression
            if (this.types[0].TryGetValue(out var typeSyntax)) {
                if (!typeSyntax.AsType(types).TryGetValue(out var type)) {
                    throw TypeException.ExpectedTypeExpression(typeSyntax.Location);
                }

                assign = assign.UnifyTo(type, types);
                assignType = type;
            }

            // Make sure we're not shadowing anybody
            if (types.TryResolveName(types.Scope, this.names[0], out _)) {
                throw TypeException.IdentifierDefined(this.Location, this.names[0]);
            }

            var path = types.Scope.Append(this.names[0]);
            var varSig = new PointerType(assignType);

            types.NominalSignatures.Add(path, varSig);

            var result = new VarStatement(
                this.Location,
                path,
                assign);

            new SyntaxTagBuilder(types)
                .BuildFor(result);

            return result.CheckTypes(types);
        }
        
        private ISyntaxTree Destructure(HelixType assignType, TypeFrame types) {
            if (!assignType.AsStruct(types).TryGetValue(out var sig)) {
                throw new TypeException(
                    this.Location,
                    "Invalid Desconstruction",
                    $"Cannot deconstruct non-struct type '{assignType}'");
            }

            if (sig.Members.Count != this.names.Count) {
                throw new TypeException(
                    this.Location,
                    "Invalid Desconstruction",
                    "The number of variables provided does not match "
                        + $"the number of members on struct type '{assignType}'");
            }

            var tempName = types.GetVariableName();
            var tempStat = new VarParseStatement(
                this.Location,
                new[] { tempName },
                new Option<ISyntaxTree>[] { Option.None },
                this.assign);

            var stats = new List<ISyntaxTree>() { tempStat };

            for (int i = 0; i < sig.Members.Count; i++) {
                var literal = new VariableAccessParseSyntax(this.Location, tempName);
                var access = new MemberAccessSyntax(this.Location, literal, sig.Members[i].Name, types.Scope);

                var assign = new VarParseStatement(
                    this.Location,
                    new[] { this.names[i] },
                    new[] { this.types[i] },
                    access);

                stats.Add(assign);
            }

            return new CompoundSyntax(this.Location, stats).CheckTypes(types);
        }
    }

    public record VarStatement : ISyntaxTree {
        private readonly ISyntaxTree assignSyntax;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.assignSyntax };

        public bool IsPure => false;

        public VarStatement(TokenLocation loc, IdentifierPath path, ISyntaxTree assign) {
            this.Location = loc;
            this.path = path;
            this.assignSyntax = assign;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            return new NominalType(this.path, NominalTypeKind.Variable);
        }

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame flow, ICStatementWriter writer) {
            throw new NotImplementedException();
        }

        private void GenerateStackAllocation(ICSyntax assign, TypeFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.path);
            var assignType = this.assignSyntax.GetReturnType(types);
            var cReturnType = writer.ConvertType(assignType, types);

            var stat = new CVariableDeclaration() {
                Type = cReturnType,
                Name = name,
                Assignment = Option.Some(assign)
            };

            writer.WriteStatement(stat);
            writer.WriteEmptyLine();
            writer.VariableKinds[this.path] = CVariableKind.Local;
        }

        private void GenerateRegionAllocation(ICSyntax assign, ICSyntax allocLifetime,
                                              TypeFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.path);
            var assignType = this.assignSyntax.GetReturnType(types);
            var cReturnType = writer.ConvertType(assignType, types);

            writer.WriteStatement(new CVariableDeclaration() {
                Name = name,
                Type = new CPointerType(cReturnType),
                Assignment = new CRegionAllocExpression() {
                    Type = cReturnType,
                    Lifetime = allocLifetime
                }
            });

            var assignmentDecl = new CAssignment() {
                Left = new CPointerDereference() {
                    Target = new CVariableLiteral(name)
                },
                Right = assign
            };

            writer.WriteStatement(assignmentDecl);
            writer.WriteEmptyLine();
            writer.VariableKinds[this.path] = CVariableKind.Allocated;
        }
    }
}