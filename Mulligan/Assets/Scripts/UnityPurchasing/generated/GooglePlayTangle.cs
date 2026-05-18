// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("7ptYESwpuoTJ5LZTaGx6Nm+y8lFqsWn2XRc6CT9MvYOX9J58Y76EN4zwLBdvCLNdpNfKA84jQMgqVd80dfb498d19v31dfb29zJXSb61W9UEcLXdgilqXqHUQVqZFahk1rmVsWDSxZF4yP0omRyHPtHM850BREJGHndrSYKY2t0cBV0EazilbgMxYlwyWkbjKam7yLKnXSSsHrCmXvLvQryNQ0B8Emyb9eyUdjJgNdhmeKOwLJXQnMG62PXmWVXMKoHBwfadvsVn4OSFHYhJfqPbfVQplSmJfpSYCcd19tXH+vH+3XG/cQD69vb28vf0YnLfkLI7ngwE/ibleXoblfQlajBnu0bTdr0xcCLPAHFyNWh+4LXc/jtIw+DWtAez2vX09vf2");
        private static int[] order = new int[] { 2,4,10,4,8,11,7,13,11,12,13,13,13,13,14 };
        private static int key = 247;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
