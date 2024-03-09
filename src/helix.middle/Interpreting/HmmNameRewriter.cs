using Helix.Common;
using Helix.Common.Hmm;

namespace Helix.MiddleEnd.Interpreting {
    internal class HmmNameRewriter : IHmmVisitor<IHmmSyntax> {
        private readonly string suffix;
        private readonly Dictionary<string, string> names = [];

        public HmmNameRewriter(string suffix) {
            this.suffix = suffix;
        }

        private string GetName(string name) {
            return this.names.GetValueOrNone(name).OrElse(() => name);
        }

        private void RegisterName(string name) {
            this.names[name] = name + this.suffix;
        }

        public IHmmSyntax VisitAddressOf(HmmAddressOf syntax) {
            this.RegisterName(syntax.Result);

            return new HmmAddressOf() {
                Location = syntax.Location,
                Operand = this.GetName(syntax.Operand),
                Result = this.GetName(syntax.Result)
            };
        }

        public IHmmSyntax VisitArrayLiteral(HmmArrayLiteral syntax) {
            this.RegisterName(syntax.Result);

            return new HmmArrayLiteral() {
                Location = syntax.Location,
                Result = this.GetName(syntax.Result),
                Args = syntax.Args.Select(this.GetName).ToArray()
            };
        }

        public IHmmSyntax VisitAssignment(HmmAssignment syntax) {
            return new HmmAssignment() {
                Location = syntax.Location,
                Value = this.GetName(syntax.Value),
                Variable = this.GetName(syntax.Variable)
            };
        }

        public IHmmSyntax VisitAsSyntax(HmmAsSyntax syntax) {
            this.RegisterName(syntax.Result);

            return new HmmAsSyntax() {
                Location = syntax.Location,
                Type = syntax.Type,
                Result = this.GetName(syntax.Result),
                Operand = this.GetName(syntax.Operand)
            };
        }

        public IHmmSyntax VisitBinarySyntax(HmmBinarySyntax syntax) {
            this.RegisterName(syntax.Result);

            return new HmmBinarySyntax() {
                Location = syntax.Location,
                Operator = syntax.Operator,
                Result = this.GetName(syntax.Result),
                Left = this.GetName(syntax.Left),
                Right = this.GetName(syntax.Right)
            };
        }

        public IHmmSyntax VisitBreak(HmmBreakSyntax syntax) => syntax;

        public IHmmSyntax VisitContinue(HmmContinueSyntax syntax) => syntax;

        public IHmmSyntax VisitDereference(HmmDereference syntax) {
            this.RegisterName(syntax.Result);

            return new HmmDereference() {
                Location = syntax.Location,
                IsLValue = syntax.IsLValue,
                Operand = this.GetName(syntax.Operand),
                Result = this.GetName(syntax.Result)
            };
        }

        public IHmmSyntax VisitFunctionDeclaration(HmmFunctionDeclaration syntax) => throw Assert.Fail();

        public IHmmSyntax VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax) => throw Assert.Fail();

        public IHmmSyntax VisitIfExpression(HmmIfExpression syntax) {
            this.RegisterName(syntax.Result);

            return new HmmIfExpression() {
                Location = syntax.Location,
                Result = this.GetName(syntax.Result),
                Condition = this.GetName(syntax.Condition),
                Affirmative = this.GetName(syntax.Affirmative),
                Negative = this.GetName(syntax.Negative),
                AffirmativeBody = syntax.AffirmativeBody.Select(x => x.Accept(this)).ToArray(),
                NegativeBody = syntax.NegativeBody.Select(x => x.Accept(this)).ToArray()
            };
        }

        public IHmmSyntax VisitIndex(HmmIndex syntax) {
            this.RegisterName(syntax.Result);

            return new HmmIndex() {
                Location = syntax.Location,
                IsLValue = syntax.IsLValue,
                Result = this.GetName(syntax.Result),
                Operand = this.GetName(syntax.Operand),
                Index = this.GetName(syntax.Index)
            };
        }

        public IHmmSyntax VisitInvoke(HmmInvokeSyntax syntax) {
            this.RegisterName(syntax.Result);

            return new HmmInvokeSyntax() {
                Location = syntax.Location,
                Result = syntax.Result,
                Target = syntax.Target,
                Arguments = syntax.Arguments.Select(this.GetName).ToArray()
            };
        }

        public IHmmSyntax VisitIs(HmmIsSyntax syntax) {
            this.RegisterName(syntax.Result);

            return new HmmIsSyntax() {
                Location = syntax.Location,
                Field = syntax.Field,
                Operand = this.GetName(syntax.Operand),
                Result = this.GetName(syntax.Result)
            };
        }

        public IHmmSyntax VisitLoop(HmmLoopSyntax syntax) {
            return new HmmLoopSyntax() {
                Location = syntax.Location,
                Body = syntax.Body.Select(x => x.Accept(this)).ToArray()
            };
        }

        public IHmmSyntax VisitMemberAccess(HmmMemberAccess syntax) {
            this.RegisterName(syntax.Result);

            return new HmmMemberAccess() {
                Location = syntax.Location,
                IsLValue = syntax.IsLValue,
                Member = syntax.Member,
                Result = this.GetName(syntax.Result),
                Operand = this.GetName(syntax.Operand)
            };
        }

        public IHmmSyntax VisitNew(HmmNewSyntax syntax) {
            this.RegisterName(syntax.Result);

            return new HmmNewSyntax() {
                Location = syntax.Location,
                Type = syntax.Type,
                Assignments = syntax.Assignments,
                Result = this.GetName(syntax.Result)
            };
        }

        public IHmmSyntax VisitReturn(HmmReturnSyntax syntax) {
            return new HmmReturnSyntax() {
                Location = syntax.Location,
                Operand = this.GetName(syntax.Operand)
            };
        }

        public IHmmSyntax VisitStructDeclaration(HmmStructDeclaration syntax) => throw Assert.Fail();

        public IHmmSyntax VisitTypeDeclaration(HmmTypeDeclaration syntax) => throw Assert.Fail();

        public IHmmSyntax VisitUnaryOperator(HmmUnaryOperator syntax) {
            this.RegisterName(syntax.Result);

            return new HmmUnaryOperator() {
                Location = syntax.Location,
                Operator = syntax.Operator,
                Result = this.GetName(syntax.Result),
                Operand = this.GetName(syntax.Operand)
            };
        }

        public IHmmSyntax VisitUnionDeclaration(HmmUnionDeclaration syntax) => throw Assert.Fail();

        public IHmmSyntax VisitVariableStatement(HmmVariableStatement syntax) {
            this.RegisterName(syntax.Variable);

            return new HmmVariableStatement() {
                Location = syntax.Location,
                IsMutable = syntax.IsMutable,
                Variable = this.GetName(syntax.Variable),
                Value = this.GetName(syntax.Value)
            };
        }
    }
}
