using System;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Domain.Guilds
{
    public sealed class GuildInventoryTests
    {
        [Test]
        public void AddAndRemove_MaintainNonNegativeQuantity()
        {
            var inventory = new GuildInventory();
            inventory.Add("potion", 3);

            Assert.That(inventory.TryRemove("potion", 2), Is.True);
            Assert.That(inventory.GetQuantity("potion"), Is.EqualTo(1));
            Assert.That(inventory.TryRemove("potion", 2), Is.False);
            Assert.That(inventory.GetQuantity("potion"), Is.EqualTo(1));
        }

        [Test]
        public void Add_WhenQuantityIsNotPositive_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GuildInventory().Add("potion", 0));
        }
    }
}
