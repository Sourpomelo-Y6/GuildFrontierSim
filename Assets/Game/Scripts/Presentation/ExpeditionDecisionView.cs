using System.Text;
using GuildFrontierSim.Application.Processing.Expeditions.Stages;
using GuildFrontierSim.Application.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace GuildFrontierSim.Presentation
{
    public sealed class ExpeditionDecisionView : MonoBehaviour
    {
        [SerializeField] private GuildSimulationController controller;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text detailsText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button returnButton;
        [SerializeField] private Button delegateButton;

        private void Start()
        {
            continueButton.onClick.AddListener(OnContinue);
            returnButton.onClick.AddListener(OnReturn);
            delegateButton.onClick.AddListener(OnDelegate);
            controller.ManualPlanningChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinue);
            if (returnButton != null) returnButton.onClick.RemoveListener(OnReturn);
            if (delegateButton != null) delegateButton.onClick.RemoveListener(OnDelegate);
            if (controller != null) controller.ManualPlanningChanged -= Refresh;
        }

        public void Refresh()
        {
            PendingExpeditionDecision pending = controller?.FlowController?
                .PendingExpeditionDecision;
            bool visible = controller != null &&
                controller.FlowController != null &&
                controller.FlowController.State ==
                    SimulationFlowState.WaitingForExpeditionDecision &&
                pending != null;
            panel.SetActive(visible);
            if (!visible) return;

            detailsText.text = BuildDetails(pending);
            continueButton.interactable = true;
            returnButton.interactable = true;
            delegateButton.interactable = true;
        }

        private void OnContinue() => Apply(ExpeditionDecision.Continue);
        private void OnReturn() => Apply(ExpeditionDecision.Return);
        private void OnDelegate() => Apply(ExpeditionDecision.DelegateToCpu);

        private void Apply(ExpeditionDecision decision)
        {
            continueButton.interactable = false;
            returnButton.interactable = false;
            delegateButton.interactable = false;
            controller.ApplyManualExpeditionDecision(decision);
            Refresh();
        }

        private static string BuildDetails(PendingExpeditionDecision pending)
        {
            var builder = new StringBuilder();
            builder.Append("遠征判断: ").Append(pending.ExpeditionId)
                .Append("  Stage ").Append(pending.StageNumber).AppendLine()
                .Append("戦闘結果: ").Append(pending.BattleResult.Outcome).AppendLine()
                .Append("今回の獲得資金: ").Append(pending.StageReward).AppendLine()
                .Append("一時戦利品資金: ").Append(pending.TemporaryFunds).AppendLine();
            if (!string.IsNullOrEmpty(pending.RescuedCharacterId))
                builder.Append("救出: ").Append(pending.RescuedCharacterId).AppendLine();
            builder.AppendLine("参加者HP");
            for (int index = 0; index < pending.ParticipantHitPoints.Count; index++)
            {
                ExpeditionParticipantHp hp = pending.ParticipantHitPoints[index];
                builder.Append(hp.CharacterId).Append("  ")
                    .Append(hp.CurrentHp).Append('/').Append(hp.MaximumHp).AppendLine();
            }
            return builder.ToString();
        }
    }
}
