using System;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.Variables {
    public class StoreParsedSyntax : IParsedSyntax {
        public TokenLocation Location { get; set; }

        public IParsedSyntax Target { get; set; }

        public IParsedSyntax AssignExpression { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.Target = Target.CheckNames(names);
            this.AssignExpression = this.AssignExpression.CheckNames(names);

            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var target = this.Target.CheckTypes(names, types);
            var assign = this.AssignExpression.CheckTypes(names, types);

            // Make sure the target is a variable type
            if (target.ReturnType is not VariableType varType) {
                throw TypeCheckingErrors.ExpectedVariableType(target.Location, target.ReturnType);
            }

            // Make sure the assign expression matches the target' inner type
            if (types.TryUnifyTo(assign, varType.InnerType).TryGetValue(out var newAssign)) {
                assign = newAssign;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(assign.Location, varType.InnerType, assign.ReturnType);
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

            return new StoreTypeCheckedSyntax() {
                Location = this.Location,
                Target = target,
                AssignExpression = assign,
                ReturnType = LanguageType.Void,
                Lifetimes = ImmutableHashSet.Create<IdentifierPath>()
            };
        }
    }

    public class StoreTypeCheckedSyntax : ITypeCheckedSyntax {
        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public ITypeCheckedSyntax Target { get; set; }

        public ITypeCheckedSyntax AssignExpression { get; set; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            var target = CExpression.Dereference(this.Target.GenerateCode(declWriter, statWriter));
            var assign = this.AssignExpression.GenerateCode(declWriter, statWriter);

            statWriter.WriteStatement(CStatement.Assignment(target, assign));

            return CExpression.IntLiteral(0);
        }
    }
}