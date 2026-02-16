using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MOBA
{
    public class MapAstarFind
    {
        private MapBaseInfo m_MapBaseInfo;
        private LogicVector3 m_MapLeftBm;

        private MapUnionFind m_MapUnionFind;

        private const int m_MaxNode = 1024;
        private AstarNode[] m_AstarNodePool = null;
        private int m_AstarPoolFreeIndex = 0;

        //限定范围
        private int m_RegionMinX = 0;
        private int m_RegionMaxX = 0;
        private int m_RegionMinZ = 0;
        private int m_RegionMaxZ = 0;
        public const int m_RegionCnt = 30;

        //提前计算的H值
        private long m_LineH = 0;
        private long m_DirH = 0;

        private long m_LineStep = 0;
        private long m_DirStep = 0;

        private PriorityQueue<AstarNode> m_OpenList = new PriorityQueue<AstarNode>(m_MaxNode);

        private List<AstarNode> m_FindPathNodeList = new List<AstarNode>();

        #region BFS
        private BfsNodeFIFO m_BfsFifo = new BfsNodeFIFO(10);

        private BfsNode[] m_BfsNodePool = null;
        private int m_BfsPoolFreeIndex = 0;
        #endregion

        private PriorityQueue<AstarNode> m_BoundList = new PriorityQueue<AstarNode>(m_MaxNode);

        private byte[] m_GridFlags = new byte[2048];

        public MapAstarFind()
        {

        }

        public void OnInit(MapBaseInfo baseInfo)
        {
            m_MapBaseInfo = baseInfo;

            int width = baseInfo.RowNum * baseInfo.CellSize;
            int height = baseInfo.ColumnNum * baseInfo.CellSize;

            int centerX = baseInfo.LeftBottomPointX + width / 2;
            int centerZ = baseInfo.LeftBottomPointZ + height / 2;

            m_MapLeftBm = new LogicVector3(baseInfo.LeftBottomPointX, 0,
                    baseInfo.LeftBottomPointZ);

            m_AstarNodePool = new AstarNode[m_RegionCnt * (m_RegionCnt + 2)];
            for (int i = 0; i < m_AstarNodePool.Length; i++)
            {
                m_AstarNodePool[i] = new AstarNode();
            }

            m_MapUnionFind = new MapUnionFind();
            m_MapUnionFind.OnInit(this);

            m_DirH = (long)baseInfo.CellSize * MathUtils.lPointUnit / MathUtils.CosD4(450);
            m_DirH *= m_DirH;
            m_LineH = baseInfo.CellSize * baseInfo.CellSize;

            m_LineStep = baseInfo.CellSize;
            m_DirStep = (long)baseInfo.CellSize * MathUtils.lPointUnit / MathUtils.CosD4(450);

            m_BfsNodePool = new BfsNode[m_RegionCnt * (m_RegionCnt + 2)];
            for (int i = 0; i < m_BfsNodePool.Length; i++)
            {
                m_BfsNodePool[i] = new BfsNode();
            }
        }

        public void OnUnInit()
        {
            m_MapUnionFind.OnUnInit();
            m_MapUnionFind = null;

            for (int i = 0; i < m_BfsNodePool.Length; i++)
            {
                m_BfsNodePool[i] = null;
            }
            m_BfsNodePool = null;

            for (int i = 0; i < m_AstarNodePool.Length; i++)
            {
                m_AstarNodePool[i] = null;
            }
            m_AstarNodePool = null;

            m_OpenList.Release();
            m_OpenList = null;

            m_FindPathNodeList.Clear();
            m_FindPathNodeList = null;

            m_BoundList.Release();
            m_BoundList = null;

            m_GridFlags = null;

            m_MapBaseInfo = null;
        }

        public AstarNode GetFindPathAstarNode(int centerX, int centerZ)
        {
            int index = (centerZ - m_RegionMinZ) * m_RegionCnt + (centerX - m_RegionMinX);
            if (index < 0 || index >= m_AstarNodePool.Length)
                return null;

            return m_AstarNodePool[index];
        }

        //取一个空闲的
        public AstarNode GetFreeAstarNode()
        {
            if (m_AstarPoolFreeIndex >= m_AstarNodePool.Length)
                return null;

            return m_AstarNodePool[m_AstarPoolFreeIndex++];
        }

        public BfsNode GetBfsNode(int x, int z)
        {
            if (x < m_RegionMinX || x > m_RegionMaxX || z < m_RegionMinZ || z > m_RegionMaxZ)
                return null;

            int index = (z - m_RegionMinZ) * m_RegionCnt + (x - m_RegionMinX);
            return m_BfsNodePool[index];
        }

        //清空节点
        public void ClearAstarPoolNode()
        {
            for (int i = 0; i < m_AstarNodePool.Length; i++)
            {
                m_AstarNodePool[i].Clear();
            }

            m_AstarPoolFreeIndex = 0;
        }

        public void ClearBfsPoolNode()
        {
            for (int i = 0; i < m_BfsNodePool.Length; i++)
            {
                m_BfsNodePool[i].Clear();
            }
            m_BfsPoolFreeIndex = 0;
        }

        #region 寻路

        //目标点是否在Astar寻路范围内
        public bool IsInAstarFindRegion(LogicVector3 start, LogicVector3 end)
        {
            int startX = 0;
            int startZ = 0;
            if (!GetCellIndexByPos(start, ref startX, ref startZ))
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("IsInAstarFindRegion start not in map:" + start);
#else
                 LogUtils.LogError("IsInAstarFindRegion start not in map:" + start);
#endif
                return false;
            }

            int endX = 0;
            int endZ = 0;
            if (!GetCellIndexByPos(end, ref endX, ref endZ))
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("IsInAstarFindRegion end not in map:" + start);
#else
                LogUtils.LogError("IsInAstarFindRegion end not in map:" + start);
#endif
                return false;
            }

            int maxX = MathUtils.Abs(endX - startX);
            int maxZ = MathUtils.Abs(endZ - startZ);

            //范围内
            if (maxX >= m_RegionCnt || maxZ >= m_RegionCnt)
                return false;

            return true;
        }

        public bool FindPath(LogicVector3 start, LogicVector3 end,
    int crowdSize, int targetCrowdSize, ref TList<LogicVector3> pathList)
        {
            int startX = 0;
            int startZ = 0;
            if (!GetCellIndexByPos(start, ref startX, ref startZ))
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("FindPath start not in map:" + start);
#else
                 LogUtils.LogError("FindPath start not in map:" + start);
#endif
                return false;
            }

            int endX = 0;
            int endZ = 0;
            if (!GetCellIndexByPos(end, ref endX, ref endZ))
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("FindPath end not in map:" + start);
#else
                 LogUtils.LogError("FindPath end not in map:" + start);
