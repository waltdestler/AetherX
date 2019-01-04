using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    internal class BodyActiveArea : BaseActiveArea
    {
        internal Body TrackedBody { get; set; }

        internal BodyActiveArea(Body trackedBody) : base()
        {
            // store the body to track
            this.TrackedBody = trackedBody;
            this.AreaType = ActiveAreaType.BodyTracking;
        }

        internal override void UpdateAABB()
        {
            if (this.TrackedBody != null)
            {
                // simply center on the body.
                this.Position = this.TrackedBody.Position;
            }

            // update AABB to body's AABB
            this.TrackedBody.World.ContactManager.BroadPhase.GetFatAABB(this.TrackedBody.ProxyId, out this.AABB);

            // add a little margin 
            const float AABB_MARGIN = 20f;
            this.AABB = new AABB(this.AABB.Center, this.AABB.Width + AABB_MARGIN, this.AABB.Height + AABB_MARGIN);

            //base.UpdateAABB();
        }
    }
}
