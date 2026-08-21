using System.Collections;
using GuildFrontierSim.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GuildFrontierSim.Tests.PlayMode
{
    public sealed class MainScenePlayModeTests
    {
        [UnityTest]
        public IEnumerator MainScene_AdvanceButtonProgressesTurnAndUpdatesSummary()
        {
            SceneManager.LoadScene("MainScene");
            yield return null;

            GuildSimulationController controller =
                Object.FindObjectOfType<GuildSimulationController>();
            GameObject buttonObject = GameObject.Find("Advance Turn");
            GameObject summaryObject = GameObject.Find("Summary");

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsInitialized, Is.True);
            Assert.That(buttonObject, Is.Not.Null);
            Assert.That(summaryObject, Is.Not.Null);
            Assert.That(controller.Guild.CurrentTurn, Is.Zero);

            buttonObject.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(controller.Guild.CurrentTurn, Is.EqualTo(1));
            Assert.That(summaryObject.GetComponent<Text>().text, Does.Contain("ターン: 1"));
            Assert.That(controller.LastAdvanceResult, Is.Not.Null);
        }
    }
}
