using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
using Game.Modding;
using Game.SceneFlow;
using System.IO;
using Unity.Entities;

namespace SmartTransportation
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(SmartTransportation)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

      public static Setting m_Setting;
        public static readonly string harmonyID = "SmartTransportation";

        // Mods Settings Folder
        public static string SettingsFolder = Path.Combine(EnvPath.kUserDataPath, "ModsSettings", nameof(SmartTransportation));
        readonly public static int kComponentVersion = 1;
        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));


            AssetDatabase.global.LoadSettings(nameof(SmartTransportation), m_Setting, new Setting(this));

            updateSystem.UpdateAt<SmartTransitSystem>(SystemUpdatePhase.GameSimulation);
            //updateSystem.UpdateAt<SmartTaxiSystem>(SystemUpdatePhase.GameSimulation);
            //updateSystem.UpdateAt<PolicySliderDataUpdaterSystem>(SystemUpdatePhase.GameSimulation);

        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }
}
