using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.Linq;
using System.Collections.Generic;


namespace Celeste.Mod.EuclidHelper.Entities;

[CustomEntity("EuclidHelper/Portal")]
public class Portal : Entity
{
    public static Portal inPortal;
    static RenderTarget2D renderTarget = new RenderTarget2D(Engine.Graphics.GraphicsDevice, 320, 184);
    Camera camera;
    Vector2 originalCamera;
    Vector2 Scale = Vector2.Zero;
    public Vector2 LoopSpeed = Vector2.Zero;
    public Vector2 InnerLoopSpeed = Vector2.Zero;
    public Vector2 LoopDistance = Vector2.Zero;
    public Vector2 InnerLoopDistance = Vector2.Zero;
    public Vector2 node;
    public Vector2 InitPosition;
    Vector2 InnerInitPosition;
    float cameraX;
    float cameraY;
    public static bool PortalRendering = false;
    static readonly Type[] Blacklist = [typeof(Player), typeof(Portal), typeof(PortalSafeSolid), typeof(SolidTiles), typeof(BackgroundTiles), typeof(Decal), typeof(Trigger)];
    public Portal(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        InitPosition = data.Position + offset;
        Depth = 2147483647;
        Scale.X = data.Width;
        Scale.Y = data.Height;

        LoopSpeed = new Vector2(data.Float("loopSpeedX", 0f), data.Float("loopSpeedY", 0f));
        InnerLoopSpeed = new Vector2(data.Float("innerLoopSpeedX", 0f), data.Float("innerLoopSpeedY", 0f));

        node = data.Nodes[0] + offset;
        InnerInitPosition = node;

        Collider = new Hitbox(Scale.X, Scale.Y, 0, 0);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        camera = SceneAs<Level>().Camera;
    }
    public Vector2 GetOffset => node - Position;
    public override void Update()
    {
        LoopDistance += LoopSpeed * Engine.DeltaTime;
        if (LoopDistance.X > Scale.X)
            LoopDistance.X -= Scale.X;

        if (LoopDistance.X < -Scale.X)
            LoopDistance.X += Scale.X;

        if (LoopDistance.Y > Scale.Y)
            LoopDistance.Y -= Scale.Y;

        if (LoopDistance.Y < -Scale.Y)
            LoopDistance.Y += Scale.Y;

        InnerLoopDistance += InnerLoopSpeed * Engine.DeltaTime;
        if (InnerLoopDistance.X > Scale.X)
            InnerLoopDistance.X -= Scale.X;

        if (InnerLoopDistance.X < -Scale.X)
            InnerLoopDistance.X += Scale.X;

        if (InnerLoopDistance.Y > Scale.Y)
            InnerLoopDistance.Y -= Scale.Y;

        if (InnerLoopDistance.Y < -Scale.Y)
            InnerLoopDistance.Y += Scale.Y;

        Position = new Vector2((int)Math.Floor(InitPosition.X + LoopDistance.X), (int)Math.Floor(InitPosition.Y + LoopDistance.Y));
        node = new Vector2((int)Math.Floor(InnerInitPosition.X + InnerLoopDistance.X), (int)Math.Floor(InnerInitPosition.Y + InnerLoopDistance.Y));

        originalCamera = camera.Position;

        Player player = SceneAs<Level>().Tracker.GetEntity<Player>();
        if (player != null)
            if (CollideCheck<Player>() && inPortal == null)
            {
                inPortal = this;
                foreach (var follower in player.Leader.Followers)
                {
                    follower.Entity.Position += node - Position;
                }
                player.Position += node - Position;
                camera.Position += node - Position;
            }

        Vector2 oldPos = Position;

        Position = node;

        if (player != null)
            if (inPortal == this && !CollideCheck<Player>())
            {
                inPortal = null;
            }

        Position = oldPos; 
        
        foreach (var entity in Scene.Entities)
        {
            if (player != null)
                if (entity != this && CollideCheck(entity) && !Blacklist.Contains(entity.GetType()) && !player.Leader.Followers.Any(follower => follower.Entity == entity))
                {
                    if (entity is Solid solid && solid.HasPlayerRider())
                    {
                        if (inPortal == null)
                        {
                            player.Position += node - Position;
                            inPortal = this;
                            camera.Position += node - Position;

                            foreach (var follower in player.Leader.Followers)
                            {
                                follower.Entity.Position += node - Position;
                            }

                            entity.Position += node - Position;
                        }
                    } else
                    {
                        entity.Position += node - Position;
                    }
                }
        }
        base.Update();
    }
    public override void Render()
    {
        if (PortalRendering) return;
        PortalRendering = true;
        Draw.SpriteBatch.End();
        
        Vector2 mainCameraPos = camera.Position;
        Vector2 offset = node - Position;

        List<Entity> touching = new();

        foreach (Entity entity in Scene.Entities)
        {
            if (CollideCheck(entity) && entity is not Portal && entity is not PortalSafeSolid && !entity.TagCheck(Tags.HUD) && entity.Visible)
            {
                touching.Add(entity);
                entity.Position += offset;
            }
        }

        Engine.Graphics.GraphicsDevice.SetRenderTarget(renderTarget);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
        Vector2 portalWorldPos = Position + (offset * (0 + 1));
        Vector2 lastPortalWorldPos = portalWorldPos;
        
        Vector2 desiredCameraPos = mainCameraPos;
        
        float viewportWidth = 320f;
        float viewportHeight = 184f;
        float minCameraX = portalWorldPos.X;
        float maxCameraX = portalWorldPos.X + Scale.X - viewportWidth;
        float minCameraY = portalWorldPos.Y;
        float maxCameraY = portalWorldPos.Y + Scale.Y - viewportHeight;

        cameraX = Calc.Clamp(desiredCameraPos.X + offset.X, minCameraX, maxCameraX);
        cameraY = Calc.Clamp(desiredCameraPos.Y + offset.Y, minCameraY, maxCameraY);

        camera.Position = new Vector2((int)Math.Floor(cameraX), (int)Math.Floor(cameraY));

        Draw.SpriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null
        );
        Draw.SpriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();

        Draw.SpriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            camera.Matrix
        );

        foreach (Entity entity in Scene.Entities
            .Where(e => !e.TagCheck(Tags.HUD) && e.Visible)
            .OrderByDescending(e => e.Depth))
        {
            entity.Render();
        }

        Draw.SpriteBatch.End();

        Vector2 renderOffset = camera.Position - lastPortalWorldPos;
        camera.Position = originalCamera;
        
        Engine.Graphics.GraphicsDevice.SetRenderTarget((RenderTarget2D)GameplayBuffers.Gameplay);
        Draw.SpriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            camera.Matrix
        );
        Draw.SpriteBatch.Draw(renderTarget, Position + renderOffset, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        foreach (Entity entity in touching)
        {
            entity.Position -= offset;
        }

        PortalRendering = false;

        // Draw.Rect(cameraX, cameraY, 320f, 184f, Color.Magenta * 0.1f);
        // Draw.Rect(Position.X + renderOffset.X, Position.Y + renderOffset.Y, 320f, 184f, Color.Red * 0.1f);
    }
}