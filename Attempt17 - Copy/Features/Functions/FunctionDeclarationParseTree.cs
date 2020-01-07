using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.Functions {
    public class FunctionDeclarationParseTree : IDeclarationParseTree {
        public TokenLocation Location { get; }

        public FunctionSignature Signature { get; }

        public IParseTree Body { get; }

        public FunctionDeclarationParseTree(TokenLocation loc, FunctionSignature sig, IParseTree body) {
            this.Location = loc;
            this.Signature = sig;
            this.Body = body;
        }

        public IDeclarationSyntaxTree Analyze(Scope scope) {
            foreach (var par in this.Signature.Parameters) {
                var path = scope.Path.Append(par.Name);
                var info = new VariableInfo(par.Type, VariableSource.Local, path);

                scope = scope.AppendVariable(par.Name, info);
            }

            var body = this.Body.Analyze(scope);

            if (body.ReturnType != this.Signature.ReturnType) {
                throw TypeCheckingErrors.UnexpectedType(this.Location, this.Signature.ReturnType, body.ReturnType);
            }

            return new FunctionDeclarationSyntaxTree(this.Signature, body);
        }

        public Scope ModifyLateralScope(Scope scope) {
            if (scope.NameExists(this.Signature.Name)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.Signature.Name);
            }

            if (!this.Signature.ReturnType.IsDefinedWithin(scope)) {
                throw TypeCheckingErrors.TypeUndefined(this.Location, this.Signature.ReturnType.ToString());
            }

            foreach (var par in this.Signature.Parameters) {
                if (scope.NameExists(par.Name)) {
                    throw TypeCheckingErrors.IdentifierDefined(this.Location, par.Name);
                }

                if (!par.Type.IsDefinedWithin(scope)) {
                    throw TypeCheckingErrors.TypeUndefined(this.Location, par.Type.ToString());
                }
            }

            return scope.AppendFunction(this.Signature);
        }

        public void ValidateTypes(Scope scope) { 

        }
    }
}