using Helix.Analysis.Types;
using Helix.Analysis;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using System.Reflection;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree IfExpression() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.TopExpression();

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Location);

                return new IfParseSyntax(loc, cond, affirm, neg);
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfParseSyntax(loc, cond, affirm);
            }
        }
    }
}

namespace Helix.Features.FlowControl {
    public record IfParseSyntax : ISyntaxTree {
        private readonly ISyntaxTree cond, iftrue, iffalse;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.cond, this.iftrue, this.iffalse };

        public bool IsPure { get; }

        public IfParseSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue) {
            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockSyntax(iftrue.Location, new ISyntaxTree[] {
                iftrue, new VoidLiteral(iftrue.Location)
            });

            this.iffalse = new VoidLiteral(location);
            this.IsPure = cond.IsPure && iftrue.IsPure;
        }

        public IfParseSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue, ISyntaxTree iffalse)
            : this(location, cond, iftrue) {

            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.IsPure = cond.IsPure && iftrue.IsPure && iffalse.IsPure;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var cond = this.cond.CheckTypes(types).ToRValue(types).UnifyTo(PrimitiveType.Bool, types);
            var iftrue = this.iftrue.CheckTypes(types).ToRValue(types);
            var iffalse = this.iffalse.CheckTypes(types).ToRValue(types);

            var iftrueTypes = new SyntaxFrame(types);
            var iffalseTypes = new SyntaxFrame(types);

            iftrue = iftrue.UnifyFrom(iffalse, iftrueTypes);
            iffalse = iffalse.UnifyFrom(iftrue, iffalseTypes);

            var resultType = types.ReturnTypes[iftrue];
            var result = new IfSyntax(this.Location, cond, iftrue, iffalse, resultType);

            var modifiedVars = iftrueTypes.Variables
                .Concat(iffalseTypes.Variables)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.SelectMany(y => y.Value.CapturedVariables));

            // Unify the branch variable signatures that overlap
            // with our variable signatures to correctly capture
            // lifetimes that were added to mutable variables
            foreach (var (path, cap) in modifiedVars) {
                var oldSig = types.Variables[path];

                // If this variable is changed in both paths, we can override the current signature
                if (iftrueTypes.Variables.ContainsKey(path) && iffalseTypes.Variables.ContainsKey(path)) {
                    types.Variables[path] = new VariableSignature(
                        path,
                        oldSig.Type,
                        oldSig.IsWritable,
                        cap.ToArray());
                }
                else {
                    // If this variable is changed in only one path, append to the current captured
                    // variables.
                    types.Variables[path] = new VariableSignature(
                        path,
                        oldSig.Type,
                        oldSig.IsWritable,
                        cap.Concat(oldSig.CapturedVariables).ToArray());
                }
            }

            types.ReturnTypes[result] = resultType;

            types.CapturedVariables[result] = types
                .CapturedVariables[iftrue]
                .Concat(types.CapturedVariables[iffalse])
                .ToArray();

            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record IfSyntax : ISyntaxTree {
        private readonly ISyntaxTree cond, iftrue, iffalse;
        private readonly HelixType returnType;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.cond, this.iftrue, this.iffalse };

        public bool IsPure { get; }

        public IfSyntax(TokenLocation loc, ISyntaxTree cond,
                         ISyntaxTree iftrue,
                         ISyntaxTree iffalse, HelixType returnType) {

            this.Location = loc;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.returnType = returnType;
            this.IsPure = cond.IsPure && iftrue.IsPure && iffalse.IsPure;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var affirm = this.iftrue.GenerateCode(affirmWriter);
            var neg = this.iffalse.GenerateCode(negWriter);

            var tempName = writer.GetVariableName();

            if (this.returnType != PrimitiveType.Void) {
                affirmWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = affirm
                });

                negWriter.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = neg
                });
            }

            var stat = new CVariableDeclaration() {
                Type = writer.ConvertType(this.returnType),
                Name = tempName
            };

            var expr = new CIf() {
                Condition = this.cond.GenerateCode(writer),
                IfTrue = affirmList,
                IfFalse = negList
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.cond.Location.Line}: If statement");

            if (this.returnType != PrimitiveType.Void) {
                writer.WriteStatement(stat);
            }

            writer.WriteStatement(expr);
            writer.WriteEmptyLine();

            if (this.returnType != PrimitiveType.Void) {
                return new CVariableLiteral(tempName);
            }
            else {
                return new CIntLiteral(0);
            }
        }
    }
}