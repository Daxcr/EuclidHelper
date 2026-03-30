using System;
using Celeste.Mod.EuclidHelper.Entities;

namespace Celeste.Mod.EuclidHelper;

public class EuclidHelperModule : EverestModule {
    public static EuclidHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(EuclidHelperModuleSettings);
    public static EuclidHelperModuleSettings Settings => (EuclidHelperModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(EuclidHelperModuleSession);
    public static EuclidHelperModuleSession Session => (EuclidHelperModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(EuclidHelperModuleSaveData);
    public static EuclidHelperModuleSaveData SaveData => (EuclidHelperModuleSaveData) Instance._SaveData;

    public EuclidHelperModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(EuclidHelperModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(EuclidHelperModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        Everest.Events.Level.OnLoadLevel += OnLoadLevel;
    }

    public override void Unload() {
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
    }

    private void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        Portal.inPortal = null;
    }
}