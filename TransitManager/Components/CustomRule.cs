using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Prefabs;
using SmartTransportation.Bridge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SmartTransportation.Components
{
    public struct CustomRule : IComponentData, IQueryTypeParameter, ISerializable
    {
        public const int CurrentVersion = 4;
        public static readonly Color DefaultRouteColor = new Color(0.0f, 0.54f, 0.85f, 1f);
        public const TransportType UnspecifiedTransportType = TransportType.None;

        public int version = CurrentVersion;
        private Colossal.Hash128 _ruleId; // Changed to a private field
        public Colossal.Hash128 ruleId => _ruleId; // Exposed as a read-only property
        public FixedString64Bytes ruleName;
        public int occupancy;
        public int stdTicket;
        public int maxTicketInc;
        public int maxTicketDec;
        public int maxVehAdj;
        public int minVehAdj;
        public Color routeColor;
        public TransportType transportType;
        public bool useRouteColor;
        public bool useVehicleModels;

        public CustomRule(FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj)
            : this(ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj, DefaultRouteColor, UnspecifiedTransportType)
        {
        }

        public CustomRule(FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj, Color routeColor)
            : this(ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj, routeColor, UnspecifiedTransportType)
        {
        }

        public CustomRule(FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj, Color routeColor, TransportType transportType)
        {
            Colossal.Hash128 ruleId;
            do
            {
                ruleId = Guid.NewGuid();
            }
            while (ManageRouteSystem.RuleNames.ContainsKey(ruleId));
            this._ruleId = ruleId;
            this.ruleName = ruleName;
            this.occupancy = occupancy;
            this.stdTicket = stdTicket;
            this.maxTicketInc = maxTicketInc;
            this.maxTicketDec = maxTicketDec;
            this.maxVehAdj = maxVehAdj;
            this.minVehAdj = minVehAdj;
            this.routeColor = routeColor;
            this.transportType = transportType;
            this.useRouteColor = false;
            this.useVehicleModels = false;
        }

        public CustomRule(Colossal.Hash128 ruleId, FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj)
            : this(ruleId, ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj, DefaultRouteColor, UnspecifiedTransportType)
        {
        }

        public CustomRule(Colossal.Hash128 ruleId, FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj, Color routeColor)
            : this(ruleId, ruleName, occupancy, stdTicket, maxTicketInc, maxTicketDec, maxVehAdj, minVehAdj, routeColor, UnspecifiedTransportType)
        {
        }

        public CustomRule(Colossal.Hash128 ruleId, FixedString64Bytes ruleName, int occupancy, int stdTicket, int maxTicketInc, int maxTicketDec, int maxVehAdj, int minVehAdj, Color routeColor, TransportType transportType)
        {
            version = CurrentVersion;
            this._ruleId = ruleId;
            this.ruleName = ruleName;
            this.occupancy = occupancy;
            this.stdTicket = stdTicket;
            this.maxTicketInc = maxTicketInc;
            this.maxTicketDec = maxTicketDec;
            this.maxVehAdj = maxVehAdj;
            this.minVehAdj = minVehAdj;
            this.routeColor = routeColor;
            this.transportType = transportType;
            this.useRouteColor = false;
            this.useVehicleModels = false;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CurrentVersion);
            writer.Write(_ruleId); // Use the private field
            writer.Write(ruleName.ToString());
            writer.Write(occupancy);
            writer.Write(stdTicket);
            writer.Write(maxTicketInc);
            writer.Write(maxTicketDec);
            writer.Write(maxVehAdj);
            writer.Write(minVehAdj);
            writer.Write(routeColor.r);
            writer.Write(routeColor.g);
            writer.Write(routeColor.b);
            writer.Write(routeColor.a);
            writer.Write((int)transportType);
            writer.Write(useRouteColor);
            writer.Write(useVehicleModels);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out version);
            reader.Read(out _ruleId); // Use the private field
            reader.Read(out string ruleNameString); // Read as string
            ruleName = ruleNameString;              // Assign to FixedString64Bytes
            reader.Read(out occupancy);
            reader.Read(out stdTicket);
            reader.Read(out maxTicketInc);
            reader.Read(out maxTicketDec);
            reader.Read(out maxVehAdj);
            reader.Read(out minVehAdj);
            if (version >= 2)
            {
                reader.Read(out float r);
                reader.Read(out float g);
                reader.Read(out float b);
                reader.Read(out float a);
                routeColor = new Color(r, g, b, a);
            }
            else
            {
                routeColor = DefaultRouteColor;
            }

            if (version >= 3)
            {
                reader.Read(out int transportTypeValue);
                transportType = Enum.IsDefined(typeof(TransportType), transportTypeValue)
                    ? (TransportType)transportTypeValue
                    : UnspecifiedTransportType;
            }
            else
            {
                transportType = UnspecifiedTransportType;
            }

            if (version >= 4)
            {
                reader.Read(out useRouteColor);
                reader.Read(out useVehicleModels);
            }
            else
            {
                useRouteColor = false;
                useVehicleModels = false;
            }
        }
    }
}
