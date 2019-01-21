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

        /// <summary>
        /// Creation time in UTC and ticks.
        /// </summary>
        private long CreationUtcTime { get; set; }

        public float SecondsAgoCreated
        {
            get
            {
                var ts = new TimeSpan(DateTime.UtcNow.Ticks - this.CreationUtcTime);
                return (float)ts.TotalSeconds;
            }
        }

        internal BodyActiveArea(Body trackedBody) : base()
        {
            // store the body to track
            this.TrackedBody = trackedBody;

            // automatically add it to the list of bodies as "totally in"
            var areaBody = new AreaBody(trackedBody);
            areaBody.PositionStatus = AreaBodyStatus.TotallyIn;
            this.Bodies.Add(areaBody);
            
            // set type
            this.AreaType = ActiveAreaType.BodyTracking;

            // store creation time
            this.RenewExpiration();
        }

        public float BodyAABBMargin = Settings.BodyActiveAreaMargin;

        internal override void Update()
        {
            if (this.TrackedBody != null)
            {
                // simply center on the body.
                this.Position = this.TrackedBody.Position;
            }

            // update AABB to body's AABB
            //this.TrackedBody.World.ContactManager.BroadPhase.GetFatAABB(this.TrackedBody.BroadphaseProxyId, out this.AABB);

            //// add a little margin 
            //const float AABB_MARGIN = Settings.BodyActiveAreaMargin;
            //this.AABB = new AABB(this.AABB.Center, this.AABB.Width + AABB_MARGIN, this.AABB.Height + AABB_MARGIN);

            this.AABB = BaseActiveArea.CalculateBodyAABB(this.TrackedBody, BodyAABBMargin);

            // update whether is expired
            this.IsExpired = this.SecondsAgoCreated >= Settings.SecondsUntilHibernate;
        }

        internal void RenewExpiration()
        {
            // snapshot this time. it could be creation or renewal.
            this.CreationUtcTime = DateTime.UtcNow.Ticks;

            // reset expiration.
            this.IsExpired = false;
        }
    }
}