#endif
                return false;
            }

            int offsetX = m_RegionCnt - MathUtils.Abs(startX - endX);
            int offsetZ = m_RegionCnt - MathUtils.Abs(startZ - endZ);
            if (offsetX < 0 || offsetZ < 0)
            {
#if UNITY_EDITOR || UNITY_SERVERS
                //Debug.LogError("FindPath  too long start:" + start + ",end:" + end);
#else
                LogUtils.LogError("FindPath  too long start:" + start + ",end:" + end );
#endif
                return false;
            }

            m_RegionMinX = MathUtils.Min(startX, endX) - offsetX / 2;
            m_RegionMaxX = m_RegionMinX + m_RegionCnt;
            m_RegionMinZ = MathUtils.Min(startZ, endZ) - offsetZ / 2; ;
            m_RegionMaxZ = m_RegionMinZ + m_RegionCnt;

            ClearAstarPoolNode();

            m_OpenList.clear();
            m_FindPathNodeList.Clear();
            bool foundPath = false;
            int searchDeepth = 0;

            //LogUtils.Log(string.Format("Target: ({0}, {1})", endX, endZ));

            AstarNode startNode = GetFindPathAstarNode(startX, startZ);
            if (startNode == null)
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("FindPath  startNode is null start:" + start + ",end:" + end);
#else
                LogUtils.LogError("FindPath  too long start:" + start + ",end:" + end );
#endif
                return false;
            }

            startNode.centerX = startX;
            startNode.centerZ = startZ;
            startNode.H = GetH(startX, startZ, endX, endZ);
            startNode.G = 0;
            startNode.parentX = -1;
            startNode.parentZ = -1;
            startNode.status = AstarNodeStatus.OPEN;
            m_OpenList.push(startNode);

            AstarNode currNode = null;

            while (true)
            {
                currNode = m_OpenList.pop();
                if (currNode == null)
                {
                    //LogUtils.Log("OPEN empty");
                    break;
                }

                //已经找到目的地
                if (currNode.centerX == endX && currNode.centerZ == endZ)
                {
                    foundPath = true;
                    break;
                }

                if (searchDeepth >= m_MaxNode)
                {
                    //LogUtils.Log("DEPTH max");
                    break;
                }

                //范围内
                if (currNode.centerX < m_RegionMinX || currNode.centerX > m_RegionMaxX ||
                    currNode.centerZ < m_RegionMinZ || currNode.centerZ > m_RegionMaxZ)
                    continue;

                ProcessNode(currNode, crowdSize, endX, endZ, targetCrowdSize);

                searchDeepth++;
            }

            if (foundPath)
            {
                AstarNode node = currNode;

                while (node != null)
                {
                    m_FindPathNodeList.Add(node);

                    int parentX = node.parentX;
                    int parentZ = node.parentZ;
                    node = GetFindPathAstarNode(parentX, parentZ);

                }

                m_FindPathNodeList.Reverse();

                SmoothPath(start, end, m_FindPathNodeList, ref pathList);
            }

            return foundPath;
        }

        // 寻找目标点路径，如果目标为障碍，就寻最近的一个
        public bool FindPosPath(LogicVector3 start, LogicVector3 end, int crowdSize, int targetCrowdSize, ref TList<LogicVector3> pathList)
        {
            int startX = 0;
            int startZ = 0;
            if (!GetCellIndexByPos(start, ref startX, ref startZ))
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("FindPath start not in map:" + start);
#else
                 LogUtils.LogError("FindPath start not in map:" + start);
#endif
                return false;
            }

            int endX = 0;
            int endZ = 0;
            if (!GetCellIndexByPos(end, ref endX, ref endZ))
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("FindPath end not in map:" + start);
#else
                 LogUtils.LogError("FindPath end not in map:" + start);
#endif
                return false;
            }

            int offsetX = m_RegionCnt - MathUtils.Abs(startX - endX);
            int offsetZ = m_RegionCnt - MathUtils.Abs(startZ - endZ);
            if (offsetX < 0 || offsetZ < 0)
            {
#if UNITY_EDITOR || UNITY_SERVERS
                //Debug.LogError("FindPath  too long start:" + start + ",end:" + end);
#else
                LogUtils.LogError("FindPath  too long start:" + start + ",end:" + end );
#endif
                return false;
            }

            m_RegionMinX = MathUtils.Min(startX, endX) - offsetX / 2;
            m_RegionMaxX = m_RegionMinX + m_RegionCnt;
            m_RegionMinZ = MathUtils.Min(startZ, endZ) - offsetZ / 2; ;
            m_RegionMaxZ = m_RegionMinZ + m_RegionCnt;

            ClearBfsPoolNode();

            m_BfsFifo.Clear();

            //LogUtils.Log(string.Format("Target: ({0}, {1})", endX, endZ));

            BfsNode startNode = GetBfsNode(startX, startZ);
            if (startNode == null)
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("FindPath  startNode is null start:" + start + ",end:" + end);
#else
                LogUtils.LogError("FindPath  too long start:" + start + ",end:" + end );
#endif
                return false;
            }

            startNode.x = startX;
            startNode.z = startZ;
            OpenNode2(startNode, null, 0);

            BfsNode currNode = null;
            BfsNode bestNode = null;
            int bestDist = int.MaxValue;

            while (true)
            {
                currNode = m_BfsFifo.Pop();
                if (currNode == null)
                    break;

                int x = currNode.x;
                int z = currNode.z;
                int dx = MathUtils.Abs(x - endX);
                int dz = MathUtils.Abs(z - endZ);
                if (dx == 0 && dz == 0)
                {
                    bestNode = currNode;
                    break;
                }

                int dist = dx + dz;
                if (dist < bestDist)
                {
                    bestNode = currNode;
                    bestDist = dist;
                }

                ProcessNode2(currNode, crowdSize, endX, endZ, targetCrowdSize);
            }

            if (bestNode != null)
            {
                ReverseBfsNode(bestNode, ref pathList);
                if (pathList.Count > 1)
                    return true;
            }

            return false;
        }

        // 寻找目标单位路径，通常是战斗中寻找攻击目标
        public bool FindTargetPath(LogicVector3 start, LogicVector3 end,
    int crowdSize, int targetCrowdSize, int minLayer, int maxLayer, ref TList<LogicVector3> pathList)
        {
            int startX = 0;
            int startZ = 0;
            if (!GetCellIndexByPos(start, ref startX, ref startZ))
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("FindPath start not in map:" + start);
#else
                 LogUtils.LogError("FindPath start not in map:" + start);
#endif
                return false;
            }

            int endX = 0;
            int endZ = 0;
            if (!GetCellIndexByPos(end, ref endX, ref endZ))
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("FindPath end not in map:" + start);
#else
                 LogUtils.LogError("FindPath end not in map:" + start);
#endif
                return false;
            }

            int offsetX = m_RegionCnt - MathUtils.Abs(startX - endX);
            int offsetZ = m_RegionCnt - MathUtils.Abs(startZ - endZ);
            if (offsetX < 0 || offsetZ < 0)
            {
#if UNITY_EDITOR || UNITY_SERVERS
                //Debug.LogError("FindPath  too long start:" + start + ",end:" + end);
#else
                LogUtils.LogError("FindPath  too long start:" + start + ",end:" + end );
#endif
                return false;
            }

            m_RegionMinX = MathUtils.Min(startX, endX) - offsetX / 2;
            m_RegionMaxX = m_RegionMinX + m_RegionCnt;
            m_RegionMinZ = MathUtils.Min(startZ, endZ) - offsetZ / 2; ;
            m_RegionMaxZ = m_RegionMinZ + m_RegionCnt;

            ClearBfsPoolNode();

            m_BfsFifo.Clear();

            //LogUtils.Log(string.Format("Target: ({0}, {1})", endX, endZ));

            BfsNode startNode = GetBfsNode(startX, startZ);
            if (startNode == null)
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("FindPath  startNode is null start:" + start + ",end:" + end);
#else
                LogUtils.LogError("FindPath  too long start:" + start + ",end:" + end );
