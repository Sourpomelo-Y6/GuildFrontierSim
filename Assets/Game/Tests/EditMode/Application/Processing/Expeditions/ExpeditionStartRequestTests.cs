using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Processing.Expeditions;
using GuildFrontierSim.Data.Definitions;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Processing.Expeditions
{
    public sealed class ExpeditionStartRequestTests
    {
        [Test]
        public void Constructor_CopiesExcludedIds()
        {
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");
            var excludedIds = new List<string> { "alice" };

            try
            {
                var request = new ExpeditionStartRequest("expedition-1", area, excludedIds);
                excludedIds.Clear();

                Assert.That(request.ExcludedCharacterIds, Is.EqualTo(new[] { "alice" }));
            }
            finally
            {
                TestAssetFactory.Destroy(area);
            }
        }

        [Test]
        public void Constructor_WhenExpeditionIdIsEmpty_Throws()
        {
            ExpeditionAreaDefinition area = TestAssetFactory.CreateExpeditionArea("forest");

            try
            {
                Assert.Throws<ArgumentException>(() => new ExpeditionStartRequest(" ", area));
            }
            finally
            {
                TestAssetFactory.Destroy(area);
            }
        }
    }
}
