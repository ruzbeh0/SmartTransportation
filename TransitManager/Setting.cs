﻿using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;
using Unity.Entities;

namespace SmartTransportation
{
    [FileLocation(nameof(SmartTransportation))]
    [SettingsUIGroupOrder(BusGroup, TramGroup, SubwayGroup, TrainGroup, SettingsGroup)]
    [SettingsUIShowGroupName(BusGroup, TramGroup, SubwayGroup, TrainGroup, SettingsGroup)]
    public class Setting : ModSetting
    {
        public const string TransitSection = "TransitSection";
        public const string SettingsSection = "SettingsSection";
        public const string BusGroup = "BusGroup";
        public const string TramGroup = "TramGroup";
        public const string SubwayGroup = "SubwayGroup";
        public const string TrainGroup = "TrainGroup";
        public const string SettingsGroup = "SettingsGroup";

        public Setting(IMod mod) : base(mod)
        {
            if (target_occupancy_bus == 0) SetDefaults();
        }

        public override void SetDefaults()
        {
            disable_bus = false;
            target_occupancy_bus = 40;
            max_ticket_increase_bus = 20;
            max_ticket_discount_bus = 40;
            disable_Tram = false;
            target_occupancy_Tram = 60;
            max_ticket_increase_Tram = 30;
            max_ticket_discount_Tram = 20;
            target_occupancy_Subway = 60;
            max_ticket_increase_Subway = 40;
            max_ticket_discount_Subway = 20;
            target_occupancy_Train = 70;
            max_ticket_increase_Train = 40;
            max_ticket_discount_Train = 20;
            standard_ticket_bus = 8;
            standard_ticket_Tram = 8;
            standard_ticket_Subway = 9;
            standard_ticket_Train = 10;
            waiting_time_weight = 0.5f;
            threshold = 10;
            debug = false;
        }

