using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;

namespace BetterEducationInfoView
{
    [FileLocation(Mod.ModName)]
    [SettingsUIGroupOrder(kOverlayGroup, kThresholdGroup)]
    [SettingsUIShowGroupName(kOverlayGroup, kThresholdGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kOverlayGroup = "Overlay";
        public const string kThresholdGroup = "Thresholds";

        public static Setting Instance { get; set; }

        public Setting(IMod mod) : base(mod)
        {
        }

        [SettingsUISection(kSection, kOverlayGroup)]
        public bool OverlayEnabled { get; set; } = true;

        [SettingsUISection(kSection, kOverlayGroup)]
        public bool HideEmptySchools { get; set; }

        [SettingsUISlider(min = 50, max = 200, step = 5, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kOverlayGroup)]
        public int OverlayScale { get; set; } = 140;

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kThresholdGroup)]
        public int YellowThreshold { get; set; } = 70;

        [SettingsUISlider(min = 1, max = 150, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kSection, kThresholdGroup)]
        public int RedThreshold { get; set; } = 90;

        public override void SetDefaults()
        {
            OverlayEnabled = true;
            HideEmptySchools = false;
            OverlayScale = 140;
            YellowThreshold = 70;
            RedThreshold = 90;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Better Education Infoview" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kOverlayGroup), "Overlay" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kThresholdGroup), "Thresholds" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayEnabled)), "Show school capacity overlay" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayEnabled)), "Shows student/capacity labels when the Education infoview is active." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.HideEmptySchools)), "Hide empty schools" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.HideEmptySchools)), "Only shows labels for schools that currently have students." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayScale)), "Overlay scale" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayScale)), "Scales the school capacity labels up or down." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.YellowThreshold)), "Yellow threshold" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.YellowThreshold)), "Capacity percentage where labels turn yellow." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RedThreshold)), "Red threshold" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RedThreshold)), "Capacity percentage where labels turn red." }
            };
        }

        public void Unload()
        {
        }
    }
}
