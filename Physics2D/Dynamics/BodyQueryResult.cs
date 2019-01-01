using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;

namespace tainicom.Aether.Physics2D.Dynamics
{
    public class BodyQueryResult 
    {
        public Body Body { get; private set; }
        public AABB BodyAabb { get; private set; }

        public BodyQueryResult( Body body, AABB BodyAabb)
        {
            this.Body = body;
            this.BodyAabb = BodyAabb;
        }

    }
}
