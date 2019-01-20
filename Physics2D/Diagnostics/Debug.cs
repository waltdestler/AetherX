using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tainicom.Aether.Physics2D.Diagnostics
{
    internal static class Debug
    {
        internal static void Assert(bool condition, string message = "unspecified")
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        internal static void WriteLine(string message)
        {
            // TODO
        }

        internal static void WriteLine(string message, params string[] placeholders)
        {
            // TODO
        }
    }
}
