using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.EuclidHelper.Entities;

[CustomEntity("EuclidHelper/Portal")]
public class Portal : Entity
{
    static Portal inPortal;
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
            renderTargets[i] = new RenderTarget2D(Engine.Graphics.GraphicsDevice, (int)Scale.X, (int)Scale.Y);
    }
    public override void Update()
    {
        base.Update();
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

        if (CollideCheck<Player>() && inPortal == null)
        {
            inPortal = this;
            Player player = SceneAs<Level>().Tracker.GetEntity<Player>();
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
            if (entity != this && CollideCheck(entity) && entity is not Player && entity is not Portal && entity is not PortalSafeSolid)
            {
                entity.Position += node - Position;
            }
        }
    }
    public override void Render()
    {
        if (PortalDepth > 0) return;

        PortalDepth = 1;
        Draw.SpriteBatch.End();
        var level = SceneAs<Level>();
        for (int i = 0; i < Targets; i++)
        {
            Engine.Graphics.GraphicsDevice.SetRenderTarget(renderTargets[i]);
            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
            Vector2 offset = node - Position;

            camera.Position = Position + (offset * (i + 1));

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
                if (!entity.TagCheck(Tags.HUD))
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
            // Draw.Rect(camera.Position.X, camera.Position.Y, Scale.X, Scale.Y, Color.Magenta * 0.1f);
            Draw.SpriteBatch.End();
        }
        PortalDepth = 0;
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
        Draw.SpriteBatch.Draw(renderTargets[Targets - 1], Position, Color.White);
    }
}