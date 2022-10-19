using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Variables;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Features.Aggregates;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax VarExpression() {
            TokenLocation startLok;
            bool isWritable;

            if (this.Peek(TokenKind.VarKeyword)) {
                startLok = this.Advance(TokenKind.VarKeyword).Location;
                isWritable = true;
            }
            else {
                startLok = this.Advance(TokenKind.LetKeyword).Location;
                isWritable = false;
            }

            var names = new List<string>();

            while (true) {
                var name = this.Advance(TokenKind.Identifier).Value;
                names.Add(name);

                if (this.TryAdvance(TokenKind.Assignment)) {
                    break;
                }
                else { 
                    this.Advance(TokenKind.Comma);
                }
            }

            var assign = this.TopExpression();
            var loc = startLok.Span(assign.Location);

            return new VarParseStatement(loc, names, assign, isWritable);
        }
    }
}

namespace Trophy {
    public record VarParseStatement : ISyntax {
        private readonly IReadOnlyList<string> names;
        private readonly ISyntax assign;
        private readonly bool isWritable;

        public TokenLocation Location { get; }

        public VarParseStatement(TokenLocation loc, IReadOnlyList<string> names, ISyntax assign, bool isWritable) {
            this.Location = loc;
            this.names = names;
            this.assign = assign;
            this.isWritable = isWritable;
        }

        public ISyntax CheckTypes(ITypesRecorder types) {
            // Type check the assignment value
            var assign = this.assign.CheckTypes(types).ToRValue(types);

            if (this.isWritable) {
                assign = assign.WithMutableType(types);
            }

            var assignType = types.GetReturnType(assign);

            // If this is a compound assignment, check if we have the right
            // number of names and then recurse
            if (this.names.Count > 1) {
                return this.Destructure(assignType, types);
            }

            var path = types.CurrentScope.Append(this.names[0]);
            var sig = new VariableSignature(path, assignType, this.isWritable);

            // Declare this variable and make sure we're not shadowing another variable
            if (!types.DeclareVariable(sig)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.names[0]);
            }

            // Put this variable's value in the value table
            types.SetValue(path, assign);

            // Set the return type of the new syntax tree
            var result = new VarStatement(this.Location, sig, assign);
            types.SetReturnType(result, PrimitiveType.Void);
            //types.SetReturnType(result, new PointerType(assignType, this.isWritable));

            return result;
        }

        private ISyntax Destructure(TrophyType assignType, ITypesRecorder types) {
            if (assignType is not NamedType named) {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Desconstruction",
                    $"Cannot deconstruct non-struct type '{ assignType }'");
            }

            if (!types.TryGetAggregate(named.Path).TryGetValue(out var sig)) {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Desconstruction",
                    $"Cannot deconstruct non-struct type '{assignType}'");
            }

            if (sig.Members.Count != this.names.Count) {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Desconstruction",
                    "The number of variables provided does not match " 
                        + $"the number of members on struct type '{named}'");
            }

            var tempName = types.GetVariableName();
            var tempStat = new VarParseStatement(
                this.Location,
                new[] { tempName },
                this.assign,
                false);

            var stats = new List<ISyntax>() { tempStat };

            for (int i = 0; i < sig.Members.Count; i++) {
                var literal = new VariableAccessParseSyntax(this.Location, tempName);
                var access = new MemberAccessSyntax(this.Location, literal, sig.Members[i].Name);
                var assign = new VarParseStatement(
                    this.Location,
                    new[] { this.names[i] },
                    access,
                    this.isWritable);

                stats.Add(assign);
            }

            return new CompoundSyntax(this.Location, stats).CheckTypes(types);
        }

        public ISyntax ToRValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public ISyntax ToLValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record VarStatement : ISyntax {
        private readonly ISyntax assign;
        private readonly VariableSignature signature;

        public TokenLocation Location { get; }

        public VarStatement(TokenLocation loc, VariableSignature sig, ISyntax assign) {
            this.Location = loc;
            this.signature = sig;
            this.assign = assign;
        }

        public ISyntax CheckTypes(ITypesRecorder types) => this;

        public ISyntax ToRValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            var stat = new CVariableDeclaration() {
                Type = writer.ConvertType(this.signature.Type),
                Name = writer.GetVariableName(this.signature.Path),
                Assignment = this.assign.GenerateCode(writer)
            };

            writer.WriteEmptyLine();
            writer.WriteComment("Variable declaration statement");
            writer.WriteStatement(stat);
            writer.WriteEmptyLine();

            return new CIntLiteral(0);
            //return CExpression.AddressOf(CExpression.VariableLiteral(name));
        }
    }
}