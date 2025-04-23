﻿using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
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

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
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
            
            new SyntaxTagBuilder(types)
                .WithReturnType(new ArrayType(totalType))
                .BuildFor(result);

            return result;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }
            
            writer.WriteComment($"Line {this.Location.Line}: Array literal");

            throw new NotImplementedException();
        }

        /*private ICSyntax GenerateRegionCode(TypeFrame types, ICStatementWriter writer) {
            var args = this.args.Select(x => x.GenerateCode(types, writer)).ToArray();
            var helixArrayType = (ArrayType)this.GetReturnType(types);

            var cArrayType = writer.ConvertType(helixArrayType, types);
            var cInnerType = writer.ConvertType(helixArrayType.InnerType, types);

            var backingName = writer.GetVariableName();
            var tempName = writer.GetVariableName(this.tempPath);

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

            for (uint i = 0; i < args.Length; i++) {
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

        private ICSyntax GenerateStackCode(TypeFrame types, ICStatementWriter writer) {
            var args = this.args.Select(x => x.GenerateCode(types, writer)).ToArray();
            var helixArrayType = (ArrayType)this.GetReturnType(types);

            var cArrayType = writer.ConvertType(helixArrayType, types);
            var cInnerType = writer.ConvertType(helixArrayType.InnerType, types);

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
        }*/
    }
}