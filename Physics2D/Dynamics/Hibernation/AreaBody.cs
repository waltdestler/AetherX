using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public class AreaBody
    {
        public Body Body { get; set; }
        public AreaBodyStatus PositionStatus { get; set; }
        public AreaBodyStatus PriorStatus { get; set; }

        public AreaBody(Body body)
        {
            this.Body = body;
        }
    }
}
