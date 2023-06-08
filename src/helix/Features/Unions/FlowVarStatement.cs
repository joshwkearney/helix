using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Collections;

namespace Helix.Features.Unions {
    public class FlowVarStatement : ISyntaxTree {
        public StructMember UnionMember { get; }

        public IdentifierPath ShadowedPath { get; }

        public PointerType ShadowedType { get; }

        public IdentifierPath Path { get; }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Array.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public FlowVarStatement(TokenLocation loc, StructMember member,
                                IdentifierPath shadowed, PointerType shadowedType,
                                IdentifierPath path) {
            this.Location = loc;
            this.UnionMember = member;
            this.ShadowedPath = shadowed;
            this.ShadowedType = shadowedType;
            this.Path = path;
        }

        public FlowVarStatement(TokenLocation loc, StructMember member,
                                IdentifierPath shadowed, PointerType shadowedType)
            : this(loc, member, shadowed, shadowedType, new IdentifierPath(shadowed.Segments.Last())) { }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var varSig = new PointerType(this.UnionMember.Type);
            var path = types.Scope.Append(this.Path);

            types.Locals = types.Locals.SetItem(path, types.Locals[this.ShadowedPath].WithType(varSig));
            types.NominalSignatures.Add(path, varSig);

            var result = new FlowVarStatement(
                this.Location, 
                this.UnionMember,
                this.ShadowedPath, 
                this.ShadowedType, 
                path);

            var cap = new VariableCapture(
                this.ShadowedPath, 
                VariableCaptureKind.LocationCapture, 
                this.ShadowedType);

            SyntaxTagBuilder.AtFrame(types)
                .WithCapturedVariables(cap)
                .WithLifetimes(new LifetimeBounds())
                .BuildFor(result);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            ICSyntax assign = new CAddressOf() {
                Target = new CMemberAccess() {
                    Target = new CMemberAccess() {
                        Target = new CVariableLiteral(writer.GetVariableName(this.ShadowedPath)),
                        MemberName = "data"
                    },
                    MemberName = this.UnionMember.Name
                }
            };

            var name = writer.GetVariableName(this.Path);
            var cReturnType = new CPointerType(writer.ConvertType(this.UnionMember.Type, types));

            var stat = new CVariableDeclaration() {
                Type = cReturnType,
                Name = name,
                Assignment = Option.Some(assign)
            };

            writer.WriteComment($"Line {this.Location.Line}: Union downcast flowtyping");
            writer.WriteStatement(stat);
            writer.WriteEmptyLine();
            writer.VariableKinds[this.Path] = CVariableKind.Allocated;

            writer.ShadowedLifetimeSources[this.ShadowedPath] = this.Path;

            return new CIntLiteral(0);
        }
    }
}
