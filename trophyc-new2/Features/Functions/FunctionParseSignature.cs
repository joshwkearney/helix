﻿using System.Collections.Generic;
using System.Collections.Immutable;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Features.Functions {
    public class FunctionParseSignature {
        public ITypeTree ReturnType { get; }

        public string Name { get; }

        public IReadOnlyList<ParseFunctionParameter> Parameters { get; }

        public TokenLocation Location { get; }

        public FunctionParseSignature(TokenLocation loc, string name, ITypeTree returnType, IReadOnlyList<ParseFunctionParameter> pars) {
            this.Location = loc;
            this.ReturnType = returnType;
            this.Name = name;
            this.Parameters = pars;
        }

        public FunctionSignature ResolveNames(IdentifierPath scope, NamesRecorder names) {
            var path = scope.Append(this.Name);
            var ret = this.ReturnType.ResolveNames(scope, names);
            var pars = this.Parameters
                .Select(x => new FunctionParameter(x.Name, x.Type.ResolveNames(scope, names)))
                .ToImmutableList();

            return new FunctionSignature(path, ret, pars);
        }
    }

    public class ParseFunctionParameter {
        public string Name { get; }

        public ITypeTree Type { get; }

        public bool IsWritable { get; }

        public TokenLocation Location { get; }

        public ParseFunctionParameter(TokenLocation loc, string name, ITypeTree type, bool isWritable) {
            this.Location = loc;
            this.Name = name;
            this.Type = type;
            this.IsWritable = isWritable;
        }
    }
}