        [SettingsUISection(TransitSection, BusGroup)]
        public bool disable_bus { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, BusGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_bus))]
        public int target_occupancy_bus { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, BusGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_bus))]
        public int standard_ticket_bus { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, BusGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_bus))]
        public int max_ticket_increase_bus { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, BusGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_bus))]
        public int max_ticket_discount_bus { get; set; }

        [SettingsUISection(TransitSection, TramGroup)]
        public bool disable_Tram { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TramGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Tram))]
        public int target_occupancy_Tram { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, TramGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Tram))]
        public int standard_ticket_Tram { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TramGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Tram))]
        public int max_ticket_increase_Tram { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TramGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Tram))]
        public int max_ticket_discount_Tram { get; set; }

        [SettingsUISection(TransitSection, SubwayGroup)]
        public bool disable_Subway { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, SubwayGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Subway))]
        public int target_occupancy_Subway { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, SubwayGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Subway))]
        public int standard_ticket_Subway { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, SubwayGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Subway))]
        public int max_ticket_increase_Subway { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, SubwayGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Subway))]
        public int max_ticket_discount_Subway { get; set; }

        [SettingsUISection(TransitSection, TrainGroup)]
        public bool disable_Train { get; set; }

        [SettingsUISlider(min = 10, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TrainGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Train))]
        public int target_occupancy_Train { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(TransitSection, TrainGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Train))]
        public int standard_ticket_Train { get; set; }

        [SettingsUISlider(min = 0, max = 300, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TrainGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Train))]
        public int max_ticket_increase_Train { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(TransitSection, TrainGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_Train))]
        public int max_ticket_discount_Train { get; set; }

        [SettingsUISlider(min = 0, max = 2, step = 0.1f, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(SettingsSection, SettingsGroup)]
        public float waiting_time_weight { get; set; }

        [SettingsUISlider(min = 5, max = 25, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SettingsSection, SettingsGroup)]
        public float threshold { get; set; }

        [SettingsUISection(SettingsSection, SettingsGroup)]
        public UpdateFreqEnum updateFreq { get; set; } = UpdateFreqEnum.min45;

        [SettingsUISection(SettingsSection, SettingsGroup)]
        public bool debug { get; set; }

        public enum UpdateFreqEnum
        {
            min45 = 32,
            min22 = 64,
            min90 = 16
        }


    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Smart Transportation" },
                { m_Setting.GetOptionTabLocaleID(Setting.TransitSection), "Transit" },
                { m_Setting.GetOptionTabLocaleID(Setting.SettingsSection), "Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.BusGroup), "Bus Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TramGroup), "Tram Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SubwayGroup), "Subway Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TrainGroup), "Train Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SettingsGroup), "Settings Settings" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.debug)), "Write Transit Information to Log" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.debug)), "Writes information used to make decision on ticket price and frequency for each route, such as transit occupancy and number of vehicles, to log." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.updateFreq)), "Update Frequency (In-game minutes)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.updateFreq)), "How frequent Smart Transportation will evaluate each route and decide to update vehicles or ticket prices. Time is in in-game minutes. Note that if you are using the slow feature from the Realistic Trip mods, this time is based on the vanilla game, with that feature you need to divide this time with the slow time factor to get the actual time with that mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.waiting_time_weight)), "Weighting Time Weight" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.waiting_time_weight)), "When this mod calculates target occupancy for each route, it will take into account the passengers that are waiting at a station stop. This weight is applied to the number of passengers waiting when doing this calculation. The assumption is that when those passengers board, some passengers that are already in the vehicle will deboard." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.threshold)), "Target Occupancy Threshold" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.threshold)), "Number of percentage points within the target occupancy that will be considered when checking if the target occupancy has been met." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_bus)), "Disable Bus Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_bus)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_bus)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_bus)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_bus)), "Max. Ticket Discount" },

                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_bus)), "Disable bus daynamic pricing and frequency and use standard values for tickets and frequency. " },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.target_occupancy_bus)), "Target occupancy for buses. The mod will change ticket prices and vehicle frequency to try to reach this occupancy target." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.standard_ticket_bus)), "Standard ticket price. Vanilla value is 8." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_increase_bus)), "Maximum ticket increase. Ticket prices will increase at peak hours to generate more revenue and discourage cims to use transit at rush hours." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_discount_bus)), "Maximum ticket decrease. Ticket prices will decrease at off-peak hours to attract more passengers." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_Tram)), "Disable Tram Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_Tram)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_Tram)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_Tram)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_Tram)), "Max. Ticket Discount" },

                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_Tram)), "Disable tram daynamic pricing and frequency and use standard values for tickets and frequency. " },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.target_occupancy_Tram)), "Target occupancy for Trams. The mod will change ticket prices and vehicle frequency to try to reach this occupancy target." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.standard_ticket_Tram)), "Standard ticket price. Vanilla value is 8." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_increase_Tram)), "Maximum ticket increase. Ticket prices will increase at peak hours to generate more revenue and discourage cims to use transit at rush hours." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_discount_Tram)), "Maximum ticket decrease. Ticket prices will decrease at off-peak hours to attract more passengers." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_Subway)), "Disable Subway Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_Subway)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_Subway)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_Subway)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_Subway)), "Max. Ticket Discount" },

                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_Subway)), "Disable subway daynamic pricing and frequency and use standard values for tickets and frequency. " },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.target_occupancy_Subway)), "Target occupancy for Subways. The mod will change ticket prices and vehicle frequency to try to reach this occupancy target." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.standard_ticket_Subway)), "Standard ticket price. Vanilla value is 8." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_increase_Subway)), "Maximum ticket increase. Ticket prices will increase at peak hours to generate more revenue and discourage cims to use transit at rush hours." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_discount_Subway)), "Maximum ticket decrease. Ticket prices will decrease at off-peak hours to attract more passengers." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_Train)), "Disable Train Dynamic Pricing & Frequency" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.target_occupancy_Train)), "Target Occupancy" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.standard_ticket_Train)), "Standard Ticket Price" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_increase_Train)), "Max. Ticket Increase" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.max_ticket_discount_Train)), "Max. Ticket Discount" },

                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_Train)), "Disable Train daynamic pricing and frequency and use standard values for tickets and frequency. " },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.target_occupancy_Train)), "Target occupancy for Trains. The mod will change ticket prices and vehicle frequency to try to reach this occupancy target." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.standard_ticket_Train)), "Standard ticket price. Vanilla value is 8." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_increase_Train)), "Maximum ticket increase. Ticket prices will increase at peak hours to generate more revenue and discourage cims to use transit at rush hours." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.max_ticket_discount_Train)), "Maximum ticket decrease. Ticket prices will decrease at off-peak hours to attract more passengers." },

                { m_Setting.GetEnumValueLocaleID(Setting.UpdateFreqEnum.min22), "22" },
                { m_Setting.GetEnumValueLocaleID(Setting.UpdateFreqEnum.min45), "45" },
                { m_Setting.GetEnumValueLocaleID(Setting.UpdateFreqEnum.min90), "90" },

            };
        }

        public void Unload()
        {

        }

    }
}
