using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    public class LogicTransform
    {
        public LogicVector3 position;
        public LogicVector3 faceToD4;

        public static float DstToFloat(int iDst)
        {
            return (float)iDst * 1 / (float)MathUtils.iPointUnit;
        }

        public LogicTransform()
        {
        }

        public LogicTransform(LogicVector3 _position, LogicVector3 _faceToD4)
        {
            position = _position;
            faceToD4 = _faceToD4;
        }

    }


}
