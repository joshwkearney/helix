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
    public class FlowVarStatement : IParseTree {
        public StructMember UnionMember { get; }

        public IdentifierPath ShadowedPath { get; }

        public PointerType ShadowedType { get; }

        public IdentifierPath Path { get; }

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => Array.Empty<IParseTree>();

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

        public IParseTree ToRValue(TypeFrame types) => this;
    }
}
