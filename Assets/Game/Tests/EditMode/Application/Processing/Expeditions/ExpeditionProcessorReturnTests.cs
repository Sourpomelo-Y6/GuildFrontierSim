using System;
using GuildFrontierSim.Application.Processing.Expeditions;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Expeditions;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Expeditions
{
    public sealed class ExpeditionProcessorReturnTests
    {
        [Test]
        public void CompleteReturn_TransfersLootRestoresCharactersAndRemovesExpedition()
        {
            CharacterRuntimeData participant = CreateCharacter("participant");
            CharacterRuntimeData rescued = CreateCharacter("rescued");
            participant.SetStatus(CharacterStatus.Expedition);
            rescued.SetStatus(CharacterStatus.Captured);
            GuildRuntimeData guild = CreateGuild(100, participant, rescued);
            guild.Inventory.Add("herb", 2);
            ExpeditionRuntimeData expedition = CreateReturningExpedition(participant.CharacterId);
            expedition.AddTemporaryFunds(75);
            expedition.TemporaryInventory.Add("herb", 3);
            expedition.TemporaryInventory.Add("ore", 1);
            expedition.AddRescuedCharacter(rescued.CharacterId);
            guild.AddExpedition(expedition);

            ExpeditionReturnResult result =
                new ExpeditionProcessor().CompleteReturn(guild, expedition.ExpeditionId);

            Assert.That(result.ExpeditionId, Is.EqualTo("expedition"));
            Assert.That(result.TransferredFunds, Is.EqualTo(75));
            Assert.That(result.TransferredItems["herb"], Is.EqualTo(3));
            Assert.That(result.TransferredItems["ore"], Is.EqualTo(1));
            Assert.That(result.RescuedCharacterIds, Is.EqualTo(new[] { "rescued" }));
            Assert.That(guild.Funds, Is.EqualTo(175));
            Assert.That(guild.Inventory.GetQuantity("herb"), Is.EqualTo(5));
            Assert.That(guild.Inventory.GetQuantity("ore"), Is.EqualTo(1));
            Assert.That(participant.Status, Is.EqualTo(CharacterStatus.Available));
            Assert.That(rescued.Status, Is.EqualTo(CharacterStatus.Available));
            Assert.That(expedition.Status, Is.EqualTo(ExpeditionStatus.Completed));
            Assert.That(expedition.TemporaryFunds, Is.Zero);
            Assert.That(expedition.TemporaryInventory.Quantities, Is.Empty);
            Assert.That(guild.Expeditions, Is.Empty);
        }

        [Test]
        public void CompleteReturn_WhenCalledTwice_DoesNotTransferLootTwice()
        {
            CharacterRuntimeData participant = CreateCharacter("participant");
            participant.SetStatus(CharacterStatus.Expedition);
            GuildRuntimeData guild = CreateGuild(100, participant);
            ExpeditionRuntimeData expedition = CreateReturningExpedition(participant.CharacterId);
            expedition.AddTemporaryFunds(25);
            guild.AddExpedition(expedition);
            var processor = new ExpeditionProcessor();

            processor.CompleteReturn(guild, expedition.ExpeditionId);

            Assert.Throws<ArgumentException>(
                () => processor.CompleteReturn(guild, expedition.ExpeditionId));
            Assert.That(guild.Funds, Is.EqualTo(125));
        }

        [Test]
        public void CompleteReturn_WhenExpeditionIsNotReturning_RejectsWithoutChanges()
        {
            CharacterRuntimeData participant = CreateCharacter("participant");
            participant.SetStatus(CharacterStatus.Expedition);
            GuildRuntimeData guild = CreateGuild(100, participant);
            var expedition = new ExpeditionRuntimeData(
                "expedition", "area", new[] { participant.CharacterId }, 10f, 2, 1f, true);
            expedition.AddTemporaryFunds(25);
            guild.AddExpedition(expedition);

            Assert.Throws<InvalidOperationException>(
                () => new ExpeditionProcessor().CompleteReturn(guild, expedition.ExpeditionId));
            Assert.That(guild.Funds, Is.EqualTo(100));
            Assert.That(participant.Status, Is.EqualTo(CharacterStatus.Expedition));
            Assert.That(guild.Expeditions, Has.Count.EqualTo(1));
        }

        [Test]
        public void CompleteReturn_WhenParticipantStateIsInvalid_IsAtomic()
        {
            CharacterRuntimeData participant = CreateCharacter("participant");
            participant.SetStatus(CharacterStatus.Expedition);
            GuildRuntimeData guild = CreateGuild(100, participant);
            ExpeditionRuntimeData expedition = CreateReturningExpedition(participant.CharacterId);
            expedition.AddTemporaryFunds(25);
            expedition.TemporaryInventory.Add("herb", 2);
            guild.AddExpedition(expedition);
            participant.SetStatus(CharacterStatus.Available);

            Assert.Throws<InvalidOperationException>(
                () => new ExpeditionProcessor().CompleteReturn(guild, expedition.ExpeditionId));
            AssertUnchanged(guild, expedition, participant);
        }

        [Test]
        public void CompleteReturn_WhenRescuedCharacterStateIsInvalid_IsAtomic()
        {
            CharacterRuntimeData participant = CreateCharacter("participant");
            CharacterRuntimeData rescued = CreateCharacter("rescued");
            participant.SetStatus(CharacterStatus.Expedition);
            GuildRuntimeData guild = CreateGuild(100, participant, rescued);
            ExpeditionRuntimeData expedition = CreateReturningExpedition(participant.CharacterId);
            expedition.AddTemporaryFunds(25);
            expedition.AddRescuedCharacter(rescued.CharacterId);
            guild.AddExpedition(expedition);

            Assert.Throws<InvalidOperationException>(
                () => new ExpeditionProcessor().CompleteReturn(guild, expedition.ExpeditionId));
            Assert.That(guild.Funds, Is.EqualTo(100));
            Assert.That(participant.Status, Is.EqualTo(CharacterStatus.Expedition));
            Assert.That(rescued.Status, Is.EqualTo(CharacterStatus.Available));
            Assert.That(expedition.Status, Is.EqualTo(ExpeditionStatus.Returning));
        }

        [Test]
        public void CompleteReturn_WhenFundsOverflow_IsAtomic()
        {
            CharacterRuntimeData participant = CreateCharacter("participant");
            participant.SetStatus(CharacterStatus.Expedition);
            GuildRuntimeData guild = CreateGuild(int.MaxValue, participant);
            ExpeditionRuntimeData expedition = CreateReturningExpedition(participant.CharacterId);
            expedition.AddTemporaryFunds(1);
            guild.AddExpedition(expedition);

            Assert.Throws<OverflowException>(
                () => new ExpeditionProcessor().CompleteReturn(guild, expedition.ExpeditionId));
            Assert.That(guild.Funds, Is.EqualTo(int.MaxValue));
            Assert.That(participant.Status, Is.EqualTo(CharacterStatus.Expedition));
            Assert.That(expedition.TemporaryFunds, Is.EqualTo(1));
            Assert.That(expedition.Status, Is.EqualTo(ExpeditionStatus.Returning));
        }

        [Test]
        public void CompleteReturn_WhenInventoryOverflows_IsAtomic()
        {
            CharacterRuntimeData participant = CreateCharacter("participant");
            participant.SetStatus(CharacterStatus.Expedition);
            GuildRuntimeData guild = CreateGuild(100, participant);
            guild.Inventory.Add("herb", int.MaxValue);
            ExpeditionRuntimeData expedition = CreateReturningExpedition(participant.CharacterId);
            expedition.AddTemporaryFunds(25);
            expedition.TemporaryInventory.Add("herb", 1);
            guild.AddExpedition(expedition);

            Assert.Throws<OverflowException>(
                () => new ExpeditionProcessor().CompleteReturn(guild, expedition.ExpeditionId));
            Assert.That(guild.Funds, Is.EqualTo(100));
            Assert.That(guild.Inventory.GetQuantity("herb"), Is.EqualTo(int.MaxValue));
            Assert.That(participant.Status, Is.EqualTo(CharacterStatus.Expedition));
            Assert.That(expedition.TemporaryFunds, Is.EqualTo(25));
            Assert.That(expedition.Status, Is.EqualTo(ExpeditionStatus.Returning));
        }

        private static void AssertUnchanged(
            GuildRuntimeData guild,
            ExpeditionRuntimeData expedition,
            CharacterRuntimeData participant)
        {
            Assert.That(guild.Funds, Is.EqualTo(100));
            Assert.That(guild.Inventory.Quantities, Is.Empty);
            Assert.That(participant.Status, Is.EqualTo(CharacterStatus.Available));
            Assert.That(expedition.TemporaryFunds, Is.EqualTo(25));
            Assert.That(expedition.TemporaryInventory.GetQuantity("herb"), Is.EqualTo(2));
            Assert.That(expedition.Status, Is.EqualTo(ExpeditionStatus.Returning));
            Assert.That(guild.Expeditions, Has.Count.EqualTo(1));
        }

        private static ExpeditionRuntimeData CreateReturningExpedition(string participantId)
        {
            var expedition = new ExpeditionRuntimeData(
                "expedition", "area", new[] { participantId }, 10f, 2, 1f, true);
            expedition.BeginReturn();
            return expedition;
        }

        private static GuildRuntimeData CreateGuild(
            int funds,
            params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData(
                "Guild", funds, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData CreateCharacter(string id)
        {
            return new CharacterRuntimeData(id, 1, 100, 10, 10, 10, 10, 30);
        }
    }
}
