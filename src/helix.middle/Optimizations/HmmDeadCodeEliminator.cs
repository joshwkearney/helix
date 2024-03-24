using Helix.Common;
using Helix.Common.Hmm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.Optimizations {
    internal class HmmDeadCodeEliminator : IHmmVisitor<Option<IHmmSyntax>> {
        private readonly HashSet<string> usedVariables = [];
        private readonly HashSet<string> usedSinceAssignment = [];
        private readonly HashSet<string> assignmentsTo = [];

        private Option<IHmmSyntax> VisitExpression(IHmmSyntax syntax, string result, params string[] args) {
            if (!this.usedVariables.Contains(result)) {
                return Option.None;
            }

            foreach (var arg in args) {
                this.UseVariable(arg);
            }

            return Option.Some(syntax);
        }

        private void UseVariable(string value) {
            if (!int.TryParse(value, out _) && !bool.TryParse(value, out _) && value != "void") {
                this.usedVariables.Add(value);
                this.usedSinceAssignment.Add(value);
            }
        }

        public Option<IHmmSyntax> VisitAddressOf(HmmAddressOf syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Operand);

        public Option<IHmmSyntax> VisitArrayLiteral(HmmArrayLiteral syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Args.ToArray());

        public Option<IHmmSyntax> VisitAssignment(HmmAssignment syntax) {
            if (this.assignmentsTo.Contains(syntax.Variable) && !this.usedSinceAssignment.Contains(syntax.Variable)) {
                return Option.None;
            }

            this.UseVariable(syntax.Variable);
            this.UseVariable(syntax.Value);
            this.usedSinceAssignment.Remove(syntax.Variable);
            this.assignmentsTo.Add(syntax.Variable);

            return syntax;
        }

        public Option<IHmmSyntax> VisitAsSyntax(HmmAsSyntax syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Operand);

        public Option<IHmmSyntax> VisitBinarySyntax(HmmBinarySyntax syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Left, syntax.Right);

        public Option<IHmmSyntax> VisitBreak(HmmBreakSyntax syntax) => syntax;

        public Option<IHmmSyntax> VisitContinue(HmmContinueSyntax syntax) => syntax;

        public Option<IHmmSyntax> VisitDereference(HmmDereference syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Operand);

        public Option<IHmmSyntax> VisitFunctionDeclaration(HmmFunctionDeclaration syntax) {
            return new HmmFunctionDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Signature = syntax.Signature,
                Body = syntax.Body.Reverse().SelectMany(x => x.Accept(this)).Reverse().ToArray()
            };
        }

        public Option<IHmmSyntax> VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax) => syntax;

        public Option<IHmmSyntax> VisitIfExpression(HmmIfExpression syntax) {
            if (!syntax.AffirmativeBody.Any() && !syntax.NegativeBody.Any()) {
                return Option.None;
            }

            this.UseVariable(syntax.Condition);

            if (this.usedVariables.Contains(syntax.Result)) {
                this.UseVariable(syntax.Affirmative);
                this.UseVariable(syntax.Negative);
            }

            return new HmmIfExpression() {
                Location = syntax.Location,
                Condition = syntax.Condition,
                Result = syntax.Result,
                Affirmative = syntax.Affirmative,
                Negative = syntax.Negative,
                AffirmativeBody = syntax.AffirmativeBody.Reverse().SelectMany(x => x.Accept(this)).Reverse().ToArray(),
                NegativeBody = syntax.NegativeBody.Reverse().SelectMany(x => x.Accept(this)).Reverse().ToArray()
            };
        }

        public Option<IHmmSyntax> VisitIndex(HmmIndex syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Operand, syntax.Index);

        public Option<IHmmSyntax> VisitInvoke(HmmInvokeSyntax syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Arguments.Append(syntax.Target).ToArray());

        public Option<IHmmSyntax> VisitIs(HmmIsSyntax syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Operand);

        public Option<IHmmSyntax> VisitLoop(HmmLoopSyntax syntax) {
            return new HmmLoopSyntax() {
                Location = syntax.Location,
                Body = syntax.Body.Reverse().SelectMany(x => x.Accept(this)).Reverse().ToArray()
            };
        }

        public Option<IHmmSyntax> VisitMemberAccess(HmmMemberAccess syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Operand);

        public Option<IHmmSyntax> VisitNew(HmmNewSyntax syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Assignments.Select(x => x.Value).ToArray());

        public Option<IHmmSyntax> VisitReturn(HmmReturnSyntax syntax) {
            this.UseVariable(syntax.Operand);

            return syntax;
        }

        public Option<IHmmSyntax> VisitStructDeclaration(HmmStructDeclaration syntax) => syntax;

        public Option<IHmmSyntax> VisitTypeDeclaration(HmmTypeDeclaration syntax) => syntax;

        public Option<IHmmSyntax> VisitUnaryOperator(HmmUnarySyntax syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Operand);

        public Option<IHmmSyntax> VisitUnionDeclaration(HmmUnionDeclaration syntax) => syntax;

        public Option<IHmmSyntax> VisitVariableStatement(HmmVariableStatement syntax) => this.VisitExpression(syntax, syntax.Variable, syntax.Value);
    }
}
