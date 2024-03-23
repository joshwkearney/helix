using Helix.Common;
using Helix.Common.Hmm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.Optimizations {
    internal class HirDeadCodeEliminator : IHirVisitor<Unit> {
        private readonly Stack<SyntaxWriter<IHirSyntax>> writers = [];
        private readonly Stack<DeadCodeFrame> scopes = [];

        public IReadOnlyList<IHirSyntax> AllLines => this.writers.Peek().AllLines;

        public HirDeadCodeEliminator() {
            this.writers.Push(new SyntaxWriter<IHirSyntax>());
        }

        private Unit VisitExpression(IHirSyntax syntax, string result, params string[] args) {
            if (this.scopes.Peek().CanRemoveVariable(result)) {
                return Unit.Instance;
            }

            foreach (var arg in args) {
                this.scopes.Peek().UseVariable(arg);
            }

            this.writers.Peek().AddLine(syntax);
            return Unit.Instance;
        }

        private Unit VisitStatement(IHirSyntax syntax) {           
            this.writers.Peek().AddLine(syntax);
            return Unit.Instance;
        }

        public Unit VisitAddressOf(HirAddressOf syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Operand);

        public Unit VisitArrayLiteral(HirArrayLiteral syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Args.ToArray());

        public Unit VisitAssignment(HmmAssignment syntax) {
            if (this.scopes.Peek().CanRemoveAssignment(syntax.Variable)) {
                return Unit.Instance;
            }

            this.scopes.Peek().AssignTo(syntax.Variable);
            this.scopes.Peek().UseVariable(syntax.Value);
            this.writers.Peek().AddLine(syntax);

            return Unit.Instance;
        }

        public Unit VisitBinarySyntax(HirBinarySyntax syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Left, syntax.Right);

        public Unit VisitBreak(HmmBreakSyntax syntax) => this.VisitStatement(syntax);

        public Unit VisitContinue(HmmContinueSyntax syntax) => this.VisitStatement(syntax);

        public Unit VisitDereference(HirDereference syntax) => this.VisitExpression(syntax, syntax.Result, syntax.Operand);

        public Unit VisitFunctionDeclaration(HirFunctionDeclaration syntax) {
            this.writers.Push(this.writers.Peek().CreateScope());
            this.scopes.Push(new DeadCodeFrame());

            foreach (var stat in syntax.Body.Reverse()) {
                stat.Accept(this);
            }

            this.scopes.Pop();
            var writer = this.writers.Pop();

            this.writers.Peek().AddLine(new HirFunctionDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Signature = syntax.Signature,
                Body = writer.AllLines.Reverse().ToArray()
            });

            return Unit.Instance;
        }

        public Unit VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax) => this.VisitStatement(syntax);

        public Unit VisitIfExpression(HirIfExpression syntax) {
            this.writers.Push(this.writers.Peek().CreateScope());
            this.scopes.Push(this.scopes.Peek().CreateScope());

            foreach (var stat in syntax.AffirmativeBody.Reverse()) {
                stat.Accept(this);
            }

            var affirmScope = this.scopes.Pop();
            var affirmWriter = this.writers.Pop();

            this.writers.Push(this.writers.Peek().CreateScope());
            this.scopes.Push(this.scopes.Peek().CreateScope());

            foreach (var stat in syntax.NegativeBody.Reverse()) {
                stat.Accept(this);
            }

            var negScope = this.scopes.Pop();
            var negWriter = this.writers.Pop();

            if (!affirmWriter.AllLines.Any() && !negWriter.AllLines.Any()) {
                return Unit.Instance;
            }

            this.scopes.Pop();
            this.scopes.Push(affirmScope.MergeWith(negScope));
            this.scopes.Peek().UseVariable(syntax.Condition);

            this.writers.Peek().AddLine(new HirIfExpression() {
                Location = syntax.Location,
                Condition = syntax.Condition,
                AffirmativeBody = affirmWriter.AllLines.Reverse().ToArray(),
                NegativeBody = negWriter.AllLines.Reverse().ToArray()
            });

            return Unit.Instance;
        }

        public Unit VisitIndex(HirIndex syntax) {
            return this.VisitExpression(syntax, syntax.Result, syntax.Operand, syntax.Index);
        }

        public Unit VisitInvoke(HirInvokeSyntax syntax) {
            // Never remove an invoke statement
            this.writers.Peek().AddLine(syntax);

            return Unit.Instance;
        }

        public Unit VisitIs(HirIsSyntax syntax) {
            return this.VisitExpression(syntax, syntax.Result, syntax.Operand);
        }

        public Unit VisitLoop(HirLoopSyntax syntax) {
            this.writers.Push(this.writers.Peek().CreateScope());

            foreach (var stat in syntax.Body.Reverse()) {
                stat.Accept(this);
            }

            var writer = this.writers.Pop();

            this.writers.Peek().AddLine(new HirLoopSyntax() {
                Location = syntax.Location,
                Body = writer.AllLines.Reverse().ToArray()
            });

            return Unit.Instance;
        }

        public Unit VisitMemberAccess(HirMemberAccess syntax) {
            return this.VisitExpression(syntax, syntax.Result, syntax.Operand);
        }

        public Unit VisitNew(HirNewSyntax syntax) {
            return this.VisitExpression(syntax, syntax.Result, syntax.Assignments.Select(x => x.Value).ToArray());
        }

        public Unit VisitReturn(HmmReturnSyntax syntax) {
            this.scopes.Peek().UseVariable(syntax.Operand);

            return this.VisitStatement(syntax);
        }

        public Unit VisitStructDeclaration(HmmStructDeclaration syntax) {
            return this.VisitStatement(syntax);
        }

        public Unit VisitTypeDeclaration(HmmTypeDeclaration syntax) {
            return this.VisitStatement(syntax);
        }

        public Unit VisitUnaryOperator(HirUnaryOperator syntax) {
            return this.VisitExpression(syntax, syntax.Result, syntax.Operand);
        }

        public Unit VisitUnionDeclaration(HmmUnionDeclaration syntax) {
            return this.VisitStatement(syntax);
        }

        public Unit VisitVariableStatement(HirVariableStatement syntax) {
            return this.VisitExpression(syntax, syntax.Variable);
        }

        public Unit VisitIntrinsicUnionMemberAccess(HirIntrinsicUnionMemberAccess syntax) {
            return this.VisitExpression(syntax, syntax.Result, syntax.Operand);
        }
    }
}
