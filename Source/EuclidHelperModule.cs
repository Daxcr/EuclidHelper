using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.EuclidHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;

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
    public static List<Entity> keepaliveCache = new();
    public static HashSet<Portal> activePortalCache = new();

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
        On.Celeste.Level.Update += OnLevelUpdate;
        On.Monocle.Entity.Render += OnEntityRender;
        On.Monocle.Entity.Added += OnEntityAdded;
        On.Monocle.Collide.Check_Entity_Entity += OnCollideCheckEntities;
    }

    public override void Unload() {
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
        On.Celeste.Level.Update -= OnLevelUpdate;
        On.Monocle.Entity.Render -= OnEntityRender;
        On.Monocle.Entity.Added -= OnEntityAdded;
        On.Monocle.Collide.Check_Entity_Entity -= OnCollideCheckEntities;
    }

    private void OnLevelUpdate(On.Celeste.Level.orig_Update orig, Level self)
    {
        orig(self);
        // Console.WriteLine(activePortalCache.Count);
    }

    private bool OnCollideCheckEntities(On.Monocle.Collide.orig_Check_Entity_Entity orig, Entity a, Entity b)
    {
        if (orig(a, b)) return true;
        if (a is Portal || b is Portal || Recolliding || portalCache.Count == 0) return false;
        if (a is not (SolidTiles or Solid) && b is not (SolidTiles or Solid)) return orig(a, b);
        Recolliding = true;
        try
        {
            foreach (Portal portal in activePortalCache)
            {
                float threshold = (portal.Width + portal.Height) * (portal.Width + portal.Height);
                Vector2 offset = portal.GetOffset;

                bool aIsClose = Vector2.DistanceSquared(a.Center, portal.Center) < threshold;
                bool bIsClose = Vector2.DistanceSquared(b.Center, portal.Center) < threshold;

                if (!aIsClose && !bIsClose) continue;

                if (aIsClose && portal.CollideCheck(a))
                {
                    a.Position += offset;
                    bool result = orig(a, b);
                    a.Position -= offset;
                    if (result) return true;
                }
                if (bIsClose && portal.CollideCheck(b))
                {
                    b.Position += offset;
                    bool result = orig(a, b);
                    b.Position -= offset;
                    if (result) return true;
                }
            }
        }
        finally
        {
            Recolliding = false;
        }
        return false;
    }

    private void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        portalCache.Clear();
        keepaliveCache.Clear();
        activePortalCache.Clear();
        portalCache.AddRange(level.Entities.OfType<Portal>());
        keepaliveCache.AddRange(level.Entities.OfType<Actor>());
        keepaliveCache.AddRange(level.Entities.OfType<Solid>());
        keepaliveCache.AddRange(level.Entities.OfType<Player>());
        Portal.inPortal = null;
    }

    private void OnEntityRender(On.Monocle.Entity.orig_Render orig, Entity self)
    {
        orig(self);
        if (self is Portal or SolidTiles or BackgroundTiles or Player || Rerendering) return;
        if (Portal.PortalRendering) return;
        
        Portal portal = portalCache.FirstOrDefault(portal => portal.CollideCheck(self) && portal.Life != 0);
        if (portal == null) return;

        Rerendering = true;
        self.Position += portal.GetOffset;
        self.Render();
        self.Position -= portal.GetOffset;
        Rerendering = false;
    }

    private void OnEntityAdded(On.Monocle.Entity.orig_Added orig, Entity self, Scene scene)
    {
        if (self is Solid or Player or Actor)
            keepaliveCache.Add(self);

        orig(self, scene);
    }
}