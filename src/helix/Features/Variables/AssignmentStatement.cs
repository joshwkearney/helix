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

            // TODO: Implement the below comment
            // Check to see if the assigned value has the same origins
            // (or more restricted origins) than the target expression.
            // If the origins are compatible, we can assign with no further
            // issue. If they are different, then we need to insert a runtime
            // check. If the lifetimes do not have runtime values, then we
            // need to throw an error

            var targetLifetimes = types.Lifetimes[target];
            var assignLifetimes = types.Lifetimes[assign];

            //if (!targetLifetime.HasCompatibleRoots(assignLifetime, types)) {
            //    throw new LifetimeException(
            //        this.Location,
            //        "Unsafe Memory Store",
            //        $"Unable to verify that the assigned value outlives its container. " + 
            //        "Please declare this function as 'pooling' to check variable " + 
            //        "lifetimes at runtime or wrap this assignment in an unsafe block.");
            //}

            // There are two possible behaviors here depending on if we are writing into
            // a local variable or writing into a remote
            // memory location. Both use the assignment syntax but have different side
            // effects when it comes to lifetimes. Writing into a remote memory location
            // does not effect any lifetimes, except that the assignment lifetimes are now
            // dependent on the target lifetimes. However, overriding a local pointer or
            // array variable replaces the current lifetime for that variable, so we need to
            // increment the mutation counter and create the new lifetime. 

            // Increment the mutation counter for modified local variables so that
            // any new accesses to this variable will be forced to get the new 
            // lifetime.
            var newLifetimes = new ValueList<VariableSignature>();

            if (target.IsLocal) {
                var lifetime = targetLifetimes[0];
                var sig = types.Variables[lifetime.Path];

                var newLifetime = new Lifetime(sig.Path, sig.Lifetime.MutationCount + 1);

                var newSig = new VariableSignature(
                    sig.Type,
                    sig.IsWritable,
                    newLifetime);

                // Replace the old variable signature
                types.Variables[lifetime.Path] = newSig;

                // We need to generate a variable for this new lifetime in the c
                newLifetimes.Add(newSig);

                // Register the new variable lifetime with the graph. Both AddDerived and 
                // AddPrecursor are used because the new lifetime is being created as an
                // alias for the assigned lifetimes, and the assigned lifetimes will be
                // dependent on whatever the new lifetime is dependent on.
                foreach (var assignTime in assignLifetimes) {
                    types.LifetimeGraph.AddPrecursor(newLifetime, assignTime);
                    types.LifetimeGraph.AddDerived(assignTime, newLifetime);
                }
            }
            else {
                // Add a dependency between every variable in the assignment statement and
                // the old lifetime. We are using AddDerived only because the target lifetime
                // will exist whether or not we write into it, since this is a dereferenced write.
                // That means that for the purposes of lifetime analysis, the target lifetime is
                // independent of the assigned lifetimes.
                foreach (var assignTime in assignLifetimes) {
                    foreach (var targetTime in targetLifetimes) {
                        types.LifetimeGraph.AddDerived(assignTime, targetTime);
                    }
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
            private readonly ValueList<VariableSignature> newLifetimes;

            public TokenLocation Location { get; }

            public IEnumerable<ISyntaxTree> Children => new[] { this.target, this.assign };

            public bool IsPure => false;

            public AssignmentStatement(TokenLocation loc, ISyntaxTree target,
                                       ISyntaxTree assign,
                                       ValueList<VariableSignature> newLifetimes) {
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
                foreach (var sig in this.newLifetimes) {
                    writer.RegisterLifetime(sig.Lifetime, new CMemberAccess() {
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