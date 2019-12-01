using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Parsing {
    //public class DictionaryReader {
    //    public AssociativeList<ISyntax, ISyntax> Dictionary { get; }
    //    private IEnumerator<KeyValuePair<SyntaxTree, SyntaxTree>> enumerator;
    //    private bool hasNext = true;

    //    public DictionaryReader(IReadOnlyDictionary<SyntaxTree, SyntaxTree> dict) {
    //        this.Dictionary = dict;
    //        this.enumerator = this.Dictionary.GetEnumerator();
    //        this.hasNext = this.enumerator.MoveNext();
    //    }

    //    public KeyValuePair<SyntaxTree, SyntaxTree> PeekPair() {
    //        if (this.hasNext == false) {
    //            throw new Exception();
    //        }

    //        var ret = this.enumerator.Current;
    //        return ret;
    //    }

    //    public KeyValuePair<T, SyntaxTree> PeekPair<T>() where T : SyntaxTree {
    //        var pair = this.PeekPair();
    //        return new KeyValuePair<T, SyntaxTree>((T)pair.Key, pair.Value);
    //    }

    //    public KeyValuePair<T1, T2> PeekPair<T1, T2>() where T1 : SyntaxTree where T2 : SyntaxTree {
    //        var pair = this.PeekPair();
    //        return new KeyValuePair<T1, T2>((T1)pair.Key, (T2)pair.Value);
    //    }

    //    public SyntaxTree PeekValue(SyntaxTree key) {
    //        var pair = this.PeekPair();

    //        if (!pair.Key.Equals(key)) {
    //            throw new Exception();
    //        }

    //        return pair.Value;
    //    }

    //    public T PeekValue<T>(SyntaxTree key) where T : SyntaxTree {
    //        var val = this.PeekValue(key);

    //        if (val is T t) {
    //            return t;
    //        }
    //        else {
    //            throw new Exception();
    //        }
    //    }

    //    public KeyValuePair<SyntaxTree, SyntaxTree> NextPair() {
    //        var ret = this.PeekPair();
    //        this.hasNext = this.enumerator.MoveNext();

    //        return ret;
    //    }

    //    public KeyValuePair<T, SyntaxTree> NextPair<T>() where T : SyntaxTree {
    //        var ret = this.PeekPair<T>();
    //        this.hasNext = this.enumerator.MoveNext();

    //        return ret;
    //    }

    //    public KeyValuePair<T1, T2> NextPair<T1, T2>() where T1 : SyntaxTree where T2 : SyntaxTree {
    //        var ret = this.PeekPair<T1, T2>();
    //        this.hasNext = this.enumerator.MoveNext();

    //        return ret;
    //    }

    //    public SyntaxTree NextValue(SyntaxTree key) {
    //        var ret = this.PeekValue(key);
    //        this.hasNext = this.enumerator.MoveNext();

    //        return ret;
    //    }

    //    public T NextValue<T>(SyntaxTree key) where T : SyntaxTree {
    //        var ret = this.PeekValue<T>(key);
    //        this.hasNext = this.enumerator.MoveNext();

    //        return ret;
    //    }

    //    public bool HasNextPair(SyntaxTree key) {
    //        if (!this.HasNextPair()) {
    //            return false;
    //        }

    //        var pair = this.enumerator.Current;
    //        if (!pair.Key.Equals(key)) {
    //            return false;
    //        }

    //        return true;
    //    }

    //    public bool HasNextPair() => this.hasNext;

    //    public bool HasNextPair<T>() {
    //        if (!this.HasNextPair()) {
    //            return false;
    //        }

    //        var pair = this.PeekPair();
    //        return pair.Key is T;
    //    }
    //}
}