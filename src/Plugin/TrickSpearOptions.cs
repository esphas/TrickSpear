using Menu.Remix.MixedUI;
using UnityEngine;

namespace TrickSpear;

public sealed class TrickSpearOptions : OptionInterface
{
    internal static TrickSpearOptions? Instance { get; private set; }

    public readonly Configurable<bool> AutoTwirlEnabled = null!;
    public readonly Configurable<bool> AutoTwirlBellySlide = null!;
    public readonly Configurable<bool> AutoTwirlTurnSkid = null!;
    public readonly Configurable<bool> AutoTwirlSlideRoll = null!;
    public readonly Configurable<bool> AutoTwirlBackflip = null!;
    public readonly Configurable<bool> SpinSmallObjectInteract = null!;
    public readonly Configurable<bool> SpinParryWindow = null!;
    public readonly Configurable<int> ParryWindowFrames = null!;
    public readonly Configurable<bool> DebugLogging = null!;

    public TrickSpearOptions()
    {
        Instance = this;

        AutoTwirlEnabled = config.Bind("AutoTwirlEnabled", false);
        AutoTwirlBellySlide = config.Bind("AutoTwirlBellySlide", true);
        AutoTwirlTurnSkid = config.Bind("AutoTwirlTurnSkid", true);
        AutoTwirlSlideRoll = config.Bind("AutoTwirlSlideRoll", true);
        AutoTwirlBackflip = config.Bind("AutoTwirlBackflip", true);
        SpinSmallObjectInteract = config.Bind("SpinSmallObjectInteract", false);
        SpinParryWindow = config.Bind("SpinParryWindow", false);
        ParryWindowFrames = config.Bind("ParryWindowFrames", 5, new ConfigAcceptableRange<int>(1, 30));
        DebugLogging = config.Bind("DebugLogging", false);

        OnConfigChanged += ApplyToRuntime;
        OnActivate += ApplyToRuntime;
    }

    public override void Initialize()
    {
        base.Initialize();

        Tabs =
        [
            BuildAutoTwirlTab(),
            BuildCombatTab(),
            BuildDebugTab(),
        ];
    }

    internal void ApplyToRuntime()
    {
        TwirlAutoTriggerConfig.Enabled = AutoTwirlEnabled.Value;
        TwirlAutoTriggerConfig.OnBellySlide = AutoTwirlBellySlide.Value;
        TwirlAutoTriggerConfig.OnTurnSkid = AutoTwirlTurnSkid.Value;
        TwirlAutoTriggerConfig.OnSlideRoll = AutoTwirlSlideRoll.Value;
        TwirlAutoTriggerConfig.OnBackflip = AutoTwirlBackflip.Value;
        TwirlCombatConfig.SpinSmallObjectInteract = SpinSmallObjectInteract.Value;
        TwirlCombatConfig.SpinParryWindow = SpinParryWindow.Value;
        TwirlCombatConfig.ParryWindowFrames = ParryWindowFrames.Value;
        TwirlDebug.Enabled = DebugLogging.Value;
    }

    private OpTab BuildAutoTwirlTab()
    {
        var tab = new OpTab(this, L(LocKeys.OptionsTabAutoTwirl));
        tab.AddItems(
            new OpLabel(40f, 530f, L(LocKeys.OptionsAutoTwirlTitle), bigText: true),
            new OpCheckBox(AutoTwirlEnabled, new Vector2(40f, 460f)),
            new OpLabel(75f, 460f, L(LocKeys.OptionsAutoTwirlEnable)),
            new OpCheckBox(AutoTwirlBellySlide, new Vector2(55f, 420f)),
            new OpLabel(90f, 420f, L(LocKeys.OptionsAutoTwirlBellySlide)),
            new OpCheckBox(AutoTwirlTurnSkid, new Vector2(55f, 380f)),
            new OpLabel(90f, 380f, L(LocKeys.OptionsAutoTwirlTurnSkid)),
            new OpCheckBox(AutoTwirlSlideRoll, new Vector2(55f, 340f)),
            new OpLabel(90f, 340f, L(LocKeys.OptionsAutoTwirlSlideRoll)),
            new OpCheckBox(AutoTwirlBackflip, new Vector2(55f, 300f)),
            new OpLabel(90f, 300f, L(LocKeys.OptionsAutoTwirlBackflip)));
        return tab;
    }

    private OpTab BuildCombatTab()
    {
        var tab = new OpTab(this, L(LocKeys.OptionsTabCombat));
        tab.AddItems(
            new OpLabel(40f, 530f, L(LocKeys.OptionsCombatTitle), bigText: true),
            new OpCheckBox(SpinSmallObjectInteract, new Vector2(40f, 460f)),
            new OpLabel(75f, 460f, L(LocKeys.OptionsCombatObjectInteract)),
            new OpLabel(55f, 420f, L(LocKeys.OptionsCombatObjectInteractDesc)),
            new OpCheckBox(SpinParryWindow, new Vector2(40f, 370f)),
            new OpLabel(75f, 370f, L(LocKeys.OptionsCombatParryWindow)),
            new OpLabel(55f, 330f, L(LocKeys.OptionsCombatParryWindowDesc)),
            new OpUpdown(ParryWindowFrames, new Vector2(55f, 290f), 80f),
            new OpLabel(145f, 290f, L(LocKeys.OptionsCombatParryFrames)));
        return tab;
    }

    private OpTab BuildDebugTab()
    {
        var tab = new OpTab(this, L(LocKeys.OptionsTabDebug));
        tab.AddItems(
            new OpLabel(40f, 530f, L(LocKeys.OptionsDebugTitle), bigText: true),
            new OpCheckBox(DebugLogging, new Vector2(40f, 460f)),
            new OpLabel(75f, 460f, L(LocKeys.OptionsDebugLogging)));
        return tab;
    }

    internal static string L(string key) => Translate(key);
}
