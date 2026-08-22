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
            Text status = GameObject.Find("Planning Status").GetComponent<Text>();

            modeButton.onClick.Invoke();
            yield return null;
            Assert.That(controller.IsManualMode, Is.True);
            Assert.That(
                advanceButton.GetComponentInChildren<Text>().text,
                Is.EqualTo("ターン計画を始める"));

            advanceButton.onClick.Invoke();
            yield return null;
            Assert.That(controller.FlowController.State,
                Is.EqualTo(SimulationFlowState.PlanningTurn));
            Assert.That(GameObject.Find("Manual Planning Panel").activeSelf, Is.True);
            Assert.That(
                applyButton.GetComponentInChildren<Text>().text,
                Is.EqualTo("この計画でターンを実行"));
            GameObject.Find("Cancel Turn Plan").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(controller.FlowController.State, Is.EqualTo(SimulationFlowState.Ready));
            Assert.That(controller.Guild.CurrentTurn, Is.Zero);

            advanceButton.onClick.Invoke();
            yield return null;
            Transform expeditionList = GameObject.Find("Expedition Member List").transform;
            Assert.That(expeditionList.childCount, Is.EqualTo(4));
            for (int index = 0; index < 3; index++)
                expeditionList.GetChild(index).GetComponent<Toggle>().isOn = true;
            Assert.That(status.text, Does.Contain("複数選択"));
            Assert.That(
                GameObject.Find("Expedition Count").GetComponent<Text>().text,
                Does.Contain("3/3人"));

            applyButton.onClick.Invoke();
            yield return null;

            Assert.That(controller.Guild.CurrentTurn, Is.EqualTo(1));
            Assert.That(controller.Guild.Expeditions, Has.Count.EqualTo(1));
            Assert.That(controller.Guild.Expeditions[0].ParticipantIds, Has.Count.EqualTo(3));
            Assert.That(controller.FlowController.State,
                Is.EqualTo(SimulationFlowState.Ready));
            Assert.That(controller.LastError, Is.Empty);

            advanceButton.onClick.Invoke();
            yield return null;
            Transform defenseList = GameObject.Find("Defense Member List").transform;
            Toggle selectedDefenseToggle = null;
            for (int index = 0; index < defenseList.childCount; index++)
            {
                Toggle candidate = defenseList.GetChild(index).GetComponent<Toggle>();
                if (candidate.interactable)
                {
                    selectedDefenseToggle = candidate;
                    candidate.isOn = true;
                    break;
                }
            }
            Assert.That(selectedDefenseToggle, Is.Not.Null);
            string selectedDefender = selectedDefenseToggle.gameObject.name;

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
