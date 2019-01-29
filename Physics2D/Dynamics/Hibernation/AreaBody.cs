using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public class AreaBody
    {
        public Body Body { get; set; }
        public AreaBodyStatus PositionStatus { get; set; }
        public AreaBodyStatus PriorStatus { get; set; }
        public AABB AABB;
        
        public AreaBody(Body body)
        {
            this.Body = body;
            this.UpdateAABB();
        }

        public void UpdateAABB()
        {
            this.AABB = BaseActiveArea.CalculateBodyAABB(this.Body);
        }
    }
}
