using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree IfExpression(BlockBuilder block) {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression(block);
            var loc = start.Location.Span(cond.Location);
            var ifId = this.scope.Append(block.GetTempName());

            this.Advance(TokenKind.ThenKeyword);

            var (affirmStats, affirm) = TopBlock();
            var affirmAssign = new SetIfBranchSyntax(affirm.Location, ifId, true, affirm);

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var (negStats, neg) = TopBlock();
                var negAssign = new SetIfBranchSyntax(affirm.Location, ifId, false, neg);

                affirmStats.Add(affirmAssign);
                negStats.Add(negAssign);
                loc = start.Location.Span(neg.Location);

                var expr = new IfParseSyntax(
                    loc,
                    ifId,
                    cond,
                    new BlockSyntax(affirm.Location, affirmStats),
                    new BlockSyntax(neg.Location, negStats));

                block.Statements.Add(expr);
                return new IfAccessSyntax(loc, ifId);
            }
            else {
                loc = start.Location.Span(affirm.Location);
                var expr = new IfParseSyntax(
                    loc,
                    ifId,
                    cond,
                    new BlockSyntax(affirm.Location, affirmStats));

                block.Statements.Add(expr);
                return new VoidLiteral(loc);
            }
        }

        (List<ISyntaxTree> StateMachineSyntax, ISyntaxTree ret) TopBlock() {
            var builder = new BlockBuilder();
            var expr = this.TopExpression(builder);

            if (builder.Statements.Any()) {
                builder.Statements.Add(expr);

                return (builder.Statements, expr);
            }
            else {
                return (new(), expr);
            }
        }
    }
}

namespace Helix.Features.FlowControl {
    public record IfParseSyntax : ISyntaxTree, IStatement {
        private readonly IdentifierPath returnVar;
        private readonly ISyntaxTree cond;
        private readonly IStatement iftrue, iffalse;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.cond, this.iftrue, this.iffalse };

        public bool IsPure { get; }

        public IfParseSyntax(TokenLocation location, IdentifierPath returnVar, ISyntaxTree cond, IStatement iftrue) {
            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockSyntax(iftrue.Location, new ISyntaxTree[] {
                iftrue, new VoidLiteral(iftrue.Location)
            });

            this.iffalse = new BlockSyntax(this.Location, Array.Empty<ISyntaxTree>());
            this.returnVar = returnVar;

            this.IsPure = this.cond.IsPure && this.iftrue.IsPure && this.iffalse.IsPure;
        }

        public IfParseSyntax(TokenLocation location, IdentifierPath returnVar, ISyntaxTree cond,
            IStatement iftrue, IStatement iffalse) {

            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.returnVar = returnVar;

            this.IsPure = this.cond.IsPure && this.iftrue.IsPure && this.iffalse.IsPure;
        }

        public bool RewriteNonlocalFlow(SyntaxFrame types, FlowRewriter flow) {
            int ifState = flow.NextState++;

            this.iftrue.RewriteNonlocalFlow(types, flow);
            int afterTrueState = flow.NextState++;

            this.iffalse.RewriteNonlocalFlow(types, flow);
            int afterFalseState = flow.NextState++;

            flow.ConditionalStates[ifState] = new ConditionalState(
                this.cond,
                this.returnVar,
                ifState + 1,
                afterTrueState + 1
            );

            flow.ConstantStates[afterTrueState] = new ConstantState() {
                Expression = new VoidLiteral(this.Location),
                NextState = flow.NextState
            };

            flow.ConstantStates[afterFalseState] = new ConstantState() {
                Expression = new VoidLiteral(this.Location),
                NextState = flow.NextState
            };

            return true;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record SetIfBranchSyntax : ISyntaxTree {
        private readonly IdentifierPath ifId;
        private readonly ISyntaxTree value;
        private readonly bool branch;
        private readonly bool isTypeChecked = false;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.value };

        public bool IsPure => false;

        public SetIfBranchSyntax(TokenLocation loc, IdentifierPath ifId, bool branch, 
            ISyntaxTree value, bool isTypeChecked = false) {
            this.Location = loc;
            this.ifId = ifId;
            this.branch = branch;
            this.value = value;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            if (this.isTypeChecked) {
                return this;
            }

            var value = this.value.CheckTypes(types).ToRValue(types);

            if (this.branch) {
                types.IfBranches[this.ifId].TrueBranch = value;
            }
            else {
                types.IfBranches[this.ifId].FalseBranch = value;
            }

            var result = new SetIfBranchSyntax(this.Location, this.ifId, this.branch, null, true);
            types.ReturnTypes[result] = PrimitiveType.Void;

            return result;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            var assign = this.branch
                ? types.IfBranches[this.ifId].TrueBranch
                : types.IfBranches[this.ifId].FalseBranch;

            writer.WriteStatement(new CAssignment() {
                Left = new CVariableLiteral(writer.GetVariableName(this.ifId)),
                Right = assign.GenerateCode(types, writer)
            });

            return new CIntLiteral(0);
        }
    }

    public record IfAccessSyntax : ISyntaxTree {
        private readonly IdentifierPath ifid;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Array.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public IfAccessSyntax(TokenLocation loc, IdentifierPath ifid, bool isTypeChecked = false) {
            this.Location = loc;
            this.ifid = ifid;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            if (this.isTypeChecked) {
                return this;
            }

            if (types.IfBranches[this.ifid].ReturnType == null) {
                var branch1 = types.IfBranches[this.ifid].TrueBranch;
                var branch2 = types.IfBranches[this.ifid].FalseBranch;

                branch1 = branch1.UnifyFrom(branch2, types);
                branch2 = branch2.UnifyFrom(branch1, types);

                types.IfBranches[this.ifid].TrueBranch = branch1;
                types.IfBranches[this.ifid].FalseBranch = branch2;
                types.IfBranches[this.ifid].ReturnType = types.ReturnTypes[branch1];
            }

            var result = new IfAccessSyntax(this.Location, this.ifid, true);

            types.ReturnTypes[result] = types.IfBranches[this.ifid].ReturnType;
            return result;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            return new CVariableLiteral(writer.GetVariableName(this.ifid));
        }
    }
}