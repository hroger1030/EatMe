using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Turns decoded audio (floats from -1 to 1) into the 16-bit little-endian PCM that streamed sound plays.
    /// </summary>
    public static class PcmConverter
    {
        /// <summary>
        /// Bytes in one 16-bit sample.
        /// </summary>
        public const int BYTES_PER_SAMPLE = 2;

        /// <summary>
        /// Writes the first count samples into output as 16-bit PCM, clamping anything outside -1 to 1, and returns
        /// the number of bytes written (count times BYTES_PER_SAMPLE).
        /// </summary>
        public static int ToPcm16(float[] samples, int count, byte[] output)
        {
            ArgumentNullException.ThrowIfNull(samples);
            ArgumentNullException.ThrowIfNull(output);

            if (count < 0 || count > samples.Length)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be within the samples");

            if (output.Length < count * BYTES_PER_SAMPLE)
                throw new ArgumentException("Output is too small for the samples", nameof(output));

            for (var i = 0; i < count; i++)
            {
                var value = (short)(Math.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                output[i * BYTES_PER_SAMPLE] = (byte)value;
                output[i * BYTES_PER_SAMPLE + 1] = (byte)(value >> 8);
            }

            return count * BYTES_PER_SAMPLE;
        }
    }
}
