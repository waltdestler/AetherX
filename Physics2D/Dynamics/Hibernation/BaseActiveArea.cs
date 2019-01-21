using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public abstract class BaseActiveArea
    {
        public AABB AABB;
        public Vector2 Position { get; protected set; }
        public float Radius { get; protected set; }
        public ActiveAreaType AreaType { get; protected set; }
        public List<AreaBody> Bodies { get; protected set; }
        public bool IsExpired { get; protected set; }

        public BaseActiveArea()
        {
            this.Bodies = new List<AreaBody>();

        }

        internal virtual void Update()
        {
            throw new NotImplementedException("Update method must be overridden in child class of BaseActiveArea.");
        }

        internal static AABB CalculateBodyAABB(Body body, float margin = Settings.BodyActiveAreaMargin)
        {
            // get body hibernation AABB
            AABB bodyAabb;
            body.World.ContactManager.BroadPhase.GetFatAABB(body.BroadphaseProxyId, out bodyAabb);

            // add a little margin 
            bodyAabb = new AABB(bodyAabb.Center, bodyAabb.Width + margin, bodyAabb.Height + margin);

            return bodyAabb;
        }
    }
}
