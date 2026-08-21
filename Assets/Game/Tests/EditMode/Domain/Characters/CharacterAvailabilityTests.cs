using GuildFrontierSim.Domain.Characters;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Domain.Characters
{
    public sealed class CharacterAvailabilityTests
    {
        [Test]
        public void CanBeAssigned_OnlyAllowsHealthyAvailableCharacter()
        {
            var available = new CharacterRuntimeData("available", 1, 100, 10, 10, 10, 10, 30);
            var captured = new CharacterRuntimeData("captured", 1, 100, 10, 10, 10, 10, 30);
            var defeated = new CharacterRuntimeData("defeated", 1, 100, 10, 10, 10, 10, 30);
            captured.SetStatus(CharacterStatus.Captured);
            defeated.ApplyDamage(defeated.MaxHp);

            Assert.That(CharacterAvailability.CanBeAssigned(available), Is.True);
            Assert.That(CharacterAvailability.CanBeAssigned(captured), Is.False);
            Assert.That(CharacterAvailability.CanBeAssigned(defeated), Is.False);
            Assert.That(CharacterAvailability.CanBeAssigned(null), Is.False);
        }
    }
}
