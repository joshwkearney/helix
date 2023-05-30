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

            var varSig = new PointerType(this.UnionMember.Type, this.UnionMember.IsWritable && this.ShadowedType.IsWritable);
            var path = types.Scope.Append(this.Path);

            types.LocalValues = types.LocalValues.SetItem(path, new LocalInfo(varSig));
            types.NominalSignatures.Add(path, varSig);

            var result = new FlowVarStatement(

                this.Location, 
                this.UnionMember,
                this.ShadowedPath, 
                this.ShadowedType, 
                path);

            result.SetReturnType(PrimitiveType.Void, types);
            result.SetCapturedVariables(this.ShadowedPath, VariableCaptureKind.LocationCapture, this.ShadowedType, types);
            result.SetPredicate(types);
            result.SetLifetimes(AnalyzeFlow(this.ShadowedPath, path, types), types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public static LifetimeBounds AnalyzeFlow(IdentifierPath shadowedPath, IdentifierPath newPath, 
                                                 TypeFrame flow) {

            var shadowedBounds = flow.LocalValues[shadowedPath];
            flow.LocalValues = flow.LocalValues.SetItem(newPath, shadowedBounds);

            return new LifetimeBounds();
        }

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
