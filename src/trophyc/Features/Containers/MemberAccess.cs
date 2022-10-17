using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;
using Trophy.Features.Meta;
using Trophy.Parsing;

namespace Trophy.Features.Containers {
    public class MemberAccessSyntaxA : ISyntaxA  {
        private readonly ISyntaxA target;
        private readonly string memberName;
        private readonly bool literalAccess;
        public TokenLocation Location { get; }

        public MemberAccessSyntaxA(TokenLocation location, ISyntaxA target, string memberName, bool literalAccess) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.literalAccess = literalAccess;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var target = this.target.CheckNames(names);

            return new MemberAccessSyntaxB(this.Location, target, this.memberName, this.literalAccess);
        }

        public IOption<ITrophyType> ResolveToType(INamesRecorder names) {
            if (!this.target.ResolveToType(names).TryGetValue(out var innerType)) {
                return Option.None<ITrophyType>();
            }

            if (!innerType.AsNamedType().TryGetValue(out var path)) {
                return Option.None<ITrophyType>();
            }

            var newPath = path.Append(this.memberName);

            if (!names.TryGetName(newPath, out var target)) {
                return Option.None<ITrophyType>();
            }

            if (target == NameTarget.Function || target == NameTarget.GenericType 
                || target == NameTarget.Struct || target == NameTarget.Union) {

                return Option.Some(new NamedType(newPath));
            }
            else {
                return Option.None<ITrophyType>();
            }
        }
    }

    public class MemberAccessSyntaxB : ISyntaxB {
        private readonly ISyntaxB target;
        private readonly string memberName;
        private readonly bool literalAccess;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.target.VariableUsage;
        }

        public MemberAccessSyntaxB(TokenLocation location, ISyntaxB target, string memberName, bool literalAccess) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.literalAccess = literalAccess;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var target = this.target.CheckTypes(types);

            // If this is an array we can get the size
            if (target.ReturnType.AsArrayType().TryGetValue(out _)) {
                if (this.memberName == "size") {
                    if (this.literalAccess) {
                        throw TypeCheckingErrors.ExpectedVariableType(this.Location, target.ReturnType);
                    }

                    return new MemberAccessTypeCheckedSyntax(
                        target, 
                        this.memberName, 
                        ITrophyType.Integer,
                        false);
                }
            }

            // If this is a fixed array we can get the size
            if (target.ReturnType.AsFixedArrayType().TryGetValue(out _)) {
                if (this.memberName == "size") {
                    if (this.literalAccess) {
                        throw TypeCheckingErrors.ExpectedVariableType(this.Location, target.ReturnType);
                    }

                    return new MemberAccessTypeCheckedSyntax(
                        target,
                        this.memberName,
                        ITrophyType.Integer,
                        false);
                }
            }

            // If this is a named type it could be a struct
            if (target.ReturnType.AsNamedType().TryGetValue(out var path)) {
                // If this is a struct we can access the fields
                if (types.TryGetStruct(path).TryGetValue(out var sig)) {
                    // Make sure this field is present
                    var fieldOpt = sig
                        .Members
                        .Where(x => x.MemberName == this.memberName)
                        .FirstOrNone();

                    if (fieldOpt.TryGetValue(out var field)) {
                        bool deref = false;
                        var returnType = field.MemberType;

                        if (!this.literalAccess && field.Kind != VariableKind.Value) {
                            returnType = (returnType as VarRefType).InnerType;
                            deref = true;
                        }

                        return new MemberAccessTypeCheckedSyntax(
                            target,
                            this.memberName,
                            returnType,
                            deref);
                    }
                }
            }

            // If this is a meta type, we are probably trying to access inner objects
            if (target.ReturnType.AsMetaType().TryGetValue(out var meta)) {
                if (meta.PayloadType.AsNamedType().TryGetValue(out path)) {
                    if (types.TryGetName(path.Append(this.memberName)).TryGetValue(out var value)) {
                        if (value.AsFunction().Any() || value.AsStruct().Any() || value.AsUnion().Any()) {
                            return new TypeSyntaxC(new MetaType(new NamedType(path.Append(this.memberName))));
                        }
                    }
                }
            }

            throw TypeCheckingErrors.MemberUndefined(this.Location, target.ReturnType, this.memberName);
        }
    }

    public class MemberAccessTypeCheckedSyntax : ISyntaxC {
        private readonly ISyntaxC target;
        private readonly string memberName;
        private readonly bool dereference;

        public ITrophyType ReturnType { get; }

        public MemberAccessTypeCheckedSyntax(
            ISyntaxC target, 
            string memberName, 
            ITrophyType returnType,
            bool dereference) {

            this.target = target;
            this.memberName = memberName;
            this.ReturnType = returnType;
            this.dereference = dereference;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            // Optimization: insert a fixed array's size directly instead of a member access
            if (this.target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArray)) {
                return CExpression.IntLiteral(fixedArray.Size);
            }

            var target = this.target.GenerateCode(declWriter, statWriter);
            var result = CExpression.MemberAccess(target, this.memberName);

            if (this.dereference) {
                result = CExpression.Dereference(result);
            }

            return result;
        }
    }
}
