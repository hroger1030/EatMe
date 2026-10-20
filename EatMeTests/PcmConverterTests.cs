using EatMeLibrary;
using System;

namespace EatMeTests
{
    public class PcmConverterTests
    {
        [Test]
        public void ToPcm16_WritesLittleEndianSamples()
        {
            var output = new byte[6];

            var written = PcmConverter.ToPcm16([0f, 1f, -1f], 3, output);

            Assert.That(written, Is.EqualTo(6));
            Assert.That(BitConverter.ToInt16(output, 0), Is.EqualTo(0));
            Assert.That(BitConverter.ToInt16(output, 2), Is.EqualTo(short.MaxValue));
            Assert.That(BitConverter.ToInt16(output, 4), Is.EqualTo(-short.MaxValue));
        }

        [Test]
        public void ToPcm16_ClampsOutOfRangeSamples()
        {
            var output = new byte[4];

            PcmConverter.ToPcm16([2f, -3f], 2, output);

            Assert.That(BitConverter.ToInt16(output, 0), Is.EqualTo(short.MaxValue));
            Assert.That(BitConverter.ToInt16(output, 2), Is.EqualTo(-short.MaxValue));
        }

        [Test]
        public void ToPcm16_ConvertsOnlyTheFirstCountSamples()
        {
            var output = new byte[4];

            var written = PcmConverter.ToPcm16([0.5f, 1f], 1, output);

            Assert.That(written, Is.EqualTo(2));
            Assert.That(BitConverter.ToInt16(output, 2), Is.EqualTo(0));
        }

        [Test]
        public void ToPcm16_RejectsAnOutputTooSmall()
        {
            Assert.Throws<ArgumentException>(() => PcmConverter.ToPcm16([0f, 0f], 2, new byte[3]));
        }

        [Test]
        public void ToPcm16_RejectsACountPastTheSamples()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PcmConverter.ToPcm16([0f], 2, new byte[4]));
        }
    }
}
