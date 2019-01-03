using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            base.UpdateAABB();
        }
    }
}
