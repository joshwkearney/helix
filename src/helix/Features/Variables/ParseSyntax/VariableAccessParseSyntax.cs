using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Features.Functions;

namespace Helix.Features.Variables {
    public record VariableAccessParseSyntax : IParseSyntax {
        public required TokenLocation Location { get; init; }
        
        public required string VariableName { get; init; }
        
        public bool IsPure => true;

        public Option<HelixType> AsType(TypeFrame types) {
            // If we're pointing at a type then return it
            if (types.TryResolveName(types.Scope, this.VariableName, out var type)) {
                return type;
            }

            return Option.None;
        }

        public ISyntax CheckTypes(TypeFrame types) {
            // Make sure this name exists
            if (!types.TryResolvePath(types.Scope, this.VariableName, out var path)) {
                throw TypeException.VariableUndefined(this.Location, this.VariableName);
            }

            if (path == new IdentifierPath("void")) {
                return new VoidLiteral {
                    Location = this.Location
                };
            }

            // See if we are accessing a variable
            if (types.TryGetVariable(path, out var type)) {
                return new VariableAccessSyntax {
                    Location = this.Location,
                    VariablePath = path,
                    VariableSignature = type,
                    IsLValue = false
                };
            }

            // See if we are accessing a function
            if (types.TryGetFunction(path, out var funcSig)) {
                return new FunctionAccessSyntax {
                    Location = this.Location,
                    FunctionPath = path,
                    FunctionSignature = funcSig,
                };
            }

            throw TypeException.VariableUndefined(this.Location, this.VariableName);
        }
    }
}