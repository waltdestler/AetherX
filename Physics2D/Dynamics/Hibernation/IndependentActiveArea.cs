using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tainicom.Aether.Physics2D.Dynamics.Hibernation
{
    public class IndependentActiveArea : BaseActiveArea
    {
        public IndependentActiveArea() : base()
        {
            this.AreaType = ActiveAreaType.Independent;
        }

        public void SetPosition(Vector2 position)
        {
            this.Position = position;
        }

        public void SetRadius(float radius)
        {
            this.Radius = radius;
        }
    }
}
