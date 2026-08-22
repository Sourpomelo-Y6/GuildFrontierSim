using System.Collections;
using GuildFrontierSim.Application.Simulation;
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

            GameObject.Find("Reset Simulation").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(controller.Guild.CurrentTurn, Is.Zero);
            Assert.That(summaryObject.GetComponent<Text>().text, Does.Contain("ターン: 0"));
        }

        [UnityTest]
        public IEnumerator MainScene_ManualModePlansAndAppliesExpeditionTurn()
        {
            SceneManager.LoadScene("MainScene");
            yield return null;

            GuildSimulationController controller =
                Object.FindObjectOfType<GuildSimulationController>();
            Button modeButton = GameObject.Find("Management Mode").GetComponent<Button>();
            Button advanceButton = GameObject.Find("Advance Turn").GetComponent<Button>();
            Button applyButton = GameObject.Find("Apply Turn Plan").GetComponent<Button>();
            Dropdown expeditionDropdown =
                GameObject.Find("Expedition Selection").GetComponent<Dropdown>();
            Text status = GameObject.Find("Planning Status").GetComponent<Text>();

            modeButton.onClick.Invoke();
            yield return null;
            Assert.That(controller.IsManualMode, Is.True);

            advanceButton.onClick.Invoke();
            yield return null;
            Assert.That(controller.FlowController.State,
                Is.EqualTo(SimulationFlowState.PlanningTurn));
            Assert.That(expeditionDropdown.interactable, Is.True);
            Assert.That(expeditionDropdown.options, Has.Count.GreaterThan(0));
            Assert.That(status.text, Does.Contain("遠征"));

            applyButton.onClick.Invoke();
            yield return null;

            Assert.That(controller.Guild.CurrentTurn, Is.EqualTo(1));
            Assert.That(controller.Guild.Expeditions, Has.Count.EqualTo(1));
            Assert.That(controller.FlowController.State,
                Is.EqualTo(SimulationFlowState.Ready));
            Assert.That(controller.LastError, Is.Empty);

            advanceButton.onClick.Invoke();
            yield return null;
            Dropdown defenseDropdown =
                GameObject.Find("Defense Selection").GetComponent<Dropdown>();
            Assert.That(defenseDropdown.interactable, Is.True);
            string selectedDefender = defenseDropdown.options[defenseDropdown.value].text;

            applyButton.onClick.Invoke();
            yield return null;

            GameObject decisionPanel = GameObject.Find("Expedition Decision Panel");
            Assert.That(decisionPanel, Is.Not.Null);
            Assert.That(decisionPanel.activeSelf, Is.True);
            Text decisionDetails = GameObject.Find("Decision Details").GetComponent<Text>();
            Assert.That(decisionDetails.text, Does.Contain("戦闘結果"));
            Assert.That(decisionDetails.text, Does.Contain("今回の獲得資金"));
            Assert.That(decisionDetails.text, Does.Contain("参加者HP"));
            Assert.That(controller.FlowController.State,
                Is.EqualTo(SimulationFlowState.WaitingForExpeditionDecision));
            Assert.That(advanceButton.interactable, Is.False);
            Assert.That(modeButton.interactable, Is.False);

            GameObject.Find("Continue Expedition").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(controller.Guild.CurrentTurn, Is.EqualTo(2));
            Assert.That(controller.LastAdvanceResult.TurnResult.DefenseResult, Is.Not.Null);
            Assert.That(
                controller.LastAdvanceResult.TurnResult.DefenseResult.DefenderIds,
                Does.Contain(selectedDefender));
        }
    }
}
