// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("SsW83MG1dEQn8CdyCHkQ+Zn/H3AYpemfuxon+r+/vr89h8zZku9h9C/RNmMNC/stCyoOOuvCfpD8PeUcKfdG2t4Dv6SGESuzJtwZNuuIHwUgSK1LB7yJ4sS7ogZPfrMOlbH9Nq3Ap7A2ydSo8hJHB1NxXnXQvn74UeNgQ1FsZ2hL5ynnlmxgYGBkYWJnXAEIqwOe9vGGpmS4jMMdTbePr7sepwHYAh2ddha7BLxU/y6cKhaqD1GHREWfjRJ9mh3ipiYa6+VyMEqX+K6Zg4DkY60INQjl08OA8ouyZ0a+DzOzR/6WMS2TcMACjLjyAeQS42BuYVHjYGtj42BgYfCg0/xe2Nh/JGnMP40hjuUpepa9tQ4WeJewyzWjQBHECvAaGGNiYGFg");
        private static int[] order = new int[] { 6,3,12,5,9,11,6,8,9,12,12,12,12,13,14 };
        private static int key = 97;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