#endif
                return false;
            }

            startNode.x = startX;
            startNode.z = startZ;
            OpenNode2(startNode, null, 0);

            BfsNode currNode = null;
            BfsNode bestNode = null;
            int bestWeight = int.MaxValue;
            int layerOffset = targetCrowdSize / 2 + targetCrowdSize % 2;

            while (true)
            {
                currNode = m_BfsFifo.Pop();
                if (currNode == null)
                    break;

                int x = currNode.x;
                int z = currNode.z;
                int dx = MathUtils.Abs(x - endX);
                int dz = MathUtils.Abs(z - endZ);
                int dmax = MathUtils.Max(dx, dz);
                int dist = dmax - layerOffset;
                if (dist >= 0)
                {
                    int layer = dist / crowdSize;
                    if (layer >= minLayer && layer <= maxLayer)
                    {
                        int weight = (MathUtils.Abs(x - startX) + MathUtils.Abs(z - startZ)) + 10 * dmax;
                        if (weight < bestWeight)
                        {
                            bestNode = currNode;
                            bestWeight = weight;
                        }

                        if (layer == minLayer)
                            break;
                    }
                }

                ProcessNode2(currNode, crowdSize, endX, endZ, targetCrowdSize);
            }

            if (bestNode != null)
            {
                ReverseBfsNode(bestNode, ref pathList);
                if (pathList.Count > 1)
                    return true;
            }

            return false;
        }

        private void ReverseBfsNode(BfsNode node, ref TList<LogicVector3> pathList)
        {
            BfsNode rev = null;

            while (node != null)
            {
                BfsNode next = node.next;
                node.next = rev;
                rev = node;
                node = next;
            }

            node = rev;
            while (node != null)
            {
                BfsNode next = node.next;
                pathList.Add(GetCellCenterPos(node.x, node.z));
                node.next = null;
                node = next;
            }
        }

        private AstarNode CalcPassNode(int newX, int newZ, AstarDir dir, int crowdSize, int endX, int endZ, int targetCrowdSize)
        {
            AstarNode node = GetFindPathAstarNode(newX, newZ);
            if (node == null)
                return null;

            if (node.status == AstarNodeStatus.CLOSE)
                return null;

            if (!IsFindPathCanPass(newX, newZ, dir, crowdSize, endX, endZ, targetCrowdSize))
                return null;

            if (node.status == AstarNodeStatus.NONE)
            {
                node.centerX = newX;
                node.centerZ = newZ;
                node.H = GetH(newX, newZ, endX, endZ);
                node.G = 0;
            }

            return node;
        }

        private BfsNode CalcPassNode2(int newX, int newZ, AstarDir dir, int crowdSize, int endX, int endZ, int targetCrowdSize)
        {
            BfsNode node = GetBfsNode(newX, newZ);
            if (node == null)
                return null;

            if (node.pass)
                return node;

            if (IsCrowdBlock(newX, newZ, dir, crowdSize))
                return null;

            node.x = newX;
            node.z = newZ;
            node.pass = true;

            return node;
        }

        private void OpenNode(AstarNode node, AstarNode parent, long g)
        {
            if (node.status == AstarNodeStatus.OPEN)
            {
                if (g < node.G)
                {
                    node.G = g;
                    node.parentX = parent.centerX;
                    node.parentZ = parent.centerZ;
                    m_OpenList.modify(node);
                }
            }
            else
            {
                node.status = AstarNodeStatus.OPEN;

                node.G = g;
                node.parentX = parent.centerX;
                node.parentZ = parent.centerZ;
                m_OpenList.push(node);
            }
        }

        private void OpenNode2(BfsNode node, BfsNode parent, long step)
        {
            if (node.status == AstarNodeStatus.OPEN)
            {
                if (step < node.step)
                {
                    node.step = step;
                    node.next = parent;
                }
            }
            else if (node.status == AstarNodeStatus.NONE)
            {
                node.status = AstarNodeStatus.OPEN;

                node.step = step;
                node.next = parent;
                m_BfsFifo.Push(node);
            }
        }

        private void ProcessNode(AstarNode curNode, int crowdSize, int endX, int endZ, int targetCrowdSize)
        {
            int x = curNode.centerX;
            int z = curNode.centerZ;

            curNode.status = AstarNodeStatus.CLOSE;

            long g = curNode.G + m_LineH;
            AstarNode top = CalcPassNode(x, z + 1, AstarDir.UP, crowdSize, endX, endZ, targetCrowdSize);
            if (top != null)
                OpenNode(top, curNode, g);

            AstarNode buttom = CalcPassNode(x, z - 1, AstarDir.DOWN, crowdSize, endX, endZ, targetCrowdSize);
            if (buttom != null)
                OpenNode(buttom, curNode, g);

            AstarNode left = CalcPassNode(x - 1, z, AstarDir.LEFT, crowdSize, endX, endZ, targetCrowdSize);
            if (left != null)
                OpenNode(left, curNode, g);

            AstarNode right = CalcPassNode(x + 1, z, AstarDir.RIGHT, crowdSize, endX, endZ, targetCrowdSize);
            if (right != null)
                OpenNode(right, curNode, g);


            g = curNode.G + m_DirH;
            if (left != null && buttom != null)
            {
                AstarNode leftbottom = CalcPassNode(x - 1, z - 1, AstarDir.LEFTBOTTOM, crowdSize, endX, endZ, targetCrowdSize);
                if (leftbottom != null)
                    OpenNode(leftbottom, curNode, g);
            }

            if (right != null && buttom != null)
            {
                AstarNode rightbottom = CalcPassNode(x + 1, z - 1, AstarDir.RIGHTBOTTOM, crowdSize, endX, endZ, targetCrowdSize);
                if (rightbottom != null)
                    OpenNode(rightbottom, curNode, g);
            }

            if (left != null && top != null)
            {
                AstarNode lefttop = CalcPassNode(x - 1, z + 1, AstarDir.LEFTTOP, crowdSize, endX, endZ, targetCrowdSize);
                if (lefttop != null)
                    OpenNode(lefttop, curNode, g);
            }
            if (right != null && top != null)
            {
                AstarNode righttop = CalcPassNode(x + 1, z + 1, AstarDir.RIGHTTOP, crowdSize, endX, endZ, targetCrowdSize);
                if (righttop != null)
                    OpenNode(righttop, curNode, g);
            }
        }

        private void ProcessNode2(BfsNode curNode, int crowdSize, int endX, int endZ, int targetCrowdSize)
        {
            int x = curNode.x;
            int z = curNode.z;

            curNode.status = AstarNodeStatus.CLOSE;

            long step = curNode.step + m_LineStep;
            BfsNode top = CalcPassNode2(x, z + 1, AstarDir.UP, crowdSize, endX, endZ, targetCrowdSize);
            if (top != null && top.status != AstarNodeStatus.CLOSE)
                OpenNode2(top, curNode, step);

            BfsNode buttom = CalcPassNode2(x, z - 1, AstarDir.DOWN, crowdSize, endX, endZ, targetCrowdSize);
            if (buttom != null && buttom.status != AstarNodeStatus.CLOSE)
                OpenNode2(buttom, curNode, step);

            BfsNode left = CalcPassNode2(x - 1, z, AstarDir.LEFT, crowdSize, endX, endZ, targetCrowdSize);
            if (left != null && left.status != AstarNodeStatus.CLOSE)
                OpenNode2(left, curNode, step);

            BfsNode right = CalcPassNode2(x + 1, z, AstarDir.RIGHT, crowdSize, endX, endZ, targetCrowdSize);
            if (right != null && right.status != AstarNodeStatus.CLOSE)
                OpenNode2(right, curNode, step);


            step = curNode.step + m_DirStep;
            if (left != null && buttom != null)
            {
                BfsNode leftbottom = CalcPassNode2(x - 1, z - 1, AstarDir.LEFTBOTTOM, crowdSize, endX, endZ, targetCrowdSize);
                if (leftbottom != null && leftbottom.status != AstarNodeStatus.CLOSE)
                    OpenNode2(leftbottom, curNode, step);
            }

            if (right != null && buttom != null)
            {
                BfsNode rightbottom = CalcPassNode2(x + 1, z - 1, AstarDir.RIGHTBOTTOM, crowdSize, endX, endZ, targetCrowdSize);
                if (rightbottom != null && rightbottom.status != AstarNodeStatus.CLOSE)
                    OpenNode2(rightbottom, curNode, step);
            }

            if (left != null && top != null)
            {
                BfsNode lefttop = CalcPassNode2(x - 1, z + 1, AstarDir.LEFTTOP, crowdSize, endX, endZ, targetCrowdSize);
                if (lefttop != null && lefttop.status != AstarNodeStatus.CLOSE)
                    OpenNode2(lefttop, curNode, step);
            }
            if (right != null && top != null)
            {
                BfsNode righttop = CalcPassNode2(x + 1, z + 1, AstarDir.RIGHTTOP, crowdSize, endX, endZ, targetCrowdSize);
                if (righttop != null && righttop.status != AstarNodeStatus.CLOSE)
                    OpenNode2(righttop, curNode, step);
            }
        }

        public void SmoothPath(LogicVector3 start, LogicVector3 end, List<AstarNode> nodeList, ref TList<LogicVector3> pathList)
        {
            if (nodeList.Count < 1)
                return;

            //   pathList.Add(GetCellCenterPos(nodeList[0].centerX, nodeList[0].centerZ));

            int lastDiffX = 0;
            int lastDiffZ = 0;

            for (int i = 1; i < nodeList.Count - 1; i++)
            {
                if (i == 1)
                {
                    lastDiffX = nodeList[1].centerX - nodeList[0].centerX;
                    lastDiffZ = nodeList[1].centerZ - nodeList[0].centerZ;
                    continue;
                }

                int diffX = nodeList[i + 1].centerX - nodeList[i].centerX;
                int diffZ = nodeList[i + 1].centerZ - nodeList[i].centerZ;

                if (diffX != lastDiffX || diffZ != lastDiffZ)
                {
                    if (i == 2)
                    {
                        pathList.Add(GetCellCenterPos(nodeList[i - 1].centerX, nodeList[i - 1].centerZ));
                    }

                    pathList.Add(GetCellCenterPos(nodeList[i].centerX, nodeList[i].centerZ));

                    lastDiffX = diffX;
                    lastDiffZ = diffZ;
                }
            }

            pathList.Add(end);

        }

        //某个点是否可以通过  正4个方向判断边缘N个点,斜角4个方向只判断斜角1个点
        private bool IsFindPathCanPass(int centerX, int centerZ, AstarDir dir, int crowdSize,
            int targetX, int targetZ, int targetcrowdSize)
        {
            int crowdSizeHalf = crowdSize / 2;
            int targetcrowdSizeHalf = targetcrowdSize / 2;

            int targetxMin = targetX - targetcrowdSizeHalf;
            int targetxMax = targetX + targetcrowdSizeHalf;
            int targetzMin = targetZ - targetcrowdSizeHalf;
            int targetzMax = targetZ + targetcrowdSizeHalf;

            int minX = centerX - crowdSizeHalf;
            int maxX = centerX + crowdSizeHalf;
            int minZ = centerZ - crowdSizeHalf;
            int maxZ = centerZ + crowdSizeHalf;

            switch (dir)
            {
                case AstarDir.UP:
                    for (int i = 0; i < crowdSize; i++)
                    {
                        int x = minX + i;
                        int z = maxZ;

                        //如果是目标范围内 强制可以通过
                        if (x >= targetxMin && x <= targetxMax &&
                            z >= targetzMin && z <= targetzMax)
                        {
                            continue;
                        }

                        //忽略移动的
                        uint blockFlags = IsBlock(x, z);
                        if ((blockFlags & (uint)CellBlockType.StaticBlock) != 0 ||
                            (blockFlags & (uint)CellBlockType.DyncActorStop_Block) != 0)
                            return false;
                    }
                    break;
                case AstarDir.DOWN:
                    for (int i = 0; i < crowdSize; i++)
                    {
                        int x = minX + i;
                        int z = minZ;

                        //如果是目标范围内 强制可以通过
                        if (x >= targetxMin && x <= targetxMax &&
                            z >= targetzMin && z <= targetzMax)
                        {
                            continue;
                        }

                        //忽略移动的
                        uint blockFlags = IsBlock(x, z);
                        if ((blockFlags & (uint)CellBlockType.StaticBlock) != 0 ||
                            (blockFlags & (uint)CellBlockType.DyncActorStop_Block) != 0)
                            return false;
                    }
                    break;
                case AstarDir.LEFT:
                    for (int i = 0; i < crowdSize; i++)
                    {
                        int x = minX;
                        int z = minZ + i;

                        //如果是目标范围内 强制可以通过
                        if (x >= targetxMin && x <= targetxMax &&
                            z >= targetzMin && z <= targetzMax)
                        {
                            continue;
                        }

                        //忽略移动的
                        uint blockFlags = IsBlock(x, z);
                        if ((blockFlags & (uint)CellBlockType.StaticBlock) != 0 ||
                            (blockFlags & (uint)CellBlockType.DyncActorStop_Block) != 0)
                            return false;
                    }
                    break;
                case AstarDir.RIGHT:
                    for (int i = 0; i < crowdSize; i++)
                    {
                        int x = maxX;
                        int z = minZ + i;

                        //如果是目标范围内 强制可以通过
                        if (x >= targetxMin && x <= targetxMax &&
                            z >= targetzMin && z <= targetzMax)
                        {
                            continue;
                        }

                        //忽略移动的
                        uint blockFlags = IsBlock(x, z);
                        if ((blockFlags & (uint)CellBlockType.StaticBlock) != 0 ||
                            (blockFlags & (uint)CellBlockType.DyncActorStop_Block) != 0)
                            return false;
                    }
                    break;
                case AstarDir.LEFTBOTTOM:
                    {
                        int x = minX;
                        int z = minZ;

                        //如果是目标范围内 强制可以通过
                        if (x >= targetxMin && x <= targetxMax &&
                            z >= targetzMin && z <= targetzMax)
                        {
                            return true;
                        }

                        //忽略移动的
                        uint blockFlags = IsBlock(x, z);
                        if ((blockFlags & (uint)CellBlockType.StaticBlock) != 0 ||
                            (blockFlags & (uint)CellBlockType.DyncActorStop_Block) != 0)
                            return false;
                    }
                    break;
                case AstarDir.RIGHTBOTTOM:
                    {
                        int x = maxX;
                        int z = minZ;

                        //如果是目标范围内 强制可以通过
                        if (x >= targetxMin && x <= targetxMax &&
                            z >= targetzMin && z <= targetzMax)
                        {
                            return true;
                        }

                        //忽略移动的
                        uint blockFlags = IsBlock(x, z);
                        if ((blockFlags & (uint)CellBlockType.StaticBlock) != 0 ||
                            (blockFlags & (uint)CellBlockType.DyncActorStop_Block) != 0)
                            return false;
                    }
                    break;
                case AstarDir.LEFTTOP:
                    {
                        int x = minX;
                        int z = maxZ;

                        //如果是目标范围内 强制可以通过
                        if (x >= targetxMin && x <= targetxMax &&
                            z >= targetzMin && z <= targetzMax)
                        {
                            return true;
                        }

                        //忽略移动的
                        uint blockFlags = IsBlock(x, z);
                        if ((blockFlags & (uint)CellBlockType.StaticBlock) != 0 ||
                            (blockFlags & (uint)CellBlockType.DyncActorStop_Block) != 0)
                            return false;
                    }
                    break;
                case AstarDir.RIGHTTOP:
                    {
                        int x = maxX;
                        int z = maxZ;

                        //如果是目标范围内 强制可以通过
                        if (x >= targetxMin && x <= targetxMax &&
                            z >= targetzMin && z <= targetzMax)
                        {
                            return true;
                        }

                        //忽略移动的
                        uint blockFlags = IsBlock(x, z);
                        if ((blockFlags & (uint)CellBlockType.StaticBlock) != 0 ||
                            (blockFlags & (uint)CellBlockType.DyncActorStop_Block) != 0)
                            return false;
                    }
                    break;
            }

            return true;
        }

        private bool IsCrowdBlock(int centerX, int centerZ, AstarDir dir, int crowdSize)
        {
            int crowdSizeHalf = crowdSize / 2;

            int minX = centerX - crowdSizeHalf;
            int maxX = centerX + crowdSizeHalf;
            int minZ = centerZ - crowdSizeHalf;
            int maxZ = centerZ + crowdSizeHalf;

            switch (dir)
            {
                case AstarDir.UP:
                    if (maxZ >= m_MapBaseInfo.ColumnNum)
                        return true;

                    if (minX < 0)
                        minX = 0;

                    if (maxX >= m_MapBaseInfo.RowNum)
                        maxX = m_MapBaseInfo.RowNum - 1;

                    for (int x = minX; x <= maxX; x++)
                    {
                        if (IsCellBlock(x, maxZ))
                            return true;
                    }
                    break;
                case AstarDir.DOWN:
                    if (minZ < 0)
                        return true;

                    if (minX < 0)
                        minX = 0;

                    if (maxX >= m_MapBaseInfo.RowNum)
                        maxX = m_MapBaseInfo.RowNum - 1;

                    for (int x = minX; x <= maxX; x++)
                    {
                        if (IsCellBlock(x, minZ))
                            return true;
                    }
                    break;
                case AstarDir.LEFT:
                    if (minX < 0)
                        return true;

                    if (minZ < 0)
                        minZ = 0;

                    if (maxZ >= m_MapBaseInfo.ColumnNum)
                        maxZ = m_MapBaseInfo.ColumnNum;

                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (IsCellBlock(minX, z))
                           return true;
                    }
                    break;
                case AstarDir.RIGHT:
                    if (maxX >= m_MapBaseInfo.RowNum)
                        return true;

                    if (minZ < 0)
                        minZ = 0;

                    if (maxZ >= m_MapBaseInfo.ColumnNum)
                        maxZ = m_MapBaseInfo.ColumnNum;

                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (IsCellBlock(maxX, z))
                            return true;
                    }
                    break;
                case AstarDir.LEFTBOTTOM:
                    {
                        if (minX < 0 || minZ < 0)
                            return true;

                        if (IsCellBlock(minX, minZ))
                            return true;
                    }
                    break;
                case AstarDir.RIGHTBOTTOM:
                    {
                        if (maxX >= m_MapBaseInfo.RowNum || minZ < 0)
                            return true;

                        if (IsCellBlock(maxX, minZ))
                            return true;
                    }
                    break;
                case AstarDir.LEFTTOP:
                    {
                        if (minX < 0 || maxZ >= m_MapBaseInfo.ColumnNum)
                            return true;

                        if (IsCellBlock(minX, maxZ))
                            return true;
                    }
                    break;
                case AstarDir.RIGHTTOP:
                    {
                        if (maxX >= m_MapBaseInfo.RowNum || maxZ >= m_MapBaseInfo.ColumnNum)
                            return true;

                        if (IsCellBlock(maxX, maxZ))
                            return true;
                    }
                    break;
            }

            return false;
        }

        private long GetH(int s_x, int s_z, int e_x, int e_z)
        {
            LogicVector3 p1 = GetCellCenterPos(s_x, s_z);
            LogicVector3 p2 = GetCellCenterPos(e_x, e_z);
            LogicVector3 dir = p1 - p2;
            dir.y = 0;

            return dir.magnitudeSq;
        }
        #endregion

        public LogicVector3 GetCellCenterPos(int x, int z)
        {
            LogicVector3 center = new LogicVector3
            (x * m_MapBaseInfo.CellSize + m_MapBaseInfo.CellSize / 2, 0,
            z * m_MapBaseInfo.CellSize + m_MapBaseInfo.CellSize / 2);

            center = center + m_MapLeftBm;

            return center;
        }

        public bool GetCellIndexByPos(LogicVector3 pos, ref int cellX, ref int cellZ)
        {
            cellX = (pos.x - m_MapLeftBm.x) / m_MapBaseInfo.CellSize;
            cellZ = (pos.z - m_MapLeftBm.z) / m_MapBaseInfo.CellSize;

            return IsCellInMap(cellX, cellZ);
        }

        public bool IsCellInMap(int x, int z)
        {
            if (x < 0 || x >= m_MapBaseInfo.RowNum ||
                z < 0 || z >= m_MapBaseInfo.ColumnNum)
                return false;

            return true;
        }

        public uint IsCanCrossCell(int oldX, int oldZ, int centerX, int centerZ, int crowdSize)
        {
            int oldxMin = oldX - crowdSize / 2;
            int oldxMax = oldX + crowdSize / 2;
            int oldzMin = oldZ - crowdSize / 2;
            int oldzMax = oldZ + crowdSize / 2;

            uint blockFlags = 0;

            for (int i = 0; i < crowdSize; i++)
            {
                for (int j = 0; j < crowdSize; j++)
                {
                    int x = centerX + (i - crowdSize / 2);
                    int z = centerZ + (j - crowdSize / 2);

                    //老的自身站的格子 忽略
                    if (x >= oldxMin && x <= oldxMax &&
                        z >= oldzMin && z <= oldzMax)
                        continue;

                    uint flag = IsBlock(x, z);
                    if (flag != 0)
                    {
                        if ((flag & (uint)CellBlockType.StaticBlock) != 0 &&
                            (blockFlags & (uint)CellBlockType.StaticBlock) == 0)
                        {
                            blockFlags |= (uint)CellBlockType.StaticBlock;
                        }

                        if ((flag & (uint)CellBlockType.DyncActorStop_Block) != 0 &&
                            (blockFlags & (uint)CellBlockType.DyncActorStop_Block) == 0)
                        {
                            blockFlags |= (uint)CellBlockType.DyncActorStop_Block;
                        }

                        if ((flag & (uint)CellBlockType.DyncActorMoving_Block) != 0 &&
                            (blockFlags & (uint)CellBlockType.DyncActorMoving_Block) == 0)
                        {
                            blockFlags |= (uint)CellBlockType.DyncActorMoving_Block;
                        }
                    }
                }
            }

            return blockFlags;
        }

        public uint GetCellPassFlag(int centerX, int centerZ, int crowdSize)
        {
            uint blockFlags = 0;

            for (int i = 0; i < crowdSize; i++)
            {
                for (int j = 0; j < crowdSize; j++)
                {
                    int x = centerX + (i - crowdSize / 2);
                    int z = centerZ + (j - crowdSize / 2);

                    uint flag = IsBlock(x, z);
                    if (flag != 0)
                    {
                        if ((flag & (uint)CellBlockType.StaticBlock) != 0 &&
                            (blockFlags & (uint)CellBlockType.StaticBlock) == 0)
                        {
                            blockFlags |= (uint)CellBlockType.StaticBlock;
                        }

                        if ((flag & (uint)CellBlockType.DyncActorStop_Block) != 0 &&
                            (blockFlags & (uint)CellBlockType.DyncActorStop_Block) == 0)
                        {
                            blockFlags |= (uint)CellBlockType.DyncActorStop_Block;
                        }

                        if ((flag & (uint)CellBlockType.DyncActorMoving_Block) != 0 &&
                            (blockFlags & (uint)CellBlockType.DyncActorMoving_Block) == 0)
                        {
                            blockFlags |= (uint)CellBlockType.DyncActorMoving_Block;
                        }
                    }
                }
            }

            return blockFlags;
        }

        public bool IsMapCellCross(int cellX, int cellZ, int crowdSize)
        {
            for (int m = 0; m < crowdSize; m++)
            {
                for (int n = 0; n < crowdSize; n++)
                {
                    int x = cellX + (m - crowdSize / 2);
                    int z = cellZ + (n - crowdSize / 2);

                    if (!IsCellInMap(x, z))
                        return false;

                    int index = m_MapBaseInfo.RowNum * z + x;
                    if (m_MapBaseInfo.cellInfo[index].block != 0 ||
                        m_MapBaseInfo.cellInfo[index].actor_block != CellActorBlockType.NONE ||
                        m_MapBaseInfo.cellInfo[index].hero_block != CellActorBlockType.NONE)
                        return false;
                }
            }

            return true;
        }

        public bool _IsBlock(int centerX, int centerZ)
        {
            int old_index = m_MapBaseInfo.RowNum * centerZ + centerX;
            if (m_MapBaseInfo.cellInfo[old_index].block != 0)
            {
                return true;
            }

            if (m_MapBaseInfo.cellInfo[old_index].actor_block != 0 ||
                m_MapBaseInfo.cellInfo[old_index].hero_block != 0)
            {
                return true;
            }

            return false;
        }

        public uint IsBlock(int centerX, int centerZ)
        {
            if (!IsCellInMap(centerX, centerZ))
                return (uint)CellBlockType.StaticBlock;

            uint blockFlags = 0;

            int old_index = m_MapBaseInfo.RowNum * centerZ + centerX;
            if (m_MapBaseInfo.cellInfo[old_index].block == 1)
            {
                blockFlags |= (uint)CellBlockType.StaticBlock;
            }

            if (m_MapBaseInfo.cellInfo[old_index].actor_block == CellActorBlockType.Stop_Block ||
                m_MapBaseInfo.cellInfo[old_index].hero_block == CellActorBlockType.Stop_Block)
            {
                blockFlags |= (uint)CellBlockType.DyncActorStop_Block;
            }

            if (m_MapBaseInfo.cellInfo[old_index].actor_block == CellActorBlockType.Moving_Block ||
                m_MapBaseInfo.cellInfo[old_index].hero_block == CellActorBlockType.Moving_Block)
            {
                blockFlags |= (uint)CellBlockType.DyncActorMoving_Block;
            }

            return blockFlags;
        }

        public bool IsCellBlock(int centerX, int centerZ)
        {
            int index = m_MapBaseInfo.RowNum * centerZ + centerX;
            return (m_MapBaseInfo.cellInfo[index].block == 1 ||
                m_MapBaseInfo.cellInfo[index].actor_block == CellActorBlockType.Stop_Block ||
                m_MapBaseInfo.cellInfo[index].hero_block == CellActorBlockType.Stop_Block);
        }

        //标记某个区域障碍信息
        public void MarkMapCellActorStatus(bool isHero, int cellX, int cellZ, int crowdSize, CellActorBlockType flag, 
            bool bHeroMark= false)
        {
            int bound = crowdSize - 1;

            for (int m = 0; m < crowdSize; m++)
            {
                for (int n = 0; n < crowdSize; n++)
                {
                    // 去掉4个顶点的障碍，这样寻路时的路径不会拐角太大看起来不会摆头厉害
                    if ((m == 0 || m == bound) && (n == 0 || n == bound))
                        continue;

                    int x = cellX + (m - crowdSize / 2);
                    int z = cellZ + (n - crowdSize / 2);

                    if (!IsCellInMap(x, z))
                        continue;

                    int index = m_MapBaseInfo.RowNum * z + x;

                    if (isHero)
                    {
                        if (bHeroMark)
                        {
                            m_MapBaseInfo.cellInfo[index].hero_block = flag;
                        }
                        else
                        {
                            if (flag == CellActorBlockType.NONE)
                            {
                                m_MapBaseInfo.cellInfo[index].hero_blockRef--;

                                if (m_MapBaseInfo.cellInfo[index].hero_blockRef <= 0)
                                {
                                    m_MapBaseInfo.cellInfo[index].hero_block = CellActorBlockType.NONE;
                                }
                            }
                            else
                            {
                                m_MapBaseInfo.cellInfo[index].hero_blockRef++;
                                m_MapBaseInfo.cellInfo[index].hero_block = flag;
                            }

                            if (m_MapBaseInfo.cellInfo[index].hero_blockRef < 0)
                            {
                                #if UNITY_EDITOR || UNITY_SERVERS
                                             Debug.LogError(" 英雄格子阻挡标记错误！");
                                #else
                                                 LogUtils.LogError(" 英雄格子阻挡标记错误！");
                                #endif
                            }

                        }
                    }
                    else
                        m_MapBaseInfo.cellInfo[index].actor_block = flag;
                }
            }
        }

        public void MarkMapCellStaticStatus(int cellX, int cellZ, int crowdSize, byte flag)
        {
            for (int m = 0; m < crowdSize; m++)
            {
                for (int n = 0; n < crowdSize; n++)
                {
                    int x = cellX + (m - crowdSize / 2);
                    int z = cellZ + (n - crowdSize / 2);
                    if (!IsCellInMap(x, z))
                        continue;

                    int index = m_MapBaseInfo.RowNum * z + x;
                    m_MapBaseInfo.cellInfo[index].block = flag;
                }
            }
        }
      
        // 计算所在的环索引
        public int GetAroundLayer(LogicVector3 start, int crowdSize, LogicVector3 target, int targetcrowdSize)
        {
           int startX = 0;
            int startZ = 0;
            int targetX = 0;
            int targetZ = 0;

            GetCellIndexByPos(start, ref startX, ref startZ);
            GetCellIndexByPos(target, ref targetX, ref targetZ);

            int layer_x = (MathUtils.Abs(startX - targetX) - targetcrowdSize / 2 - targetcrowdSize%2) / crowdSize;
            int layer_z = (MathUtils.Abs(startZ - targetZ) - targetcrowdSize / 2 - targetcrowdSize%2) / crowdSize;

            return MathUtils.Max(layer_x, layer_z);
        }

        //目标是否可以连通
        //layerIndex 体积层级
        public bool FindAroundPosByLayer(LogicVector3 start, int crowdSize, LogicVector3 target, int targetcrowdSize, 
            int layerIndex,  ref int outX, ref int outZ, bool bUnion = false)
        {
            int startX = 0;
            int startZ = 0;
            int targetX = 0;
            int targetZ = 0;

            GetCellIndexByPos(start, ref startX, ref startZ);
            GetCellIndexByPos(target, ref targetX, ref targetZ);

            ClearAstarPoolNode();
            m_BoundList.clear();

            int size = targetcrowdSize + crowdSize * (layerIndex + 1);

            //不检测的范围
            int unsize = targetcrowdSize;
            if(layerIndex == 0)
            {
                unsize += crowdSize / 2;
            }
            else
            {
                unsize += crowdSize * layerIndex ;
            }

            int xMin = targetX - unsize / 2;
            int xMax = targetX + unsize / 2;
            int zMin = targetZ - unsize / 2;
            int zMax = targetZ + unsize / 2;

            int targetMinX = targetX - size / 2;
            int targetMaxX = targetX + size / 2;
            int targetMinZ = targetZ - size / 2;
            int targetMaxZ = targetZ + size / 2;

            targetMinX = (targetMinX >= 0) ? targetMinX : 0;
            targetMinZ = (targetMinZ >= 0) ? targetMinZ : 0;
            int targetWidth = targetMaxX - targetMinX + 1;

            for (int i = 0; i < m_GridFlags.Length; i++ )
            {
                m_GridFlags[i] = 0;
            }

            for (int z = targetMinZ; z <= targetMaxZ; z++)
            {
                for (int x = targetMinX; x <= targetMaxX; x++)
                {
                     int index = (z - targetMinZ) * targetWidth + (x - targetMinX) ;
                     if (index <m_GridFlags.Length &&  m_GridFlags[index] != 0)
                         continue;

                    if (IsBlock(x, z) != 0)
                    {
                        if (x < targetMaxX)
                        {
                            int index1 = index + 1;
                            if (index1 < m_GridFlags.Length)
                            {
                                m_GridFlags[index1] = 1;
                            }
                        }

                        //优化搜索
                        if (z < targetMaxZ)
                        {
                            int index2 = index + targetWidth;
                            if (index2 < m_GridFlags.Length)
                            {
                                m_GridFlags[index2] = 1;
                            }
                            else
                            {
                                #if UNITY_EDITOR || UNITY_SERVERS
                                    Debug.LogError("error ~!targetWidth:" + targetWidth);
                                #else
                                      LogUtils.LogError("error ~!targetWidth:" + targetWidth);
                                #endif
                            }

                            int index3 = index + targetWidth - 1;
                            if (index3 < m_GridFlags.Length)
                            {
                                m_GridFlags[index3] = 1;
                            }

                            int index4 = index + targetWidth + 1;
                            if (index4 < m_GridFlags.Length)
                            {
                                m_GridFlags[index4] = 1;
                            }
                        }

                        continue;
                    }

                    //目标范围忽略
                    if (x >= xMin && x <= xMax &&
                        z >= zMin && z <= zMax)
                        continue;

                    if (IsMapCellCross(x, z, crowdSize))
                    {
                        //层数
                        int max = MathUtils.Max(MathUtils.Abs(x - targetX), MathUtils.Abs(z - targetZ));

                        AstarNode node = GetFreeAstarNode();
                        if (node != null)
                        {
                            node.centerX = x;
                            node.centerZ = z;
                            node.G = (MathUtils.Abs(x - startX) + MathUtils.Abs(z - startZ)) + 10 * max;
                            m_BoundList.push(node);
                        }
                    }
                }
            }

            if (!m_BoundList.empty())
            {
                //剔除不可连通的
                if (bUnion)
                {
                    for (int i = 0; i < 3 && i < m_BoundList.getSize(); i++)
                    {
                        int centerX = m_BoundList.GetAt(i).centerX;
                        int centerZ = m_BoundList.GetAt(i).centerZ;

                        if (IsUnion(start, GetCellCenterPos(centerX, centerZ), crowdSize))
                        {
                            outX = centerX;
                            outZ = centerZ;
                            return true;
                        }
                    }

                    return false;
                }
                else
                {
                    outX = m_BoundList.top().centerX;
                    outZ = m_BoundList.top().centerZ;
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        //找到某个层级范围内 可以移动的格子
        public bool FindAroundPosBetweenLayer(LogicVector3 start, int crowdSize, LogicVector3 target, int targetcrowdSize,
          int start_layerIndex, int  end_layerIndex,  ref int outX, ref int outZ)
        {
            bool bFind = false;
            int i = end_layerIndex;

            //从外层开始 
            for(; i >= start_layerIndex; i--)
            {
                if (!FindAroundPosByLayer(start, crowdSize, target, targetcrowdSize, i, ref outX, ref outZ))
                    break;

                bFind = true;
            }

            return bFind;
        }

        //找到某个层级范围内 可以移动的格子
        public bool FindAroundPosBetweenLayerUnion(LogicVector3 start, int crowdSize, LogicVector3 target, int targetcrowdSize,
          int start_layerIndex, int end_layerIndex, ref int outX, ref int outZ)
        {
            bool bFind = false;
            int i = end_layerIndex;

            //从外层开始 
            for (; i >= start_layerIndex; i--)
            {
                if (!FindAroundPosByLayer(start, crowdSize, target, targetcrowdSize, i, ref outX, ref outZ, true))
                    break;

                bFind = true;
            }

            return bFind;
        }

        //判定2个点是否可以连通
        public bool IsUnion(LogicVector3 start, LogicVector3 end, int crowdSize)
        {
            if (!IsInAstarFindRegion(start, end))
                return false;

            int startX = 0;
            int startZ = 0;
            if (!GetCellIndexByPos(start, ref startX, ref startZ))
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("IsUnion start not in map:" + start);
#else
                LogUtils.LogError("IsUnion start not in map:" + start);
#endif
                return false;
            }

            int endX = 0;
            int endZ = 0;
            if (!GetCellIndexByPos(end, ref endX, ref endZ))
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("IsUnion end not in map:" + start);
#else
                LogUtils.LogError("IsUnion end not in map:" + start);
#endif
                return false;
            }

            int offsetX = m_RegionCnt - MathUtils.Abs(startX - endX);
            int offsetZ = m_RegionCnt - MathUtils.Abs(startZ - endZ);
            if (offsetX < 0 || offsetZ < 0)
            {
#if UNITY_EDITOR || UNITY_SERVERS
                Debug.LogError("FindPath  too long start:" + start + ",end:" + end);
#else
                LogUtils.LogError("FindPath  too long start:" + start + ",end:" + end);
#endif
                return false;
            }

            m_RegionMinX = MathUtils.Min(startX, endX) - offsetX / 2;
            m_RegionMaxX = m_RegionMinX + m_RegionCnt;
            m_RegionMinZ = MathUtils.Min(startZ, endZ) - offsetZ / 2; ;
            m_RegionMaxZ = m_RegionMinZ + m_RegionCnt;

            m_MapUnionFind.BuildUnion(m_RegionMinX, m_RegionMinZ, startX, startZ, endX, endZ, crowdSize);

            return m_MapUnionFind.IsUnion(m_RegionMinX, m_RegionMinZ, 
                startX, startZ, endX, endZ);
			
        }

        //查找某一层任意可以移动的点
        public bool FindUnBlockPosByLayer(LogicVector3 target, int crowdSize, int layerIndex, ref int outX, ref int outZ)
        {
            int targetX = 0;
            int targetZ = 0;
            if(!GetCellIndexByPos(target, ref targetX, ref targetZ))
            {
                outX = 0;
                outZ = 0;
                return false;
            }

            int size = crowdSize + crowdSize * (layerIndex + 1);

            //不检测的范围
            int unsize = crowdSize;
            if (layerIndex == 0)
            {
                unsize += crowdSize / 2;
            }
            else
            {
                unsize += crowdSize * layerIndex;
            }

            int xMin = targetX - unsize / 2;
            int xMax = targetX + unsize / 2;
            int zMin = targetZ - unsize / 2;
            int zMax = targetZ + unsize / 2;

            int x = 0;
            int z = 0;
            for (int i = 0; i <= size; i++)
            {
                for (int j = 0; j <= size; j++)
                {
                    x = targetX + (i - size / 2);
                    z = targetZ + (j - size / 2);

                    //目标范围忽略
                    if (x >= xMin && x <= xMax &&
                        z >= zMin && z <= zMax)
                        continue;

                    if (IsMapCellCross(x, z, crowdSize))
                    {
                        outX = x;
                        outZ = z;
                        return true;
                    }
                }
            }

            return false;
        }

        bool Line_rect_Intersection(LogicVector3 start_p, LogicVector3 end_p, LogicVector3 rect_center, int size)
        {
            long a = start_p.z - end_p.z;
            long b = end_p.x - start_p.x;
            long c = (long)start_p.x * (long)end_p.z - (long)end_p.x * (long)start_p.z;

            long left = rect_center.x - size / 2;
            long right = rect_center.x + size / 2;
            long top = rect_center.z + size / 2;
            long bottom = rect_center.z - size / 2;

            long d1 = a * left + b * top + c;
            long d2 = a * right + b * bottom + c;
            long d3 = a * left + b * bottom + c;
            long d4 = a * right + b * top + c;
         
            if ((d1 >= 0 && d2 <= 0) || (d2 <= 0 && d2 >= 0) ||
                (d3 >= 0 && d4 <= 0) || (d3 <= 0 && d4 >= 0))
            {
                if (left > right)
                {
                    long temp = left;
                    left = right;
                    right = temp;
                }

                if (top < bottom)
                {
                    long temp = top;
                    top = bottom;
                    bottom = temp;
                }

                ///判断线段是否在矩形一侧
                if ((start_p.x < left && end_p.x < left) ||
                    (start_p.x > right && end_p.x > right) ||
                    (start_p.z > top && end_p.z > top) ||
                    (start_p.z < bottom && end_p.z < bottom))  
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        //寻找路径上可以移动点
        public bool FindBetweenTargetNearstPos(LogicVector3 line_start, LogicVector3 line_end, LogicVector3 pos,  int crowdSize, 
            ref int outX, ref int outZ)
        {
            int endX = 0;
            int endZ = 0;
            int curX = 0;
            int curZ = 0;

            GetCellIndexByPos(line_end, ref endX, ref endZ);
            GetCellIndexByPos(pos, ref curX, ref curZ);

            int temp1 = curX;
            int temp2 = endX;
            if (MathUtils.Abs(curX - endX) > m_RegionCnt)
            {
                temp2 = temp1 + MathUtils.Abs(endX - curX) * m_RegionCnt / (endX - curX);
            }
            int regionMinX = MathUtils.Min(temp1, temp2);
            int regionMaxX = MathUtils.Max(temp1, temp2);

            temp1 = curZ;
            temp2 = endZ;
            if (MathUtils.Abs(curZ - endZ) > m_RegionCnt)
            {
                temp2 = temp1 + MathUtils.Abs(endZ - curZ) * m_RegionCnt / (endZ - curZ);
            }
            int regionMinZ = MathUtils.Min(temp1, temp2);
            int regionMaxZ = MathUtils.Max(temp1, temp2);
         

            for (int i = regionMinX; i <= regionMaxX; i++)
            {
                for (int j = regionMinZ; j <= regionMaxZ; j++)
                {
                    LogicVector3 center = GetCellCenterPos(i, j);
                    if (!Line_rect_Intersection(line_start, line_end, center, m_MapBaseInfo.CellSize))
                        continue;

                    if(!IsMapCellCross(i, j, crowdSize))
                        continue;

                    if (FindAroundPosBetweenLayer(line_start, crowdSize, center, crowdSize, 0, 1, ref  outX, ref  outZ))
                     {
                         outX = i;
                         outZ = j;

                         return true;
                     }

                }
            }
               
      
            return false;
        }

    }
}
