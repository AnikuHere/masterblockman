#define NO_USE_FLOAT_SQRT

using System;
using System.Collections;

namespace MOBA
{
    [Serializable]
    public struct LogicVector3
    {
        public static LogicVector3 forward = new LogicVector3(0, 0, 10000);
        public static LogicVector3 right = new LogicVector3(10000, 0, 0);
        public static LogicVector3 down = new LogicVector3(0, -10000, 0);
        public static LogicVector3 up = new LogicVector3(0, 10000, 0);

        public static LogicVector3 zero = new LogicVector3(0, 0, 0);
        public static LogicVector3 one = new LogicVector3(10000, 10000, 10000);

        public int x;
        public int y;
        public int z;


        public override string ToString()
        {
            return string.Format("[LogicVector3:x={0}, y={1}, z={2}, magnitudeD4={3}]", x, y, z, magnitudeD4);
        }

        public static LogicVector3 Zero()
        {
            return zero;
        }

        public static LogicVector3 One()
        {
            return one;
        }

        public LogicVector3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public LogicVector3(ref LogicVector3 src)
        {
            this.x = src.x;
            this.y = src.y;
            this.z = src.z;
        }

        public void Set(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void Set(LogicVector3 src)
        {
            this.x = src.x;
            this.y = src.y;
            this.z = src.z;
        }

        public void Rotate(int angleD4)
        {
            long cosTheta = (long)MathUtils.CosD4(angleD4);
            long sinTheta = (long)MathUtils.SinD4(angleD4);
            int x = (int)(((long)this.x * cosTheta - (long)this.z * sinTheta) / MathUtils.iPointUnit);
            int z = (int)(((long)this.z * cosTheta + (long)this.x * sinTheta) / MathUtils.iPointUnit);
            this.x = x;
            this.z = z;
        }


        public LogicVector3 ProductD4(int val)
        {
            LogicVector3 ret = new LogicVector3();
            ret.x = (int)((long)x * (long)val / MathUtils.lPointUnit);
            ret.y = (int)((long)y * (long)val / MathUtils.lPointUnit);
            ret.z = (int)((long)z * (long)val / MathUtils.lPointUnit);

            x = ret.x;
            y = ret.y;
            z = ret.z;

            return ret;
        }

        public static LogicVector3 operator +(LogicVector3 a, LogicVector3 b)
        {
            LogicVector3 ret = new LogicVector3();
            ret.x = a.x + b.x;
            ret.y = a.y + b.y;
            ret.z = a.z + b.z;
            return ret;
        }
        public static LogicVector3 operator -(LogicVector3 a, LogicVector3 b)
        {
            LogicVector3 ret = new LogicVector3();
            ret.x = a.x - b.x;
            ret.y = a.y - b.y;
            ret.z = a.z - b.z;
            return ret;
        }
        public static LogicVector3 operator *(LogicVector3 a, int b)
        {
            LogicVector3 ret = new LogicVector3();
            ret.x = a.x * b;
            ret.y = a.y * b;
            ret.z = a.z * b;
            return ret;
        }

        public static LogicVector3 operator *(LogicVector3 a, long b)
        {
            LogicVector3 ret = new LogicVector3();
            ret.x = (int)((long)a.x * b);
            ret.y = (int)((long)a.y * b);
            ret.z = (int)((long)a.z * b);
            return ret;
        }

        public static LogicVector3 operator /(LogicVector3 a, int b)
        {
            LogicVector3 ret = new LogicVector3();
            ret.x = a.x / b;
            ret.y = a.y / b;
            ret.z = a.z / b;
            return ret;
        }

        public static int DotD4(LogicVector3 a, LogicVector3 b)
        {
            int iRet =
                (int)((long)a.x * (long)b.x / (long)MathUtils.iPointUnit)
                + (int)((long)a.y * (long)b.y / (long)MathUtils.iPointUnit)
                + (int)((long)a.z * (long)b.z / (long)MathUtils.iPointUnit);
            return iRet;
        }

        public LogicVector3 CrossD4(LogicVector3 b)
        {
            return new LogicVector3(
                    (int)(((long)y * (long)b.z - (long)z * (long)b.y) / MathUtils.lPointUnit),
                    (int)(((long)z * (long)b.x - (long)x * (long)b.z) / MathUtils.lPointUnit),
                    (int)(((long)x * (long)b.y - (long)y * (long)b.x) / MathUtils.lPointUnit));
        }

        public static LogicVector3 CrossD4(LogicVector3 a, LogicVector3 b)
        {
            return new LogicVector3(
                    (int)(((long)a.y * (long)b.z - (long)a.z * (long)b.y) / MathUtils.lPointUnit),
                    (int)(((long)a.z * (long)b.x - (long)a.x * (long)b.z) / MathUtils.lPointUnit),
                    (int)(((long)a.x * (long)b.y - (long)a.y * (long)b.x) / MathUtils.lPointUnit));
        }

        public QuaternionD4 getRotationTo(LogicVector3 dest)
        {
            // Based on Stan Melax's article in Game Programming Gems
            QuaternionD4 q = new QuaternionD4();
            // Copy, since cannot modify local
            LogicVector3 v0 = this;
            LogicVector3 v1 = dest;

            v0.NormalizeD4();
            v1.NormalizeD4();

            long d = LogicVector3.DotD4(v0, v1);//v0.dotProduct(v1);
            // If dot == 1, vectors are the same
            if (d >= MathUtils.iPointUnit)
            {
                return new QuaternionD4(MathUtils.iPointUnit, 0, 0, 0);
            }
            if (d <= (0 - MathUtils.iPointUnit))
            {
                LogicVector3 xzhou = new LogicVector3(MathUtils.iPointUnit, 0, 0);
                LogicVector3 axis = xzhou.CrossD4(this);
                if (axis.magnitudeD4 < 100)
                {
                    LogicVector3 yzhou = new LogicVector3(0, MathUtils.iPointUnit, 0);
                    axis = yzhou.CrossD4(this);
                }
                axis.NormalizeD4();
                q.FromAngleAxis(1800000, axis);
            }
            else
            {
                long s = MathUtils.SqrtD4((MathUtils.iPointUnit + d) * 2);//Real s = Math::Sqrt( (1+d)*2 );
                long invs = MathUtils.iPointUnit * MathUtils.lPointUnit / s;

                LogicVector3 c = v0.CrossD4(v1);

                q.x = (int)(c.x * invs / MathUtils.lPointUnit);
                q.y = (int)(c.y * invs / MathUtils.lPointUnit);
                q.z = (int)(c.z * invs / MathUtils.lPointUnit);
                q.w = (int)(s * 5000 / MathUtils.lPointUnit);

                q.normalise();
            }
            return q;
        }

        public static LogicVector3 LerpD4(LogicVector3 a, LogicVector3 b, int iBlendD4)
        {
            LogicVector3 ret = new LogicVector3();
            ret.x = MathUtils.LerpD4(a.x, b.x, iBlendD4);
            ret.y = MathUtils.LerpD4(a.y, b.y, iBlendD4);
            ret.z = MathUtils.LerpD4(a.z, b.z, iBlendD4);
            return ret;
        }

        public long magnitudeSq
        {
            get
            {
                return (long)x * (long)x + (long)y * (long)y + (long)z * (long)z;
            }
        }

        public int magnitudeD4
        {
            get
            {
            //按4位精度定点数计算
			long ret = MathUtils.SqrtD4(magnitudeSq / (long) MathUtils.iPointUnit);
            return (int)ret;
            }
        }

        public void NormalizeD4()
        {
            int iMag = magnitudeD4;
            if (iMag == 0)
            {
                return;
            }
            x = (int)(x * MathUtils.lPointUnit / iMag);
            y = (int)(y * MathUtils.lPointUnit / iMag);
            z = (int)(z * MathUtils.lPointUnit / iMag);
        }

        public LogicVector3 ForwardOnDir(int iLen)
        {
            int mag = magnitudeD4;
            LogicVector3 ret = zero;
            if (mag != 0)
            {
                ret.x = (int)((long)iLen * (long)x / (long)mag);
                ret.y = (int)((long)iLen * (long)y / (long)mag);
                ret.z = (int)((long)iLen * (long)z / (long)mag);
            }
            return ret;
        }
        //public static implicit operator LogicVector3(LogicVector3 src)
        // {
        //     return new LogicVector3(src);
        //}

        public LogicVector3 TransformPosAsDir2D(LogicVector3 pos)
        {
            LogicVector3 ret = new LogicVector3();

            LogicVector3 dirX = new LogicVector3(ref this);
            dirX.y = 0;
            dirX.NormalizeD4();
            LogicVector3 dirZ = new LogicVector3();
            dirZ.x = -dirX.z;
            dirZ.y = 0;
            dirZ.z = dirX.x;

            LogicVector3 valueX = dirX.ForwardOnDir(pos.x);
            LogicVector3 valueZ = dirZ.ForwardOnDir(pos.z);

            ret = valueX + valueZ;

            return ret;
        }

        public LogicVector3 TransformPosAsDir3D(LogicVector3 pos)
        {
            LogicVector3 ret = new LogicVector3();

            LogicVector3 dirX = new LogicVector3(ref this);
            dirX.y = 0;
            dirX.NormalizeD4();
            LogicVector3 dirZ = new LogicVector3();
            dirZ.x = -dirX.z;
            dirZ.y = 0;
            dirZ.z = dirX.x;

            LogicVector3 valueX = dirX.ForwardOnDir(pos.x);
            LogicVector3 valueZ = dirZ.ForwardOnDir(pos.z);

            ret = valueX + valueZ;

            return ret;
        }


        public static bool operator ==(LogicVector3 src, LogicVector3 dst)
        {
            if (dst.x == src.x && dst.y == src.y && dst.z == src.z)
                return true;
            return false;
        }

        public static bool operator !=(LogicVector3 src, LogicVector3 dst)
        {
            if (dst.x == src.x && dst.y == src.y && dst.z == src.z)
                return false;
            return true;
        }

        public override bool Equals(object o)
        {
            return this == (LogicVector3)o;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool IsZero()
        {
            return (x == 0 && y == 0 && z == 0);
        }

        public int CompareLen(long len)
        {
            if (len < 0)
                return 1;

            long myLen = magnitudeSq;
            len *= len;

            if (myLen > len)
                return 1;
            else if (myLen < len)
                return -1;

            return 0;
        }

        public static LogicVector3 FromFloat(UnityEngine.Vector3 vector3)
        {
            LogicVector3 ret = new LogicVector3();
            ret.x = (int)(vector3.x * (float)MathUtils.iPointUnit);
            ret.y = (int)(vector3.y * (float)MathUtils.iPointUnit);
            ret.z = (int)(vector3.z * (float)MathUtils.iPointUnit);
            return ret;
        }


        public UnityEngine.Vector3 ToFloat()
        {
            UnityEngine.Vector3 ret = new UnityEngine.Vector3();
            ret.x = (float)x / (float)MathUtils.iPointUnit;
            ret.y = (float)y / (float)MathUtils.iPointUnit;
            ret.z = (float)z / (float)MathUtils.iPointUnit;
            return ret;
        }

    }

}