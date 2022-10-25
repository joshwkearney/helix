using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Primitives;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree AssignmentStatement() {
            var start = this.TopExpression();

            if (this.TryAdvance(TokenKind.Assignment)) {
                var assign = this.TopExpression();
                var loc = start.Location.Span(assign.Location);
                var result = new AssignmentParseStatement(loc, start, assign);

                return result;
            }
            else {
                BinaryOperationKind op;

                if (this.TryAdvance(TokenKind.PlusAssignment)) {
                    op = BinaryOperationKind.Add;
                }
                else if (this.TryAdvance(TokenKind.MinusAssignment)) {
                    op = BinaryOperationKind.Subtract;
                }
                else if (this.TryAdvance(TokenKind.StarAssignment)) {
                    op = BinaryOperationKind.Multiply;
                }
                else if (this.TryAdvance(TokenKind.DivideAssignment)) {
                    op = BinaryOperationKind.FloorDivide;
                }
                else if (this.TryAdvance(TokenKind.ModuloAssignment)) {
                    op = BinaryOperationKind.Modulo;
                }
                else {
                    return start;
                }

                var second = this.TopExpression();
                var loc = start.Location.Span(second.Location);
                var assign = new BinarySyntax(loc, start, second, op);
                var stat = new AssignmentParseStatement(loc, start, assign);

                return stat;
            }
        }
    }
}

namespace Helix.Features.Variables {
    public record AssignmentParseStatement : ISyntaxTree {
        private readonly ISyntaxTree target, assign;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target, this.assign };

        public bool IsPure => false;

        public AssignmentParseStatement(TokenLocation loc, ISyntaxTree target, ISyntaxTree assign) {
            this.Location = loc;
            this.target = target;
            this.assign = assign;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var target = this.target.CheckTypes(types).ToLValue(types);
            var targetType = types.ReturnTypes[target];

            var assign = this.assign
                .CheckTypes(types)
                .ToRValue(types)
                .UnifyTo(targetType, types);

            // TODO: Fix all of this
            // Check to see if the assigned value has the same origins
            // (or more restricted origins) than the target expression.
            // If the origins are compatible, we can assign with no further
            // issue. If they are different, then we need to insert a runtime
            // check. If the lifetimes do not have runtime values, then we
            // need to throw an error

            var targetLifetimes = types.Lifetimes[target];
            var assignLifetimes = types.Lifetimes[assign];

            // TODO: Insert compile time lifetime check
            // TODO: Insert runtime lifetime check if possible

            //if (!targetLifetime.HasCompatibleRoots(assignLifetime, types)) {
            //    throw new LifetimeException(
            //        this.Location,
            //        "Unsafe Memory Store",
            //        $"Unable to verify that the assigned value outlives its container. " + 
            //        "Please declare this function as 'pooling' to check variable " + 
            //        "lifetimes at runtime or wrap this assignment in an unsafe block.");
            //}

            var newLifetimes = new Dictionary<VariableSignature, bool>();

            var isLocalMutation = targetLifetimes.Count == 1
                && types.Variables.TryGetValue(targetLifetimes[0].Path, out var localSig)
                && targetType == localSig.Type;

            // Increment the mutation counter for modified variables so that
            // any new accesses to this variable will be forced to get the new 
            // lifetime.
            if (isLocalMutation) {
                foreach (var lifetime in targetLifetimes) {
                    if (!types.Variables.TryGetValue(lifetime.Path, out var sig)) {
                        continue;
                    }

                    var newLifetime = new Lifetime(sig.Path, sig.MutationCount + 1, lifetime.IsRoot);

                    var newSig = new VariableSignature(
                        sig.Path,
                        sig.Type,
                        sig.IsWritable,
                        sig.MutationCount + 1,
                        sig.IsLifetimeRoot);

                    // Replace the old variable signature
                    types.Variables[lifetime.Path] = newSig;

                    // We need to generate a variable for this new lifetime in the c
                    newLifetimes.Add(newSig, lifetime.IsRoot);

                    // Add this to the running list of availible lifetimes
                    types.AvailibleLifetimes.Add(newLifetime);
                }
           }

            // Add a dependency between every variable in the assignment statement and
            // the old lifetime
            foreach (var assignTime in assignLifetimes) {
                foreach (var targetTime in targetLifetimes) {
                    types.AddDependency(assignTime, targetTime);
                }
            }

            var result = new AssignmentStatement(this.Location, target, assign, newLifetimes);
            types.ReturnTypes[result] = PrimitiveType.Void;
            types.Lifetimes[result] = Array.Empty<Lifetime>();

            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }

        public record AssignmentStatement : ISyntaxTree {
            private readonly ISyntaxTree target, assign;
            private readonly IReadOnlyDictionary<VariableSignature, bool> newLifetimes;

            public TokenLocation Location { get; }

            public IEnumerable<ISyntaxTree> Children => new[] { this.target, this.assign };

            public bool IsPure => false;

            public AssignmentStatement(TokenLocation loc, ISyntaxTree target,
                                       ISyntaxTree assign, 
                                       IReadOnlyDictionary<VariableSignature, bool> newLifetimes) {
                this.Location = loc;
                this.target = target;
                this.assign = assign;
                this.newLifetimes = newLifetimes;
            }

            public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

            public ISyntaxTree ToRValue(SyntaxFrame types) => this;

            public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
                var target = this.target.GenerateCode(types, writer);
                var assign = this.assign.GenerateCode(types, writer);

                writer.WriteEmptyLine();
                writer.WriteComment($"Line {this.Location.Line}: Assignment statement");

                writer.WriteStatement(new CAssignment() {
                    Left = target,
                    Right = assign
                });

                // Write a variable for any new mutation lifetimes this assignment created
                foreach (var (sig, isRoot) in this.newLifetimes) {
                    var lifetime = new Lifetime(sig.Path, sig.MutationCount, isRoot);

                    writer.RegisterLifetime(lifetime, new CMemberAccess() {
                        Target = assign,
                        MemberName = "pool"
                    });
                }

                writer.WriteEmptyLine();

                return new CIntLiteral(0);
            }
        }
    }
}