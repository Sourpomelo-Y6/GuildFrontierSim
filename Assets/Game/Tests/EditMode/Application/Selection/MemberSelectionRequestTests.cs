using System;
using System.Collections.Generic;
using GuildFrontierSim.Application.Selection;
using NUnit.Framework;

namespace GuildFrontierSim.Tests.Application.Selection
{
    public sealed class MemberSelectionRequestTests
    {
        [Test]
        public void Constructor_CopiesAndDeduplicatesExcludedIds()
        {
            var source = new List<string> { "alice", "alice", "bob" };
            var request = new MemberSelectionRequest(
                MemberSelectionPurpose.Expedition,
                source);
            source.Clear();

            Assert.That(request.ExcludedCharacterIds, Has.Count.EqualTo(2));
            Assert.That(request.IsExcluded("alice"), Is.True);
            Assert.That(request.IsExcluded("bob"), Is.True);
        }

        [Test]
        public void Constructor_WhenExcludedIdIsEmpty_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new MemberSelectionRequest(
                    MemberSelectionPurpose.Expedition,
                    new[] { "" }));
        }
    }
}
