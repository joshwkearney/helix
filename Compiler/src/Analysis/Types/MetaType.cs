using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Trophy.Analysis.Types {
    public class MetaType : ITrophyType {
        public ITrophyType PayloadType { get; }

        public MetaType(ITrophyType payload) {
            this.PayloadType = payload;
        }

        public bool Equals([AllowNull] ITrophyType other) {
            return other is MetaType meta && this.PayloadType.Equals(meta.PayloadType);
        }

        public override bool Equals(object obj) {
            return this.Equals(obj as ITrophyType);
        }

        public override int GetHashCode() {
            return this.PayloadType.GetHashCode() * 11;
        }

        public override string ToString() {
            return "$meta[" + this.PayloadType + "]";
        }

        public TypeCopiability GetCopiability(ITypeRecorder types) => TypeCopiability.Unconditional;

        public bool HasDefaultValue(ITypeRecorder types) => false;

        public IOption<MetaType> AsMetaType() => Option.Some(this);
    }
}