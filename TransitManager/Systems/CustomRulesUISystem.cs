using System;
using Colossal;
using Colossal.Logging;
using Colossal.UI.Binding;
using SmartTransportation.Bridge;
using SmartTransportation.Domain;
using SmartTransportation.Extensions;


namespace SmartTransportation.Systems
{
    public partial class CustomRulesUISystem : ExtendedUISystemBase
    {
        private ILog _log;

        private CustomRules _customRules = new();
        private TriggerBinding<string> _deleteCustomRuleBinding;

        protected override void OnCreate()
        {
            base.OnCreate();

            _log = LogManager.GetLogger(
                $"{nameof(SmartTransportation)}.{nameof(CustomRulesUISystem)}");

            // Custom rules JSON binding
            AddBinding(new RawValueBinding(
                "smartTransportation",
                "customRulesJson",
                GetCustomRulesJson
            ));
            AddBinding(new RawValueBinding(
                "smartTransportation",
                "transportVehicleOptionsJson",
                WriteTransportVehicleOptions
            ));
            AddBinding(_deleteCustomRuleBinding = new TriggerBinding<string>(
                Mod.modName,
                "deleteCustomRule",
                DeleteCustomRule
            ));
            
        }
        private void GetCustomRulesJson(IJsonWriter writer)
        {
            _customRules.Clear();
            var rules = ManageRouteBridge.GetCustomRuleDetails();
            var settings = Mod.m_Setting;
            
            foreach(var rule in rules) 
            {
                // Skip disabled transport type rules based on settings
                var ruleName = rule.ruleName;
                
                if (ruleName == "Bus" && settings.disable_bus) continue;
                if (ruleName == "Tram" && settings.disable_Tram) continue;
                if (ruleName == "Subway" && settings.disable_Subway) continue;
                if (ruleName == "Train" && settings.disable_Train) continue;
                if (ruleName == "Ship" && settings.disable_Ship) continue;
                if (ruleName == "Airplane" && settings.disable_Airplane) continue;
                if (ruleName == "Ferry" && settings.disable_Ferry) continue;
                
                _customRules.Add(new CustomRule(
                    rule.ruleId, 
                    rule.ruleName, 
                    rule.occupancy, 
                    rule.stdTicket, 
                    rule.maxTicketInc, 
                    rule.maxTicketDec, 
                    rule.maxVehAdj, 
                    rule.minVehAdj,
                    rule.adjustVehicles,
                    rule.routeColor,
                    rule.useRouteColor,
                    rule.transportType == Game.Prefabs.TransportType.None ? "NotSpecified" : rule.transportType.ToString(),
                    rule.useVehicleModels,
                    rule.useVehicleColors,
                    rule.vehicleColor0,
                    rule.vehicleColor1,
                    rule.vehicleColor2,
                    rule.useRouteNaming,
                    rule.sequentialRouteNaming,
                    rule.routeNamePrefix,
                    rule.selectedPrimaryVehicles,
                    rule.selectedSecondaryVehicles
                ));
            }   
            _customRules.Write(writer);
        }

        private void WriteTransportVehicleOptions(IJsonWriter writer)
        {
            var options = ManageRouteBridge.GetTransportVehicleOptions();

            writer.ArrayBegin(options.Length);
            foreach (var option in options)
            {
                writer.TypeBegin(Mod.Name + ".TransportVehicleOptions");
                writer.PropertyName("transportType");
                writer.Write(option.transportType);
                writer.PropertyName("availablePrimaryVehicles");
                WriteVehicles(writer, option.availablePrimaryVehicles);
                writer.PropertyName("availableSecondaryVehicles");
                WriteVehicles(writer, option.availableSecondaryVehicles);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private static void WriteVehicles(IJsonWriter writer, ManageRouteSystem.VehiclePrefabInfo[] vehicles)
        {
            writer.ArrayBegin(vehicles.Length);
            foreach (var vehicle in vehicles)
            {
                writer.TypeBegin(Mod.Name + ".VehiclePrefabInfo");
                writer.PropertyName("entity");
                writer.Write(vehicle.entity);
                writer.PropertyName("id");
                writer.Write(vehicle.id ?? string.Empty);
                writer.PropertyName("locked");
                writer.Write(vehicle.locked);
                writer.PropertyName("multiunit");
                writer.Write(vehicle.multiunit);
                writer.PropertyName("requirements");
                writer.ArrayBegin(0);
                writer.ArrayEnd();
                writer.PropertyName("thumbnail");
                writer.Write(vehicle.thumbnail ?? string.Empty);
                writer.PropertyName("objectRequirementIcons");
                writer.WriteNull();
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private void DeleteCustomRule(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                _log?.Warn("DeleteCustomRule: empty id");
                return;
            }

            // Look up the rule in the current list
            var rule = _customRules.Find(r => r.RuleId.ToString() == ruleId);
            if (rule == null)
            {
                _log?.Warn($"DeleteCustomRule: rule '{ruleId}' not found");
                return;
            }

            // Protect built-in rules (Bus, Tram, Train, etc.)
            if (rule.RuleName is "Bus"
                or "Tram"
                or "Train"
                or "Subway"
                or "Ship"
                or "Airplane" 
                or "Ferry")
            {
                _log?.Info($"DeleteCustomRule: ignoring delete for built-in rule '{rule.RuleName}'");
                return;
            }

            try
            {
                var parsedId = new Hash128(ruleId);
                ManageRouteBridge.RemoveCustomRule(parsedId);
            }
            catch (Exception ex)
            {
                _log?.Warn(ex, $"DeleteCustomRule: invalid id '{ruleId}'");
                return;
            }

            var index = _customRules.FindIndex(r => r.RuleId.ToString() == ruleId);
            if (index >= 0)
            {
                _customRules.RemoveAt(index);
            }
        }

    }
}
