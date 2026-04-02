using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EuclidHelper.Entities;

[CustomEntity("EuclidHelper/PortalSafeSolid")]
public class PortalSafeSolid : Solid
{
    Portal portal;
    Vector2 offset;
    Vector2 node;
    public PortalSafeSolid(EntityData data, Vector2 offset)
        : base(data.Position + offset, 16f, 16f, false)
    {
        Collider = new Hitbox(data.Width, data.Height, 0, 0);

        node = data.Nodes[0] + offset;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        Vector2 initPos = Position;
        Position = node;
        foreach (Entity entity in Scene.Entities)
        {
            if (entity is Portal && entity.CollideCheck(this))
            {
                this.portal = (Portal)entity;
                break;
            }
        }
        Position = initPos;
        if (portal == null)
            throw new Exception("PortalSafeSolid must be placed on top of a Portal");

        offset = Position - portal.Position;
    }
    public override void Update()
    {
        base.Update();
        Vector2 oldPos = Position;
        Position = portal.node + offset - portal.LoopDistance;
        Vector2 delta = Position - oldPos;

        if (HasPlayerRider())
        {
            Player player = GetPlayerRider();
            player.MoveH(delta.X);
            player.MoveV(delta.Y);
        }
    }
}