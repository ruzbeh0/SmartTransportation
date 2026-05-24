using Colossal.Logging;
using Colossal.UI.Binding;
using Game.UI;
using SmartTransportation.Bridge;
using SmartTransportation.Extensions;
using System;
using Colossal;
using UnityColor = UnityEngine.Color;

namespace SmartTransportation.Systems
{
    public partial class AddCustomRuleUISystem : ExtendedUISystemBase
    {
        private ILog _log;

        protected override void OnCreate()
        {
            base.OnCreate();

            _log = LogManager.GetLogger(
                $"{nameof(SmartTransportation)}.{nameof(AddCustomRuleUISystem)}");

            // Trigger from AddCustomRulePanel.tsx
            AddBinding(new TriggerBinding<AddCustomRule>(
                "smartTransportation",
                "addCustomRule",
                AddCustomRuleFromUI,
                new GenericUIReader<AddCustomRule>()
            ));

            AddBinding(new TriggerBinding<EditCustomRule>(
                "smartTransportation",
                "editCustomRule",
                EditCustomRuleFromUI,
                new GenericUIReader<EditCustomRule>()
            ));
        }

        // Shape of the JSON payload sent from AddCustomRulePanel.tsx
        public class AddCustomRule
        {
            public string ruleName { get; set; } = string.Empty;
            public int occupancy { get; set; }
            public int stdTicket { get; set; } = 10;
            public int maxTicketInc { get; set; }
            public int maxTicketDec { get; set; }
            public int maxVehAdj { get; set; }
            public int minVehAdj { get; set; }
            public UnityColor routeColor { get; set; } = SmartTransportation.Components.CustomRule.DefaultRouteColor;
        }

        public class EditCustomRule : AddCustomRule
        {
            public string ruleId { get; set; } = string.Empty;
        }

        private void AddCustomRuleFromUI(AddCustomRule dto)
        {
            try
            {
                Hash128 ruleId = ManageRouteBridge.AddCustomRule();
                ManageRouteBridge.SetCustomRule(
                    ruleId,
                    dto.ruleName ?? string.Empty,
                    dto.occupancy,
                    dto.stdTicket,
                    dto.maxTicketInc,
                    dto.maxTicketDec,
                    dto.maxVehAdj,
                    dto.minVehAdj,
                    dto.routeColor
                );
            }
            catch (Exception ex)
            {
                _log?.Error(ex, $"Error in {nameof(AddCustomRuleFromUI)}");
            }
            
        }

        private void EditCustomRuleFromUI(EditCustomRule dto)
        {
            try
            {
                if (dto == null)
                {
                    _log?.Warn($"{nameof(EditCustomRuleFromUI)} called with null dto.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(dto.ruleId))
                {
                    _log?.Warn($"{nameof(EditCustomRuleFromUI)} called with empty ruleId.");
                    return;
                }

                Hash128 ruleId;
                try
                {
                    ruleId = new Hash128(dto.ruleId);
                }
                catch (Exception ex)
                {
                    _log?.Warn(ex, $"{nameof(EditCustomRuleFromUI)} invalid ruleId: {dto.ruleId}");
                    return;
                }

                // Do not allow editing built-in/default rules like Bus, Tram, Disabled, etc.
                // Those are synced from settings and should be changed from the settings page instead.
                if (ManageRouteSystem.RuleNames.ContainsKey(ruleId))
                {
                    _log?.Warn($"{nameof(EditCustomRuleFromUI)} refused to edit built-in rule: {dto.ruleId}");
                    return;
                }

                var existing = ManageRouteBridge.GetCustomRule(ruleId);
                if (!existing.ruleId.Equals(ruleId))
                {
                    _log?.Warn($"{nameof(EditCustomRuleFromUI)} rule not found: {dto.ruleId}");
                    return;
                }

                ManageRouteBridge.SetCustomRule(
                    ruleId,
                    dto.ruleName ?? string.Empty,
                    dto.occupancy,
                    dto.stdTicket,
                    dto.maxTicketInc,
                    dto.maxTicketDec,
                    dto.maxVehAdj,
                    dto.minVehAdj,
                    dto.routeColor
                );
            }
            catch (Exception ex)
            {
                _log?.Error(ex, $"Error in {nameof(EditCustomRuleFromUI)}");
            }
        }
    }
}
