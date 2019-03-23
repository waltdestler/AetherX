using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tainicom.Aether.Physics2D.Utilities
{
    public interface IScreen
    {
        Vector2 ConvertWorldToScreen(Vector2 position);
        Vector2 ConvertScreenToWorld(int x, int y);
    }
}
