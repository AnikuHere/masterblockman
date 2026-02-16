using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MOBA
{

    public class NavMeshMath
    {
      
        /// <summary>
        /// 将线段终点延长
        /// </summary>
        /// <param name="startPos">起始点</param>
        /// <param name="tarPos">终点</param>
        /// <param name="length">延长长度</param>
        /// <returns></returns>
        public static LogicVector3 ExtendPos(LogicVector3 startPos, LogicVector3 tarPos, int length)
        {
            LogicVector3 newPos = tarPos;
            int slopeRate = (int)Math.Abs((long)(tarPos.z - startPos.z) * MathUtils.lPointUnit/ (long)((tarPos.x - startPos.x)));
            int xLength, zLength;
            if (slopeRate < MathUtils.iPointUnit)
            {
                zLength = length;
                xLength = (int)((long)length * MathUtils.lPointUnit/ slopeRate);
            }
            else
            {
                xLength = length;
                zLength = (int)(((long)length * (long)slopeRate) / MathUtils.iPointUnit);
            }

            if (tarPos.x > startPos.x)
                newPos.x += xLength;
            else
                newPos.x -= xLength;

            if (tarPos.z > startPos.z)
                newPos.z += zLength;
            else
                newPos.z -= zLength;

            return newPos;
        }

    
        /// <summary>
        /// r=multiply(sp,ep,op),得到(sp-op)*(ep-op)的叉积 
        ///	r>0:ep在矢量opsp的逆时针方向； 
        ///	r=0：opspep三点共线； 
        ///	r<0:ep在矢量opsp的顺时针方向 
        /// </summary>
        /// <param name="p1">opsp</param>
        /// <param name="p2">opep</param>
        /// <returns></returns>
        public static long CrossProduct(LogicVector3 p1, LogicVector3 p2)
        {
            return ((long)p1.x * (long)p2.z - (long)p1.z * (long)p2.x);
        }

        /// <summary>
        /// 点是否等于0
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool IsEqualZero(LogicVector3 data)
        {
            if (data.x == 0 /*&& IsEqualZero(data.y) */&&  data.z == 0)
                return true;
            else
                return false;
        }

        public static long dtDistancePtSegSqr2D(LogicVector3 pt, LogicVector3 p, LogicVector3 q, ref int t)
        {
            long pqx = q.x - p.x;
            long pqz = q.z - p.z;
            long dx = pt.x - p.x;
            long dz = pt.z- p.z;
            long d = pqx * pqx + pqz * pqz;
            long tem_t = pqx * dx + pqz * dz;

            if (d > 0)
                tem_t = tem_t * MathUtils.lPointUnit / d;
            if (tem_t < 0)
                tem_t = 0;
            else if (tem_t > MathUtils.iPointUnit)
                tem_t = MathUtils.iPointUnit;

            t = (int)tem_t;

            dx = p.x + tem_t * pqx / MathUtils.iPointUnit - pt.x;
            dz = p.z + tem_t * pqz / MathUtils.iPointUnit - pt.z;
            return dx * dx + dz * dz;
        }

     

        public static int Distance2D(LogicVector3 a, LogicVector3 b)
        {
            LogicVector3 dir = a - b;
            dir.y = 0;
            return dir.magnitudeD4;
        }

        public static long Distance2DSqrt(LogicVector3 a, LogicVector3 b)
        {
            LogicVector3 dir = a - b;
            dir.y = 0;
            return dir.magnitudeSq;
        }


        public static int Area2D(LogicVector3 a, LogicVector3 b, LogicVector3 c)
        {
            long abx = b.x - a.x;
            long abz = b.z - a.z;
            long acx = c.x - a.x;
            long acz = c.z - a.z;
            return (int) ((acx * abz - abx * acz) / MathUtils.iPointUnit);
        }

        internal static long PointToSegment2DSquared(ref LogicVector3 pt, ref LogicVector3 p, ref LogicVector3 q, out int t)
        {
            //distance from P to Q in the xz plane
            long segmentDeltaX = q.x - p.x;
            long segmentDeltaZ = q.z - p.z;

            //distance from P to lone point in xz plane
            long dx = pt.x - p.x;
            long dz = pt.z - p.z;

            long segmentMagnitudeSquared = segmentDeltaX * segmentDeltaX + segmentDeltaZ * segmentDeltaZ;
            long tem_t = segmentDeltaX * dx + segmentDeltaZ * dz;

            if (segmentMagnitudeSquared > 0)
                tem_t = tem_t * MathUtils.lPointUnit /  segmentMagnitudeSquared;

            //keep t between 0 and 1
            if (tem_t < 0)
                tem_t = 0;
            else if (tem_t > MathUtils.iPointUnit)
                tem_t = MathUtils.iPointUnit;

            t = (int)tem_t;

            dx = p.x + t * segmentDeltaX / MathUtils.iPointUnit - pt.x;
            dz = p.z + t * segmentDeltaZ / MathUtils.iPointUnit - pt.z;

            return dx * dx + dz * dz;
        }

        internal static long Dot2D( LogicVector3 left,  LogicVector3 right)
        {
            return ((long)left.x * (long)right.x + (long)left.z * (long)right.z)/ MathUtils.iPointUnit;
        }

        internal static int dtVdist2D(LogicVector3 v1, LogicVector3 v2)
        {
	        long dx = v2.x - v1.x;
            long dz = v2.z - v1.z;
            long temp = (dx*dx + dz*dz) /MathUtils.lPointUnit;
            return (int)MathUtils.SqrtD4(temp);
        }

        public static bool RaySegment(LogicVector3 origin, LogicVector3 dir, LogicVector3 a, LogicVector3 b, out int t)
        {
            //default if not intersectng
            t = 0;

            LogicVector3 v = b - a;
            LogicVector3 w = origin - a;

            int d;

            PerpDotXZ(ref dir, ref v, out d);
            d *= -1;
            if (Math.Abs(d) <= 0)  
                return false;

            PerpDotXZ(ref v, ref w, out t);
            t  = t * MathUtils.iPointUnit / -d;
            if (t < 0 || t > MathUtils.iPointUnit)
                return false;

            int s;
            PerpDotXZ(ref dir, ref w, out s);
            s = s * MathUtils.iPointUnit / -d;
            if (s < 0 || s > MathUtils.iPointUnit)
                return false;

            return true;
        }

        internal static void PerpDotXZ(ref LogicVector3 a, ref LogicVector3 b, out int result)
        {
            result = (int) (((long)a.x * (long)b.z - (long)a.z * (long)b.x) / MathUtils.iPointUnit);
        }

       
        public static long dtDistancePtSegSqr2D(int x, int z, int px, int pz,  int qx, int qz)
        {
            long pqx = qx - px;
            long pqz = qz - pz;
            long dx = x - px;
            long dz = z - pz;
            long d = pqx * pqx + pqz * pqz;
            long t = pqx * dx + pqz * dz;
            if (d > 0)
                t = t * MathUtils.iPointUnit / d;
            if (t < 0)
                t = 0;
            else if (t > MathUtils.iPointUnit)
                t = MathUtils.iPointUnit;

            dx = px + t * pqx / MathUtils.iPointUnit - x;
            dz = pz + t * pqz / MathUtils.iPointUnit - z;

            return dx * dx + dz * dz;

        }


        public static int Ilog2(int v)
        {
            int r;
            int shift;
            r = (v > 0xffff ? 1 : 0) << 4; v >>= r;
            shift = (v > 0xff ? 1 : 0) << 3; v >>= shift; r |= shift;
            shift = (v > 0xf ? 1 : 0) << 2; v >>= shift; r |= shift;
            shift = (v > 0x3 ? 1 : 0) << 1; v >>= shift; r |= shift;
            r |= (v >> 1);
            return r;
        }

        public static int Min(int a, int b) { return a < b ? a : b; }

        static public byte Max(byte a, byte b) { return a > b ? a : b; }
        static public int Max(int a, int b) { return a > b ? a : b; }

        public  static int Abs(int a) { return a < 0 ? -a : a; }
        public static long Abs(long a) { return a < 0 ? -a : a; }


        static public void Swap(ref ushort a, ref ushort b) { ushort t = a; a = b; b = t; }
        static public  void Swap(ref int a, ref int b) { int t = a; a = b; b = t; }

        internal static int Clamp(int val, int min, int max)
        {
            return val < min ? min : (val > max ? max : val);
        }

        public static int NextPow2(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        public static void Vmin(ref LogicVector3 mn, LogicVector3 v)
        {
	        mn.x = Min(mn.x, v.x);
	        mn.y = Min(mn.y, v.y);
	        mn.z = Min(mn.z, v.z);
        }

        public static void Vmax(ref LogicVector3 mx, LogicVector3 v)
        {
	        mx.x = Max(mx.x, v.x);
	        mx.y = Max(mx.y, v.y);
	        mx.z = Max(mx.z, v.z);
        }

        public static void Vmad(ref LogicVector3 dest, LogicVector3 v1, LogicVector3 v2, int s_d4)
        {
            dest.x = v1.x + (int)((long)v2.x * (long)s_d4 / MathUtils.iPointUnit);
            dest.y = v1.y + (int)((long)v2.y * (long)s_d4 / MathUtils.iPointUnit);
            dest.z = v1.z + (int)((long)v2.z * (long)s_d4 / MathUtils.iPointUnit);
        }


        static public bool dtOverlapBounds(LogicVector3 amin, LogicVector3 amax, LogicVector3 bmin, LogicVector3 bmax)
        {
            bool overlap = true;
            overlap = (amin.x > bmax.x || amax.x < bmin.x) ? false : overlap;
           // overlap = (amin[1] > bmax[1] || amax[1] < bmin[1]) ? false : overlap;
            overlap = (amin.z > bmax.z || amax.z < bmin.z) ? false : overlap;
            return overlap;
        }

        static public bool dtDistancePtPolyEdgesSqr(LogicVector3 pt, int[] verts, int nverts,
							  ref long[] ed, ref int[] et)
        {
            int i, j;
            bool c = false;
            LogicVector3 vi;
            LogicVector3 vj;
            for (i = 0, j = nverts - 1; i < nverts; j = i++)
            {
                vi.x = verts[i * 3];
                vi.y = verts[i * 3  +1];
                vi.z = verts[i * 3  +2];

                vj.x = verts[j * 3];
                vj.y = verts[j * 3 + 1];
                vj.z = verts[j * 3 + 2];

                if (((vi.z > pt.z) != (vj.z > pt.z)) &&
                    (pt.x < (long)(vj.x - vi.x) * (long)(pt.z - vi.z) / (vj.z - vi.z) + vi.x))
                    c = !c;
                ed[j] = dtDistancePtSegSqr2D(pt, vj, vi, ref et[j]);
            }
            return c;
        }

        static public long dtTriArea2D(LogicVector3 a, LogicVector3 b, LogicVector3 c)
        {
            long abx = b.x - a.x;
            long abz = b.z - a.z;
            long acx = c.x - a.x;
            long acz = c.z - a.z;
	        return acx*abz - abx*acz;
        }

         static public bool dtVequal(LogicVector3 p0, LogicVector3 p1)
        {
            if (p0.x == p1.x && p0.z == p1.z)
                return true;

            return false;
        }

         public static long Vperp2D(LogicVector3 u, LogicVector3 v)
        {
            return (long)u.z * (long)v.x - (long)u.x * (long)v.z;
        }

         static public bool dtIntersectSegSeg2D(LogicVector3 ap, LogicVector3 aq,
                                 LogicVector3 bp, LogicVector3 bq, ref  int s, ref int t)
        {
            LogicVector3 u = aq - ap;
            LogicVector3 v = bq - bp;
            LogicVector3  w = ap - bp;
            long d = (long)u.x * (long)v.z - (long)u.z * (long)v.x;

	        if (Abs(d) <= 0) return false;

            long d2 = (long)v.x * (long)w.z - (long)v.z * (long)w.x;
            long d3 = (long)u.x * (long)w.z - (long)u.z * (long)w.x;
            s = (int)(d2 * MathUtils.lPointUnit/ d);
            t = (int)(d3 * MathUtils.lPointUnit / d);
	        return true;
        }

         static public bool dtIntersectSegmentPoly2D(LogicVector3 p0, LogicVector3 p1, int[] verts, int nverts, 
            ref int tmin, ref int tmax, ref int segMin, ref int segMax)
        {
	      
	        tmin = 0;
	        tmax = MathUtils.iPointUnit;
	        segMin = -1;
	        segMax = -1;

            LogicVector3 dir = p1 - p0;

            LogicVector3 edge;
            LogicVector3 diff;
	
	        for (int i = 0, j = nverts-1; i < nverts; j=i++)
	        {
                edge.x = verts[i * 3] - verts[j * 3];
                edge.y = verts[i * 3+1] - verts[j * 3+1];
                edge.z = verts[i * 3+2] - verts[j * 3+2];

                diff.x = p0.x - verts[j * 3];
                diff.y = p0.y - verts[j * 3+1];
                diff.z = p0.z - verts[j * 3+2];

		        long n = Vperp2D(edge, diff);
                long d = Vperp2D(dir, edge);
		        if (Abs(d) <= 0)
		        {
			        // S is nearly parallel to this edge
			        if (n < 0)
				        return false;
			        else
				        continue;
		        }

		        int t = (int)(n  * MathUtils.lPointUnit / d);
		        if (d < 0)
		        {
			        // segment S is entering across this edge
			        if (t > tmin)
			        {
				        tmin = t;
				        segMin = j;
				        // S enters after leaving polygon
				        if (tmin > tmax)
					        return false;
			        }
		        }
		        else
		        {
			        // segment S is leaving across this edge
			        if (t < tmax)
			        {
				        tmax = t;
				        segMax = j;
				        // S leaves before entering polygon
				        if (tmax < tmin)
					        return false;
			        }
		        }
	        }
	
	        return true;
        }

        static public bool dtClosestHeightPointTriangle(LogicVector3 p, LogicVector3 a, LogicVector3 b, 
            LogicVector3 c, ref int h)
        {
            LogicVector3 v0 = c - a;
            LogicVector3 v1 =  b - a;
            LogicVector3  v2 = p - a;
	
            long dot00 = Dot2D(v0, v0);
            long dot01 = Dot2D(v0, v1);
            long dot02 = Dot2D(v0, v2);
            long dot11 = Dot2D(v1, v1);
            long dot12 = Dot2D(v1, v2);
	
	        // Compute barycentric coordinates
	        long invDenom = dot00 * dot11 - dot01 * dot01;
	        long u = (dot11 * dot02 - dot01 * dot12)  / invDenom;
            long v = (dot00 * dot12 - dot01 * dot02)  / invDenom;

            
	        // If point lies inside the triangle, return interpolated ycoord.
            if (MathUtils.Abs((int)u) >= 0 && MathUtils.Abs((int)v) >= 0 && (u + v) <= MathUtils.iPointUnit)
	        {
		        h = (int)(a.y + v0.y*u + v1.y*v);
		        return true;
	        }
	
	        return false;
        }

    }
}
