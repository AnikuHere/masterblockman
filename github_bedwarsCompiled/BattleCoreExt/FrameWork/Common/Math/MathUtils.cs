#define NO_USE_FLOAT_SQRT

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Text;

namespace MOBA
{
    public class MathUtils
    {
        //缩放比
        public const int SCALE_10K = 10000;
        public const int SCALE_100K = 1000000;

        public const int iPointUnit = 10000;
        public const long lPointUnit = 10000;
        public const float fPointUnit = 10000.0f;

        public const long iSecondInt = 10000;

        const int iSqrtInit = 100000;

        static public void StableSort<T>(List<T> list, System.Comparison<T> comp)
        {
            int iElemCount = list.Count;

            for (int i = 0; i < iElemCount; i++)
            {
                int iCurr = i;
                for (int k = 0; k < iCurr; k++)
                {
                    if (comp(list[k], list[iCurr]) > 0)
                    {
                        T currItem = list[iCurr];
                        list.RemoveAt(iCurr);
                        list.Insert(k, currItem);
                    }
                }
            }
        }

        /*
        static public float DirToAngleFloat(LogicVector3 vDirD4)
        {
            if (vDirD4.x > MathUtils.iPointUnit)
            {
                vDirD4.x = MathUtils.iPointUnit;
            }

            if (vDirD4.x < -MathUtils.iPointUnit)
            {
                vDirD4.x = -MathUtils.iPointUnit;
            }

            double fRad2Deg = 57.2958f;
            double fAngle = System.Math.Acos(LogicTransform.DstToFloat(vDirD4.x)) * fRad2Deg;

            if (vDirD4.z < 0)
                fAngle = 360.0f - fAngle;

            return (float)fAngle;
        }
        */
        static public long clampAngleD4(long angle)
        {
            while (angle > 3600000)
                angle -= 3600000;
            while (angle < 0f)
                angle += 3600000;

            return angle;
        }

        static public LogicVector3 AngleD4ToDir(int lAngle)
        {
            long angle = clampAngleD4((long)lAngle);
            QuaternionD4 quat = new QuaternionD4();
            quat.FromAngleAxis((int)angle, LogicVector3.up);
            LogicVector3 dir = quat * LogicVector3.forward;
            return dir;
        }

        static public QuaternionD4 AngleD4ToQuat(int lAngle)
        {
            long angle = clampAngleD4((long)lAngle);
            QuaternionD4 quat = new QuaternionD4();
            quat.FromAngleAxis((int)angle, LogicVector3.up);
            return quat;
        }

        public static int FloatD3ToIntD4(float fSrc)
        {
            return (int)((fSrc + 0.0001f) * 1000.0f) * 10;
        }

        public static float IntD4ToFloatD3(int iSrc)
        {
            return (float)(iSrc / 10) / 1000.0f;
        }



        public static int LerpD4(int iNum1, int iNum2, int iBlendD4)
        {
            long lRetVal = (long)iNum1 * lPointUnit + (long)(iNum2 - iNum1) * (long)iBlendD4;
            return (int)(lRetVal / lPointUnit);
        }

        public static long SqrtD4(long lNum)
        {
            if(lNum == 0)
                return 0;

            long lCurr = (long)iSqrtInit;
		    long lLast = lCurr;
		    long lPointUnit = (long)iPointUnit;

		    long lDelta = 100000;
		    int iIter = 0;
		    do
		    {
			    lLast = lCurr;
			    lCurr = (lCurr + lNum * lPointUnit / lCurr) / 2;
			    lDelta = lCurr - lLast;
			    iIter++;
		    }
		    while ((lDelta <= -2 || lDelta > 2) && iIter < 10 && lCurr != 0);
            return lCurr;

        }

        public static int SinD4(int iAngleD1)
        {
            iAngleD1 = iAngleD1 % 3600;
            int iPositiveAngle = iAngleD1;
            if (iAngleD1 < 0)
            {
                iPositiveAngle = -iAngleD1;
            }

            int iValue = 0;
            int iDomain = iPositiveAngle / 900;
            if (iDomain == 0)
                iValue = sinTable[iPositiveAngle].iValueD4;
            else if (iDomain == 1)
                iValue = sinTable[1800 - iPositiveAngle].iValueD4;
            else if (iDomain == 2)
                iValue = -sinTable[iPositiveAngle - 1800].iValueD4;
            else if (iDomain == 3)
                iValue = -sinTable[3600 - iPositiveAngle].iValueD4;

            if (iAngleD1 < 0)
                return -iValue;
            return iValue;
        }

        public static int CosD4(int iAngleD1)
        {
            return SinD4(iAngleD1 + 900);
        }

        public static int Abs(int val)
        {
            if (val < 0)
            {
                return 0 - val;
            }
            return val;
        }
        public static int Mul(int iVal1, int iVal2)
        {
            return (int)((long)iVal1 * (long)iVal2 / lPointUnit);
        }

        public static int Div(int iVal1, int iVal2)
        {
            return (int)((long)iVal1 * lPointUnit / (long)iVal2);
        }
        struct SinTableElement
        {
            public int iAngleD1;
            public int iValueD4;
            public SinTableElement(int _iAngleD1, int _iValueD4)
            {
                iAngleD1 = _iAngleD1;
                iValueD4 = _iValueD4;
            }
        };

        public static long Product(long a, long b)
        {
            return a * b / MathUtils.lPointUnit;
        }

        public static long Divid(long a, long b)
        {
            return a * MathUtils.lPointUnit / b;
        }

        public static long Clamp(long a, long from, long to)
        {
            if (a < from) return from;
            if (a > to) return to;
            return a;
        }

        public static int Clamp(int a, int from, int to)
        {
            if (a < from) return from;
            if (a > to) return to;
            return a;
        }

        public static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        public static long Max(long a, long b)
        {
            return a > b ? a : b;
        }

        public static int Min(int a, int b)
        {
            return a < b ? a : b;
        }

