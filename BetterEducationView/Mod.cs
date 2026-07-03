using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using BetterEducationInfoView.Systems;

namespace BetterEducationInfoView
{
    public class Mod : IMod
    {
        public const string ModName = "BetterEducationInfoView";
        public static ILog log = LogManager.GetLogger($"{ModName}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            Setting.Instance = m_Setting;
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));


            AssetDatabase.global.LoadSettings(ModName, m_Setting, new Setting(this));
            updateSystem.UpdateAt<EducationOverlayRendererSystem>(SystemUpdatePhase.Rendering);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
                Setting.Instance = null;
            }
        }
    }
}
