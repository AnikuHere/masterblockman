using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOBA
{
    public struct UnionFindNode
    {
        public uint block;
        public uint gridBlock;
        public int id;
        public bool bPass;
    }

    public struct SameMarkTable
    {
        public int key1;
        public int key2;
        public bool bUnion;
    }

    public class UnionMarkTable
    {
        public int[] keyList = new int[32];
        public int cnt; 

        public bool HasSameKey(int key)
        {
            for(int i=0; i < cnt; i++)
            {
                if (keyList[i] == key)
                    return true;
            }

            return false;
        }

    }

    //连通图
    public class MapUnionFind
    {
        private MapAstarFind m_MapAstarFind;

        private UnionFindNode[] m_Unionodes  = null;
        private int m_Width = 30;
        private int m_Height = 20;

        public SameMarkTable[] m_SameMarkTabel = new SameMarkTable[256];
        public int m_MarkTabelCnt = 0;

        public  UnionMarkTable[] m_UnionMarkTable = new UnionMarkTable[32];
        public int m_UnionMarkTablCnt = 0;

        private int[] m_TempkeyList = new int[9];

         public MapUnionFind()
        {

        }

         public void OnInit(MapAstarFind mapAstarFind)
         {
             m_MapAstarFind = mapAstarFind;

             m_Width = MapAstarFind.m_RegionCnt;
             m_Height = MapAstarFind.m_RegionCnt;
             m_Unionodes = new UnionFindNode[m_Width * m_Height];

             for(int i=0; i <m_UnionMarkTable.Length; i++ )
             {
                 m_UnionMarkTable[i] = new UnionMarkTable();
             }
         }

        public void OnUnInit()
        {
            m_TempkeyList = null;

            for (int i = 0; i < m_UnionMarkTable.Length; i++)
            {
                m_UnionMarkTable[i] = null;
            }
            m_UnionMarkTable = null;

            m_SameMarkTabel = null;
            m_MapAstarFind = null;

            m_Unionodes = null;
        }

        //构建连接表 附带起始点和目标点 忽略阻挡
        public void BuildUnion(int LeftX, int TopZ, int startX, int startZ, int endX, int endZ, int crowdSize)
        {
            m_UnionMarkTablCnt = 0;
            m_MarkTabelCnt = 0;

            BuildBlock(LeftX, TopZ, startX,  startZ,  endX,  endZ,  crowdSize);
            BuildUnionPass(crowdSize);
        }

        public void BuildUnion(int LeftX, int TopZ, int crowdSize)
        {
            m_UnionMarkTablCnt = 0;
            m_MarkTabelCnt = 0;

            BuildBlock(LeftX, TopZ, crowdSize);
            BuildUnionPass(crowdSize);
        }

        public bool IsUnion(int startX, int startZ, int px1, int pz1, int px2, int pz2)
        {
            int index1 = (pz1 - startZ) * m_Width + (px1 - startX);
            int index2 = (pz2 - startZ) * m_Width + (px2 - startX);
            int key1 = m_Unionodes[index1].id;

            int key2 = m_Unionodes[index2].id;

            for (int i = 0; i < m_UnionMarkTablCnt; i++)
            {
                if (m_UnionMarkTable[i].HasSameKey(key1) && 
                    m_UnionMarkTable[i].HasSameKey(key2))
                    return true;
            }

             return false;
        }


        private void BuildBlock(int LeftX, int TopZ, int startX, int startZ, int endX, int endZ, int crowdSize)
        {
            int _endX = LeftX + m_Width;
            int _endZ = TopZ + m_Height;

            for (int j = TopZ; j < _endZ; j++)
            {
                for (int i = LeftX; i < _endX; i++)
                {
                    int index = (j - TopZ) * m_Width + (i - LeftX);

                    m_Unionodes[index].block = m_MapAstarFind._IsBlock(i, j) ? (uint)1 : 0;
                    m_Unionodes[index].id = 0;
                    m_Unionodes[index].bPass = false;
                }
            }

            for (int i = 0; i < crowdSize; i++)
            {
                for (int j = 0; j < crowdSize; j++)
                {
                    int x = startX + (i - crowdSize / 2) - LeftX; 
                    int z = startZ + (j - crowdSize / 2) - TopZ;

                    if (x < 0 || x >= m_Width ||
                        z < 0 || z >= m_Height)
                        continue;

                    int index = z * m_Width + x;
                    m_Unionodes[index].block = 0;
                }
            }

            for (int i = 0; i < crowdSize; i++)
            {
                for (int j = 0; j < crowdSize; j++)
                {
                    int x = endX + (i - crowdSize / 2) - LeftX;
                    int z = endZ + (j - crowdSize / 2) - TopZ;

                    if (x < 0 || x >= m_Width ||
                        z < 0 || z >= m_Height)
                        continue;
  
                    int index = z * m_Width + x;
                    m_Unionodes[index].block = 0;
                }
            }


            //生成范围阻挡信息
            for (int j = 0; j < m_Height; j++)
            {
                for (int i = 0; i < m_Width; i++)
                {
                    int index = j * m_Width + i;
                    m_Unionodes[index].gridBlock = GetCellPassFlag(i, j, crowdSize);
                }
            }
        }

        private void BuildBlock(int LeftX, int TopZ, int crowdSize)
        {
            int _endX = LeftX + m_Width;
            int _endZ = TopZ + m_Height;

            //生成 单个格子阻挡信息
            for (int j = TopZ; j < _endZ; j++)
            {
                for (int i = LeftX; i < _endX; i++)
                {
                    int index = (j - TopZ) * m_Width + (i - LeftX);
                    m_Unionodes[index].block = m_MapAstarFind.IsBlock(i, j);
                    m_Unionodes[index].id = 0;
                    m_Unionodes[index].bPass = false;
                }
            }

            //生成范围阻挡信息
            for (int j = 0; j < m_Height; j++)
            {
                for (int i = 0; i < m_Width; i++)
                {
                    int index = j * m_Width + i;
                    m_Unionodes[index].gridBlock = GetCellPassFlag(i, j, crowdSize);
                }
            }
        }

        public uint GetCellPassFlag(int centerX, int centerZ, int crowdSize)
        {
            for (int i = 0; i < crowdSize; i++)
            {
                for (int j = 0; j < crowdSize; j++)
                {
                    int x = centerX + (i - crowdSize / 2);
                    int z = centerZ + (j - crowdSize / 2);
                    if (x < 0 || x >= m_Width ||
                        z < 0 || z >= m_Height)
                        continue;

                    int index = z * m_Width + x;
                    if (m_Unionodes[index].block != 0)
                    {
                        return 1;
                    }
                }
            }

            return 0;
        }

        private void BuildUnionPass(int crowdSize)
        {
            //生成编号
            int passID = 0;

            for (int j = 0; j < m_Height; j++)
            {
                for (int i = 0; i < m_Width; i++)
                {
                    int index = j * m_Width + i;

                    //自身阻挡忽略
                    if (m_Unionodes[index].block != 0)
                        continue;

                    if (m_Unionodes[index].gridBlock != 0)
                    {
                        passID++;
                        m_Unionodes[index].id = passID;
                        m_Unionodes[index].bPass = false;
                        continue;
                    }

                    int keyListCnt = 0;
                    int midPass = GetCellCanPassID(i, j, crowdSize, ref m_TempkeyList, ref keyListCnt);
                    if (midPass > passID)
                    {
                        passID++;
                        midPass = passID;
                    }

                    m_Unionodes[index].id = midPass;
                    m_Unionodes[index].bPass = true;

                    AttachSameLabel(m_TempkeyList, keyListCnt);
                }
            }

            //剔除单个重复的
            for (int i = 0; i < m_MarkTabelCnt; i++)
            {
                int key1 = m_SameMarkTabel[i].key1;
                int key2 = m_SameMarkTabel[i].key2;
                if (key1 != 0 && key2 != 0)
                    continue;

                bool bFind = false;

                for (int j = i + 1; j < m_MarkTabelCnt; j++)
                {
                    int _key1 = m_SameMarkTabel[j].key1;
                    int _key2 = m_SameMarkTabel[j].key2;

                    if (_key1 == key1 || _key2 == key1)
                    {
                        bFind = true;
                        break;
                    }
                }

                m_SameMarkTabel[i].bUnion = bFind;
            }


            for (int i = 0; i < m_MarkTabelCnt; i++)
            {
                if (m_SameMarkTabel[i].bUnion)
                    continue;

                UnionMarkTable markTable = m_UnionMarkTable[m_UnionMarkTablCnt];
                markTable.cnt = 0;
                m_UnionMarkTablCnt++;

                int key1 = m_SameMarkTabel[i].key1;
                int key2 = m_SameMarkTabel[i].key2;

                markTable.keyList[markTable.cnt] = key1;
                markTable.cnt++;

                if (key2 == 0)
                {
                    continue;
                }

                markTable.keyList[markTable.cnt] = key2;
                markTable.cnt++;

                for (int j = i + 1; j < m_MarkTabelCnt; j++)
                {
                    if (m_SameMarkTabel[j].bUnion)
                        continue;

                    int _key1 = m_SameMarkTabel[j].key1;
                    int _key2 = m_SameMarkTabel[j].key2;

                    bool bC1 = markTable.HasSameKey(_key1);
                    bool bC2 = markTable.HasSameKey(_key2);

                    if (!bC1 && !bC2)
                        continue;

                    if (!bC1)
                    {
                        markTable.keyList[markTable.cnt] = _key1;
                        markTable.cnt++;
                    }

                    if (!bC2)
                    {
                        markTable.keyList[markTable.cnt] = _key2;
                        markTable.cnt++;
                    }

                    m_SameMarkTabel[j].bUnion = true;
                }
            }
        }

        private void AttachSameLabel(int[] newkeyList, int keyListCnt)
        {
            if (keyListCnt == 1)
            {
                bool bFind = false;
                for (int i = 0; i < m_MarkTabelCnt; i++)
                {
                    if (m_SameMarkTabel[i].key1 == newkeyList[0] ||
                        m_SameMarkTabel[i].key2 == newkeyList[0])
                    {
                        bFind = true;
                        break;
                    }
                }

                if(!bFind)
                {
                    m_SameMarkTabel[m_MarkTabelCnt].key1 = newkeyList[0];
                    m_SameMarkTabel[m_MarkTabelCnt].key2 = 0;
                    m_SameMarkTabel[m_MarkTabelCnt].bUnion = false;
                    m_MarkTabelCnt++;
                }
            }

            for (int i = 1; i < keyListCnt; i++)
            {
                int key1 = newkeyList[i - 1];
                int key2 = newkeyList[i];

                bool bFind = false;
                for (int j = 0; j < m_MarkTabelCnt; j++)
                {
                    if ((m_SameMarkTabel[j].key1 == key1 &&m_SameMarkTabel[j].key2 == key2) ||
                        (m_SameMarkTabel[j].key1 == key2 && m_SameMarkTabel[j].key2 == key1))
                    {
                        bFind = true;
                        break;
                    }
                }

                if (!bFind)
                {
                    m_SameMarkTabel[m_MarkTabelCnt].key1 = key1;
                    m_SameMarkTabel[m_MarkTabelCnt].key2 = key2;
                    m_SameMarkTabel[m_MarkTabelCnt].bUnion = false;
                    m_MarkTabelCnt++;
                }
            }

        }



        private int GetCellCanPassID(int cellX, int cellZ, int crowdSize, ref int[] keyList, ref int cnt)
        {
            int minID = int.MaxValue;

            for (int m = 0; m < crowdSize; m++)
            {
                for (int n = 0; n < crowdSize; n++)
                {
                    int x = cellX + (m - crowdSize / 2);
                    int z = cellZ + (n - crowdSize / 2);

                    if (x < 0 || x >= m_Width ||
                       z < 0 || z >= m_Height)
                    {
                        continue;
                    }

                    int index = m_Width * z + x;

                    if(m_Unionodes[index].bPass)
                    {
                        int id = m_Unionodes[index].id;

                        bool bFind = false;
                        for(int k=0; k < cnt; k++)
                        {
                            if(keyList[k] == id)
                            {
                                bFind = true;
                                break;
                            }
                        }

                        if (!bFind)
                        {
                            keyList[cnt] = id;
                            cnt++;
                        }


                        if (id < minID)
                            minID = id;
                    }
                }
            }

            return minID;

        }

        public int GetWidth()
        {
            return m_Width;
        }

        public int GetHeight()
        {
            return m_Height;
        }

        public UnionFindNode[] GetUnionodes()
        {
            return m_Unionodes;
        }


    }
}
