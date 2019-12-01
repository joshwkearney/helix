﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt10 {
    public class Real64Literal : ISyntaxTree {
        public ITrophyType ExpressionType => PrimitiveTrophyType.Real64Type;

        public Scope Scope { get; }

        public double Value { get; }

        public Real64Literal(double value, Scope scope) {
            this.Value = value;
            this.Scope = scope;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}