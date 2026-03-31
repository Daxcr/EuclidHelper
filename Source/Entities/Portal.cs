using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.Linq;


namespace Celeste.Mod.EuclidHelper.Entities;

[CustomEntity("EuclidHelper/Portal")]
public class Portal : Entity
{
    public static Portal inPortal;
    static RenderTarget2D[] renderTargets = new RenderTarget2D[10];
    Camera camera;
    static int PortalDepth = 0;
    Vector2 originalCamera;
    Vector2 Scale = Vector2.Zero;
    int Targets = 1;
    Vector2 LoopSpeed = Vector2.Zero;
    Vector2 InnerLoopSpeed = Vector2.Zero;
    Vector2 LoopDistance = Vector2.Zero;
    Vector2 InnerLoopDistance = Vector2.Zero;
    public Vector2 node;
    Vector2 InitPosition;
    Vector2 InnerInitPosition;
    static readonly Type[] Blacklist = [typeof(Player), typeof(Portal), typeof(PortalSafeSolid), typeof(SolidTiles), typeof(BackgroundTiles)];
    public Portal(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        InitPosition = data.Position + offset;
        Depth = -2147483648;
        Scale.X = data.Width;
        Scale.Y = data.Height;

        Targets = data.Int("iterations", 1);
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
        for (int i = 0; i < Targets; i++)
            renderTargets[i] = new RenderTarget2D(Engine.Graphics.GraphicsDevice, 320, 184);
    }
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

        if (inPortal == this && !CollideCheck<Player>())
        {
            inPortal = null;
        }

        Position = oldPos;
        
        foreach (var entity in Scene.Entities)
        {
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
        if (PortalDepth > 0) return;
        PortalDepth = 1;
        Draw.SpriteBatch.End();
        Vector2 lastPortalWorldPos = Vector2.Zero;
        
        Vector2 mainCameraPos = camera.Position;
        
        for (int i = 0; i < Targets; i++)
        {
            Engine.Graphics.GraphicsDevice.SetRenderTarget(renderTargets[i]);
            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
            Vector2 offset = node - Position;
            Vector2 portalWorldPos = Position + (offset * (i + 1));
            lastPortalWorldPos = portalWorldPos;
            
            Vector2 desiredCameraPos = mainCameraPos;
            
            float viewportWidth = 320f;
            float viewportHeight = 184f;
            float minCameraX = portalWorldPos.X;
            float maxCameraX = portalWorldPos.X + Scale.X - viewportWidth;
            float minCameraY = portalWorldPos.Y;
            float maxCameraY = portalWorldPos.Y + Scale.Y - viewportHeight;
            float cameraX = Math.Max(minCameraX, Math.Min(desiredCameraPos.X, maxCameraX));
            float cameraY = Math.Max(minCameraY, Math.Min(desiredCameraPos.Y, maxCameraY));
            camera.Position = new Vector2((int)Math.Floor(cameraX), (int)Math.Floor(cameraY));

            if (i > 0)
            {
                Draw.SpriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone,
                    null
                );
                Draw.SpriteBatch.Draw(renderTargets[i - 1], Vector2.Zero, Color.White);
                Draw.SpriteBatch.End();
            }

            Draw.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                camera.Matrix
            );
            foreach (var entity in Scene.Entities)
            {
                if (!entity.TagCheck(Tags.HUD) && entity.Visible)
                {
                    entity.Render();
                    if (CollideCheck(entity) && entity is not Portal && entity is not PortalSafeSolid)
                    {
                        entity.Position += offset;
                        entity.Render();
                        entity.Position -= offset;
                    }
                }
            }
            Draw.SpriteBatch.End();
        }
        
        PortalDepth = 0;

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
        Draw.SpriteBatch.Draw(renderTargets[Targets - 1], Position + renderOffset, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
    }
}