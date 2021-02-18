using System;
namespace Attempt20 {
    public static class RegionHelper {
        public static string Heap { get; } = "heap";

        public static string Stack { get; } = Heap + "$stack";
    }
}
