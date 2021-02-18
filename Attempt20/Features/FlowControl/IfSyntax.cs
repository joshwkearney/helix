using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.FlowControl {
    public class IfParsedSyntax : IParsedSyntax {
        public TokenLocation Location { get; set; }

        public IParsedSyntax Condition { get; set; }

        public IParsedSyntax TrueBranch { get; set; }

        public IOption<IParsedSyntax> FalseBranch { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.Condition = this.Condition.CheckNames(names);
            this.TrueBranch = this.TrueBranch.CheckNames(names);
            this.FalseBranch = this.FalseBranch.Select(x => x.CheckNames(names));

            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var cond = this.Condition.CheckTypes(names, types);
            var affirm = this.TrueBranch.CheckTypes(names, types);
            var negOpt = this.FalseBranch.Select(x => x.CheckTypes(names, types));

            // Make sure that the condition is a boolean
            if (!cond.ReturnType.IsBoolType) {
                throw TypeCheckingErrors.UnexpectedType(cond.Location, LanguageType.Boolean, cond.ReturnType);
            }

            if (negOpt.TryGetValue(out var neg)) {
                // Make sure that the branches are the same type
                if (types.TryUnifyTo(neg, affirm.ReturnType).TryGetValue(out var newNeg)) {
                    neg = newNeg;
                }
                else if (types.TryUnifyTo(affirm, neg.ReturnType).TryGetValue(out var newAffirm)) {
                    affirm = newAffirm;
                }
                else {
                    throw TypeCheckingErrors.UnexpectedType(cond.Location, affirm.ReturnType, neg.ReturnType);
                }

                return new IfTypeCheckedSyntax() {
                    Location = this.Location,
                    ReturnType = affirm.ReturnType,
                    Lifetimes = cond.Lifetimes.Union(affirm.Lifetimes).Union(neg.Lifetimes),
                    Condition = cond,
                    TrueBranch = affirm,
                    FalseBranch = Option.Some(neg)
                };
            }
            else {
                return new IfTypeCheckedSyntax() {
                    Location = this.Location,
                    ReturnType = LanguageType.Void,
                    Lifetimes = ImmutableHashSet.Create<IdentifierPath>(),
                    Condition = cond,
                    TrueBranch = affirm,
                    FalseBranch = Option.None<ITypeCheckedSyntax>()
                };
            }
        }
    }

    public class IfTypeCheckedSyntax : ITypeCheckedSyntax {
        private static int ifTemp = 0;

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public ITypeCheckedSyntax Condition { get; set; }

        public ITypeCheckedSyntax TrueBranch { get; set; }

        public IOption<ITypeCheckedSyntax> FalseBranch { get; set; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            var affirmList = new List<CStatement>();
            var affirmWriter = new CStatementWriter();

            affirmWriter.StatementWritten += (s, e) => affirmList.Add(e);

            var cond = this.Condition.GenerateCode(declWriter, statWriter);
            var affirm = this.TrueBranch.GenerateCode(declWriter, affirmWriter);

            if (this.FalseBranch.TryGetValue(out var negTree)) {
                var negList = new List<CStatement>();
                var negWriter = new CStatementWriter();

                negWriter.StatementWritten += (s, e) => negList.Add(e);

                var neg = negTree.GenerateCode(declWriter, negWriter);
                var tempName = "$if_temp_" + ifTemp++;
                var returnType = declWriter.ConvertType(this.ReturnType);

                affirmList.Add(CStatement.Assignment(CExpression.VariableLiteral(tempName), affirm));
                negList.Add(CStatement.Assignment(CExpression.VariableLiteral(tempName), neg));

                statWriter.WriteStatement(CStatement.VariableDeclaration(returnType, tempName));
                statWriter.WriteStatement(CStatement.If(cond, affirmList, negList));
                statWriter.WriteStatement(CStatement.NewLine());

                return CExpression.VariableLiteral(tempName);
            }
            else {
                statWriter.WriteStatement(CStatement.If(cond, affirmList));
                statWriter.WriteStatement(CStatement.NewLine());

                return CExpression.IntLiteral(0);
            }
        }
    }
}
