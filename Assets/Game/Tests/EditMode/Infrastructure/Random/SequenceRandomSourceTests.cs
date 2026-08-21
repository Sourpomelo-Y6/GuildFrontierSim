using System;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Infrastructure.Random
{
    public sealed class SequenceRandomSourceTests
    {
        [Test]
        public void Range_ReturnsIntegerValuesInOrder()
        {
            var random = new SequenceRandomSource(integerValues: new[] { 2, 7 });

            Assert.That(random.Range(0, 10), Is.EqualTo(2));
            Assert.That(random.Range(5, 10), Is.EqualTo(7));
        }

        [Test]
        public void Value_ReturnsFloatValuesInOrder()
        {
            var random = new SequenceRandomSource(floatValues: new[] { 0.25f, 0.75f });

            Assert.That(random.Value, Is.EqualTo(0.25f));
            Assert.That(random.Value, Is.EqualTo(0.75f));
        }

        [Test]
        public void Range_WhenValueIsOutsideRequestedRange_Throws()
        {
            var random = new SequenceRandomSource(integerValues: new[] { 10 });

            Assert.Throws<ArgumentOutOfRangeException>(() => random.Range(0, 10));
        }

        [Test]
        public void Value_WhenSequenceIsEmpty_Throws()
        {
            var random = new SequenceRandomSource();

            Assert.Throws<InvalidOperationException>(() => _ = random.Value);
        }
    }
}
