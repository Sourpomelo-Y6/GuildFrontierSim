using System;
using GuildFrontierSim.Domain.Expeditions;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Domain.Expeditions
{
    public sealed class ExpeditionRuntimeDataTests
    {
        [Test]
        public void Constructor_CreatesEmptyActiveExpeditionAtFirstStage()
        {
            var expedition = CreateExpedition("alice", "bob");

            Assert.That(expedition.ExpeditionId, Is.EqualTo("expedition-1"));
            Assert.That(expedition.AreaId, Is.EqualTo("forest"));
            Assert.That(expedition.ParticipantIds, Is.EqualTo(new[] { "alice", "bob" }));
            Assert.That(expedition.CurrentStage, Is.EqualTo(1));
            Assert.That(expedition.TemporaryFunds, Is.Zero);
            Assert.That(expedition.TemporaryInventory.Quantities, Is.Empty);
            Assert.That(expedition.RescuedCharacterIds, Is.Empty);
            Assert.That(expedition.Status, Is.EqualTo(ExpeditionStatus.Active));
        }

        [Test]
        public void Constructor_WhenParticipantIdsAreDuplicated_Throws()
        {
            Assert.Throws<ArgumentException>(() => CreateExpedition("alice", "alice"));
        }

        [Test]
        public void TemporaryLoot_IsKeptSeparateInsideExpedition()
        {
            var expedition = CreateExpedition("alice");

            expedition.AddTemporaryFunds(50);
            expedition.TemporaryInventory.Add("potion", 2);
            expedition.AddRescuedCharacter("captive");
            expedition.AddRescuedCharacter("captive");

            Assert.That(expedition.TemporaryFunds, Is.EqualTo(50));
            Assert.That(expedition.TemporaryInventory.GetQuantity("potion"), Is.EqualTo(2));
            Assert.That(expedition.RescuedCharacterIds, Is.EqualTo(new[] { "captive" }));
        }

        [Test]
        public void Lifecycle_AdvancesReturnsAndCompletes()
        {
            var expedition = CreateExpedition("alice");

            expedition.AdvanceStage();
            expedition.BeginReturn();
            expedition.Complete();

            Assert.That(expedition.CurrentStage, Is.EqualTo(2));
            Assert.That(expedition.Status, Is.EqualTo(ExpeditionStatus.Completed));
            Assert.Throws<InvalidOperationException>(() => expedition.AdvanceStage());
        }

        private static ExpeditionRuntimeData CreateExpedition(params string[] participantIds)
        {
            return new ExpeditionRuntimeData(
                "expedition-1",
                "forest",
                participantIds,
                100f,
                3,
                1f,
                true);
        }
    }
}
