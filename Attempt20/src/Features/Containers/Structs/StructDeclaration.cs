using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.Functions;
using Attempt20.Parsing;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt20.Features.Containers {
    public enum AggregateKind {
        Struct, Union
    }

    public class AggregateParsedDeclaration : IParsedDeclaration {
        public TokenLocation Location { get; set; }

        public StructSignature Signature { get; set; }

        public IReadOnlyList<IParsedDeclaration> Declarations { get; set; }

        public AggregateKind Kind { get; set; }

        public void DeclareNames(INameRecorder names) {
            // Make sure this name isn't taken
            if (names.TryFindName(this.Signature.Name, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.Signature.Name);
            }

            var structPath = names.CurrentScope.Append(this.Signature.Name);

            // Declare this aggregate
            if (this.Kind == AggregateKind.Struct) {
                names.DeclareGlobalName(structPath, NameTarget.Struct);
            }
            else {
                names.DeclareGlobalName(structPath, NameTarget.Union);
            }

            // Declare the members
            foreach (var mem in this.Signature.Members) {
                names.DeclareGlobalName(structPath.Append(mem.MemberName), NameTarget.Reserved);
            }

            var parNames = this.Signature.Members.Select(x => x.MemberName).ToArray();
            var unique = parNames.Distinct().ToArray();

            // Check for duplicate member names
            if (parNames.Length != unique.Length) {
                var dup = parNames.Except(unique).First();

                throw TypeCheckingErrors.IdentifierDefined(this.Location, dup);
            }

            // Rewrite function declarations to be methods
            this.Declarations = this.Declarations
                .Select(x => {
                    if (x is FunctionParseDeclaration func) {
                        var structType = new NamedType(names.CurrentScope.Append(this.Signature.Name));
                        var newPars = func.Signature.Parameters.Prepend(new FunctionParameter("this", structType)).ToImmutableList();
                        var newSig = new FunctionSignature(func.Signature.Name, func.Signature.ReturnType, newPars);

                        return new FunctionParseDeclaration() {
                            Body = func.Body,
                            Location = func.Location,
                            Signature = newSig
                        };
                    }
                    else {
                        return x;
                    }
                })
                .ToArray();

            // Process the rest of the nested declarations
            names.PushScope(names.CurrentScope.Append(this.Signature.Name));
            foreach (var decl in this.Declarations) {
                decl.DeclareNames(names);
            }
            names.PopScope();
        }

        public void ResolveNames(INameRecorder names) {
            // Resolve members
            var mems = this.Signature
                .Members
                .Select(x => new StructMember(x.MemberName, names.ResolveTypeNames(x.MemberType, this.Location)))
                .ToArray();

            this.Signature = new StructSignature(this.Signature.Name, mems);

            // Process the rest of the nested declarations
            names.PushScope(names.CurrentScope.Append(this.Signature.Name));
            foreach (var decl in this.Declarations) {
                decl.ResolveNames(names);
            }
            names.PopScope();
        }

        public void DeclareTypes(INameRecorder names, ITypeRecorder types) {
            var structPath = names.CurrentScope.Append(this.Signature.Name);
            var structType = new NamedType(structPath);

            if (this.Kind == AggregateKind.Struct) {
                types.DeclareStruct(structPath, this.Signature);
            }
            else {
                types.DeclareUnion(structPath, this.Signature);
            }

            // Process the rest of the nested declarations
            names.PushScope(names.CurrentScope.Append(this.Signature.Name));
            foreach (var decl in this.Declarations) {
                decl.DeclareTypes(names, types);
            }
            names.PopScope();

            // Register methods
            foreach (var decl in this.Declarations) {
                if (decl is FunctionParseDeclaration func) {
                    types.DeclareMethodPath(structType, func.Signature.Name, structPath.Append(func.Signature.Name));
                }
            }
        }

        public IDeclaration ResolveTypes(INameRecorder names, ITypeRecorder types) {
            // Process the rest of the nested declarations
            names.PushScope(names.CurrentScope.Append(this.Signature.Name));
            var decls = this.Declarations.Select(x => x.ResolveTypes(names, types)).ToArray();
            names.PopScope();

            if (this.Kind == AggregateKind.Struct) {
                return new StructTypeCheckedDeclaration() {
                    Location = this.Location,
                    Signature = this.Signature,
                    StructPath = names.CurrentScope.Append(this.Signature.Name),
                    Declarations = decls
                };
            }
            else {
                return new UnionTypeCheckedDeclaration() {
                    Location = this.Location,
                    Signature = this.Signature,
                    StructPath = names.CurrentScope.Append(this.Signature.Name),
                    Declarations = decls
                };
            }
        }
    }

    public class UnionTypeCheckedDeclaration : IDeclaration {
        private static int counter = 0;

        public TokenLocation Location { get; set; }

        public StructSignature Signature { get; set; }

        public IdentifierPath StructPath { get; set; }

        public IReadOnlyList<IDeclaration> Declarations { get; set; }

        public void GenerateCode(ICWriter writer) {
            var unionName = $"$UnionType_" + counter++;
            var structName = this.StructPath.ToString();
            var mems = this.Signature.Members
                    .Select(x => new CParameter(writer.ConvertType(x.MemberType), x.MemberName))
                    .ToArray();

            // Write the union declarations
            writer.WriteForwardDeclaration(CDeclaration.UnionPrototype(unionName));
            writer.WriteForwardDeclaration(CDeclaration.Union(unionName, mems));
            writer.WriteForwardDeclaration(CDeclaration.EmptyLine());

            // Write the struct declarations
            writer.WriteForwardDeclaration(CDeclaration.StructPrototype(structName));
            writer.WriteForwardDeclaration(CDeclaration.Struct(structName, new[] { 
                new CParameter(CType.Integer, "tag"),
                new CParameter(CType.NamedType(unionName), "data")
            }));
            writer.WriteForwardDeclaration(CDeclaration.EmptyLine());
        }
    }

    public class StructTypeCheckedDeclaration : IDeclaration {
        public TokenLocation Location { get; set; }

        public StructSignature Signature { get; set; }

        public IdentifierPath StructPath { get; set; }

        public IReadOnlyList<IDeclaration> Declarations { get; set; }

        public void GenerateCode(ICWriter declWriter) {
            // Write forward declaration
            declWriter.WriteForwardDeclaration(CDeclaration.StructPrototype(this.StructPath.ToString()));

            // Write full struct
            declWriter.WriteForwardDeclaration(CDeclaration.Struct(
                this.StructPath.ToString(),
                this.Signature.Members
                    .Select(x => new CParameter(declWriter.ConvertType(x.MemberType), x.MemberName))
                    .ToArray()));

            declWriter.WriteForwardDeclaration(CDeclaration.EmptyLine());

            // Write nested declarations
            foreach (var decl in this.Declarations) {
                decl.GenerateCode(declWriter);
            }
        }
    }
}