        public static long Min(long a, long b)
        {
            return a < b ? a : b;
        }

        //角度，90填900
        public static bool InSector(LogicVector3 From, LogicVector3 To, LogicVector3 faceTo, int radius, int angle)
        {
            From.y = 0;
            faceTo.y = 0;

            LogicVector3 enimyPos = To;
            enimyPos.y = 0;

            //先做距离过滤
            LogicVector3 vDir = enimyPos - From;

            if (MathUtils.Abs(vDir.x) <= radius || MathUtils.Abs(vDir.z) <= radius)
            {
                if (vDir.CompareLen(radius) <= 0/*vDir.magnitudeD4 <= radius*/)
                {
                    //再做角度过滤
                    vDir.NormalizeD4();
                    faceTo.NormalizeD4();

                    int cos = LogicVector3.DotD4(faceTo, vDir);
                    int min = CosD4(angle / 2);
                    if (min < cos)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool InRect(LogicVector3 p1, LogicVector3 p2, LogicVector3 pos, long radius)
        {
            p1.y = 0;
            p2.y = 0;
            pos.y = 0;

            int halfWidth = (int)(radius / 2);

            LogicVector3 d = (p2 - p1);

            // 坐标旋转后的pos的x, y //
            LogicVector3 c = pos - p1;

            // 因为万单位的偏差，所以2个点的距离小于0.5米，那么则按照圆形来判断 //
            if (d.CompareLen(5000) < 0/*length < 5000*/)
            {
                if (c.CompareLen(halfWidth) <= 0/*c.magnitudeD4 <= halfWidth*/)
                {
                    return true;
                }
                return false;
            }

            int length = d.magnitudeD4;
            d.NormalizeD4();

            long x = (long)(c.x) * (long)(d.x) + (long)(c.z) * (long)(d.z);
            long z = (long)(-c.x) * (long)(d.z) + (long)(c.z) * (long)(d.x);

            long ix = x / iPointUnit;
            long iz = z / iPointUnit;
            int posx = (int)ix;
            int posz = (int)iz;

            if (posx >= 0 && posx <= length &&
                posz >= -halfWidth && posz <= halfWidth
                )
            {
                return true;
            }
            return false;
        }

        public static bool InRect(LogicVector3 target, LogicVector3 center, LogicVector3 faceTo, int iLen, int iHeight)
        {
            LogicVector3 p1, p2, p3, p4;
            QuaternionD4 r = LogicVector3.right.getRotationTo(faceTo);

            target = r * target;
            center = r * center;

            p1.x = center.x - iHeight / 2;
            p1.y = 0;
            p1.z = center.z - iLen / 2;

            p2.x = center.x + iHeight / 2;
            p2.y = 0;
            p2.z = center.z - iLen / 2;

            p3.x = center.x - iHeight / 2;
            p3.y = 0;
            p3.z = center.z + iLen / 2;

            p4.x = center.x + iHeight / 2;
            p4.y = 0;
            p4.z = center.z + iLen / 2;

            if (target.x >= p1.x && target.x <= p2.x && target.z >= p1.z && target.z <= p3.z)
                return true;

            return false;
        }



        static SinTableElement[] sinTable =
	{
		new SinTableElement(0, 0), 
		new SinTableElement(1, 17), 
		new SinTableElement(2, 35), 
		new SinTableElement(3, 52), 
		new SinTableElement(4, 70), 
		new SinTableElement(5, 87), 
		new SinTableElement(6, 105), 
		new SinTableElement(7, 122), 
		new SinTableElement(8, 140), 
		new SinTableElement(9, 157), 
		new SinTableElement(10, 175), 
		new SinTableElement(11, 192), 
		new SinTableElement(12, 209), 
		new SinTableElement(13, 227), 
		new SinTableElement(14, 244), 
		new SinTableElement(15, 262), 
		new SinTableElement(16, 279), 
		new SinTableElement(17, 297), 
		new SinTableElement(18, 314), 
		new SinTableElement(19, 332), 
		new SinTableElement(20, 349), 
		new SinTableElement(21, 366), 
		new SinTableElement(22, 384), 
		new SinTableElement(23, 401), 
		new SinTableElement(24, 419), 
		new SinTableElement(25, 436), 
		new SinTableElement(26, 454), 
		new SinTableElement(27, 471), 
		new SinTableElement(28, 488), 
		new SinTableElement(29, 506), 
		new SinTableElement(30, 523), 
		new SinTableElement(31, 541), 
		new SinTableElement(32, 558), 
		new SinTableElement(33, 576), 
		new SinTableElement(34, 593), 
		new SinTableElement(35, 610), 
		new SinTableElement(36, 628), 
		new SinTableElement(37, 645), 
		new SinTableElement(38, 663), 
		new SinTableElement(39, 680), 
		new SinTableElement(40, 698), 
		new SinTableElement(41, 715), 
		new SinTableElement(42, 732), 
		new SinTableElement(43, 750), 
		new SinTableElement(44, 767), 
		new SinTableElement(45, 785), 
		new SinTableElement(46, 802), 
		new SinTableElement(47, 819), 
		new SinTableElement(48, 837), 
		new SinTableElement(49, 854), 
		new SinTableElement(50, 872), 
		new SinTableElement(51, 889), 
		new SinTableElement(52, 906), 
		new SinTableElement(53, 924), 
		new SinTableElement(54, 941), 
		new SinTableElement(55, 958), 
		new SinTableElement(56, 976), 
		new SinTableElement(57, 993), 
		new SinTableElement(58, 1011), 
		new SinTableElement(59, 1028), 
		new SinTableElement(60, 1045), 
		new SinTableElement(61, 1063), 
		new SinTableElement(62, 1080), 
		new SinTableElement(63, 1097), 
		new SinTableElement(64, 1115), 
		new SinTableElement(65, 1132), 
		new SinTableElement(66, 1149), 
		new SinTableElement(67, 1167), 
		new SinTableElement(68, 1184), 
		new SinTableElement(69, 1201), 
		new SinTableElement(70, 1219), 
		new SinTableElement(71, 1236), 
		new SinTableElement(72, 1253), 
		new SinTableElement(73, 1271), 
		new SinTableElement(74, 1288), 
		new SinTableElement(75, 1305), 
		new SinTableElement(76, 1323), 
		new SinTableElement(77, 1340), 
		new SinTableElement(78, 1357), 
		new SinTableElement(79, 1374), 
		new SinTableElement(80, 1392), 
		new SinTableElement(81, 1409), 
		new SinTableElement(82, 1426), 
		new SinTableElement(83, 1444), 
		new SinTableElement(84, 1461), 
		new SinTableElement(85, 1478), 
		new SinTableElement(86, 1495), 
		new SinTableElement(87, 1513), 
		new SinTableElement(88, 1530), 
		new SinTableElement(89, 1547), 
		new SinTableElement(90, 1564), 
		new SinTableElement(91, 1582), 
		new SinTableElement(92, 1599), 
		new SinTableElement(93, 1616), 
		new SinTableElement(94, 1633), 
		new SinTableElement(95, 1650), 
		new SinTableElement(96, 1668), 
		new SinTableElement(97, 1685), 
		new SinTableElement(98, 1702), 
		new SinTableElement(99, 1719), 
		new SinTableElement(100, 1736), 
		new SinTableElement(101, 1754), 
		new SinTableElement(102, 1771), 
		new SinTableElement(103, 1788), 
		new SinTableElement(104, 1805), 
		new SinTableElement(105, 1822), 
		new SinTableElement(106, 1840), 
		new SinTableElement(107, 1857), 
		new SinTableElement(108, 1874), 
		new SinTableElement(109, 1891), 
		new SinTableElement(110, 1908), 
		new SinTableElement(111, 1925), 
		new SinTableElement(112, 1942), 
		new SinTableElement(113, 1959), 
		new SinTableElement(114, 1977), 
		new SinTableElement(115, 1994), 
		new SinTableElement(116, 2011), 
		new SinTableElement(117, 2028), 
		new SinTableElement(118, 2045), 
		new SinTableElement(119, 2062), 
		new SinTableElement(120, 2079), 
		new SinTableElement(121, 2096), 
		new SinTableElement(122, 2113), 
		new SinTableElement(123, 2130), 
		new SinTableElement(124, 2147), 
		new SinTableElement(125, 2164), 
		new SinTableElement(126, 2181), 
		new SinTableElement(127, 2198), 
		new SinTableElement(128, 2215), 
		new SinTableElement(129, 2233), 
		new SinTableElement(130, 2250), 
		new SinTableElement(131, 2267), 
		new SinTableElement(132, 2284), 
		new SinTableElement(133, 2300), 
		new SinTableElement(134, 2317), 
		new SinTableElement(135, 2334), 
		new SinTableElement(136, 2351), 
		new SinTableElement(137, 2368), 
		new SinTableElement(138, 2385), 
		new SinTableElement(139, 2402), 
		new SinTableElement(140, 2419), 
		new SinTableElement(141, 2436), 
		new SinTableElement(142, 2453), 
		new SinTableElement(143, 2470), 
		new SinTableElement(144, 2487), 
		new SinTableElement(145, 2504), 
		new SinTableElement(146, 2521), 
		new SinTableElement(147, 2538), 
		new SinTableElement(148, 2554), 
		new SinTableElement(149, 2571), 
		new SinTableElement(150, 2588), 
		new SinTableElement(151, 2605), 
		new SinTableElement(152, 2622), 
		new SinTableElement(153, 2639), 
		new SinTableElement(154, 2656), 
		new SinTableElement(155, 2672), 
		new SinTableElement(156, 2689), 
		new SinTableElement(157, 2706), 
		new SinTableElement(158, 2723), 
		new SinTableElement(159, 2740), 
		new SinTableElement(160, 2756), 
		new SinTableElement(161, 2773), 
		new SinTableElement(162, 2790), 
		new SinTableElement(163, 2807), 
		new SinTableElement(164, 2823), 
		new SinTableElement(165, 2840), 
		new SinTableElement(166, 2857), 
		new SinTableElement(167, 2874), 
		new SinTableElement(168, 2890), 
		new SinTableElement(169, 2907), 
		new SinTableElement(170, 2924), 
		new SinTableElement(171, 2940), 
		new SinTableElement(172, 2957), 
		new SinTableElement(173, 2974), 
		new SinTableElement(174, 2990), 
		new SinTableElement(175, 3007), 
		new SinTableElement(176, 3024), 
		new SinTableElement(177, 3040), 
		new SinTableElement(178, 3057), 
		new SinTableElement(179, 3074), 
		new SinTableElement(180, 3090), 
		new SinTableElement(181, 3107), 
		new SinTableElement(182, 3123), 
		new SinTableElement(183, 3140), 
		new SinTableElement(184, 3156), 
		new SinTableElement(185, 3173), 
		new SinTableElement(186, 3190), 
		new SinTableElement(187, 3206), 
		new SinTableElement(188, 3223), 
		new SinTableElement(189, 3239), 
		new SinTableElement(190, 3256), 
		new SinTableElement(191, 3272), 
		new SinTableElement(192, 3289), 
		new SinTableElement(193, 3305), 
		new SinTableElement(194, 3322), 
		new SinTableElement(195, 3338), 
		new SinTableElement(196, 3355), 
		new SinTableElement(197, 3371), 
		new SinTableElement(198, 3387), 
		new SinTableElement(199, 3404), 
		new SinTableElement(200, 3420), 
		new SinTableElement(201, 3437), 
		new SinTableElement(202, 3453), 
		new SinTableElement(203, 3469), 
		new SinTableElement(204, 3486), 
		new SinTableElement(205, 3502), 
		new SinTableElement(206, 3518), 
		new SinTableElement(207, 3535), 
		new SinTableElement(208, 3551), 
		new SinTableElement(209, 3567), 
		new SinTableElement(210, 3584), 
		new SinTableElement(211, 3600), 
		new SinTableElement(212, 3616), 
		new SinTableElement(213, 3633), 
		new SinTableElement(214, 3649), 
		new SinTableElement(215, 3665), 
		new SinTableElement(216, 3681), 
		new SinTableElement(217, 3697), 
		new SinTableElement(218, 3714), 
		new SinTableElement(219, 3730), 
		new SinTableElement(220, 3746), 
		new SinTableElement(221, 3762), 
		new SinTableElement(222, 3778), 
		new SinTableElement(223, 3795), 
		new SinTableElement(224, 3811), 
		new SinTableElement(225, 3827), 
		new SinTableElement(226, 3843), 
		new SinTableElement(227, 3859), 
		new SinTableElement(228, 3875), 
		new SinTableElement(229, 3891), 
		new SinTableElement(230, 3907), 
		new SinTableElement(231, 3923), 
		new SinTableElement(232, 3939), 
		new SinTableElement(233, 3955), 
		new SinTableElement(234, 3971), 
		new SinTableElement(235, 3987), 
		new SinTableElement(236, 4003), 
		new SinTableElement(237, 4019), 
		new SinTableElement(238, 4035), 
		new SinTableElement(239, 4051), 
		new SinTableElement(240, 4067), 
		new SinTableElement(241, 4083), 
		new SinTableElement(242, 4099), 
		new SinTableElement(243, 4115), 
		new SinTableElement(244, 4131), 
		new SinTableElement(245, 4147), 
		new SinTableElement(246, 4163), 
		new SinTableElement(247, 4179), 
		new SinTableElement(248, 4195), 
		new SinTableElement(249, 4210), 
		new SinTableElement(250, 4226), 
		new SinTableElement(251, 4242), 
		new SinTableElement(252, 4258), 
		new SinTableElement(253, 4274), 
		new SinTableElement(254, 4289), 
		new SinTableElement(255, 4305), 
		new SinTableElement(256, 4321), 
		new SinTableElement(257, 4337), 
		new SinTableElement(258, 4352), 
		new SinTableElement(259, 4368), 
		new SinTableElement(260, 4384), 
		new SinTableElement(261, 4399), 
		new SinTableElement(262, 4415), 
		new SinTableElement(263, 4431), 
		new SinTableElement(264, 4446), 
		new SinTableElement(265, 4462), 
		new SinTableElement(266, 4478), 
		new SinTableElement(267, 4493), 
		new SinTableElement(268, 4509), 
		new SinTableElement(269, 4524), 
		new SinTableElement(270, 4540), 
		new SinTableElement(271, 4555), 
		new SinTableElement(272, 4571), 
		new SinTableElement(273, 4586), 
		new SinTableElement(274, 4602), 
		new SinTableElement(275, 4617), 
		new SinTableElement(276, 4633), 
		new SinTableElement(277, 4648), 
		new SinTableElement(278, 4664), 
		new SinTableElement(279, 4679), 
		new SinTableElement(280, 4695), 
		new SinTableElement(281, 4710), 
		new SinTableElement(282, 4726), 
		new SinTableElement(283, 4741), 
		new SinTableElement(284, 4756), 
		new SinTableElement(285, 4772), 
		new SinTableElement(286, 4787), 
		new SinTableElement(287, 4802), 
		new SinTableElement(288, 4818), 
		new SinTableElement(289, 4833), 
		new SinTableElement(290, 4848), 
		new SinTableElement(291, 4863), 
		new SinTableElement(292, 4879), 
		new SinTableElement(293, 4894), 
		new SinTableElement(294, 4909), 
		new SinTableElement(295, 4924), 
		new SinTableElement(296, 4939), 
		new SinTableElement(297, 4955), 
		new SinTableElement(298, 4970), 
		new SinTableElement(299, 4985), 
		new SinTableElement(300, 5000), 
		new SinTableElement(301, 5015), 
		new SinTableElement(302, 5030), 
		new SinTableElement(303, 5045), 
		new SinTableElement(304, 5060), 
		new SinTableElement(305, 5075), 
		new SinTableElement(306, 5090), 
		new SinTableElement(307, 5105), 
		new SinTableElement(308, 5120), 
		new SinTableElement(309, 5135), 
		new SinTableElement(310, 5150), 
		new SinTableElement(311, 5165), 
		new SinTableElement(312, 5180), 
		new SinTableElement(313, 5195), 
		new SinTableElement(314, 5210), 
		new SinTableElement(315, 5225), 
		new SinTableElement(316, 5240), 
		new SinTableElement(317, 5255), 
		new SinTableElement(318, 5270), 
		new SinTableElement(319, 5284), 
		new SinTableElement(320, 5299), 
		new SinTableElement(321, 5314), 
		new SinTableElement(322, 5329), 
		new SinTableElement(323, 5344), 
		new SinTableElement(324, 5358), 
		new SinTableElement(325, 5373), 
		new SinTableElement(326, 5388), 
		new SinTableElement(327, 5402), 
		new SinTableElement(328, 5417), 
		new SinTableElement(329, 5432), 
		new SinTableElement(330, 5446), 
		new SinTableElement(331, 5461), 
		new SinTableElement(332, 5476), 
		new SinTableElement(333, 5490), 
		new SinTableElement(334, 5505), 
		new SinTableElement(335, 5519), 
		new SinTableElement(336, 5534), 
		new SinTableElement(337, 5548), 
		new SinTableElement(338, 5563), 
		new SinTableElement(339, 5577), 
		new SinTableElement(340, 5592), 
		new SinTableElement(341, 5606), 
		new SinTableElement(342, 5621), 
		new SinTableElement(343, 5635), 
		new SinTableElement(344, 5650), 
		new SinTableElement(345, 5664), 
		new SinTableElement(346, 5678), 
		new SinTableElement(347, 5693), 
		new SinTableElement(348, 5707), 
		new SinTableElement(349, 5721), 
		new SinTableElement(350, 5736), 
		new SinTableElement(351, 5750), 
		new SinTableElement(352, 5764), 
		new SinTableElement(353, 5779), 
		new SinTableElement(354, 5793), 
		new SinTableElement(355, 5807), 
		new SinTableElement(356, 5821), 
		new SinTableElement(357, 5835), 
		new SinTableElement(358, 5850), 
		new SinTableElement(359, 5864), 
		new SinTableElement(360, 5878), 
		new SinTableElement(361, 5892), 
		new SinTableElement(362, 5906), 
		new SinTableElement(363, 5920), 
		new SinTableElement(364, 5934), 
		new SinTableElement(365, 5948), 
		new SinTableElement(366, 5962), 
		new SinTableElement(367, 5976), 
		new SinTableElement(368, 5990), 
		new SinTableElement(369, 6004), 
		new SinTableElement(370, 6018), 
		new SinTableElement(371, 6032), 
		new SinTableElement(372, 6046), 
		new SinTableElement(373, 6060), 
		new SinTableElement(374, 6074), 
		new SinTableElement(375, 6088), 
		new SinTableElement(376, 6101), 
		new SinTableElement(377, 6115), 
		new SinTableElement(378, 6129), 
		new SinTableElement(379, 6143), 
		new SinTableElement(380, 6157), 
		new SinTableElement(381, 6170), 
		new SinTableElement(382, 6184), 
		new SinTableElement(383, 6198), 
		new SinTableElement(384, 6211), 
		new SinTableElement(385, 6225), 
		new SinTableElement(386, 6239), 
		new SinTableElement(387, 6252), 
		new SinTableElement(388, 6266), 
		new SinTableElement(389, 6280), 
		new SinTableElement(390, 6293), 
		new SinTableElement(391, 6307), 
		new SinTableElement(392, 6320), 
		new SinTableElement(393, 6334), 
		new SinTableElement(394, 6347), 
		new SinTableElement(395, 6361), 
		new SinTableElement(396, 6374), 
		new SinTableElement(397, 6388), 
		new SinTableElement(398, 6401), 
		new SinTableElement(399, 6414), 
		new SinTableElement(400, 6428), 
		new SinTableElement(401, 6441), 
		new SinTableElement(402, 6455), 
		new SinTableElement(403, 6468), 
		new SinTableElement(404, 6481), 
		new SinTableElement(405, 6494), 
		new SinTableElement(406, 6508), 
		new SinTableElement(407, 6521), 
		new SinTableElement(408, 6534), 
		new SinTableElement(409, 6547), 
		new SinTableElement(410, 6561), 
		new SinTableElement(411, 6574), 
		new SinTableElement(412, 6587), 
		new SinTableElement(413, 6600), 
		new SinTableElement(414, 6613), 
		new SinTableElement(415, 6626), 
		new SinTableElement(416, 6639), 
		new SinTableElement(417, 6652), 
		new SinTableElement(418, 6665), 
		new SinTableElement(419, 6678), 
		new SinTableElement(420, 6691), 
		new SinTableElement(421, 6704), 
		new SinTableElement(422, 6717), 
		new SinTableElement(423, 6730), 
		new SinTableElement(424, 6743), 
		new SinTableElement(425, 6756), 
		new SinTableElement(426, 6769), 
		new SinTableElement(427, 6782), 
		new SinTableElement(428, 6794), 
		new SinTableElement(429, 6807), 
		new SinTableElement(430, 6820), 
		new SinTableElement(431, 6833), 
		new SinTableElement(432, 6845), 
		new SinTableElement(433, 6858), 
		new SinTableElement(434, 6871), 
		new SinTableElement(435, 6884), 
		new SinTableElement(436, 6896), 
		new SinTableElement(437, 6909), 
		new SinTableElement(438, 6921), 
		new SinTableElement(439, 6934), 
		new SinTableElement(440, 6947), 
		new SinTableElement(441, 6959), 
		new SinTableElement(442, 6972), 
		new SinTableElement(443, 6984), 
		new SinTableElement(444, 6997), 
		new SinTableElement(445, 7009), 
		new SinTableElement(446, 7022), 
		new SinTableElement(447, 7034), 
		new SinTableElement(448, 7046), 
		new SinTableElement(449, 7059), 
		new SinTableElement(450, 7071), 
		new SinTableElement(451, 7083), 
		new SinTableElement(452, 7096), 
		new SinTableElement(453, 7108), 
		new SinTableElement(454, 7120), 
		new SinTableElement(455, 7133), 
		new SinTableElement(456, 7145), 
		new SinTableElement(457, 7157), 
		new SinTableElement(458, 7169), 
		new SinTableElement(459, 7181), 
		new SinTableElement(460, 7193), 
		new SinTableElement(461, 7206), 
		new SinTableElement(462, 7218), 
		new SinTableElement(463, 7230), 
		new SinTableElement(464, 7242), 
		new SinTableElement(465, 7254), 
		new SinTableElement(466, 7266), 
		new SinTableElement(467, 7278), 
		new SinTableElement(468, 7290), 
		new SinTableElement(469, 7302), 
		new SinTableElement(470, 7314), 
		new SinTableElement(471, 7325), 
		new SinTableElement(472, 7337), 
		new SinTableElement(473, 7349), 
		new SinTableElement(474, 7361), 
		new SinTableElement(475, 7373), 
		new SinTableElement(476, 7385), 
		new SinTableElement(477, 7396), 
		new SinTableElement(478, 7408), 
		new SinTableElement(479, 7420), 
		new SinTableElement(480, 7431), 
		new SinTableElement(481, 7443), 
		new SinTableElement(482, 7455), 
		new SinTableElement(483, 7466), 
		new SinTableElement(484, 7478), 
		new SinTableElement(485, 7490), 
		new SinTableElement(486, 7501), 
		new SinTableElement(487, 7513), 
		new SinTableElement(488, 7524), 
		new SinTableElement(489, 7536), 
		new SinTableElement(490, 7547), 
		new SinTableElement(491, 7559), 
		new SinTableElement(492, 7570), 
		new SinTableElement(493, 7581), 
		new SinTableElement(494, 7593), 
		new SinTableElement(495, 7604), 
		new SinTableElement(496, 7615), 
		new SinTableElement(497, 7627), 
		new SinTableElement(498, 7638), 
		new SinTableElement(499, 7649), 
		new SinTableElement(500, 7660), 
		new SinTableElement(501, 7672), 
		new SinTableElement(502, 7683), 
		new SinTableElement(503, 7694), 
		new SinTableElement(504, 7705), 
		new SinTableElement(505, 7716), 
		new SinTableElement(506, 7727), 
		new SinTableElement(507, 7738), 
		new SinTableElement(508, 7749), 
		new SinTableElement(509, 7760), 
		new SinTableElement(510, 7771), 
		new SinTableElement(511, 7782), 
		new SinTableElement(512, 7793), 
		new SinTableElement(513, 7804), 
		new SinTableElement(514, 7815), 
		new SinTableElement(515, 7826), 
		new SinTableElement(516, 7837), 
		new SinTableElement(517, 7848), 
		new SinTableElement(518, 7859), 
		new SinTableElement(519, 7869), 
		new SinTableElement(520, 7880), 
		new SinTableElement(521, 7891), 
		new SinTableElement(522, 7902), 
		new SinTableElement(523, 7912), 
		new SinTableElement(524, 7923), 
		new SinTableElement(525, 7934), 
		new SinTableElement(526, 7944), 
		new SinTableElement(527, 7955), 
		new SinTableElement(528, 7965), 
		new SinTableElement(529, 7976), 
		new SinTableElement(530, 7986), 
		new SinTableElement(531, 7997), 
		new SinTableElement(532, 8007), 
		new SinTableElement(533, 8018), 
		new SinTableElement(534, 8028), 
		new SinTableElement(535, 8039), 
		new SinTableElement(536, 8049), 
		new SinTableElement(537, 8059), 
		new SinTableElement(538, 8070), 
		new SinTableElement(539, 8080), 
		new SinTableElement(540, 8090), 
		new SinTableElement(541, 8100), 
		new SinTableElement(542, 8111), 
		new SinTableElement(543, 8121), 
		new SinTableElement(544, 8131), 
		new SinTableElement(545, 8141), 
		new SinTableElement(546, 8151), 
		new SinTableElement(547, 8161), 
		new SinTableElement(548, 8171), 
		new SinTableElement(549, 8181), 
		new SinTableElement(550, 8192), 
		new SinTableElement(551, 8202), 
		new SinTableElement(552, 8211), 
		new SinTableElement(553, 8221), 
		new SinTableElement(554, 8231), 
		new SinTableElement(555, 8241), 
		new SinTableElement(556, 8251), 
		new SinTableElement(557, 8261), 
		new SinTableElement(558, 8271), 
		new SinTableElement(559, 8281), 
		new SinTableElement(560, 8290), 
		new SinTableElement(561, 8300), 
		new SinTableElement(562, 8310), 
		new SinTableElement(563, 8320), 
		new SinTableElement(564, 8329), 
		new SinTableElement(565, 8339), 
		new SinTableElement(566, 8348), 
		new SinTableElement(567, 8358), 
		new SinTableElement(568, 8368), 
		new SinTableElement(569, 8377), 
		new SinTableElement(570, 8387), 
		new SinTableElement(571, 8396), 
		new SinTableElement(572, 8406), 
		new SinTableElement(573, 8415), 
		new SinTableElement(574, 8425), 
		new SinTableElement(575, 8434), 
		new SinTableElement(576, 8443), 
		new SinTableElement(577, 8453), 
		new SinTableElement(578, 8462), 
		new SinTableElement(579, 8471), 
		new SinTableElement(580, 8480), 
		new SinTableElement(581, 8490), 
		new SinTableElement(582, 8499), 
		new SinTableElement(583, 8508), 
		new SinTableElement(584, 8517), 
		new SinTableElement(585, 8526), 
		new SinTableElement(586, 8536), 
		new SinTableElement(587, 8545), 
		new SinTableElement(588, 8554), 
		new SinTableElement(589, 8563), 
		new SinTableElement(590, 8572), 
		new SinTableElement(591, 8581), 
		new SinTableElement(592, 8590), 
		new SinTableElement(593, 8599), 
		new SinTableElement(594, 8607), 
		new SinTableElement(595, 8616), 
		new SinTableElement(596, 8625), 
		new SinTableElement(597, 8634), 
		new SinTableElement(598, 8643), 
		new SinTableElement(599, 8652), 
		new SinTableElement(600, 8660), 
		new SinTableElement(601, 8669), 
		new SinTableElement(602, 8678), 
		new SinTableElement(603, 8686), 
		new SinTableElement(604, 8695), 
		new SinTableElement(605, 8704), 
		new SinTableElement(606, 8712), 
		new SinTableElement(607, 8721), 
		new SinTableElement(608, 8729), 
		new SinTableElement(609, 8738), 
		new SinTableElement(610, 8746), 
		new SinTableElement(611, 8755), 
		new SinTableElement(612, 8763), 
		new SinTableElement(613, 8771), 
		new SinTableElement(614, 8780), 
		new SinTableElement(615, 8788), 
		new SinTableElement(616, 8796), 
		new SinTableElement(617, 8805), 
		new SinTableElement(618, 8813), 
		new SinTableElement(619, 8821), 
		new SinTableElement(620, 8829), 
		new SinTableElement(621, 8838), 
		new SinTableElement(622, 8846), 
		new SinTableElement(623, 8854), 
		new SinTableElement(624, 8862), 
		new SinTableElement(625, 8870), 
		new SinTableElement(626, 8878), 
		new SinTableElement(627, 8886), 
		new SinTableElement(628, 8894), 
		new SinTableElement(629, 8902), 
		new SinTableElement(630, 8910), 
		new SinTableElement(631, 8918), 
		new SinTableElement(632, 8926), 
		new SinTableElement(633, 8934), 
		new SinTableElement(634, 8942), 
		new SinTableElement(635, 8949), 
		new SinTableElement(636, 8957), 
		new SinTableElement(637, 8965), 
		new SinTableElement(638, 8973), 
		new SinTableElement(639, 8980), 
		new SinTableElement(640, 8988), 
		new SinTableElement(641, 8996), 
		new SinTableElement(642, 9003), 
		new SinTableElement(643, 9011), 
		new SinTableElement(644, 9018), 
		new SinTableElement(645, 9026), 
		new SinTableElement(646, 9033), 
		new SinTableElement(647, 9041), 
		new SinTableElement(648, 9048), 
		new SinTableElement(649, 9056), 
		new SinTableElement(650, 9063), 
		new SinTableElement(651, 9070), 
		new SinTableElement(652, 9078), 
		new SinTableElement(653, 9085), 
		new SinTableElement(654, 9092), 
		new SinTableElement(655, 9100), 
		new SinTableElement(656, 9107), 
		new SinTableElement(657, 9114), 
		new SinTableElement(658, 9121), 
		new SinTableElement(659, 9128), 
		new SinTableElement(660, 9135), 
		new SinTableElement(661, 9143), 
		new SinTableElement(662, 9150), 
		new SinTableElement(663, 9157), 
		new SinTableElement(664, 9164), 
		new SinTableElement(665, 9171), 
		new SinTableElement(666, 9178), 
		new SinTableElement(667, 9184), 
		new SinTableElement(668, 9191), 
		new SinTableElement(669, 9198), 
		new SinTableElement(670, 9205), 
		new SinTableElement(671, 9212), 
		new SinTableElement(672, 9219), 
		new SinTableElement(673, 9225), 
		new SinTableElement(674, 9232), 
		new SinTableElement(675, 9239), 
		new SinTableElement(676, 9245), 
		new SinTableElement(677, 9252), 
		new SinTableElement(678, 9259), 
		new SinTableElement(679, 9265), 
		new SinTableElement(680, 9272), 
		new SinTableElement(681, 9278), 
		new SinTableElement(682, 9285), 
		new SinTableElement(683, 9291), 
		new SinTableElement(684, 9298), 
		new SinTableElement(685, 9304), 
		new SinTableElement(686, 9311), 
		new SinTableElement(687, 9317), 
		new SinTableElement(688, 9323), 
		new SinTableElement(689, 9330), 
		new SinTableElement(690, 9336), 
		new SinTableElement(691, 9342), 
		new SinTableElement(692, 9348), 
		new SinTableElement(693, 9354), 
		new SinTableElement(694, 9361), 
		new SinTableElement(695, 9367), 
		new SinTableElement(696, 9373), 
		new SinTableElement(697, 9379), 
		new SinTableElement(698, 9385), 
		new SinTableElement(699, 9391), 
		new SinTableElement(700, 9397), 
		new SinTableElement(701, 9403), 
		new SinTableElement(702, 9409), 
		new SinTableElement(703, 9415), 
		new SinTableElement(704, 9421), 
		new SinTableElement(705, 9426), 
		new SinTableElement(706, 9432), 
		new SinTableElement(707, 9438), 
		new SinTableElement(708, 9444), 
		new SinTableElement(709, 9449), 
		new SinTableElement(710, 9455), 
		new SinTableElement(711, 9461), 
		new SinTableElement(712, 9466), 
		new SinTableElement(713, 9472), 
		new SinTableElement(714, 9478), 
		new SinTableElement(715, 9483), 
		new SinTableElement(716, 9489), 
		new SinTableElement(717, 9494), 
		new SinTableElement(718, 9500), 
		new SinTableElement(719, 9505), 
		new SinTableElement(720, 9511), 
		new SinTableElement(721, 9516), 
		new SinTableElement(722, 9521), 
		new SinTableElement(723, 9527), 
		new SinTableElement(724, 9532), 
		new SinTableElement(725, 9537), 
		new SinTableElement(726, 9542), 
		new SinTableElement(727, 9548), 
		new SinTableElement(728, 9553), 
		new SinTableElement(729, 9558), 
		new SinTableElement(730, 9563), 
		new SinTableElement(731, 9568), 
		new SinTableElement(732, 9573), 
		new SinTableElement(733, 9578), 
		new SinTableElement(734, 9583), 
		new SinTableElement(735, 9588), 
		new SinTableElement(736, 9593), 
		new SinTableElement(737, 9598), 
		new SinTableElement(738, 9603), 
		new SinTableElement(739, 9608), 
		new SinTableElement(740, 9613), 
		new SinTableElement(741, 9617), 
		new SinTableElement(742, 9622), 
		new SinTableElement(743, 9627), 
		new SinTableElement(744, 9632), 
		new SinTableElement(745, 9636), 
		new SinTableElement(746, 9641), 
		new SinTableElement(747, 9646), 
		new SinTableElement(748, 9650), 
		new SinTableElement(749, 9655), 
		new SinTableElement(750, 9659), 
		new SinTableElement(751, 9664), 
		new SinTableElement(752, 9668), 
		new SinTableElement(753, 9673), 
		new SinTableElement(754, 9677), 
		new SinTableElement(755, 9681), 
		new SinTableElement(756, 9686), 
		new SinTableElement(757, 9690), 
		new SinTableElement(758, 9694), 
		new SinTableElement(759, 9699), 
		new SinTableElement(760, 9703), 
		new SinTableElement(761, 9707), 
		new SinTableElement(762, 9711), 
		new SinTableElement(763, 9715), 
		new SinTableElement(764, 9720), 
		new SinTableElement(765, 9724), 
		new SinTableElement(766, 9728), 
		new SinTableElement(767, 9732), 
		new SinTableElement(768, 9736), 
		new SinTableElement(769, 9740), 
		new SinTableElement(770, 9744), 
		new SinTableElement(771, 9748), 
		new SinTableElement(772, 9751), 
		new SinTableElement(773, 9755), 
		new SinTableElement(774, 9759), 
		new SinTableElement(775, 9763), 
		new SinTableElement(776, 9767), 
		new SinTableElement(777, 9770), 
		new SinTableElement(778, 9774), 
		new SinTableElement(779, 9778), 
		new SinTableElement(780, 9781), 
		new SinTableElement(781, 9785), 
		new SinTableElement(782, 9789), 
		new SinTableElement(783, 9792), 
		new SinTableElement(784, 9796), 
		new SinTableElement(785, 9799), 
		new SinTableElement(786, 9803), 
		new SinTableElement(787, 9806), 
		new SinTableElement(788, 9810), 
		new SinTableElement(789, 9813), 
		new SinTableElement(790, 9816), 
		new SinTableElement(791, 9820), 
		new SinTableElement(792, 9823), 
		new SinTableElement(793, 9826), 
		new SinTableElement(794, 9829), 
		new SinTableElement(795, 9833), 
		new SinTableElement(796, 9836), 
		new SinTableElement(797, 9839), 
		new SinTableElement(798, 9842), 
		new SinTableElement(799, 9845), 
		new SinTableElement(800, 9848), 
		new SinTableElement(801, 9851), 
		new SinTableElement(802, 9854), 
		new SinTableElement(803, 9857), 
		new SinTableElement(804, 9860), 
		new SinTableElement(805, 9863), 
		new SinTableElement(806, 9866), 
		new SinTableElement(807, 9869), 
		new SinTableElement(808, 9871), 
		new SinTableElement(809, 9874), 
		new SinTableElement(810, 9877), 
		new SinTableElement(811, 9880), 
		new SinTableElement(812, 9882), 
		new SinTableElement(813, 9885), 
		new SinTableElement(814, 9888), 
		new SinTableElement(815, 9890), 
		new SinTableElement(816, 9893), 
		new SinTableElement(817, 9895), 
		new SinTableElement(818, 9898), 
		new SinTableElement(819, 9900), 
		new SinTableElement(820, 9903), 
		new SinTableElement(821, 9905), 
		new SinTableElement(822, 9907), 
		new SinTableElement(823, 9910), 
		new SinTableElement(824, 9912), 
		new SinTableElement(825, 9914), 
		new SinTableElement(826, 9917), 
		new SinTableElement(827, 9919), 
		new SinTableElement(828, 9921), 
		new SinTableElement(829, 9923), 
		new SinTableElement(830, 9925), 
		new SinTableElement(831, 9928), 
		new SinTableElement(832, 9930), 
		new SinTableElement(833, 9932), 
		new SinTableElement(834, 9934), 
		new SinTableElement(835, 9936), 
		new SinTableElement(836, 9938), 
		new SinTableElement(837, 9940), 
		new SinTableElement(838, 9942), 
		new SinTableElement(839, 9943), 
		new SinTableElement(840, 9945), 
		new SinTableElement(841, 9947), 
		new SinTableElement(842, 9949), 
		new SinTableElement(843, 9951), 
		new SinTableElement(844, 9952), 
		new SinTableElement(845, 9954), 
		new SinTableElement(846, 9956), 
		new SinTableElement(847, 9957), 
		new SinTableElement(848, 9959), 
		new SinTableElement(849, 9960), 
		new SinTableElement(850, 9962), 
		new SinTableElement(851, 9963), 
		new SinTableElement(852, 9965), 
		new SinTableElement(853, 9966), 
		new SinTableElement(854, 9968), 
		new SinTableElement(855, 9969), 
		new SinTableElement(856, 9971), 
		new SinTableElement(857, 9972), 
		new SinTableElement(858, 9973), 
		new SinTableElement(859, 9974), 
		new SinTableElement(860, 9976), 
		new SinTableElement(861, 9977), 
		new SinTableElement(862, 9978), 
		new SinTableElement(863, 9979), 
		new SinTableElement(864, 9980), 
		new SinTableElement(865, 9981), 
		new SinTableElement(866, 9982), 
		new SinTableElement(867, 9983), 
		new SinTableElement(868, 9984), 
		new SinTableElement(869, 9985), 
		new SinTableElement(870, 9986), 
		new SinTableElement(871, 9987), 
		new SinTableElement(872, 9988), 
		new SinTableElement(873, 9989), 
		new SinTableElement(874, 9990), 
		new SinTableElement(875, 9990), 
		new SinTableElement(876, 9991), 
		new SinTableElement(877, 9992), 
		new SinTableElement(878, 9993), 
		new SinTableElement(879, 9993), 
		new SinTableElement(880, 9994), 
		new SinTableElement(881, 9995), 
		new SinTableElement(882, 9995), 
		new SinTableElement(883, 9996), 
		new SinTableElement(884, 9996), 
		new SinTableElement(885, 9997), 
		new SinTableElement(886, 9997), 
		new SinTableElement(887, 9997), 
		new SinTableElement(888, 9998), 
		new SinTableElement(889, 9998), 
		new SinTableElement(890, 9998), 
		new SinTableElement(891, 9999), 
		new SinTableElement(892, 9999), 
		new SinTableElement(893, 9999), 
		new SinTableElement(894, 9999), 
		new SinTableElement(895, 10000), 
		new SinTableElement(896, 10000), 
		new SinTableElement(897, 10000), 
		new SinTableElement(898, 10000), 
		new SinTableElement(899, 10000),
		new SinTableElement(900, 10000)
	};
    }
}