using System;
using GuildFrontierSim.Application.Assignments.Defense;
using GuildFrontierSim.Application.Assignments.Expeditions;
using GuildFrontierSim.Application.Planning;
using GuildFrontierSim.Domain.Characters;
using GuildFrontierSim.Domain.Guilds;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Planning
{
    public sealed class TurnPlanBuilderTests
    {
        [Test]
        public void Begin_InCpuMode_ImmediatelyProducesReadySession()
        {
            GuildRuntimeData guild = Guild(Character("leader"));

            TurnPlanningSession session = new TurnPlanBuilder().Begin(
                guild,
                new GuildControlPolicy(GuildControlMode.Cpu),
                requiresDefense: true,
                requiresExpedition: true,
                requiresActingLeader: true);

            Assert.That(session.Status, Is.EqualTo(TurnPlanningStatus.Ready));
            Assert.That(session.TargetTurn, Is.EqualTo(1));
            Assert.That(session.IsResolved(TurnDecisionType.DefenseMembers), Is.True);
            Assert.That(session.IsResolved(TurnDecisionType.ExpeditionMembers), Is.True);
            Assert.That(session.IsResolved(TurnDecisionType.ExpeditionArea), Is.True);
            Assert.That(session.IsResolved(TurnDecisionType.ActingLeader), Is.True);
        }

        [Test]
        public void Begin_WithAuthorizedPlayer_WaitsForRequiredInput()
        {
            GuildRuntimeData guild = Guild(Character("leader", isPlayer: true));

            TurnPlanningSession session = new TurnPlanBuilder().Begin(
                guild,
                new GuildControlPolicy(GuildControlMode.Player, "leader"),
                requiresDefense: true,
                requiresExpedition: false);

            Assert.That(session.Status, Is.EqualTo(TurnPlanningStatus.WaitingForInput));
            Assert.That(session.IsRequired(TurnDecisionType.DefenseMembers), Is.True);
            Assert.That(session.IsResolved(TurnDecisionType.DefenseMembers), Is.False);
        }

        [Test]
        public void Begin_WhenPlayerHasNoAuthority_FallsBackToCpu()
        {
            CharacterRuntimeData leader = Character("leader");
            CharacterRuntimeData player = Character("player", isPlayer: true);
            GuildRuntimeData guild = Guild(leader, player);

            TurnPlanningSession session = new TurnPlanBuilder().Begin(
                guild,
                new GuildControlPolicy(GuildControlMode.Player, "player"),
                requiresDefense: true,
                requiresExpedition: false);

            Assert.That(session.Status, Is.EqualTo(TurnPlanningStatus.Ready));
            TurnPlan plan = new TurnPlanBuilder().Build(guild, session);
            Assert.That(plan.IsDelegatedToCpu(TurnDecisionType.DefenseMembers), Is.True);
        }

        [Test]
        public void SubmitAssignments_ThenBuild_CreatesImmutablePlan()
        {
            GuildRuntimeData guild = Guild(
                Character("leader", isPlayer: true),
                Character("defender"),
                Character("scout"));
            var builder = new TurnPlanBuilder();
            TurnPlanningSession session = builder.Begin(
                guild,
                new GuildControlPolicy(GuildControlMode.Player, "leader"),
                requiresDefense: true,
                requiresExpedition: true);
            var defense = new DefenseAssignment(100f, new[] { "defender" });
            var expedition = new ExpeditionAssignment(
                "exp-1", "forest", new[] { "scout" });

            session.SubmitDefense(defense, guild.Revision);
            session.SubmitExpedition(expedition, guild.Revision);
            TurnPlan plan = builder.Build(guild, session);

            Assert.That(session.Status, Is.EqualTo(TurnPlanningStatus.Finalized));
            Assert.That(plan.DefenseAssignment, Is.SameAs(defense));
            Assert.That(plan.ExpeditionAssignment, Is.SameAs(expedition));
            Assert.That(plan.GuildRevision, Is.EqualTo(guild.Revision));
            Assert.Throws<InvalidOperationException>(() =>
                session.SubmitDefense(defense, guild.Revision));
        }

        [Test]
        public void SubmitDecisionTwice_IsRejected()
        {
            GuildRuntimeData guild = Guild(Character("leader", isPlayer: true));
            TurnPlanningSession session = new TurnPlanBuilder().Begin(
                guild,
                new GuildControlPolicy(GuildControlMode.Player, "leader"),
                requiresDefense: true,
                requiresExpedition: false);
            var assignment = new DefenseAssignment(100f, new string[0]);

            session.SubmitDefense(assignment, guild.Revision);

            Assert.Throws<InvalidOperationException>(() =>
                session.SubmitDefense(assignment, guild.Revision));
        }

        [Test]
        public void Build_WithOverlappingParticipants_IsRejectedWithoutFinalizing()
        {
            GuildRuntimeData guild = Guild(
                Character("leader", isPlayer: true), Character("member"));
            var builder = new TurnPlanBuilder();
            TurnPlanningSession session = builder.Begin(
                guild,
                new GuildControlPolicy(GuildControlMode.Player, "leader"),
                requiresDefense: true,
                requiresExpedition: true);
            session.SubmitDefense(
                new DefenseAssignment(100f, new[] { "member" }), guild.Revision);
            session.SubmitExpedition(
                new ExpeditionAssignment("exp-1", "forest", new[] { "member" }),
                guild.Revision);

            Assert.Throws<InvalidOperationException>(() => builder.Build(guild, session));
            Assert.That(session.Status, Is.EqualTo(TurnPlanningStatus.Ready));
        }

        [Test]
        public void Build_AfterGuildRevisionChanges_IsRejected()
        {
            GuildRuntimeData guild = Guild(Character("leader", isPlayer: true));
            var builder = new TurnPlanBuilder();
            TurnPlanningSession session = builder.Begin(
                guild,
                new GuildControlPolicy(GuildControlMode.Player, "leader"),
                requiresDefense: true,
                requiresExpedition: false);
            session.SubmitDefense(
                new DefenseAssignment(100f, new string[0]), guild.Revision);
            guild.MarkStateChanged();

            Assert.Throws<InvalidOperationException>(() => builder.Build(guild, session));
            Assert.That(session.Status, Is.EqualTo(TurnPlanningStatus.Ready));
        }

        [Test]
        public void Delegate_WhenPolicyDisallowsDelegation_IsRejected()
        {
            GuildRuntimeData guild = Guild(Character("leader", isPlayer: true));
            TurnPlanningSession session = new TurnPlanBuilder().Begin(
                guild,
                new GuildControlPolicy(
                    GuildControlMode.Player,
                    "leader",
                    canDelegateToCpu: false),
                requiresDefense: true,
                requiresExpedition: false);

            Assert.Throws<InvalidOperationException>(() =>
                session.DelegateToCpu(TurnDecisionType.DefenseMembers, guild.Revision));
            Assert.That(session.Status, Is.EqualTo(TurnPlanningStatus.WaitingForInput));
        }

        [Test]
        public void DelegateExpedition_ResolvesMembersAndAreaTogether()
        {
            GuildRuntimeData guild = Guild(Character("leader", isPlayer: true));
            TurnPlanningSession session = new TurnPlanBuilder().Begin(
                guild,
                new GuildControlPolicy(GuildControlMode.Player, "leader"),
                requiresDefense: false,
                requiresExpedition: true);

            session.DelegateToCpu(TurnDecisionType.ExpeditionMembers, guild.Revision);

            Assert.That(session.Status, Is.EqualTo(TurnPlanningStatus.Ready));
            Assert.That(session.IsResolved(TurnDecisionType.ExpeditionMembers), Is.True);
            Assert.That(session.IsResolved(TurnDecisionType.ExpeditionArea), Is.True);
        }

        [Test]
        public void MarkApplied_CanOnlySucceedOnceForCurrentRevision()
        {
            GuildRuntimeData guild = Guild(Character("leader"));
            var builder = new TurnPlanBuilder();
            TurnPlanningSession session = builder.Begin(
                guild,
                new GuildControlPolicy(GuildControlMode.Cpu),
                requiresDefense: true,
                requiresExpedition: false);
            builder.Build(guild, session);

            session.MarkApplied(guild.Revision);

            Assert.That(session.Status, Is.EqualTo(TurnPlanningStatus.Applied));
            Assert.Throws<InvalidOperationException>(() => session.MarkApplied(guild.Revision));
        }

        private static GuildRuntimeData Guild(params CharacterRuntimeData[] characters)
        {
            return new GuildRuntimeData("Guild", 100, characters, characters[0].CharacterId);
        }

        private static CharacterRuntimeData Character(string id, bool isPlayer = false)
        {
            return new CharacterRuntimeData(
                id, 1, 100, 10, 10, 10, 10, 30, isPlayerCharacter: isPlayer);
        }
    }
}
