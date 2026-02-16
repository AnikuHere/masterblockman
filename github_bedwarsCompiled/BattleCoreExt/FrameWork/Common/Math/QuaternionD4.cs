using UnityEngine;
using System.Collections;


/*
 例子代码：
 * LogicVector3 needRot = new LogicVector3(0,0,10000);
 * QuaternionD4 q = QuaternionD4.DefaultDir.getRotationTo(new LogicVector3(10000,0,0));
 * needRot = q.Rotate(needRot);//needRot就是旋转后的向量//
 */
namespace MOBA
{

    public struct QuaternionD4
    {
        static public LogicVector3 DefaultDir = new LogicVector3(0, 0, MathUtils.iPointUnit);//默认z正向为正前方//

        public int x, y, z, w;

        public QuaternionD4(int _w, int _x, int _y, int _z)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        public LogicVector3 Rotate(LogicVector3 v)
        {
            // nVidia SDK implementation
            LogicVector3 uv;
            LogicVector3 uuv;

            LogicVector3 qvec = new LogicVector3(x, y, z);

            uv = qvec.CrossD4(v);
            uuv = qvec.CrossD4(uv);
            uv *= (20000 * w) / MathUtils.iPointUnit;
            uuv *= 20000;

            uv /= MathUtils.iPointUnit;
            uuv /= MathUtils.iPointUnit;

            return v + uv + uuv;
        }

        public void FromAngleAxis(int AngleD4, LogicVector3 rkAxis)
        {
            // assert:  axis[] is unit length
            //
            // The quaternion representing the rotation is
            //   q = cos(A/2)+sin(A/2)*(x*i+y*j+z*k)

            if (AngleD4 < 0)
            {
                //Negative_FromAngleAxis(-AngleD4, rkAxis);
                //return;
            }

            long HalfAngle = ((long)AngleD4 * (long)5000 / MathUtils.lPointUnit);//Radian fHalfAngle ( 0.5*rfAngle );
            int fSin = MathUtils.SinD4((int)HalfAngle / 1000);

            w = MathUtils.CosD4((int)HalfAngle / 1000);//Math::Cos(fHalfAngle);
            x = fSin * rkAxis.x / MathUtils.iPointUnit;
            y = fSin * rkAxis.y / MathUtils.iPointUnit;
            z = fSin * rkAxis.z / MathUtils.iPointUnit;
        }

        private void Negative_FromAngleAxis(int AngleD4, LogicVector3 rkAxis)
        {
            long HalfAngle = ((long)AngleD4 * (long)5000 / MathUtils.lPointUnit);//Radian fHalfAngle ( 0.5*rfAngle );
            int fSin = -MathUtils.SinD4((int)HalfAngle / 1000);

            w = MathUtils.CosD4((int)HalfAngle / 1000);//Math::Cos(fHalfAngle);
            x = fSin * rkAxis.x / MathUtils.iPointUnit;
            y = fSin * rkAxis.y / MathUtils.iPointUnit;
            z = fSin * rkAxis.z / MathUtils.iPointUnit;
        }

        //-----------------------------------------------------------------------
        long Norm()
        {
            return ((long)w * (long)w + (long)x * (long)x + (long)y * (long)y + (long)z * (long)z) / MathUtils.lPointUnit;
        }

        public int normalise()
        {
            long len = Norm();
            long factor = 10000 * MathUtils.lPointUnit / MathUtils.SqrtD4(len);

            this.product(factor);

            return (int)len;
        }

        public void product(long flagD4)
        {
            x = (int)((long)x * flagD4 / MathUtils.lPointUnit);
            y = (int)((long)y * flagD4 / MathUtils.lPointUnit);
            z = (int)((long)z * flagD4 / MathUtils.lPointUnit);
            w = (int)((long)w * flagD4 / MathUtils.lPointUnit);
        }

        public QuaternionD4 Inverse()
        {
            long ww = w;
            long xx = x;
            long yy = y;
            long zz = z;

            long fNorm = (ww * ww + xx * xx + yy * yy + zz * zz) / MathUtils.lPointUnit;

            if (fNorm > 0.0)
            {
                long fInvNorm = MathUtils.lPointUnit * MathUtils.lPointUnit / fNorm;
                ww = ww * fInvNorm / MathUtils.lPointUnit;
                xx = -xx * fInvNorm / MathUtils.lPointUnit;
                yy = -yy * fInvNorm / MathUtils.lPointUnit;
                zz = -zz * fInvNorm / MathUtils.lPointUnit;
                return new QuaternionD4((int)ww, (int)xx, (int)yy, (int)zz);
            }
            else
            {
                // return an invalid result to flag the error
                return new QuaternionD4(0, 0, 0, 0);
            }
        }

        public QuaternionD4 UnitInverse()
        {
            // assert:  'this' is unit length
            return new QuaternionD4(w, -x, -y, -z);
        }

        public static QuaternionD4 operator *(QuaternionD4 a, QuaternionD4 b)
        {
            // NOTE:  Multiplication is not generally commutative, so in most
            // cases p*q != q*p.

            return new QuaternionD4
            (
                (int)(((long)a.w * (long)b.w - (long)a.x * (long)b.x - (long)a.y * (long)b.y - (long)a.z * (long)b.z) / MathUtils.lPointUnit),
                (int)(((long)a.w * (long)b.x + (long)a.x * (long)b.w + (long)a.y * (long)b.z - (long)a.z * (long)b.y) / MathUtils.lPointUnit),
                (int)(((long)a.w * (long)b.y + (long)a.y * (long)b.w + (long)a.z * (long)b.x - (long)a.x * (long)b.z) / MathUtils.lPointUnit),
                (int)(((long)a.w * (long)b.z + (long)a.z * (long)b.w + (long)a.x * (long)b.y - (long)a.y * (long)b.x) / MathUtils.lPointUnit)
            );
        }

        public static LogicVector3 operator *(QuaternionD4 a, LogicVector3 b)
        {
            return a.Rotate(b);
        }

        public Quaternion GetUnityRot()
        {
            float _x = MathUtils.IntD4ToFloatD3(x);
            float _y = MathUtils.IntD4ToFloatD3(y);
            float _z = MathUtils.IntD4ToFloatD3(z);
            float _w = MathUtils.IntD4ToFloatD3(w);

            Quaternion q = new Quaternion(_x, _y, _z, _w);

            return q;
        }
    }

}