using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Features.Functions;
using System.Collections.Immutable;
using Helix.Features.Arrays;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree ArrayLiteral() {
            var start = this.Advance(TokenKind.OpenBracket);
            var args = new List<ISyntaxTree>();

            while (!this.Peek(TokenKind.CloseBracket)) {
                args.Add(this.TopExpression());

                if (!this.Peek(TokenKind.CloseBracket)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return new ArrayLiteralSyntax(loc, args);
        }
    }
}

namespace Helix.Features.Arrays {
    public record ArrayLiteralSyntax : ISyntaxTree {
        private static int tempCounter = 0;

        private readonly IReadOnlyList<ISyntaxTree> args;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.args;

        public bool IsPure => this.args.All(x => x.IsPure);

        public ArrayLiteralSyntax(TokenLocation loc, IReadOnlyList<ISyntaxTree> args) {
            this.Location = loc;
            this.args = args;
            this.tempPath = new IdentifierPath("$array" + tempCounter++);
        }

        public ArrayLiteralSyntax(TokenLocation loc, IReadOnlyList<ISyntaxTree> args, IdentifierPath tempPath) {
            this.Location = loc;
            this.args = args;
            this.tempPath = tempPath;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            if (this.args.Count == 0) {
                return new VoidLiteral(this.Location).CheckTypes(types);
            }

            var args = this.args.Select(x => x.CheckTypes(types).ToRValue(types)).ToArray();
            var totalType = args[0].GetReturnType(types);

            for (int i = 1; i < args.Length; i++) {
                var argType = args[i].GetReturnType(types);

                if (argType.CanUnifyTo(totalType, types)) {
                    continue;
                }

                if (!argType.CanUnifyFrom(totalType, types, out totalType)) {
                    throw TypeException.UnexpectedType(args[i].Location, totalType, argType);
                }
            }

            args = args.Select(x => x.UnifyTo(totalType, types)).ToArray();

            var result = new ArrayLiteralSyntax(
                this.Location, 
                args, 
                types.Scope.Append(this.tempPath));

            result.SetCapturedVariables(args, types);
            result.SetPredicate(args, types);
            result.SetReturnType(new ArrayType(totalType), types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            foreach (var arg in this.args) {
                arg.AnalyzeFlow(flow);
            }

            var arrayLifetime = new InferredLocationLifetime(
                this.Location, 
                this.tempPath.ToVariablePath(), 
                flow.LifetimeRoots, 
                LifetimeOrigin.TempValue);

            var arrayType = (ArrayType)this.GetReturnType(flow);

            foreach (var (relPath, _) in arrayType.InnerType.GetMembers(flow)) {
                foreach (var arg in this.args) {
                    var valueLifetime = arg.GetLifetimes(flow)[relPath].ValueLifetime;

                    flow.LifetimeGraph.AddStored(valueLifetime, arrayLifetime, null);
                }
            }

            var dict = new Dictionary<IdentifierPath, LifetimeBounds>() {
                { new IdentifierPath(), new LifetimeBounds(arrayLifetime) }
            };

            this.SetLifetimes(new LifetimeBundle(dict), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            var lifetime = this.GetLifetimes(types)[new IdentifierPath()].ValueLifetime;
            var roots = types.GetMaximumRoots(lifetime);

            writer.WriteComment($"Line {this.Location.Line}: Array literal");

            if (roots.Any()) {
                return this.GenerateRegionCode(types, writer);
            }
            else {
                return this.GenerateStackCode(types, writer);
            }
        }

        private ICSyntax GenerateRegionCode(FlowFrame types, ICStatementWriter writer) {
            var args = this.args.Select(x => x.GenerateCode(types, writer)).ToArray();
            var lifetime = this.GetLifetimes(types)[new IdentifierPath()].ValueLifetime;
            var helixArrayType = (ArrayType)this.GetReturnType(types);

            var cArrayType = writer.ConvertType(helixArrayType);
            var cInnerType = writer.ConvertType(helixArrayType.InnerType);

            var backingName = writer.GetVariableName();
            var tempName = writer.GetVariableName(this.tempPath);
            var cLifetime = lifetime.GenerateCode(types, writer);

            var backingAssign = new CVariableDeclaration() {
                Name = backingName,
                Type = new CPointerType(cInnerType),
                Assignment = new CRegionAllocExpression() {
                    Type = cInnerType,
                    Amount = args.Length,
                    Lifetime = cLifetime
                }
            };

            writer.WriteStatement(backingAssign);

            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];

                var argAssign = new CAssignment() {
                    Left = new CIndexExpression() {
                        Target = new CVariableLiteral(backingName),
                        Index = new CIntLiteral(i)
                    },
                    Right = arg
                };

                writer.WriteStatement(argAssign);
            }

            var assign = new CVariableDeclaration() {
                Type = cArrayType,
                Name = tempName,
                Assignment = new CCompoundExpression() {
                    Type = cArrayType,
                    Arguments = new[] {
                            new CVariableLiteral(backingName),
                            cLifetime
                        }
                }
            };

            writer.WriteStatement(assign);
            writer.WriteEmptyLine();

            return new CVariableLiteral(tempName);
        }

        private ICSyntax GenerateStackCode(FlowFrame types, ICStatementWriter writer) {
            var args = this.args.Select(x => x.GenerateCode(types, writer)).ToArray();
            var helixArrayType = (ArrayType)this.GetReturnType(types);

            var cArrayType = writer.ConvertType(helixArrayType);
            var cInnerType = writer.ConvertType(helixArrayType.InnerType);

            var backingName = writer.GetVariableName();
            var tempName = writer.GetVariableName(this.tempPath);

            var backingAssign = new CArrayDeclaration() {
                Name = backingName,
                ElementType = cInnerType,
                Elements = args
            };

            var assign = new CVariableDeclaration() {
                Type = cArrayType,
                Name = tempName,
                Assignment = new CCompoundExpression() {
                    Type = cArrayType,
                    Arguments = new[] {
                            new CVariableLiteral(backingName),
                            new CVariableLiteral("_return_region")
                        }
                }
            };

            writer.WriteStatement(backingAssign);
            writer.WriteStatement(assign);
            writer.WriteEmptyLine();

            return new CVariableLiteral(tempName);
        }
    }
}