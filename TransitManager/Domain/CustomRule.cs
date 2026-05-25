using Colossal.UI.Binding;
using Unity.Entities;
using UnityEngine;

namespace SmartTransportation.Domain
{
    public class CustomRule : IJsonWritable
    {
        public Colossal.Hash128 RuleId { get; }
        public string RuleName { get; }
        public int Occupancy { get; }
        public int StdTicket { get; }
        public int MaxTicketInc { get; }
        public int MaxTicketDec { get; }
        public int MaxVehAdj { get; }
        public int MinVehAdj { get; }
        public Color RouteColor { get; }
        public bool UseRouteColor { get; }
        public string TransportType { get; }
        public bool UseVehicleModels { get; }
        public Entity[] SelectedPrimaryVehicles { get; }
        public Entity[] SelectedSecondaryVehicles { get; }

        public CustomRule(Colossal.Hash128 ruleId, string ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj, Color routeColor)
            : this(ruleId, ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj, routeColor, false, "NotSpecified", false, null, null)
        {
        }

        public CustomRule(
            Colossal.Hash128 ruleId,
            string ruleName,
            int occupancy,
            int stdTicket,
            int maxTicketInc,
            int maxTicketDec,
            int maxVehAdj,
            int minVehAdj,
            Color routeColor,
            bool useRouteColor,
            string transportType,
            bool useVehicleModels,
            Entity[] selectedPrimaryVehicles,
            Entity[] selectedSecondaryVehicles)
        {
            RuleId = ruleId;
            RuleName = ruleName;
            Occupancy = occupancy;
            StdTicket = stdTicket;
            MaxTicketInc = maxTicketInc;
            MaxTicketDec = maxTicketDec;
            MaxVehAdj = maxVehAdj;
            MinVehAdj = minVehAdj;
            RouteColor = routeColor;
            UseRouteColor = useRouteColor;
            TransportType = string.IsNullOrWhiteSpace(transportType) ? "NotSpecified" : transportType;
            UseVehicleModels = useVehicleModels;
            SelectedPrimaryVehicles = selectedPrimaryVehicles ?? System.Array.Empty<Entity>();
            SelectedSecondaryVehicles = selectedSecondaryVehicles ?? System.Array.Empty<Entity>();
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName("ruleId");
            writer.Write(RuleId.ToString()); // Convert Hash128 to string
            writer.PropertyName("ruleName");
            writer.Write(RuleName);
            writer.PropertyName("occupancy");
            writer.Write(Occupancy);
            writer.PropertyName("stdTicket");
            writer.Write(StdTicket);
            writer.PropertyName("maxTicketInc");
            writer.Write(MaxTicketInc);
            writer.PropertyName("maxTicketDec");
            writer.Write(MaxTicketDec);
            writer.PropertyName("maxVehAdj");
            writer.Write(MaxVehAdj);
            writer.PropertyName("minVehAdj");
            writer.Write(MinVehAdj);
            writer.PropertyName("routeColor");
            writer.Write(RouteColor);
            writer.PropertyName("useRouteColor");
            writer.Write(UseRouteColor);
            writer.PropertyName("transportType");
            writer.Write(TransportType);
            writer.PropertyName("useVehicleModels");
            writer.Write(UseVehicleModels);
            writer.PropertyName("selectedPrimaryVehicles");
            WriteEntities(writer, SelectedPrimaryVehicles);
            writer.PropertyName("selectedSecondaryVehicles");
            WriteEntities(writer, SelectedSecondaryVehicles);
            writer.TypeEnd();
        }

        private static void WriteEntities(IJsonWriter writer, Entity[] entities)
        {
            writer.ArrayBegin(entities.Length);
            foreach (var entity in entities)
            {
                writer.Write(entity);
            }
            writer.ArrayEnd();
        }
    }
}
