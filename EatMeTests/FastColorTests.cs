using EatMeLibrary;

namespace EatMeTests
{
    public class FastColorTests
    {
        [TestCase((byte)0, (byte)0, (byte)0, (byte)0, 0x00000000u)]
        [TestCase((byte)255, (byte)255, (byte)255, (byte)255, 0xFFFFFFFFu)]
        [TestCase((byte)255, (byte)0, (byte)0, (byte)0, 0xFF000000u)]
        [TestCase((byte)0, (byte)255, (byte)0, (byte)0, 0x000000FFu)]
        [TestCase((byte)0, (byte)0, (byte)255, (byte)0, 0x0000FF00u)]
        [TestCase((byte)0, (byte)0, (byte)0, (byte)255, 0x00FF0000u)]
        [TestCase((byte)1, (byte)0, (byte)0, (byte)0, 0x01000000u)]
        [TestCase((byte)0, (byte)1, (byte)0, (byte)0, 0x00000001u)]
        [TestCase((byte)0, (byte)0, (byte)1, (byte)0, 0x00000100u)]
        [TestCase((byte)0, (byte)0, (byte)0, (byte)1, 0x00010000u)]
        [TestCase((byte)127, (byte)255, (byte)255, (byte)255, 0x7FFFFFFFu)]
        [TestCase((byte)128, (byte)0, (byte)0, (byte)0, 0x80000000u)]
        [TestCase((byte)10, (byte)20, (byte)30, (byte)40, 0x0A281E14u)]
        [TestCase((byte)255, (byte)10, (byte)20, (byte)30, 0xFF1E140Au)]
        public void FromArgb_PacksChannels(byte a, byte r, byte g, byte b, uint expected)
        {
            var color = FastColor.FromArgb(a, r, g, b);

            Assert.That(color.PackedValue, Is.EqualTo(expected));
            Assert.That(color.A, Is.EqualTo(a));
            Assert.That(color.R, Is.EqualTo(r));
            Assert.That(color.G, Is.EqualTo(g));
            Assert.That(color.B, Is.EqualTo(b));
        }

        [Test]
        public void FromArgb_EveryValueInEveryChannel_StaysInItsOwnByte()
        {
            // Other channels hold distinct values so a value leaking into a neighbour shows up.
            for (var i = 0; i <= byte.MaxValue; i++)
            {
                var value = (byte)i;
                var alpha = FastColor.FromArgb(value, 0x11, 0x22, 0x33);
                var red = FastColor.FromArgb(0x44, value, 0x55, 0x66);
                var green = FastColor.FromArgb(0x77, 0x88, value, 0x99);
                var blue = FastColor.FromArgb(0xAA, 0xBB, 0xCC, value);

                Assert.That(new[] { alpha.A, alpha.R, alpha.G, alpha.B }, Is.EqualTo(new byte[] { value, 0x11, 0x22, 0x33 }), $"alpha {value}");
                Assert.That(new[] { red.A, red.R, red.G, red.B }, Is.EqualTo(new byte[] { 0x44, value, 0x55, 0x66 }), $"red {value}");
                Assert.That(new[] { green.A, green.R, green.G, green.B }, Is.EqualTo(new byte[] { 0x77, 0x88, value, 0x99 }), $"green {value}");
                Assert.That(new[] { blue.A, blue.R, blue.G, blue.B }, Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, value }), $"blue {value}");

                Assert.That(alpha.PackedValue, Is.EqualTo((uint)value << 24 | 0x00332211u), $"alpha {value}");
                Assert.That(red.PackedValue, Is.EqualTo(0x44665500u | value), $"red {value}");
                Assert.That(green.PackedValue, Is.EqualTo(0x77990088u | (uint)value << 8), $"green {value}");
                Assert.That(blue.PackedValue, Is.EqualTo(0xAA00CCBBu | (uint)value << 16), $"blue {value}");
            }
        }

        [TestCase((byte)0, (byte)0, (byte)0)]
        [TestCase((byte)255, (byte)255, (byte)255)]
        [TestCase((byte)1, (byte)2, (byte)3)]
        [TestCase((byte)255, (byte)184, (byte)174)]
        [TestCase((byte)0, (byte)128, (byte)255)]
        public void FromRgb_IsOpaqueAndMatchesFromArgb(byte r, byte g, byte b)
        {
            var color = FastColor.FromRgb(r, g, b);

            Assert.That(color.A, Is.EqualTo(byte.MaxValue));
            Assert.That(color.PackedValue, Is.EqualTo(FastColor.FromArgb(byte.MaxValue, r, g, b).PackedValue));
        }

        [Test]
        public void WriteColor_RoundTripsWithTopBitSet()
        {
            var color = FastColor.FromArgb(255, 10, 20, 30);
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            NetworkHelper.WriteColor(writer, color);

            stream.Position = 0;
            using var reader = new BinaryReader(stream);

            Assert.That(NetworkHelper.ReadColor(reader).PackedValue, Is.EqualTo(0xFF1E140Au));
        }

        [Test]
        public void Equality_ComparesPackedValue()
        {
            var color = FastColor.FromArgb(200, 10, 20, 30);
            var same = new FastColor(color.PackedValue);
            var different = FastColor.FromArgb(200, 10, 20, 31);

            Assert.That(color == same, Is.True);
            Assert.That(color != different, Is.True);
            Assert.That(color.Equals(same), Is.True);
            Assert.That(color.Equals(different), Is.False);
            Assert.That(color.Equals((object)same), Is.True);
            Assert.That(color.Equals(null), Is.False);
            Assert.That(color.GetHashCode(), Is.EqualTo(same.GetHashCode()));
        }

        [TestCase(0f, 1f, 1f, 255, 0, 0)]
        [TestCase(60f, 1f, 1f, 255, 255, 0)]
        [TestCase(120f, 1f, 1f, 0, 255, 0)]
        [TestCase(180f, 1f, 1f, 0, 255, 255)]
        [TestCase(240f, 1f, 1f, 0, 0, 255)]
        [TestCase(300f, 1f, 1f, 255, 0, 255)]
        [TestCase(0f, 0f, 1f, 255, 255, 255)]
        [TestCase(0f, 1f, 0f, 0, 0, 0)]
        [TestCase(0f, 0f, 0.5f, 128, 128, 128)]
        [TestCase(360f, 1f, 1f, 255, 0, 0)]
        [TestCase(-120f, 1f, 1f, 0, 0, 255)]
        [TestCase(30f, 2f, -1f, 0, 0, 0)]
        public void FromHsv_GivesKnownColors(float hue, float saturation, float value, int r, int g, int b)
        {
            var color = FastColor.FromHsv(hue, saturation, value);

            Assert.That(new[] { (int)color.R, color.G, color.B }, Is.EqualTo(new[] { r, g, b }));
            Assert.That(color.A, Is.EqualTo(255));
        }
    }
}
