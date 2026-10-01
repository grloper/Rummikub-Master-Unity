// Minimal adapters for dependency-free logic checks only. These do not simulate
// Unity lifecycle, components, scene serialization, drag/drop or the AI coroutine.
namespace UnityEngine {
    public class MonoBehaviour { }
    public class Transform { }
    public static class Random {
        private static readonly System.Random Source = new(20261001);
        public static int Range(int min, int max) => Source.Next(min, max);
    }
}
