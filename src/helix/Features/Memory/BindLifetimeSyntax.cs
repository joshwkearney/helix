﻿using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Features.Memory {
    public class BindLifetimeSyntax : ISyntaxTree {
        private readonly IdentifierPath varPath;
        private readonly IdentifierPath componentPath;
        private readonly Lifetime lifetime;
        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Array.Empty<ISyntaxTree>();

        public bool IsPure => false;

        public BindLifetimeSyntax(TokenLocation loc, Lifetime lifetime, IdentifierPath path, IdentifierPath compPath) {
            this.Location = loc;
            this.lifetime = lifetime;
            this.varPath = path;
            this.componentPath = compPath;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            types.ReturnTypes[this] = PrimitiveType.Void;
            types.Lifetimes[this] = new ScalarLifetimeBundle();

            return this;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Saving lifetime '{this.lifetime.Path}'");

            var hack = writer.GetVariableName(this.varPath);
            if (this.componentPath != new IdentifierPath()) {
                hack += "." + string.Join('.', this.componentPath.Segments);
            }

            writer.RegisterLifetime(this.lifetime, new CMemberAccess() {
                Target = new CVariableLiteral(hack),
                MemberName = "pool"
            });

            writer.WriteEmptyLine();

            return new CIntLiteral(0);
        }
    }
}