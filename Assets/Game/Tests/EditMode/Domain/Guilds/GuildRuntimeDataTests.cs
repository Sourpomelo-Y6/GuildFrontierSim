using System;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Domain.Guilds
{
    public sealed class GuildRuntimeDataTests
    {
        [Test]
        public void Constructor_WhenCharacterIdsAreDuplicated_Throws()
        {
            CharacterRuntimeData first = CreateCharacter("same-id");
            CharacterRuntimeData second = CreateCharacter("same-id");

            Assert.Throws<ArgumentException>(
                () => new GuildRuntimeData("Guild", 100, new[] { first, second }, "same-id"));
        }

        [Test]
        public void Constructor_WhenLeaderDoesNotBelongToGuild_Throws()
        {
            CharacterRuntimeData member = CreateCharacter("member");

            Assert.Throws<ArgumentException>(
                () => new GuildRuntimeData("Guild", 100, new[] { member }, "outsider"));
        }

        [Test]
        public void Funds_CannotBeSpentBelowZero()
        {
            CharacterRuntimeData member = CreateCharacter("member");
            var guild = new GuildRuntimeData("Guild", 100, new[] { member }, member.CharacterId);

            Assert.That(guild.TrySpendFunds(101), Is.False);
            Assert.That(guild.Funds, Is.EqualTo(100));
            Assert.That(guild.TrySpendFunds(40), Is.True);
            Assert.That(guild.Funds, Is.EqualTo(60));
        }

        private static CharacterRuntimeData CreateCharacter(string id)
        {
            return new CharacterRuntimeData(id, 1, 100, 10, 10, 10, 10, 30);
        }
    }
}
