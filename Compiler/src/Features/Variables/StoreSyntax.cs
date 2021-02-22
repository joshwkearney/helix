using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Variables {

    public class StoreSyntaxA : ISyntaxA {
        private readonly ISyntaxA target, assign;

        public TokenLocation Location { get; }

        public StoreSyntaxA(TokenLocation loc, ISyntaxA target, ISyntaxA assign) {
            this.Location = loc;
            this.target = target;
            this.assign = assign;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var target = this.target.CheckNames(names);
            var assign = this.assign.CheckNames(names);

            return new StoreSyntaxB(this.Location, target, assign);
        }
    }

    public class StoreSyntaxB : ISyntaxB {
        private readonly ISyntaxB target, assign;

        public TokenLocation Location { get; }

        public StoreSyntaxB(TokenLocation loc, ISyntaxB target, ISyntaxB assign) {
            this.Location = loc;
            this.target = target;
            this.assign = assign;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var target = this.target.CheckTypes(types);
            var assign = this.assign.CheckTypes(types);

            // Make sure the target is a variable type
            if (target.ReturnType is not VariableType varType) {
                throw TypeCheckingErrors.ExpectedVariableType(this.target.Location, target.ReturnType);
            }

            // Make sure the assign expression matches the target' inner type
            if (types.TryUnifyTo(assign, varType.InnerType).TryGetValue(out var newAssign)) {
                assign = newAssign;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.assign.Location, varType.InnerType, assign.ReturnType);
            }

            // Make sure all escaping variables in value outlive all of the
            // escaping variables in target
            foreach (var targetCap in target.Lifetimes) {
                foreach (var valueCap in assign.Lifetimes) {
                    if (!valueCap.Outlives(targetCap)) {
                        throw TypeCheckingErrors.LifetimeExceeded(this.Location, targetCap, valueCap);
                    }
                }
            }

            return new StoreSyntaxC(target, assign);
        }
    }

    public class StoreSyntaxC : ISyntaxC {
        private readonly ISyntaxC target, assign;

        public TrophyType ReturnType => TrophyType.Void;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public StoreSyntaxC(ISyntaxC target, ISyntaxC assign) {
            this.target = target;
            this.assign = assign;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var target = CExpression.Dereference(this.target.GenerateCode(declWriter, statWriter));
            var assign = this.assign.GenerateCode(declWriter, statWriter);

            statWriter.WriteStatement(CStatement.Assignment(target, assign));

            return CExpression.IntLiteral(0);
        }
    }
}