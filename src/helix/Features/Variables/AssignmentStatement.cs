using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Features.Memory;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree AssignmentStatement() {
            var start = this.TopExpression();

            if (this.TryAdvance(TokenKind.Assignment)) {
                var assign = this.TopExpression();
                var loc = start.Location.Span(assign.Location);
                var result = new AssignmentStatement(loc, start, assign);

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
                var stat = new AssignmentStatement(loc, start, assign);

                return stat;
            }
        }
    }
}

namespace Helix.Features.Variables {
    public record AssignmentStatement : ISyntaxTree {
        private readonly ISyntaxTree target, assign;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target, this.assign };

        public bool IsPure => false;

        public AssignmentStatement(TokenLocation loc, ISyntaxTree target, 
            ISyntaxTree assign, bool isTypeChecked = false) {

            this.Location = loc;
            this.target = target;
            this.assign = assign;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            if (this.isTypeChecked) {
                return this;
            }

            var target = this.target.CheckTypes(types).ToLValue(types);
            var targetType = types.ReturnTypes[target];

            var assign = this.assign
                .CheckTypes(types)
                .ToRValue(types)
                .UnifyTo(targetType, types);            

            var result = (ISyntaxTree)new AssignmentStatement(this.Location, target, assign, true);
            var bindings = this.CalculateLifetimes(target, assign, types);

            types.ReturnTypes[result] = PrimitiveType.Void;
            types.Lifetimes[result] = new ScalarLifetimeBundle();

            // Prepend is significant because we want the assignment to generate before the new
            // lifetime binding
            result = new BlockSyntax(this.Location, bindings.Prepend(result).ToArray());
            result = result.CheckTypes(types);

            return result;
        }

        private IEnumerable<ISyntaxTree> CalculateLifetimes(ILValue target, ISyntaxTree assign, SyntaxFrame types) {
            var targetType = types.ReturnTypes[target];

            // TODO: Implement the below comment
            // Check to see if the assigned value has the same origins
            // (or more restricted origins) than the target expression.
            // If the origins are compatible, we can assign with no further
            // issue. If they are different, then we need to insert a runtime
            // check. If the lifetimes do not have runtime values, then we
            // need to throw an error

            var targetBundle = types.Lifetimes[target];
            var assignBundle = types.Lifetimes[assign];

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
            // must outlive the target lifetimes. However, overriding a local pointer or
            // array variable replaces the current lifetime for that variable, so we need to
            // increment the mutation counter and create the new lifetime. 

            var lifetimeBindings = new List<ISyntaxTree>();

            if (target.IsLocal) {
                // Because structs are basically just bags of locals, we could actually be
                // setting multiple variables with this one assignment if we are assigning
                // a struct type. Therefore, loop through all the possible variables and
                // members and set them correctly
                foreach (var (relPath, type) in VariablesHelper.GetMemberPaths(targetType, types)) {
                    if (assignBundle.ComponentLifetimes[relPath].Count != 1) {
                        throw new Exception("Compiler bug: invalid state");
                    }

                    if (targetBundle.ComponentLifetimes[relPath].Count != 1) {
                        throw new Exception("Compiler bug: invalid state");
                    }

                    // Increment the mutation counter for modified local variables so that
                    // any new accesses to this variable will be forced to get the new 
                    // lifetime.
                    var assignLifetime = assignBundle.ComponentLifetimes[relPath][0];
                    var oldLifetime = targetBundle.ComponentLifetimes[relPath][0];
                    var sig = types.Variables[oldLifetime.Path];

                    var newLifetime = new Lifetime(
                        sig.Path,
                        sig.Lifetime.MutationCount + 1,
                        assignLifetime.IsRoot);

                    var newSig = new VariableSignature(
                        sig.Type,
                        sig.IsWritable,
                        newLifetime);

                    // Replace the old variable signature
                    types.Variables[oldLifetime.Path] = newSig;

                    // We need to generate a variable for this new lifetime in the c
                    var binding = new BindLifetimeSyntax(
                        this.Location,
                        newLifetime,
                        newSig.Path,
                        relPath);

                    lifetimeBindings.Add(binding);

                    // Register the new variable lifetime with the graph. Both AddDerived and 
                    // AddPrecursor are used because the new lifetime is being created as an
                    // alias for the assigned lifetimes, and the assigned lifetimes will be
                    // dependent on whatever the new lifetime is dependent on.
                    types.LifetimeGraph.AddPrecursor(newLifetime, assignLifetime);
                    types.LifetimeGraph.AddDerived(assignLifetime, newLifetime);
                }
            }
            else {
                // Add a dependency between every variable in the assignment statement and
                // the old lifetime. We are using AddDerived only because the target lifetime
                // will exist whether or not we write into it, since this is a dereferenced write.
                // That means that for the purposes of lifetime analysis, the target lifetime is
                // independent of the assigned lifetimes.
                foreach (var assignTime in assignBundle.AllLifetimes) {
                    foreach (var targetTime in targetBundle.AllLifetimes) {
                        types.LifetimeGraph.AddDerived(assignTime, targetTime);
                    }
                }
            }

            return lifetimeBindings;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);
            var assign = this.assign.GenerateCode(types, writer);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Assignment statement");

            writer.WriteStatement(new CAssignment() {
                Left = target,
                Right = assign
            });

            writer.WriteEmptyLine();

            return new CIntLiteral(0);
        }
    }
}