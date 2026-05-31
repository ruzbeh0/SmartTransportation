
﻿using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Common;
using Game;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.SceneFlow;
using Game.Settings;
using Game.UI;
using Game.UI.Localization;
using SmartTransportation.Components;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityColor = UnityEngine.Color;
using UnityColor32 = UnityEngine.Color32;
using static Unity.Collections.Unicode;
using Colossal.Serialization.Entities;


namespace SmartTransportation.Bridge
{
    public partial class ManageRouteSystem : GameSystemBase
    {
        private EntityQuery entityQuery;
        private EntityQuery transportVehiclePrefabQuery;
        private EntityQuery routePrefabQuery;
        private PrefabSystem m_PrefabSystem;
        private ImageSystem m_ImageSystem;
        private const int disabled_int_id = 999; // Used for disabled routes
        private bool firstUpdate = false;

        public static readonly Dictionary<Colossal.Hash128, string> RuleNames = new()
        {
            { new Colossal.Hash128((uint)disabled_int_id,0,0,0), "Disabled" },
            { new Colossal.Hash128((uint)TransportType.Bus,0,0,0), TransportType.Bus.ToString()},
            { new Colossal.Hash128((uint)TransportType.Train,0,0,0), TransportType.Train.ToString()},
            { new Colossal.Hash128((uint)TransportType.Tram,0,0,0), TransportType.Tram.ToString()},
            { new Colossal.Hash128((uint)TransportType.Subway,0,0,0), TransportType.Subway.ToString()},
            { new Colossal.Hash128((uint)TransportType.Ship,0,0,0), TransportType.Ship.ToString()},
            { new Colossal.Hash128((uint)TransportType.Airplane,0,0,0), TransportType.Airplane.ToString()},
            { new Colossal.Hash128((uint)TransportType.Ferry,0,0,0), TransportType.Ferry.ToString()},
        };

        private static readonly TransportType[] SupportedTransportTypes =
        {
            TransportType.Bus,
            TransportType.Tram,
            TransportType.Subway,
            TransportType.Train,
            TransportType.Ship,
            TransportType.Airplane,
            TransportType.Ferry
        };

        public struct CustomRuleInfo
        {
            public Colossal.Hash128 ruleId;
            public string ruleName;
            public int occupancy;
            public int stdTicket;
            public int maxTicketInc;
            public int maxTicketDec;
            public int maxVehAdj;
            public int minVehAdj;
            public bool adjustVehicles;
            public UnityColor routeColor;
            public bool useRouteColor;
            public TransportType transportType;
            public bool useVehicleModels;
            public bool useVehicleColors;
            public UnityColor vehicleColor0;
            public UnityColor vehicleColor1;
            public UnityColor vehicleColor2;
            public bool useRouteNaming;
            public bool sequentialRouteNaming;
            public string routeNamePrefix;
            public Entity[] selectedPrimaryVehicles;
            public Entity[] selectedSecondaryVehicles;
        }

        public struct VehiclePrefabInfo
        {
            public Entity entity;
            public string id;
            public bool locked;
            public bool multiunit;
            public string thumbnail;
        }

        public struct TransportVehicleOptions
        {
            public string transportType;
            public VehiclePrefabInfo[] availablePrimaryVehicles;
            public VehiclePrefabInfo[] availableSecondaryVehicles;
        }


