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
        public List<AreaBody> AreaBodies { get; protected set; }
        public bool IsExpired { get; protected set; }

        public BaseActiveArea()
        {
            this.AreaBodies = new List<AreaBody>();

        }
        
        internal void UpdateAreaBodyAABBs()
        {
            foreach( var areaBody in this.AreaBodies )
            {
                areaBody.UpdateAABB();
            }
        }

        internal virtual void UpdateAABB()
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
