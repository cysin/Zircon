using System;

namespace Client.Rendering.SDL3OpenGL
{
    /// <summary>
    /// Pure-math light and poison texture generators that do not depend on GDI+.
    /// </summary>
    internal static class LightGeneratorGL
    {
        /// <summary>
        /// Creates a radial gradient light texture in BGRA format.
        /// The gradient fades from (200, 200, 200, 255) at the center to fully transparent at the edges.
        /// </summary>
        public static byte[] CreateLightData(int width, int height)
        {
            byte[] result = new byte[width * height * 4];

            float cx = width / 2f;
            float cy = height / 2f;
            float rx = width / 2f;
            float ry = height / 2f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    float alpha = MathF.Max(0f, 1f - dist);

                    int i = (y * width + x) * 4;

                    // BGRA order
                    result[i + 0] = (byte)(200 * alpha); // B
                    result[i + 1] = (byte)(200 * alpha); // G
                    result[i + 2] = (byte)(200 * alpha); // R
                    result[i + 3] = (byte)(255 * alpha); // A
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a poison overlay texture in BGRA format (48x48, fully transparent by default).
        /// </summary>
        public static byte[] CreatePoisonData(int width, int height)
        {
            return new byte[width * height * 4];
        }
    }
}
