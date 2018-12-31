using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    internal class BodyActiveArea : BaseActiveArea
    {
        private Body TrackedBody { get; set; }

        internal BodyActiveArea(Body trackedBody)
        {
            // store the body to track
            this.TrackedBody = trackedBody;
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
