using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Celeste.Mod.EuclidHelper.Entities;
using Monocle;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.EuclidHelper;

public class EuclidHelperModule : EverestModule {
    public static EuclidHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(EuclidHelperModuleSettings);
    public static EuclidHelperModuleSettings Settings => (EuclidHelperModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(EuclidHelperModuleSession);
    public static EuclidHelperModuleSession Session => (EuclidHelperModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(EuclidHelperModuleSaveData);
    public static EuclidHelperModuleSaveData SaveData => (EuclidHelperModuleSaveData) Instance._SaveData;
    
    static bool Rerendering = false;
    static bool Recolliding = false;
    public static List<Portal> portalCache = new();

    public EuclidHelperModule() {
        Instance = this;
#if DEBUG
        Logger.SetLogLevel(nameof(EuclidHelperModule), LogLevel.Verbose);
#else
        Logger.SetLogLevel(nameof(EuclidHelperModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        Everest.Events.Level.OnLoadLevel += OnLoadLevel;
        On.Monocle.Entity.Render += OnEntityRender;
    }

    public override void Unload() {
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
        On.Monocle.Entity.Render -= OnEntityRender;
    }

    private void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        portalCache.Clear();
        Portal.inPortal = null;
    }

    private void OnEntityRender(On.Monocle.Entity.orig_Render orig, Entity self)
    {
        orig(self);
        if (self is Portal or PortalSafeSolid or SolidTiles or BackgroundTiles or Player || Rerendering) return;
        if (Portal.PortalRendering) return;
        
        Portal portal = portalCache.FirstOrDefault(portal => portal.CollideCheck(self));
        if (portal == null) return;

        Rerendering = true;
        self.Position += portal.GetOffset;
        self.Render();
        self.Position -= portal.GetOffset;
        Rerendering = false;
    }
}