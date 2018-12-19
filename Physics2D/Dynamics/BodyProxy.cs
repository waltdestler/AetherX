using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tainicom.Aether.Physics2D.Collision;

namespace tainicom.Aether.Physics2D.Dynamics
{
    public struct BodyProxy : IProxy
    {
        public AABB AABB;
        public Body Body;
        public int ProxyId;

        AABB IProxy.AABB => AABB;
    }
}