        protected override void OnCreate()
        {
            base.OnCreate();

            entityQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
            ComponentType.ReadOnly<RouteNumber>(),
            ComponentType.ReadOnly<TransportLine>(),
            ComponentType.ReadOnly<PrefabRef>()
                }
            });

            transportVehiclePrefabQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadOnly<PrefabData>(),
                    ComponentType.ReadOnly<PublicTransportVehicleData>()
                }
            });

            routePrefabQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[] {
                    ComponentType.ReadOnly<PrefabData>(),
                    ComponentType.ReadOnly<TransportLineData>(),
                    ComponentType.ReadOnly<RouteData>()
                }
            });

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();

            RequireForUpdate(entityQuery);
        }

        private static bool IsSmartTransportationSupportedRoute(TransportLineData transportLineData)
        {
            if (!transportLineData.m_PassengerTransport)
                return false;

            return transportLineData.m_TransportType switch
            {
                TransportType.Bus => true,
                TransportType.Tram => true,
                TransportType.Subway => true,
                TransportType.Train => true,
                TransportType.Ship => true,
                TransportType.Airplane => true,
                TransportType.Ferry => true,
                _ => false
            };
        }

        private static bool IsSupportedTransportType(TransportType transportType)
        {
            return transportType switch
            {
                TransportType.Bus => true,
                TransportType.Tram => true,
                TransportType.Subway => true,
                TransportType.Train => true,
                TransportType.Ship => true,
                TransportType.Airplane => true,
                TransportType.Ferry => true,
                _ => false
            };
        }

        private static TransportType NormalizeRuleTransportType(TransportType transportType)
        {
            return IsSupportedTransportType(transportType)
                ? transportType
                : CustomRule.UnspecifiedTransportType;
        }

        private static TransportType ParseRuleTransportType(string transportType)
        {
            if (string.IsNullOrWhiteSpace(transportType) ||
                string.Equals(transportType, "Not Specified", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(transportType, "NotSpecified", StringComparison.OrdinalIgnoreCase))
            {
                return CustomRule.UnspecifiedTransportType;
            }

            return Enum.TryParse(transportType, true, out TransportType parsed)
                ? NormalizeRuleTransportType(parsed)
                : CustomRule.UnspecifiedTransportType;
        }

        private static TransportType GetBuiltInRuleTransportType(Colossal.Hash128 ruleId)
        {
            foreach (var transportType in SupportedTransportTypes)
            {
                if (ruleId == new Colossal.Hash128((uint)transportType, 0, 0, 0))
                    return transportType;
            }

            return CustomRule.UnspecifiedTransportType;
        }

        private UnityColor GetDefaultRouteColor(TransportType transportType)
        {
            if (!IsSupportedTransportType(transportType) || routePrefabQuery == null)
                return CustomRule.DefaultRouteColor;

            using var entities = routePrefabQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                var transportLineData = EntityManager.GetComponentData<TransportLineData>(entity);
                if (!IsSmartTransportationSupportedRoute(transportLineData) ||
                    transportLineData.m_TransportType != transportType)
                {
                    continue;
                }

                var routeData = EntityManager.GetComponentData<RouteData>(entity);
                UnityColor routeColor = routeData.m_Color;
                return NormalizeColor(routeColor);
            }

            return CustomRule.DefaultRouteColor;
        }

        private static bool RuleAppliesToTransport(CustomRule rule, TransportType transportType)
        {
            return rule.transportType == CustomRule.UnspecifiedTransportType ||
                   rule.transportType == transportType;
        }

        public bool CustomRuleAppliesToTransport(Colossal.Hash128 ruleId, TransportType transportType)
        {
            if (RuleNames.ContainsKey(ruleId))
            {
                var builtInTransportType = GetBuiltInRuleTransportType(ruleId);
                return builtInTransportType == CustomRule.UnspecifiedTransportType ||
                       builtInTransportType == transportType;
            }

            return TryGetCustomRuleEntity(ruleId, out _, out var rule) &&
                   RuleAppliesToTransport(rule, transportType);
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);

            RemoveDuplicateCustomRuleEntities();
            SyncDefaultRulesFromSettings();
            ApplyAllCustomRuleColors();
            ApplyAllCustomRuleVehicleModels();
            ApplyAllCustomRuleVehicleColors();
            ApplyAllCustomRuleRouteNames();

            // This system only needs to run on load.
            firstUpdate = true;
            Enabled = false;
        }

        private void RemoveDuplicateCustomRuleEntities()
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CustomRule>());
            using var entities = query.ToEntityArray(Allocator.Temp);
            using var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            var seen = new HashSet<Colossal.Hash128>();

            // Destroy any duplicate entities with the same ruleId.
            for (int i = 0; i < rules.Length; i++)
            {
                var id = rules[i].ruleId;
                if (seen.Add(id))
                    continue;

                EntityManager.DestroyEntity(entities[i]);
            }
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);

            // We’re about to load/start a city. Clear any rules from the previously loaded city.
            ClearAllCustomRules();

            // Ensure we resync defaults for the next city after it finishes loading.
            firstUpdate = false;
            Enabled = true;
        }

        private void ClearAllCustomRules()
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SmartTransportation.Components.CustomRule>());
            EntityManager.DestroyEntity(query);

            Mod.log.Info("[ManageRouteSystem] Cleared CustomRule entities on GamePreload.");
        }


        private void SyncDefaultRulesFromSettings()
        {
            foreach (var (ruleId, ruleName) in RuleNames)
            {
                int occ = 0, ticket = 0, inc = 0, dec = 0, maxAdj = 0, minAdj = 0;

                if (ruleName == "Disabled")
                {
                    // All values remain zero
                }
                else if (ruleName == "Bus")
                {
                    occ = Mod.m_Setting.target_occupancy_bus;
                    ticket = Mod.m_Setting.standard_ticket_bus;
                    inc = Mod.m_Setting.max_ticket_increase_bus;
                    dec = Mod.m_Setting.max_ticket_discount_bus;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_bus;
                    minAdj = Mod.m_Setting.min_vahicles_adj_bus;
                }
                else if (ruleName == "Tram")
                {
                    occ = Mod.m_Setting.target_occupancy_Tram;
                    ticket = Mod.m_Setting.standard_ticket_Tram;
                    inc = Mod.m_Setting.max_ticket_increase_Tram;
                    dec = Mod.m_Setting.max_ticket_discount_Tram;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Tram;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Tram;
                }
                else if (ruleName == "Subway")
                {
                    occ = Mod.m_Setting.target_occupancy_Subway;
                    ticket = Mod.m_Setting.standard_ticket_Subway;
                    inc = Mod.m_Setting.max_ticket_increase_Subway;
                    dec = Mod.m_Setting.max_ticket_discount_Subway;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Subway;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Subway;
                }
                else if (ruleName == "Train")
                {
                    occ = Mod.m_Setting.target_occupancy_Train;
                    ticket = Mod.m_Setting.standard_ticket_Train;
                    inc = Mod.m_Setting.max_ticket_increase_Train;
                    dec = Mod.m_Setting.max_ticket_discount_Train;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Train;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Train;
                }
                else if (ruleName == "Ship")
                {
                    occ = Mod.m_Setting.target_occupancy_Ship;
                    ticket = Mod.m_Setting.standard_ticket_Ship;
                    inc = Mod.m_Setting.max_ticket_increase_Ship;
                    dec = Mod.m_Setting.max_ticket_discount_Ship;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Ship;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Ship;
                }
                else if (ruleName == "Airplane")
                {
                    occ = Mod.m_Setting.target_occupancy_Airplane;
                    ticket = Mod.m_Setting.standard_ticket_Airplane;
                    inc = Mod.m_Setting.max_ticket_increase_Airplane;
                    dec = Mod.m_Setting.max_ticket_discount_Airplane;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Airplane;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Airplane;
                }
                else if (ruleName == "Ferry")
                {
                    occ = Mod.m_Setting.target_occupancy_Ferry;
                    ticket = Mod.m_Setting.standard_ticket_Ferry;
                    inc = Mod.m_Setting.max_ticket_increase_Ferry;
                    dec = Mod.m_Setting.max_ticket_discount_Ferry;
                    maxAdj = Mod.m_Setting.max_vahicles_adj_Ferry;
                    minAdj = Mod.m_Setting.min_vahicles_adj_Ferry;
                }
                else
                {
                    continue; // Unknown built-in name
                }

                // Check if the rule already exists
                var (_, existingName, _, _, _, _, _, _) = GetCustomRule(ruleId);
                var ruleTransportType = GetBuiltInRuleTransportType(ruleId);
                var defaultRouteColor = GetDefaultRouteColor(ruleTransportType);
                if (!string.IsNullOrEmpty(existingName))
                {
                    GetCustomRuleVehicleSelections(ruleId, out var selectedPrimaryVehicles, out var selectedSecondaryVehicles);
                    GetCustomRuleCosmeticSettings(
                        ruleId,
                        out var useRouteColor,
                        out var useVehicleModels,
                        out var useVehicleColors,
                        out var vehicleColor0,
                        out var vehicleColor1,
                        out var vehicleColor2);
                    GetCustomRuleRouteNamingSettings(ruleId, out var useRouteNaming, out var sequentialRouteNaming, out var routeNamePrefix);
                    var adjustVehicles = GetCustomRuleAdjustVehiclesOrDefault(ruleId);
                    var routeColor = useRouteColor
                        ? GetCustomRuleColorOrDefault(ruleId)
                        : defaultRouteColor;

                    // Update
                    SetCustomRule(
                        ruleId,
                        ruleName,
                        occ,
                        ticket,
                        inc,
                        dec,
                        maxAdj,
                        minAdj,
                        adjustVehicles,
                        routeColor,
                        ruleTransportType,
                        useRouteColor,
                        useVehicleModels,
                        useVehicleColors,
                        vehicleColor0,
                        vehicleColor1,
                        vehicleColor2,
                        useRouteNaming,
                        sequentialRouteNaming,
                        routeNamePrefix,
                        selectedPrimaryVehicles,
                        selectedSecondaryVehicles);
                }
                else
                {
                    // Create
                    Entity entity = EntityManager.CreateEntity(typeof(CustomRule));
                    var rule = new CustomRule(ruleId, ruleName, occ, ticket, inc, dec, maxAdj, minAdj, defaultRouteColor, ruleTransportType);

                    EntityManager.SetComponentData(entity, rule);
                }
            }

            Mod.log.Info("[ManageRouteSystem] Synced all default + disabled route rules.");
        }


        private Entity GetRouteEntityFromId(int routeId, TransportType transportType)
        {
            var entities = entityQuery.ToEntityArray(Allocator.Temp);
            //Mod.log.Info($"Entities: {entities.Length}");
            try
            {
                foreach (var ent in entities)
                {
                    PrefabRef prefab = EntityManager.GetComponentData<PrefabRef>(ent);
                    TransportLine transportLine = EntityManager.GetComponentData<TransportLine>(ent);
                    RouteNumber routeNumber = EntityManager.GetComponentData<RouteNumber>(ent);
                    TransportLineData transportLineData = EntityManager.GetComponentData<TransportLineData>(prefab.m_Prefab);

                    if (routeNumber.m_Number == routeId &&
                        transportLineData.m_TransportType == transportType)
                    {
                        return ent;
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }

            return Entity.Null; // Not found
        }

        public void SetRouteRule(Entity routeEntity, Colossal.Hash128 routeRuleId)
        {
            var routeRule = new RouteRule(routeRuleId);

            //Mod.log.Info($"RouteEntity: {routeEntity}");
            if (EntityManager.HasComponent<RouteRule>(routeEntity))
            {
                EntityManager.SetComponentData(routeEntity, routeRule);
            }
            else
            {
                EntityManager.AddComponentData(routeEntity, routeRule);
            }
        }

        public (Colossal.Hash128, string) GetRouteRule(Entity routeEntity)
        {
            Colossal.Hash128 ruleId = default;
            // First, try to get a custom rule from the RouteRule component
            if (EntityManager.TryGetComponent<RouteRule>(routeEntity, out RouteRule routeRule))
            {
                ruleId = routeRule.customRule;

                if (!RuleNames.ContainsKey(ruleId) &&
                    EntityManager.TryGetComponent<PrefabRef>(routeEntity, out var assignedPrefab) &&
                    EntityManager.HasComponent<TransportLineData>(assignedPrefab.m_Prefab))
                {
                    var assignedTransportLineData = EntityManager.GetComponentData<TransportLineData>(assignedPrefab.m_Prefab);
                    if (TryGetCustomRuleEntity(ruleId, out _, out var assignedRule) &&
                        !RuleAppliesToTransport(assignedRule, assignedTransportLineData.m_TransportType))
                    {
                        EntityManager.RemoveComponent<RouteRule>(routeEntity);
                        return GetRouteRule(routeEntity);
                    }
                }
            }
            else
            {
                // Try to get prefab info for transport type fallback
                if (EntityManager.TryGetComponent<PrefabRef>(routeEntity, out PrefabRef prefab))
                {
                    var transportLineData = EntityManager.GetComponentData<TransportLineData>(prefab.m_Prefab);
                    TransportType transportType = transportLineData.m_TransportType;

                    if (!IsSmartTransportationSupportedRoute(transportLineData))
                        return default;

                    // Check if this transport type is disabled in settings
                    bool isDisabled = transportType switch
                    {
                        TransportType.Bus => Mod.m_Setting.disable_bus,
                        TransportType.Tram => Mod.m_Setting.disable_Tram,
                        TransportType.Subway => Mod.m_Setting.disable_Subway,
                        TransportType.Train => Mod.m_Setting.disable_Train,
                        TransportType.Ship => Mod.m_Setting.disable_Ship,
                        TransportType.Airplane => Mod.m_Setting.disable_Airplane,
                        TransportType.Ferry => Mod.m_Setting.disable_Ferry,
                        _ => true
                    };


                    if (!isDisabled)
                    {
                        Colossal.Hash128 defaultId = new Colossal.Hash128((uint)transportType, 0, 0, 0);
                        string defaultName = RuleNames.TryGetValue(defaultId, out var name) ? name : transportType.ToString();

                        return (defaultId, defaultName);
                    }
                }
            }

            // 1) Built-in rules first
            if (RuleNames.TryGetValue(ruleId, out var ruleName))
            {
                return (ruleId, ruleName);
            }

            // 2) Try to resolve as a custom rule
            var custom = GetCustomRule(ruleId);
            if (!string.IsNullOrEmpty(custom.Item2)) // Item2 = ruleName
            {
                return (custom.ruleId, custom.Item2);
            }

            return default;

        }



        public (Colossal.Hash128, string)[] GetRouteRules(Entity routeEntity)
        {
            if (!EntityManager.TryGetComponent<PrefabRef>(routeEntity, out PrefabRef prefab))
                return Array.Empty<(Colossal.Hash128, string)>(); // Invalid route, return empty

            if (!EntityManager.HasComponent<TransportLineData>(prefab.m_Prefab))
                return Array.Empty<(Colossal.Hash128, string)>(); // No transport data

            var transportLineData = EntityManager.GetComponentData<TransportLineData>(prefab.m_Prefab);
            var transportType = transportLineData.m_TransportType;

            if (!IsSmartTransportationSupportedRoute(transportLineData))
                return Array.Empty<(Colossal.Hash128, string)>();

            // Check if this transport type is disabled in the mod settings
            bool isDisabled = transportType switch
            {
                TransportType.Bus => Mod.m_Setting.disable_bus,
                TransportType.Tram => Mod.m_Setting.disable_Tram,
                TransportType.Subway => Mod.m_Setting.disable_Subway,
                TransportType.Train => Mod.m_Setting.disable_Train,
                TransportType.Ship => Mod.m_Setting.disable_Ship,
                TransportType.Airplane => Mod.m_Setting.disable_Airplane,
                TransportType.Ferry => Mod.m_Setting.disable_Ferry,
                _ => true
            };

            var result = new List<(Colossal.Hash128, string)>();

            // If transport type is disabled, only return the "Disabled" option
            if (isDisabled)
            {
                var disabledId = new Colossal.Hash128((uint)disabled_int_id, 0, 0, 0);
                if (RuleNames.TryGetValue(disabledId, out var disabledName))
                {
                    result.Add((disabledId, disabledName));
                }
                return result.ToArray();
            }

            // 1. Add built-in rule for the transport type
            var defaultId = new Colossal.Hash128((uint)transportType, 0, 0, 0);
            if (RuleNames.TryGetValue(defaultId, out var defaultName))
            {
                result.Add((defaultId, defaultName));
            }

            // 2. Add custom rules (excluding built-in ones from RuleNames)
            var customRules = GetCustomRuleDetails();
            foreach (var rule in customRules)
            {
                if (RuleNames.ContainsKey(rule.ruleId))
                    continue; // Skip built-in rule

                if (rule.transportType != CustomRule.UnspecifiedTransportType && rule.transportType != transportType)
                    continue;

                result.Add((rule.ruleId, rule.ruleName));
            }

            return result.ToArray();
        }


        public (Colossal.Hash128 ruleId, string, int, int, int, int, int, int) GetCustomRule(Colossal.Hash128 ruleId)
        {
            if (TryGetCustomRuleEntity(ruleId, out _, out var rule))
            {
                return (rule.ruleId, rule.ruleName.ToString(), rule.occupancy, rule.stdTicket, rule.maxTicketInc, rule.maxTicketDec, rule.maxVehAdj, rule.minVehAdj);
            }

            return default;
        }

        public CustomRuleInfo GetCustomRuleDetails(Colossal.Hash128 ruleId)
        {
            if (TryGetCustomRuleEntity(ruleId, out var entity, out var rule))
                return CreateCustomRuleInfo(entity, rule);

            return default;
        }

        public void SetCustomRule(Colossal.Hash128 ruleId, FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj)
        {
            SetCustomRule(ruleId, ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj, GetCustomRuleColorOrDefault(ruleId));
        }

        public void SetCustomRule(Colossal.Hash128 ruleId, FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj, UnityColor routeColor)
        {
            var transportType = GetCustomRuleTransportTypeOrDefault(ruleId);
            GetCustomRuleVehicleSelections(ruleId, out var selectedPrimaryVehicles, out var selectedSecondaryVehicles);
            GetCustomRuleCosmeticSettings(ruleId, out var useRouteColor, out var useVehicleModels, out var useVehicleColors, out var vehicleColor0, out var vehicleColor1, out var vehicleColor2);
            GetCustomRuleRouteNamingSettings(ruleId, out var useRouteNaming, out var sequentialRouteNaming, out var routeNamePrefix);
            var adjustVehicles = GetCustomRuleAdjustVehiclesOrDefault(ruleId);
            SetCustomRule(ruleId, ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj, adjustVehicles, routeColor, transportType, useRouteColor, useVehicleModels, useVehicleColors, vehicleColor0, vehicleColor1, vehicleColor2, useRouteNaming, sequentialRouteNaming, routeNamePrefix, selectedPrimaryVehicles, selectedSecondaryVehicles);
        }

        public void SetCustomRule(
            Colossal.Hash128 ruleId,
            FixedString64Bytes ruleName,
            int occupancy,
            int stdTicket,
            int maxTicketInc,
            int maxTicketDec,
            int maxVehAdj,
            int minVehAdj,
            UnityColor routeColor,
            string transportType,
            Entity[] selectedPrimaryVehicles,
            Entity[] selectedSecondaryVehicles)
        {
            SetCustomRule(
                ruleId,
                ruleName,
                occupancy,
                stdTicket,
                maxTicketInc,
                maxTicketDec,
                maxVehAdj,
                minVehAdj,
                GetCustomRuleAdjustVehiclesOrDefault(ruleId),
                routeColor,
                transportType,
                selectedPrimaryVehicles,
                selectedSecondaryVehicles);
        }

        public void SetCustomRule(
            Colossal.Hash128 ruleId,
            FixedString64Bytes ruleName,
            int occupancy,
            int stdTicket,
            int maxTicketInc,
            int maxTicketDec,
            int maxVehAdj,
            int minVehAdj,
            UnityColor routeColor,
            string transportType,
            bool useRouteColor,
            bool useVehicleModels,
            bool useVehicleColors,
            UnityColor vehicleColor0,
            UnityColor vehicleColor1,
            UnityColor vehicleColor2,
            bool useRouteNaming,
            bool sequentialRouteNaming,
            string routeNamePrefix,
            Entity[] selectedPrimaryVehicles,
            Entity[] selectedSecondaryVehicles)
        {
            SetCustomRule(
                ruleId,
                ruleName,
                occupancy,
                stdTicket,
                maxTicketInc,
                maxTicketDec,
                maxVehAdj,
                minVehAdj,
                GetCustomRuleAdjustVehiclesOrDefault(ruleId),
                routeColor,
                transportType,
                useRouteColor,
                useVehicleModels,
                useVehicleColors,
                vehicleColor0,
                vehicleColor1,
                vehicleColor2,
                useRouteNaming,
                sequentialRouteNaming,
                routeNamePrefix,
                selectedPrimaryVehicles,
                selectedSecondaryVehicles);
        }

        public void SetCustomRule(
            Colossal.Hash128 ruleId,
            FixedString64Bytes ruleName,
            int occupancy,
            int stdTicket,
            int maxTicketInc,
            int maxTicketDec,
            int maxVehAdj,
            int minVehAdj,
            bool adjustVehicles,
            UnityColor routeColor,
            string transportType,
            Entity[] selectedPrimaryVehicles,
            Entity[] selectedSecondaryVehicles)
        {
            GetCustomRuleCosmeticSettings(ruleId, out var useRouteColor, out var useVehicleModels, out var useVehicleColors, out var vehicleColor0, out var vehicleColor1, out var vehicleColor2);
            GetCustomRuleRouteNamingSettings(ruleId, out var useRouteNaming, out var sequentialRouteNaming, out var routeNamePrefix);

            SetCustomRule(
                ruleId,
                ruleName,
                occupancy,
                stdTicket,
                maxTicketInc,
                maxTicketDec,
                maxVehAdj,
                minVehAdj,
                adjustVehicles,
                routeColor,
                ParseRuleTransportType(transportType),
                useRouteColor,
                useVehicleModels,
                useVehicleColors,
                vehicleColor0,
                vehicleColor1,
                vehicleColor2,
                useRouteNaming,
                sequentialRouteNaming,
                routeNamePrefix,
                selectedPrimaryVehicles,
                selectedSecondaryVehicles);
        }

        public void SetCustomRule(
            Colossal.Hash128 ruleId,
            FixedString64Bytes ruleName,
            int occupancy,
            int stdTicket,
            int maxTicketInc,
            int maxTicketDec,
            int maxVehAdj,
            int minVehAdj,
            bool adjustVehicles,
            UnityColor routeColor,
            string transportType,
            bool useRouteColor,
            bool useVehicleModels,
            bool useVehicleColors,
            UnityColor vehicleColor0,
            UnityColor vehicleColor1,
            UnityColor vehicleColor2,
            bool useRouteNaming,
            bool sequentialRouteNaming,
            string routeNamePrefix,
            Entity[] selectedPrimaryVehicles,
            Entity[] selectedSecondaryVehicles)
        {
            SetCustomRule(
                ruleId,
                ruleName,
                occupancy,
                stdTicket,
                maxTicketInc,
                maxTicketDec,
                maxVehAdj,
                minVehAdj,
                adjustVehicles,
                routeColor,
                ParseRuleTransportType(transportType),
                useRouteColor,
                useVehicleModels,
                useVehicleColors,
                vehicleColor0,
                vehicleColor1,
                vehicleColor2,
                useRouteNaming,
                sequentialRouteNaming,
                routeNamePrefix,
                selectedPrimaryVehicles,
                selectedSecondaryVehicles);
        }

        public void SetCustomRule(
            Colossal.Hash128 ruleId,
            FixedString64Bytes ruleName,
            int occupancy,
            int stdTicket,
            int maxTicketInc,
            int maxTicketDec,
            int maxVehAdj,
            int minVehAdj,
            bool adjustVehicles,
            UnityColor routeColor,
            TransportType transportType,
            bool useRouteColor,
            bool useVehicleModels,
            bool useVehicleColors,
            UnityColor vehicleColor0,
            UnityColor vehicleColor1,
            UnityColor vehicleColor2,
            bool useRouteNaming,
            bool sequentialRouteNaming,
            string routeNamePrefix,
            Entity[] selectedPrimaryVehicles,
            Entity[] selectedSecondaryVehicles)
        {
            EntityQuery query = EntityManager.CreateEntityQuery(typeof(CustomRule));
            var entities = query.ToEntityArray(Allocator.Temp);
            var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            try
            {
                for (int i = 0; i < rules.Length; i++)
                {
                    if (rules[i].ruleId == ruleId)
                    {
                        var isBuiltInRule = RuleNames.TryGetValue(ruleId, out var builtInRuleName);
                        var updated = rules[i];
                        if (isBuiltInRule)
                            updated.ruleName = builtInRuleName;
                        else
                            updated.ruleName = ruleName;
                        updated.occupancy = occupancy;
                        updated.stdTicket = stdTicket;
                        updated.maxTicketInc = maxTicketInc;
                        updated.maxTicketDec = maxTicketDec;
                        updated.maxVehAdj = maxVehAdj;
                        updated.minVehAdj = minVehAdj;
                        updated.adjustVehicles = adjustVehicles;
                        updated.routeColor = NormalizeColor(routeColor);
                        updated.transportType = isBuiltInRule
                            ? GetBuiltInRuleTransportType(ruleId)
                            : NormalizeRuleTransportType(transportType);
                        updated.useRouteColor = useRouteColor;
                        updated.useVehicleModels = useVehicleModels &&
                            updated.transportType != CustomRule.UnspecifiedTransportType;
                        updated.useVehicleColors = useVehicleColors;
                        updated.vehicleColor0 = NormalizeColor(vehicleColor0);
                        updated.vehicleColor1 = NormalizeColor(vehicleColor1);
                        updated.vehicleColor2 = NormalizeColor(vehicleColor2);
                        updated.useRouteNaming = useRouteNaming;
                        updated.sequentialRouteNaming = sequentialRouteNaming;
                        updated.routeNamePrefix = TruncateRouteNamePrefix(routeNamePrefix);

                        if (isBuiltInRule && updated.transportType != CustomRule.UnspecifiedTransportType)
                        {
                            ApplyBuiltInRuleToSettings(
                                updated.transportType,
                                occupancy,
                                stdTicket,
                                maxTicketInc,
                                maxTicketDec,
                                maxVehAdj,
                                minVehAdj);
                        }

                        EntityManager.SetComponentData(entities[i], updated);
                        SetRuleVehicleSelections(
                            entities[i],
                            updated.transportType,
                            updated.useVehicleModels ? selectedPrimaryVehicles : Array.Empty<Entity>(),
                            updated.useVehicleModels ? selectedSecondaryVehicles : Array.Empty<Entity>());
                        RemoveRuleFromIncompatibleRoutes(ruleId, updated.transportType);
                        ApplyRuleColorToRoutes(ruleId, updated.routeColor);
                        ApplyRuleVehicleModelsToRoutes(ruleId);
                        ApplyRuleVehicleColorsToRoutes(ruleId);
                        ApplyRuleRouteNamesToRoutes(ruleId);

                        return;
                    }
                }

            }
            finally
            {
                entities.Dispose();
                rules.Dispose();
            }
        }

        private static void ApplyBuiltInRuleToSettings(
            TransportType transportType,
            int occupancy,
            int stdTicket,
            int maxTicketInc,
            int maxTicketDec,
            int maxVehAdj,
            int minVehAdj)
        {
            if (Mod.m_Setting == null)
                return;

            var changed = false;

            switch (transportType)
            {
                case TransportType.Bus:
                    changed =
                        Mod.m_Setting.target_occupancy_bus != occupancy ||
                        Mod.m_Setting.standard_ticket_bus != stdTicket ||
                        Mod.m_Setting.max_ticket_increase_bus != maxTicketInc ||
                        Mod.m_Setting.max_ticket_discount_bus != maxTicketDec ||
                        Mod.m_Setting.max_vahicles_adj_bus != maxVehAdj ||
                        Mod.m_Setting.min_vahicles_adj_bus != minVehAdj;
                    Mod.m_Setting.target_occupancy_bus = occupancy;
                    Mod.m_Setting.standard_ticket_bus = stdTicket;
                    Mod.m_Setting.max_ticket_increase_bus = maxTicketInc;
                    Mod.m_Setting.max_ticket_discount_bus = maxTicketDec;
                    Mod.m_Setting.max_vahicles_adj_bus = maxVehAdj;
                    Mod.m_Setting.min_vahicles_adj_bus = minVehAdj;
                    break;
                case TransportType.Tram:
                    changed =
                        Mod.m_Setting.target_occupancy_Tram != occupancy ||
                        Mod.m_Setting.standard_ticket_Tram != stdTicket ||
                        Mod.m_Setting.max_ticket_increase_Tram != maxTicketInc ||
                        Mod.m_Setting.max_ticket_discount_Tram != maxTicketDec ||
                        Mod.m_Setting.max_vahicles_adj_Tram != maxVehAdj ||
                        Mod.m_Setting.min_vahicles_adj_Tram != minVehAdj;
                    Mod.m_Setting.target_occupancy_Tram = occupancy;
                    Mod.m_Setting.standard_ticket_Tram = stdTicket;
                    Mod.m_Setting.max_ticket_increase_Tram = maxTicketInc;
                    Mod.m_Setting.max_ticket_discount_Tram = maxTicketDec;
                    Mod.m_Setting.max_vahicles_adj_Tram = maxVehAdj;
                    Mod.m_Setting.min_vahicles_adj_Tram = minVehAdj;
                    break;
                case TransportType.Subway:
                    changed =
                        Mod.m_Setting.target_occupancy_Subway != occupancy ||
                        Mod.m_Setting.standard_ticket_Subway != stdTicket ||
                        Mod.m_Setting.max_ticket_increase_Subway != maxTicketInc ||
                        Mod.m_Setting.max_ticket_discount_Subway != maxTicketDec ||
                        Mod.m_Setting.max_vahicles_adj_Subway != maxVehAdj ||
                        Mod.m_Setting.min_vahicles_adj_Subway != minVehAdj;
                    Mod.m_Setting.target_occupancy_Subway = occupancy;
                    Mod.m_Setting.standard_ticket_Subway = stdTicket;
                    Mod.m_Setting.max_ticket_increase_Subway = maxTicketInc;
                    Mod.m_Setting.max_ticket_discount_Subway = maxTicketDec;
                    Mod.m_Setting.max_vahicles_adj_Subway = maxVehAdj;
                    Mod.m_Setting.min_vahicles_adj_Subway = minVehAdj;
                    break;
                case TransportType.Train:
                    changed =
                        Mod.m_Setting.target_occupancy_Train != occupancy ||
                        Mod.m_Setting.standard_ticket_Train != stdTicket ||
                        Mod.m_Setting.max_ticket_increase_Train != maxTicketInc ||
                        Mod.m_Setting.max_ticket_discount_Train != maxTicketDec ||
                        Mod.m_Setting.max_vahicles_adj_Train != maxVehAdj ||
                        Mod.m_Setting.min_vahicles_adj_Train != minVehAdj;
                    Mod.m_Setting.target_occupancy_Train = occupancy;
                    Mod.m_Setting.standard_ticket_Train = stdTicket;
                    Mod.m_Setting.max_ticket_increase_Train = maxTicketInc;
                    Mod.m_Setting.max_ticket_discount_Train = maxTicketDec;
                    Mod.m_Setting.max_vahicles_adj_Train = maxVehAdj;
                    Mod.m_Setting.min_vahicles_adj_Train = minVehAdj;
                    break;
                case TransportType.Ship:
                    changed =
                        Mod.m_Setting.target_occupancy_Ship != occupancy ||
                        Mod.m_Setting.standard_ticket_Ship != stdTicket ||
                        Mod.m_Setting.max_ticket_increase_Ship != maxTicketInc ||
                        Mod.m_Setting.max_ticket_discount_Ship != maxTicketDec ||
                        Mod.m_Setting.max_vahicles_adj_Ship != maxVehAdj ||
                        Mod.m_Setting.min_vahicles_adj_Ship != minVehAdj;
                    Mod.m_Setting.target_occupancy_Ship = occupancy;
                    Mod.m_Setting.standard_ticket_Ship = stdTicket;
                    Mod.m_Setting.max_ticket_increase_Ship = maxTicketInc;
                    Mod.m_Setting.max_ticket_discount_Ship = maxTicketDec;
                    Mod.m_Setting.max_vahicles_adj_Ship = maxVehAdj;
                    Mod.m_Setting.min_vahicles_adj_Ship = minVehAdj;
                    break;
                case TransportType.Airplane:
                    changed =
                        Mod.m_Setting.target_occupancy_Airplane != occupancy ||
                        Mod.m_Setting.standard_ticket_Airplane != stdTicket ||
                        Mod.m_Setting.max_ticket_increase_Airplane != maxTicketInc ||
                        Mod.m_Setting.max_ticket_discount_Airplane != maxTicketDec ||
                        Mod.m_Setting.max_vahicles_adj_Airplane != maxVehAdj ||
                        Mod.m_Setting.min_vahicles_adj_Airplane != minVehAdj;
                    Mod.m_Setting.target_occupancy_Airplane = occupancy;
                    Mod.m_Setting.standard_ticket_Airplane = stdTicket;
                    Mod.m_Setting.max_ticket_increase_Airplane = maxTicketInc;
                    Mod.m_Setting.max_ticket_discount_Airplane = maxTicketDec;
                    Mod.m_Setting.max_vahicles_adj_Airplane = maxVehAdj;
                    Mod.m_Setting.min_vahicles_adj_Airplane = minVehAdj;
                    break;
                case TransportType.Ferry:
                    changed =
                        Mod.m_Setting.target_occupancy_Ferry != occupancy ||
                        Mod.m_Setting.standard_ticket_Ferry != stdTicket ||
                        Mod.m_Setting.max_ticket_increase_Ferry != maxTicketInc ||
                        Mod.m_Setting.max_ticket_discount_Ferry != maxTicketDec ||
                        Mod.m_Setting.max_vahicles_adj_Ferry != maxVehAdj ||
                        Mod.m_Setting.min_vahicles_adj_Ferry != minVehAdj;
                    Mod.m_Setting.target_occupancy_Ferry = occupancy;
                    Mod.m_Setting.standard_ticket_Ferry = stdTicket;
                    Mod.m_Setting.max_ticket_increase_Ferry = maxTicketInc;
                    Mod.m_Setting.max_ticket_discount_Ferry = maxTicketDec;
                    Mod.m_Setting.max_vahicles_adj_Ferry = maxVehAdj;
                    Mod.m_Setting.min_vahicles_adj_Ferry = minVehAdj;
                    break;
            }

            if (changed)
                _ = AssetDatabase.global.SaveSettings();
        }


        public Colossal.Hash128 AddCustomRule()
        {
            var newRuleEntity = EntityManager.CreateEntity(typeof(CustomRule));
            CustomRule customRule = new CustomRule("Unnamed", 0, 10, 0, 0, 0, 0, CustomRule.DefaultRouteColor);
            EntityManager.SetComponentData(newRuleEntity, customRule);

            return customRule.ruleId; // Return the generated ruleId
        }

        public static void RemoveCustomRule(Colossal.Hash128 ruleId)
        {
            // Do not remove built-in rules (Bus, Tram, Train, etc.)
            if (RuleNames.ContainsKey(ruleId))
            {
                if (Mod.log != null && RuleNames.TryGetValue(ruleId, out var name))
                {
                    Mod.log.Info($"[ManageRouteSystem] Ignoring delete for built-in rule '{name}'");
                }
                return;
            }

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(CustomRule));
            var entities = query.ToEntityArray(Allocator.Temp);
            var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].ruleId == ruleId)
                {
                    entityManager.DestroyEntity(entities[i]);
                    break;
                }
            }
        }



        public (Colossal.Hash128, string, int, int, int, int, int, int)[] GetCustomRules()
        {
            var rules = GetCustomRuleDetails();
            var result = new (Colossal.Hash128, string, int, int, int, int, int, int)[rules.Length];

            for (int i = 0; i < rules.Length; i++)
            {
                var r = rules[i];
                result[i] = (
                    r.ruleId,
                    r.ruleName,
                    r.occupancy,
                    r.stdTicket,
                    r.maxTicketInc,
                    r.maxTicketDec,
                    r.maxVehAdj,
                    r.minVehAdj
                );
            }

            return result;
        }

        public (Colossal.Hash128 ruleId, string ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj, UnityColor routeColor)[] GetCustomRulesWithColor()
        {
            var rules = GetCustomRuleDetails();
            var result = new (Colossal.Hash128, string, int, int, int, int, int, int, UnityColor)[rules.Length];

            for (int i = 0; i < rules.Length; i++)
            {
                var r = rules[i];
                result[i] = (
                    r.ruleId,
                    r.ruleName,
                    r.occupancy,
                    r.stdTicket,
                    r.maxTicketInc,
                    r.maxTicketDec,
                    r.maxVehAdj,
                    r.minVehAdj,
                    r.routeColor
                );
            }

            return result;
        }

        public CustomRuleInfo[] GetCustomRuleDetails()
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CustomRule>());
            using var entities = query.ToEntityArray(Allocator.Temp);
            using var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            var result = new CustomRuleInfo[rules.Length];

            for (int i = 0; i < rules.Length; i++)
            {
                result[i] = CreateCustomRuleInfo(entities[i], rules[i]);
            }

            return result;
        }

        public TransportVehicleOptions[] GetTransportVehicleOptions()
        {
            using var vehicleEntities = transportVehiclePrefabQuery.ToEntityArray(Allocator.Temp);
            using var transportVehicleData = transportVehiclePrefabQuery.ToComponentDataArray<PublicTransportVehicleData>(Allocator.Temp);

            var result = new List<TransportVehicleOptions>();

            foreach (var transportType in SupportedTransportTypes)
            {
                var primary = new List<VehiclePrefabInfo>();
                var secondary = new List<VehiclePrefabInfo>();

                for (int i = 0; i < vehicleEntities.Length; i++)
                {
                    var vehicleData = transportVehicleData[i];
                    if (vehicleData.m_TransportType != transportType ||
                        (vehicleData.m_PurposeMask & PublicTransportPurpose.TransportLine) == 0)
                    {
                        continue;
                    }

                    var vehicle = CreateVehiclePrefabInfo(vehicleEntities[i]);
                    if (IsSecondaryVehiclePrefab(vehicleEntities[i]))
                        secondary.Add(vehicle);
                    else
                        primary.Add(vehicle);
                }

                primary.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.OrdinalIgnoreCase));
                secondary.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.OrdinalIgnoreCase));

                result.Add(new TransportVehicleOptions
                {
                    transportType = transportType.ToString(),
                    availablePrimaryVehicles = primary.ToArray(),
                    availableSecondaryVehicles = secondary.ToArray()
                });
            }

            return result.ToArray();
        }

        private VehiclePrefabInfo CreateVehiclePrefabInfo(Entity vehicleEntity)
        {
            var thumbnail = m_ImageSystem?.GetThumbnail(vehicleEntity);
            if (string.IsNullOrWhiteSpace(thumbnail) && m_ImageSystem != null)
                thumbnail = m_ImageSystem.placeholderIcon;

            return new VehiclePrefabInfo
            {
                entity = vehicleEntity,
                id = m_PrefabSystem?.GetPrefabName(vehicleEntity) ?? vehicleEntity.ToString(),
                locked = EntityManager.HasComponent<Locked>(vehicleEntity),
                multiunit = EntityManager.HasComponent<MultipleUnitTrainData>(vehicleEntity),
                thumbnail = thumbnail ?? string.Empty
            };
        }

        private bool IsSecondaryVehiclePrefab(Entity vehicleEntity)
        {
            return EntityManager.HasComponent<TrainCarriageData>(vehicleEntity) &&
                   !EntityManager.HasComponent<TrainEngineData>(vehicleEntity) &&
                   !EntityManager.HasComponent<MultipleUnitTrainData>(vehicleEntity);
        }

        private bool TryGetCustomRuleEntity(Colossal.Hash128 ruleId, out Entity entity, out CustomRule rule)
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CustomRule>());
            using var entities = query.ToEntityArray(Allocator.Temp);
            using var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].ruleId == ruleId)
                {
                    entity = entities[i];
                    rule = rules[i];
                    return true;
                }
            }

            entity = Entity.Null;
            rule = default;
            return false;
        }

        private CustomRuleInfo CreateCustomRuleInfo(Entity entity, CustomRule rule)
        {
            GetRuleVehicleSelections(entity, out var selectedPrimaryVehicles, out var selectedSecondaryVehicles);

            return new CustomRuleInfo
            {
                ruleId = rule.ruleId,
                ruleName = rule.ruleName.ToString(),
                occupancy = rule.occupancy,
                stdTicket = rule.stdTicket,
                maxTicketInc = rule.maxTicketInc,
                maxTicketDec = rule.maxTicketDec,
                maxVehAdj = rule.maxVehAdj,
                minVehAdj = rule.minVehAdj,
                adjustVehicles = rule.adjustVehicles,
                routeColor = NormalizeColor(rule.routeColor),
                useRouteColor = rule.useRouteColor,
                transportType = RuleNames.ContainsKey(rule.ruleId)
                    ? GetBuiltInRuleTransportType(rule.ruleId)
                    : NormalizeRuleTransportType(rule.transportType),
                useVehicleModels = rule.useVehicleModels,
                useVehicleColors = rule.useVehicleColors,
                vehicleColor0 = NormalizeColor(rule.vehicleColor0),
                vehicleColor1 = NormalizeColor(rule.vehicleColor1),
                vehicleColor2 = NormalizeColor(rule.vehicleColor2),
                useRouteNaming = rule.useRouteNaming,
                sequentialRouteNaming = rule.sequentialRouteNaming,
                routeNamePrefix = rule.routeNamePrefix.ToString(),
                selectedPrimaryVehicles = selectedPrimaryVehicles,
                selectedSecondaryVehicles = selectedSecondaryVehicles
            };
        }

        private TransportType GetCustomRuleTransportTypeOrDefault(Colossal.Hash128 ruleId)
        {
            if (RuleNames.ContainsKey(ruleId))
                return GetBuiltInRuleTransportType(ruleId);

            return TryGetCustomRuleEntity(ruleId, out _, out var rule)
                ? NormalizeRuleTransportType(rule.transportType)
                : CustomRule.UnspecifiedTransportType;
        }

        public bool GetRuleAdjustVehiclesOrDefault(Colossal.Hash128 ruleId)
        {
            return GetCustomRuleAdjustVehiclesOrDefault(ruleId);
        }

        private bool GetCustomRuleAdjustVehiclesOrDefault(Colossal.Hash128 ruleId)
        {
            return TryGetCustomRuleEntity(ruleId, out _, out var rule)
                ? rule.adjustVehicles
                : true;
        }

        private void GetCustomRuleCosmeticSettings(
            Colossal.Hash128 ruleId,
            out bool useRouteColor,
            out bool useVehicleModels,
            out bool useVehicleColors,
            out UnityColor vehicleColor0,
            out UnityColor vehicleColor1,
            out UnityColor vehicleColor2)
        {
            if (TryGetCustomRuleEntity(ruleId, out _, out var rule))
            {
                useRouteColor = rule.useRouteColor;
                useVehicleModels = rule.useVehicleModels;
                useVehicleColors = rule.useVehicleColors;
                vehicleColor0 = NormalizeColor(rule.vehicleColor0);
                vehicleColor1 = NormalizeColor(rule.vehicleColor1);
                vehicleColor2 = NormalizeColor(rule.vehicleColor2);
                return;
            }

            useRouteColor = false;
            useVehicleModels = false;
            useVehicleColors = false;
            vehicleColor0 = CustomRule.DefaultVehicleColor;
            vehicleColor1 = CustomRule.DefaultVehicleColor;
            vehicleColor2 = CustomRule.DefaultVehicleColor;
        }

        private void GetCustomRuleRouteNamingSettings(
            Colossal.Hash128 ruleId,
            out bool useRouteNaming,
            out bool sequentialRouteNaming,
            out string routeNamePrefix)
        {
            if (TryGetCustomRuleEntity(ruleId, out _, out var rule))
            {
                useRouteNaming = rule.useRouteNaming;
                sequentialRouteNaming = rule.sequentialRouteNaming;
                routeNamePrefix = rule.routeNamePrefix.ToString();
                return;
            }

            useRouteNaming = false;
            sequentialRouteNaming = false;
            routeNamePrefix = string.Empty;
        }

        private void GetCustomRuleVehicleSelections(Colossal.Hash128 ruleId, out Entity[] selectedPrimaryVehicles, out Entity[] selectedSecondaryVehicles)
        {
            if (TryGetCustomRuleEntity(ruleId, out var entity, out _))
            {
                GetRuleVehicleSelections(entity, out selectedPrimaryVehicles, out selectedSecondaryVehicles);
                return;
            }

            selectedPrimaryVehicles = Array.Empty<Entity>();
            selectedSecondaryVehicles = Array.Empty<Entity>();
        }

        private void GetRuleVehicleSelections(Entity ruleEntity, out Entity[] selectedPrimaryVehicles, out Entity[] selectedSecondaryVehicles)
        {
            if (ruleEntity == Entity.Null || !EntityManager.HasBuffer<VehicleModel>(ruleEntity))
            {
                selectedPrimaryVehicles = Array.Empty<Entity>();
                selectedSecondaryVehicles = Array.Empty<Entity>();
                return;
            }

            var primary = new List<Entity>();
            var secondary = new List<Entity>();
            var vehicleModels = EntityManager.GetBuffer<VehicleModel>(ruleEntity, true);

            foreach (var vehicleModel in vehicleModels)
            {
                if (vehicleModel.m_PrimaryPrefab != Entity.Null)
                    primary.Add(vehicleModel.m_PrimaryPrefab);

                if (vehicleModel.m_SecondaryPrefab != Entity.Null)
                    secondary.Add(vehicleModel.m_SecondaryPrefab);
            }

            selectedPrimaryVehicles = primary.ToArray();
            selectedSecondaryVehicles = secondary.ToArray();
        }

        private void SetRuleVehicleSelections(Entity ruleEntity, TransportType transportType, Entity[] selectedPrimaryVehicles, Entity[] selectedSecondaryVehicles)
        {
            if (!EntityManager.HasBuffer<VehicleModel>(ruleEntity))
                EntityManager.AddBuffer<VehicleModel>(ruleEntity);

            var vehicleModels = EntityManager.GetBuffer<VehicleModel>(ruleEntity);
            vehicleModels.Clear();

            if (transportType == CustomRule.UnspecifiedTransportType)
                return;

            AddVehicleSelections(vehicleModels, selectedPrimaryVehicles, primary: true);
            AddVehicleSelections(vehicleModels, selectedSecondaryVehicles, primary: false);
        }

        private static void AddVehicleSelections(DynamicBuffer<VehicleModel> vehicleModels, Entity[] selectedVehicles, bool primary)
        {
            if (selectedVehicles == null)
                return;

            foreach (var vehicle in selectedVehicles)
            {
                if (vehicle == Entity.Null)
                    continue;

                vehicleModels.Add(new VehicleModel
                {
                    m_PrimaryPrefab = primary ? vehicle : Entity.Null,
                    m_SecondaryPrefab = primary ? Entity.Null : vehicle
                });
            }
        }

        private UnityColor GetCustomRuleColorOrDefault(Colossal.Hash128 ruleId)
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CustomRule>());
            using var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            foreach (var r in rules)
            {
                if (r.ruleId == ruleId)
                    return NormalizeColor(r.routeColor);
            }

            return CustomRule.DefaultRouteColor;
        }

        private bool TryGetCustomRuleColor(Colossal.Hash128 ruleId, out UnityColor routeColor)
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CustomRule>());
            using var rules = query.ToComponentDataArray<CustomRule>(Allocator.Temp);

            foreach (var r in rules)
            {
                if (r.ruleId == ruleId)
                {
                    routeColor = NormalizeColor(r.routeColor);
                    return true;
                }
            }

            routeColor = CustomRule.DefaultRouteColor;
            return false;
        }

        private void ApplyAllCustomRuleColors()
        {
            foreach (var rule in GetCustomRulesWithColor())
            {
                ApplyRuleColorToRoutes(rule.ruleId, rule.routeColor);
            }
        }

        private void ApplyAllCustomRuleVehicleModels()
        {
            foreach (var rule in GetCustomRuleDetails())
            {
                RemoveRuleFromIncompatibleRoutes(rule.ruleId, rule.transportType);
                ApplyRuleVehicleModelsToRoutes(rule.ruleId);
            }
        }

        private void ApplyAllCustomRuleVehicleColors()
        {
            foreach (var rule in GetCustomRuleDetails())
            {
                ApplyRuleVehicleColorsToRoutes(rule.ruleId);
            }
        }

        private void ApplyAllCustomRuleRouteNames()
        {
            foreach (var rule in GetCustomRuleDetails())
            {
                ApplyRuleRouteNamesToRoutes(rule.ruleId);
            }
        }

        private static bool IsDisabledRule(Colossal.Hash128 ruleId)
        {
            return ruleId == new Colossal.Hash128((uint)disabled_int_id, 0, 0, 0);
        }

        private bool TryGetRouteTransportType(Entity routeEntity, out TransportType transportType)
        {
            transportType = CustomRule.UnspecifiedTransportType;

            if (!EntityManager.TryGetComponent<PrefabRef>(routeEntity, out var prefabRef) ||
                !EntityManager.HasComponent<TransportLineData>(prefabRef.m_Prefab))
            {
                return false;
            }

            var transportLineData = EntityManager.GetComponentData<TransportLineData>(prefabRef.m_Prefab);
            if (!IsSmartTransportationSupportedRoute(transportLineData))
                return false;

            transportType = transportLineData.m_TransportType;
            return true;
        }

        private bool RouteUsesRuleForCosmetics(Entity routeEntity, Colossal.Hash128 ruleId, TransportType ruleTransportType)
        {
            if (RuleNames.ContainsKey(ruleId))
            {
                if (ruleTransportType == CustomRule.UnspecifiedTransportType ||
                    !TryGetRouteTransportType(routeEntity, out var routeTransportType) ||
                    routeTransportType != ruleTransportType)
                {
                    return false;
                }

                return !EntityManager.TryGetComponent<RouteRule>(routeEntity, out var routeRule) ||
                       routeRule.customRule == ruleId;
            }

            if (!EntityManager.TryGetComponent<RouteRule>(routeEntity, out var assignedRule) ||
                assignedRule.customRule != ruleId)
            {
                return false;
            }

            return ruleTransportType == CustomRule.UnspecifiedTransportType ||
                   (TryGetRouteTransportType(routeEntity, out var assignedTransportType) &&
                    assignedTransportType == ruleTransportType);
        }

        private void ApplyRuleColorToRouteIfConfigured(Entity routeEntity, Colossal.Hash128 ruleId)
        {
            if (IsDisabledRule(ruleId))
                return;

            if (!TryGetCustomRuleEntity(ruleId, out _, out var rule) || !rule.useRouteColor)
                return;

            ApplyRouteColor(routeEntity, rule.routeColor);
        }

        private void ApplyRuleColorToRoutes(Colossal.Hash128 ruleId, UnityColor routeColor)
        {
            if (IsDisabledRule(ruleId) || !TryGetCustomRuleEntity(ruleId, out _, out var rule))
                return;

            if (!rule.useRouteColor)
                return;

            var ruleTransportType = RuleNames.ContainsKey(ruleId)
                ? GetBuiltInRuleTransportType(ruleId)
                : NormalizeRuleTransportType(rule.transportType);

            using var entities = entityQuery.ToEntityArray(Allocator.Temp);

            foreach (var ent in entities)
            {
                if (RouteUsesRuleForCosmetics(ent, ruleId, ruleTransportType))
                    ApplyRouteColor(ent, routeColor);
            }
        }

        private void RemoveRuleFromIncompatibleRoutes(Colossal.Hash128 ruleId, TransportType ruleTransportType)
        {
            if (ruleTransportType == CustomRule.UnspecifiedTransportType)
                return;

            using var entities = entityQuery.ToEntityArray(Allocator.Temp);

            foreach (var ent in entities)
            {
                if (!EntityManager.TryGetComponent<RouteRule>(ent, out var routeRule) || routeRule.customRule != ruleId)
                    continue;

                if (!EntityManager.TryGetComponent<PrefabRef>(ent, out var prefabRef) ||
                    !EntityManager.HasComponent<TransportLineData>(prefabRef.m_Prefab))
                    continue;

                var transportLineData = EntityManager.GetComponentData<TransportLineData>(prefabRef.m_Prefab);
                if (transportLineData.m_TransportType != ruleTransportType)
                    EntityManager.RemoveComponent<RouteRule>(ent);
            }
        }

        private void ApplyRuleVehicleModelsToRoutes(Colossal.Hash128 ruleId)
        {
            if (IsDisabledRule(ruleId))
                return;

            if (!TryGetCustomRuleEntity(ruleId, out var ruleEntity, out var rule) ||
                !rule.useVehicleModels ||
                !EntityManager.HasBuffer<VehicleModel>(ruleEntity))
            {
                return;
            }

            var ruleTransportType = RuleNames.ContainsKey(ruleId)
                ? GetBuiltInRuleTransportType(ruleId)
                : NormalizeRuleTransportType(rule.transportType);

            if (ruleTransportType == CustomRule.UnspecifiedTransportType)
                return;

            var ruleVehicleModels = EntityManager.GetBuffer<VehicleModel>(ruleEntity, true);
            if (ruleVehicleModels.Length == 0)
                return;

            using var entities = entityQuery.ToEntityArray(Allocator.Temp);

            foreach (var ent in entities)
            {
                if (RouteUsesRuleForCosmetics(ent, ruleId, ruleTransportType))
                    ApplyVehicleModelsToRoute(ent, ruleVehicleModels);
            }
        }

        private void ApplyRuleVehicleModelsToRouteIfConfigured(Entity routeEntity, Colossal.Hash128 ruleId)
        {
            if (IsDisabledRule(ruleId))
                return;

            if (!TryGetCustomRuleEntity(ruleId, out var ruleEntity, out var rule) ||
                !rule.useVehicleModels ||
                !EntityManager.HasBuffer<VehicleModel>(ruleEntity))
            {
                return;
            }

            var ruleTransportType = RuleNames.ContainsKey(ruleId)
                ? GetBuiltInRuleTransportType(ruleId)
                : NormalizeRuleTransportType(rule.transportType);

            if (ruleTransportType == CustomRule.UnspecifiedTransportType ||
                !TryGetRouteTransportType(routeEntity, out var routeTransportType) ||
                routeTransportType != ruleTransportType)
            {
                return;
            }

            var ruleVehicleModels = EntityManager.GetBuffer<VehicleModel>(ruleEntity, true);
            if (ruleVehicleModels.Length > 0)
                ApplyVehicleModelsToRoute(routeEntity, ruleVehicleModels);
        }

        private void ApplyRuleVehicleColorsToRoutes(Colossal.Hash128 ruleId)
        {
            if (IsDisabledRule(ruleId))
                return;

            if (!TryGetCustomRuleEntity(ruleId, out _, out var rule) || !rule.useVehicleColors)
                return;

            var ruleTransportType = RuleNames.ContainsKey(ruleId)
                ? GetBuiltInRuleTransportType(ruleId)
                : NormalizeRuleTransportType(rule.transportType);
            var colorSet = CreateVehicleColorSet(rule);

            using var entities = entityQuery.ToEntityArray(Allocator.Temp);

            foreach (var ent in entities)
            {
                if (RouteUsesRuleForCosmetics(ent, ruleId, ruleTransportType))
                    ApplyVehicleColorsToRoute(ent, colorSet);
            }
        }

        private void ApplyRuleVehicleColorsToRouteIfConfigured(Entity routeEntity, Colossal.Hash128 ruleId)
        {
            if (IsDisabledRule(ruleId))
                return;

            if (!TryGetCustomRuleEntity(ruleId, out _, out var rule) || !rule.useVehicleColors)
                return;

            var ruleTransportType = RuleNames.ContainsKey(ruleId)
                ? GetBuiltInRuleTransportType(ruleId)
                : NormalizeRuleTransportType(rule.transportType);

            if (ruleTransportType != CustomRule.UnspecifiedTransportType &&
                (!TryGetRouteTransportType(routeEntity, out var routeTransportType) ||
                 routeTransportType != ruleTransportType))
            {
                return;
            }

            ApplyVehicleColorsToRoute(routeEntity, CreateVehicleColorSet(rule));
        }

        private void ApplyRuleRouteNamesToRoutes(Colossal.Hash128 ruleId)
        {
            if (IsDisabledRule(ruleId))
                return;

            if (!TryGetCustomRuleEntity(ruleId, out _, out var rule) || !rule.useRouteNaming)
                return;

            var nameSystem = GetNameSystem();
            if (nameSystem == null)
                return;

            var ruleTransportType = RuleNames.ContainsKey(ruleId)
                ? GetBuiltInRuleTransportType(ruleId)
                : NormalizeRuleTransportType(rule.transportType);

            using var entities = entityQuery.ToEntityArray(Allocator.Temp);

            if (rule.sequentialRouteNaming)
            {
                var matchingRoutes = new List<(Entity entity, int routeNumber)>();
                foreach (var ent in entities)
                {
                    if (!RouteUsesRuleForCosmetics(ent, ruleId, ruleTransportType) ||
                        !EntityManager.TryGetComponent<RouteNumber>(ent, out var routeNumber))
                    {
                        continue;
                    }

                    matchingRoutes.Add((ent, routeNumber.m_Number));
                }

                matchingRoutes.Sort((left, right) => left.routeNumber.CompareTo(right.routeNumber));

                for (var i = 0; i < matchingRoutes.Count; i++)
                    ApplyRouteNameToRoute(matchingRoutes[i].entity, rule, nameSystem, i + 1);
            }
            else
            {
                foreach (var ent in entities)
                {
                    if (RouteUsesRuleForCosmetics(ent, ruleId, ruleTransportType))
                        ApplyRouteNameToRoute(ent, rule, nameSystem);
                }
            }
        }

        private void ApplyRuleRouteNameToRouteIfConfigured(Entity routeEntity, Colossal.Hash128 ruleId)
        {
            if (IsDisabledRule(ruleId))
                return;

            if (!TryGetCustomRuleEntity(ruleId, out _, out var rule) || !rule.useRouteNaming)
                return;

            var ruleTransportType = RuleNames.ContainsKey(ruleId)
                ? GetBuiltInRuleTransportType(ruleId)
                : NormalizeRuleTransportType(rule.transportType);

            if (ruleTransportType != CustomRule.UnspecifiedTransportType &&
                (!TryGetRouteTransportType(routeEntity, out var routeTransportType) ||
                 routeTransportType != ruleTransportType))
            {
                return;
            }

            if (rule.sequentialRouteNaming)
            {
                ApplyRuleRouteNamesToRoutes(ruleId);
                return;
            }

            var nameSystem = GetNameSystem();
            if (nameSystem != null)
                ApplyRouteNameToRoute(routeEntity, rule, nameSystem);
        }

        private void ApplyRouteNameToRoute(Entity routeEntity, CustomRule rule, NameSystem nameSystem)
        {
            if (!EntityManager.TryGetComponent<RouteNumber>(routeEntity, out var routeNumber))
                return;

            ApplyRouteNameToRoute(routeEntity, rule, nameSystem, routeNumber.m_Number);
        }

        private void ApplyRouteNameToRoute(Entity routeEntity, CustomRule rule, NameSystem nameSystem, int number)
        {
            var desiredName = $"{rule.routeNamePrefix.ToString()}{number}";
            if (string.IsNullOrWhiteSpace(desiredName))
                return;

            if (nameSystem.TryGetCustomName(routeEntity, out var currentName) &&
                string.Equals(currentName, desiredName, StringComparison.Ordinal))
            {
                return;
            }

            nameSystem.SetCustomName(routeEntity, desiredName);
        }

        private NameSystem GetNameSystem()
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                return world?.GetExistingSystemManaged<NameSystem>();
            }
            catch
            {
                return null;
            }
        }

        private void ApplyVehicleModelsToRoute(Entity routeEntity, DynamicBuffer<VehicleModel> sourceVehicleModels)
        {
            if (!EntityManager.HasBuffer<VehicleModel>(routeEntity))
                EntityManager.AddBuffer<VehicleModel>(routeEntity);

            var targetVehicleModels = EntityManager.GetBuffer<VehicleModel>(routeEntity);
            targetVehicleModels.Clear();

            foreach (var vehicleModel in sourceVehicleModels)
            {
                if (vehicleModel.m_PrimaryPrefab == Entity.Null && vehicleModel.m_SecondaryPrefab == Entity.Null)
                    continue;

                targetVehicleModels.Add(vehicleModel);
            }
        }

        private static ColorSet CreateVehicleColorSet(CustomRule rule)
        {
            return new ColorSet
            {
                m_Channel0 = NormalizeColor(rule.vehicleColor0),
                m_Channel1 = NormalizeColor(rule.vehicleColor1),
                m_Channel2 = NormalizeColor(rule.vehicleColor2)
            };
        }

        private void ApplyVehicleColorsToRoute(Entity routeEntity, ColorSet colorSet)
        {
            if (!EntityManager.HasBuffer<RouteVehicle>(routeEntity))
                return;

            var routeVehicles = EntityManager.GetBuffer<RouteVehicle>(routeEntity, true);
            foreach (var routeVehicle in routeVehicles)
            {
                ApplyVehicleColorsToObject(routeVehicle.m_Vehicle, colorSet);
            }
        }

        private void ApplyVehicleColorsToObject(Entity vehicleEntity, ColorSet colorSet)
        {
            if (vehicleEntity == Entity.Null || !EntityManager.Exists(vehicleEntity))
                return;

            SetCustomMeshColor(vehicleEntity, colorSet);

            if (!EntityManager.HasBuffer<Game.Objects.SubObject>(vehicleEntity))
                return;

            var subObjects = EntityManager.GetBuffer<Game.Objects.SubObject>(vehicleEntity, true);
            foreach (var subObject in subObjects)
            {
                if (subObject.m_SubObject != Entity.Null && EntityManager.Exists(subObject.m_SubObject))
                    SetCustomMeshColor(subObject.m_SubObject, colorSet);
            }
        }

        private void SetCustomMeshColor(Entity entity, ColorSet colorSet)
        {
            if (!EntityManager.HasBuffer<CustomMeshColor>(entity))
                EntityManager.AddBuffer<CustomMeshColor>(entity);

            var customMeshColors = EntityManager.GetBuffer<CustomMeshColor>(entity);
            customMeshColors.Clear();
            customMeshColors.Add(new CustomMeshColor
            {
                m_ColorSet = colorSet
            });
        }

        private void ApplyRouteColor(Entity routeEntity, UnityColor routeColor)
        {
            var routeColorComponent = new Game.Routes.Color((UnityColor32)NormalizeColor(routeColor));

            if (EntityManager.HasComponent<Game.Routes.Color>(routeEntity))
                EntityManager.SetComponentData(routeEntity, routeColorComponent);
            else
                EntityManager.AddComponentData(routeEntity, routeColorComponent);

            if (EntityManager.HasBuffer<RouteVehicle>(routeEntity))
            {
                var routeVehicles = EntityManager.GetBuffer<RouteVehicle>(routeEntity, true);
                foreach (var routeVehicle in routeVehicles)
                {
                    var vehicle = routeVehicle.m_Vehicle;
                    if (vehicle == Entity.Null || !EntityManager.Exists(vehicle))
                        continue;

                    if (EntityManager.HasComponent<Game.Routes.Color>(vehicle))
                        EntityManager.SetComponentData(vehicle, routeColorComponent);
                    else
                        EntityManager.AddComponentData(vehicle, routeColorComponent);
                }
            }

            var colorUpdated = EntityManager.CreateEntity(typeof(ColorUpdated));
            EntityManager.SetComponentData(colorUpdated, new ColorUpdated(routeEntity));
        }

        private static UnityColor NormalizeColor(UnityColor color)
        {
            if (color.a <= 0f)
                color.a = 1f;

            color.r = UnityEngine.Mathf.Clamp01(color.r);
            color.g = UnityEngine.Mathf.Clamp01(color.g);
            color.b = UnityEngine.Mathf.Clamp01(color.b);
            color.a = UnityEngine.Mathf.Clamp01(color.a);
            return color;
        }

        private static FixedString32Bytes TruncateRouteNamePrefix(string prefix)
        {
            var value = (prefix ?? string.Empty).Trim();
            if (value.Length > 28)
                value = value.Substring(0, 28);

            return value;
        }

        public struct RouteInfoForUI
        {
            public int routeNumber;
            public string routeName;
            public string transportType;
            public string ruleName;
            public Colossal.Hash128 ruleId;
        }

        public void SetRouteRuleForRoute(string transportTypeString, int routeNumber, Colossal.Hash128? ruleIdOrNull)
        {
            var entities = EntityManager.GetAllEntities(Allocator.Temp);
            try
            {
                foreach (var ent in entities)
                {
                    if (!EntityManager.HasComponent<TransportLine>(ent)) continue;
                    if (!EntityManager.HasComponent<RouteNumber>(ent)) continue;
                    if (!EntityManager.HasComponent<PrefabRef>(ent)) continue;

                    var rn = EntityManager.GetComponentData<RouteNumber>(ent);
                    if (rn.m_Number != routeNumber) continue;

                    var prefabRef = EntityManager.GetComponentData<PrefabRef>(ent);
                    if (!EntityManager.HasComponent<TransportLineData>(prefabRef.m_Prefab)) continue;

                    var tld = EntityManager.GetComponentData<TransportLineData>(prefabRef.m_Prefab);

                    if (!IsSmartTransportationSupportedRoute(tld))
                        continue;

                    var tTypeString = tld.m_TransportType.ToString();

                    if (!string.Equals(tTypeString, transportTypeString, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var defaultRuleId = new Colossal.Hash128((uint)tld.m_TransportType, 0, 0, 0);
                    var disabledRuleId = new Colossal.Hash128((uint)disabled_int_id, 0, 0, 0);

                    if (!ruleIdOrNull.HasValue)
                    {
                        // No rule means clear override.
                        if (EntityManager.HasComponent<RouteRule>(ent))
                            EntityManager.RemoveComponent<RouteRule>(ent);

                        ApplyRuleColorToRouteIfConfigured(ent, defaultRuleId);
                        ApplyRuleVehicleModelsToRouteIfConfigured(ent, defaultRuleId);
                        ApplyRuleVehicleColorsToRouteIfConfigured(ent, defaultRuleId);
                        ApplyRuleRouteNameToRouteIfConfigured(ent, defaultRuleId);
                    }
                    else if (ruleIdOrNull.Value.Equals(defaultRuleId))
                    {
                        // Reverting to the original Bus/Tram/etc. rule should restore vanilla/default behavior.
                        if (EntityManager.HasComponent<RouteRule>(ent))
                            EntityManager.RemoveComponent<RouteRule>(ent);

                        ApplyRuleColorToRouteIfConfigured(ent, defaultRuleId);
                        ApplyRuleVehicleModelsToRouteIfConfigured(ent, defaultRuleId);
                        ApplyRuleVehicleColorsToRouteIfConfigured(ent, defaultRuleId);
                        ApplyRuleRouteNameToRouteIfConfigured(ent, defaultRuleId);
                    }
                    else
                    {
                        // Custom rule, Disabled rule, or other explicit override.
                        var selectedRuleId = ruleIdOrNull.Value;
                        if (!RuleNames.ContainsKey(selectedRuleId) &&
                            (!TryGetCustomRuleEntity(selectedRuleId, out _, out var selectedRule) ||
                             !RuleAppliesToTransport(selectedRule, tld.m_TransportType)))
                        {
                            if (EntityManager.HasComponent<RouteRule>(ent))
                                EntityManager.RemoveComponent<RouteRule>(ent);
                        }
                        else
                        {
                            SetRouteRule(ent, selectedRuleId);
                            ApplyRuleColorToRouteIfConfigured(ent, selectedRuleId);
                            ApplyRuleVehicleModelsToRouteIfConfigured(ent, selectedRuleId);
                            ApplyRuleVehicleColorsToRouteIfConfigured(ent, selectedRuleId);
                            ApplyRuleRouteNameToRouteIfConfigured(ent, selectedRuleId);
                        }
                    }

                    break;
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        public void ApplyConfiguredCosmeticsToRoute(Entity routeEntity)
        {
            if (routeEntity == Entity.Null || !EntityManager.Exists(routeEntity))
                return;

            var (ruleId, ruleName) = GetRouteRule(routeEntity);
            if (string.IsNullOrEmpty(ruleName))
                return;

            ApplyRuleColorToRouteIfConfigured(routeEntity, ruleId);
            ApplyRuleVehicleModelsToRouteIfConfigured(routeEntity, ruleId);
            ApplyRuleVehicleColorsToRouteIfConfigured(routeEntity, ruleId);
            ApplyRuleRouteNameToRouteIfConfigured(routeEntity, ruleId);
        }

        /// <summary>
        /// Collects all transit routes and returns simple DTOs for the UI.
        /// </summary>
        public RouteInfoForUI[] GetRoutesForUI()
        {
            // Try to get the game's NameSystem so we can use custom route names
            NameSystem nameSystem = null;
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null)
                {
                    nameSystem = world.GetExistingSystemManaged<NameSystem>();
                }
            }
            catch
            {
                // If this fails (e.g. wrong context), we'll just fall back to default names
            }

            var result = new List<RouteInfoForUI>();

            var entities = EntityManager.GetAllEntities(Allocator.Temp);
            try
            {
                foreach (var ent in entities)
                {
                    // Only consider entities that are actual transport lines
                    if (!EntityManager.HasComponent<TransportLine>(ent))
                        continue;

                    if (!EntityManager.HasComponent<RouteNumber>(ent))
                        continue;

                    if (!EntityManager.HasComponent<PrefabRef>(ent))
                        continue;

                    var routeNumber = EntityManager.GetComponentData<RouteNumber>(ent);
                    var prefabRef = EntityManager.GetComponentData<PrefabRef>(ent);

                    if (!EntityManager.HasComponent<TransportLineData>(prefabRef.m_Prefab))
                        continue;

                    var transportLineData = EntityManager.GetComponentData<TransportLineData>(prefabRef.m_Prefab);

                    if (!IsSmartTransportationSupportedRoute(transportLineData))
                        continue;

                    var transportType = transportLineData.m_TransportType;

                    // Determine rule currently assigned to this route
                    var (ruleId, ruleName) = GetRouteRule(ent);

                    // Fallback to "Disabled" if nothing came back
                    if (string.IsNullOrEmpty(ruleName))
                    {
                        if (RuleNames.TryGetValue(new Colossal.Hash128((uint)disabled_int_id, 0, 0, 0),
                                                  out var disabledName))
                        {
                            ruleName = disabledName;
                        }
                        else
                        {
                            ruleName = "Disabled";
                        }
                    }

                    // Route "display" name:
                    // 1) Prefer the user's custom name (from NameSystem)
                    // 2) Fall back to a simple "Type Line X" pattern
                    string routeName;

                    // Try to use the game’s custom name for this route, if any
                    if (nameSystem != null &&
                        nameSystem.TryGetCustomName(ent, out var customName) &&
                        !string.IsNullOrWhiteSpace(customName))
                    {
                        routeName = customName;
                    }
                    else
                    {
                        // Fallback pattern when there's no custom name
                        routeName = $"{transportType} Line {routeNumber.m_Number}";
                    }

                    result.Add(new RouteInfoForUI
                    {
                        routeNumber = routeNumber.m_Number,
                        routeName = routeName,
                        transportType = transportType.ToString(),
                        ruleName = ruleName,
                        ruleId = ruleId
                    });

                }
            }
            finally
            {
                entities.Dispose();
            }

            return result.ToArray();
        }


        protected override void OnUpdate()
        {
            if (firstUpdate) return;

            SyncDefaultRulesFromSettings();

            //Entity routeEntity = GetRouteEntityFromId(1, TransportType.Bus);
            //if (routeEntity == Entity.Null)
            //{
            //    Mod.log.Warn("[TEST] Could not find Bus route with ID 1.");
            //    this.Enabled = false;
            //    return;
            //}
            //
            //Mod.log.Info("=== STARTING ManageRouteSystem TEST ===");
            //
            //// 1. Add Custom Rules (use AddCustomRule + SetCustomRule)
            //var alphaId = AddCustomRule();
            //SetCustomRule(alphaId, "Alpha", 40, 10, 20, 10, 25, 5);
            //
            //var betaId = AddCustomRule();
            //SetCustomRule(betaId, "Beta", 60, 15, 30, 15, 20, 10);
            //
            //Mod.log.Info("[TEST] Added Custom Rules");
            //
            //// 2. Get All Custom Rules
            //var allCustomRules = GetCustomRules();
            //foreach (var (id, name, occ, ticket, inc, dec, maxAdj, minAdj) in allCustomRules)
            //{
            //    Mod.log.Info($"[TEST] CustomRule - ID: {id}, Name: {name}, Occ: {occ}, StdTicket: {ticket}, MaxInc: {inc}, MaxDec: {dec}, MaxAdj: {maxAdj}, MinAdj: {minAdj}");
            //}
            //
            //// 3. SetRouteRule: Assign the first custom rule to the route
            //var (testRuleId, _, _, _, _, _, _, _) = allCustomRules.Last();
            //SetRouteRule(routeEntity, testRuleId);
            //Mod.log.Info($"[TEST] SetRouteRule to custom rule ID: {testRuleId}");
            //
            //// 4. GetRouteRule: Confirm assignment
            //var (assignedRuleId, assignedName) = GetRouteRule(routeEntity);
            //Mod.log.Info($"[TEST] GetRouteRule => ID: {assignedRuleId}, Name: {assignedName}");
            //
            //// 5. GetRouteRules: List all valid rules for the route
            //var routeRules = GetRouteRules(routeEntity);
            //foreach (var (id, name) in routeRules)
            //{
            //    Mod.log.Info($"[TEST] Valid RouteRule => ID: {id}, Name: {name}");
            //}
            //
            //// 6. GetCustomRule: Get the full data of the assigned custom rule
            //var (_, rName, rOcc, rTicket, rInc, rDec, rMax, rMin) = GetCustomRule(testRuleId);
            //Mod.log.Info($"[TEST] GetCustomRule => Name: {rName}, Occ: {rOcc}, StdTicket: {rTicket}, MaxInc: {rInc}, MaxDec: {rDec}, MaxAdj: {rMax}, MinAdj: {rMin}");
            //
            //// 7. Update the custom rule
            //SetCustomRule(testRuleId, "Alpha Updated", 55, 12, 22, 8, 18, 6);
            //var (_, uName, uOcc, uTicket, uInc, uDec, uMax, uMin) = GetCustomRule(testRuleId);
            //Mod.log.Info($"[TEST] Updated CustomRule => Name: {uName}, Occ: {uOcc}, StdTicket: {uTicket}, MaxInc: {uInc}, MaxDec: {uDec}, MaxAdj: {uMax}, MinAdj: {uMin}");
            //
            //// 8. Remove the custom rule
            //RemoveCustomRule(testRuleId);
            //Mod.log.Info($"[TEST] Removed CustomRule with ID: {testRuleId}");
            //
            //// 9. Confirm removal
            //var afterRemoval = GetCustomRules();
            //Mod.log.Info("[TEST] Remaining Custom Rules:");
            //foreach (var (id, name, occ, ticket, inc, dec, maxAdj, minAdj) in afterRemoval)
            //{
            //    Mod.log.Info($"[TEST] Remaining => ID: {id}, Name: {name}");
            //}
            //
            //Mod.log.Info("=== END OF ManageRouteSystem TEST ===");

            firstUpdate = true;

            this.Enabled = false;
        }

    }
}
