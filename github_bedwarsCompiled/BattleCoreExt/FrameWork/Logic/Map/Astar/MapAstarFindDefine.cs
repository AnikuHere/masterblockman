using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOBA
{
    public enum AstarDir
    {
        UP,
        DOWN,
        LEFT,
        RIGHT,
        LEFTBOTTOM,
        RIGHTBOTTOM,
        LEFTTOP,
        RIGHTTOP,
    }

    public enum AstarNodeStatus
    {
        NONE,
        OPEN,
        CLOSE,
    }

}
