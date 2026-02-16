using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOBA
{
    public class RandomMgr
    {
        private const int NTAB = 32;
        private ulong MAX_RANDOM_RANGE = 0x7FFFFFFF;

        private int m_idum = 0;
        private int m_iy = 0;

        private int[] m_iv = new int[NTAB];
	  
        public RandomMgr()
        {
        }

        public void OnInit(int iSeed)
        {
            SetSeed(iSeed);
        }

        public void OnUnInit()
        {
            m_iv = null;
        }

        public virtual void SetSeed(int iSeed)
        {
            m_idum = ((iSeed < 0) ? iSeed : -iSeed);
            m_iy = 0;
        }

        public virtual int RandomInt(int iLow, int iHigh)
        {
            int maxAcceptable = 0;
            int x = iHigh - iLow + 1;
            int n;
	        if (x <= 1 || MAX_RANDOM_RANGE < (ulong)(x-1))
	        {
		        return iLow;
	        }

	        maxAcceptable = (int)(MAX_RANDOM_RANGE - ((MAX_RANDOM_RANGE+1) % (ulong)x ));
	        do
	        {
		        n = GenerateRandomNumber();
	        } while (n > maxAcceptable);

	        return iLow + (n % x);
        }

        private int GenerateRandomNumber()
        {
            int IA = 16807;
            int IM = 2147483647;
            int IQ = 127773;
            int IR = 2836;

            int j;
            int k;

            if (m_idum <= 0 || m_iy == 0)
            {
                if (-(m_idum) < 1)
                    m_idum = 1;
                else
                    m_idum = -(m_idum);

                for (j = NTAB + 7; j >= 0; j--)
                {
                    k = (m_idum) / IQ;
                    m_idum = IA * (m_idum - k * IQ) - IR * k;
                    if (m_idum < 0)
                        m_idum += IM;
                    if (j < NTAB)
                        m_iv[j] = m_idum;
                }
                m_iy = m_iv[0];
            }

            k = (m_idum) / IQ;
            m_idum = IA * (m_idum - k * IQ) - IR * k;
            if (m_idum < 0)
                m_idum += IM;
            j = m_iy / (1 + (IM - 1) / NTAB);
            m_iy = m_iv[j];
            m_iv[j] = m_idum;

            return m_iy;
        }


    }
}
