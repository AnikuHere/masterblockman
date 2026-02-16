using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MOBA
{
    public interface dtPolyQuery
    {
         void process(dtMeshTile tile, dtPoly[] polys, uint[] refs, int count);
    };

    public class dtFindNearestPolyQuery :  dtPolyQuery
    {
	    private dtNavMeshQuery m_query;
        private LogicVector3 m_center;

	    private long m_nearestDistanceSqr;
	    private uint m_nearestRef;
        private LogicVector3 m_nearestPoint;

	   public dtFindNearestPolyQuery(dtNavMeshQuery query)
	    {
            m_nearestDistanceSqr = long.MaxValue;
            m_query = query;
	    }

       public void Init(LogicVector3 center)
       {
           m_nearestDistanceSqr = long.MaxValue;
           m_center = center;
       }

        public void UnInit()
       {
           m_query = null;
       }

	    public uint nearestRef()  { return m_nearestRef; }
	    public LogicVector3 nearestPoint()  { return m_nearestPoint; }

	    public  void  process(dtMeshTile tile,  dtPoly[] polys,  uint[] refs, int count)
	    {
		    for (int i = 0; i < count; ++i)
		    {
			    uint _ref = refs[i];
                LogicVector3 closestPtPoly = LogicVector3.zero;
                LogicVector3 diff;
			    bool posOverPoly = false;
			    long d;
			    m_query.closestPointOnPoly(_ref, m_center, ref closestPtPoly, ref posOverPoly);

			    // If a point is directly over a polygon and closer than
			    // climb height, favor that instead of straight line nearest point.
                diff = m_center - closestPtPoly;
			    if (posOverPoly)
			    {
				    d = NavMeshMath.Abs(diff.y) - tile.header.walkableClimb;
				    d = d > 0 ? d*d : 0;			
			    }
			    else
			    {
                    d = diff.magnitudeSq;
			    }
			
			    if (d < m_nearestDistanceSqr)
			    {
                    m_nearestPoint = closestPtPoly;

				    m_nearestDistanceSqr = d;
				    m_nearestRef = _ref;
			    }
		    }
	    }
    };


    public class dtNavMeshQuery
    {
        private dtNavMesh m_NavMesh;

       private dtNodePool m_NodePool;
       private dtNodePool m_TinyNodePool;

       private dtNodeQueue m_OpenList;

       //====运行时 
       private const int MAX_NEIS = 32;
       private dtMeshTile[] m_TempNeis = new dtMeshTile[MAX_NEIS];

       private dtFindNearestPolyQuery m_FindQuery = null;

       public void OnInit(dtNavMesh navMesh, int maxNodes)
        {
            m_NavMesh = navMesh;

            m_NodePool = new dtNodePool(maxNodes, NavMeshMath. NextPow2(maxNodes/4));
            m_TinyNodePool = new dtNodePool(64, 32);
            m_OpenList = new dtNodeQueue(maxNodes);

            m_FindQuery = new dtFindNearestPolyQuery(this);
        }

        public void OnUnInit()
        {
            m_NodePool.clear();
            m_NodePool = null;
            
            m_TinyNodePool.clear();
            m_TinyNodePool = null;

            m_OpenList.clear();
            m_OpenList = null;

            m_FindQuery.UnInit();
            m_FindQuery = null;

            m_TempNeis = null;
        }


        public bool findNearestPoly(LogicVector3 center,LogicVector3 extents, ref uint nearestRef,
            ref LogicVector3 nearestPt)
        {
            LogicVector3 bmin = center - extents;
            LogicVector3 bmax = center + extents;
          
            // Find tiles the query touches.
	        int minx = 0, miny = 0, maxx = 0, maxy = 0;
	        m_NavMesh.calcTileLoc(bmin, ref minx, ref miny);
	        m_NavMesh.calcTileLoc(bmax, ref maxx, ref maxy);

            m_FindQuery.Init(center);
	
	        for (int y = miny; y <= maxy; ++y)
	        {
		        for (int x = minx; x <= maxx; ++x)
		        {
                    int nneis = m_NavMesh.getTilesAt(x, y, ref m_TempNeis, MAX_NEIS);
			        for (int j = 0; j < nneis; ++j)
			        {
                        queryPolygonsInTile(m_TempNeis[j], bmin, bmax, m_FindQuery);
			        }
		        }
	        }

            nearestRef = m_FindQuery.nearestRef();
            LogicVector3 nearstPt = m_FindQuery.nearestPoint();
            nearestPt.x = nearstPt.x;
            nearestPt.y = nearstPt.y;
            nearestPt.z = nearstPt.z;

            return true;
        }

        public bool SamplePosition(LogicVector3 center)
        {
            LogicVector3 bmin = center;
            LogicVector3 bmax = center;

            // Find tiles the query touches.
            int minx = 0, miny = 0, maxx = 0, maxy = 0;
            m_NavMesh.calcTileLoc(bmin, ref minx, ref miny);
            m_NavMesh.calcTileLoc(bmax, ref maxx, ref maxy);

            LogicVector3 _center = center;

            for (int y = miny; y <= maxy; ++y)
            {
                for (int x = minx; x <= maxx; ++x)
                {
                    int nneis = m_NavMesh.getTilesAt(x, y, ref m_TempNeis, MAX_NEIS);
                    for (int j = 0; j < nneis; ++j)
                    {
                        bool bFind = queryPointInPolygons(_center, m_TempNeis[j]);
                        if (bFind)
                            return true;
                    }
                }
            }

          
            return false;
        }


        private int[] m_QueryTempVerts = new int[NavMeshBuilderDefine.DT_VERTS_PER_POLYGON * 3];	

        private bool queryPointInPolygons(LogicVector3 center, dtMeshTile tile)
        {
            for (int i = 0; i < tile.header.polyCount; ++i)
            {
                dtPoly poly = tile.polys[i];

                for (int j = 0; j < poly.vertCount; j++)
                {
                    m_QueryTempVerts[j * 3] = tile.verts[poly.verts[j] * 3];
                    m_QueryTempVerts[j * 3 + 1] = tile.verts[poly.verts[j] * 3 + 1];
                    m_QueryTempVerts[j * 3 + 2] = tile.verts[poly.verts[j] * 3 + 2];
                }

                if (dtPointInPolygon(center, m_QueryTempVerts, poly.vertCount))
                    return true;
            }

            return false;
        }


#region 查找

        private const int m_BatchSize = 32;
        private uint[] m_QueryPolyRefs = new uint[m_BatchSize];
        private dtPoly[] m_QueryPolys = new dtPoly[m_BatchSize];

        private void queryPolygonsInTile(dtMeshTile tile, LogicVector3 qmin, LogicVector3 qmax, dtPolyQuery query)
        {
           
            int n = 0;

            LogicVector3 bV;
            LogicVector3 bmin = LogicVector3.zero;
            LogicVector3 bmax = LogicVector3.zero;
            uint _base = m_NavMesh.getPolyRefBase(tile);
            for (int i = 0; i < tile.header.polyCount; ++i)
            {
                dtPoly p = tile.polys[i];
                // Do not return off-mesh connection polygons.
                if (p.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                    continue;

                uint _ref = _base | (uint)i;

                bmin.x = tile.verts[p.verts[0] * 3];
                bmin.y = tile.verts[p.verts[0] * 3 + 1];
                bmin.z = tile.verts[p.verts[0] * 3 + 2];

                bmax = bmin;

                for (int j = 1; j < p.vertCount; ++j)
                {
                    bV.x = tile.verts[p.verts[j] * 3];
                    bV.y = tile.verts[p.verts[j] * 3 + 1];
                    bV.z = tile.verts[p.verts[j] * 3 + 2];

                    NavMeshMath.Vmin(ref bmin, bV);
                    NavMeshMath.Vmax(ref bmax, bV);
                }

                if (NavMeshMath.dtOverlapBounds(qmin, qmax, bmin, bmax))
                {
                    m_QueryPolyRefs[n] = _ref;
                    m_QueryPolys[n] = p;

                    if (n == m_BatchSize - 1)
                    {
                        query.process(tile, m_QueryPolys, m_QueryPolyRefs, m_BatchSize);
                        n = 0;
                    }
                    else
                    {
                        n++;
                    }
                }
            }

            if (n > 0)
                query.process(tile, m_QueryPolys, m_QueryPolyRefs, n);
        }


        private int[] m_TempVerts = new int[NavMeshBuilderDefine.DT_VERTS_PER_POLYGON * 3];
        private long[] m_TempEdged = new long[NavMeshBuilderDefine.DT_VERTS_PER_POLYGON];
        private  int[] m_TempEdget = new int[NavMeshBuilderDefine.DT_VERTS_PER_POLYGON];

        public dtStatus closestPointOnPoly(uint _ref, LogicVector3 pos, ref LogicVector3 closest, ref bool posOverPoly)
        {
	         dtMeshTile tile = null;
	         dtPoly poly = null;
	        if (m_NavMesh.getTileAndPolyByRef(_ref, ref tile, ref poly) != dtStatus.DT_SUCCESS)
		        return dtStatus.DT_FAILURE;
	   
	        // Clamp point to be inside the polygon.
	     
	         int nv = poly.vertCount;
	        for (int i = 0; i < nv; ++i)
            {
                m_TempVerts[i * 3] = tile.verts[poly.verts[i] * 3];
                m_TempVerts[i * 3 + 1] = tile.verts[poly.verts[i] * 3 + 1];
                m_TempVerts[i * 3 + 2] = tile.verts[poly.verts[i] * 3 + 2];
            }

            closest = pos;

            if (!NavMeshMath.dtDistancePtPolyEdgesSqr(pos, m_TempVerts, nv, ref m_TempEdged, ref m_TempEdget))
	        {
		        // Point is outside the polygon, dtClamp to nearest edge.
                long dmin = m_TempEdged[0];
		        int imin = 0;
		        for (int i = 1; i < nv; ++i)
		        {
                    if (m_TempEdged[i] < dmin)
			        {
                        dmin = m_TempEdged[i];
				        imin = i;
			        }
		        }

                closest.x = m_TempVerts[imin * 3 + 0] + (int)((long)(m_TempVerts[((imin + 1) % nv) * 3 + 0] - m_TempVerts[imin * 3 + 0]) *
                    (long)m_TempEdget[imin] / MathUtils.iPointUnit);
                closest.y = m_TempVerts[imin * 3 + 1] + (int)((long)(m_TempVerts[((imin + 1) % nv) * 3 + 1] - m_TempVerts[imin * 3 + 1]) *
                    (long)m_TempEdget[imin] / MathUtils.iPointUnit);
                closest.z = m_TempVerts[imin * 3 + 2] + (int)((long)(m_TempVerts[((imin + 1) % nv) * 3 + 2] - m_TempVerts[imin * 3 + 2]) *
                    (long)m_TempEdget[imin] / MathUtils.iPointUnit);

			   posOverPoly = false;
	        }
	        else
	        {
			     posOverPoly = true;
	        }

            int height = 0;
            if(GetPolyHeight(_ref, pos, ref height) == dtStatus.DT_SUCCESS)
            {
                closest.y = height;
            }

            return dtStatus.DT_SUCCESS;
        }


        private bool dtPointInPolygon(LogicVector3 pt, int[] verts, int nverts)
        {
	        // TODO: Replace pnpoly with triArea2D tests?
	        int i, j;
	        bool c = false;
	        for (i = 0, j = nverts-1; i < nverts; j = i++)
	        {
		        int vi = i*3;
		        int vj = j*3; 
		        if (((verts[vi+2] > pt.z) != (verts[vj+2] > pt.z)) &&
			        (pt.x < (long)(verts[vj+0]-verts[vi+0]) *  (long)(pt.z-verts[vi+2]) /  (long)(verts[vj+2]-verts[vi+2]) + verts[vi+0]) )
			        c = !c;
	        }
	        return c;
        }


        private byte[] m_PolyHeighByte = new byte[3];
        private LogicVector3[] m_PolyHeighVert = new LogicVector3[3];

        public dtStatus GetPolyHeight(uint _ref, LogicVector3 pos, ref int height)
        {
	        dtMeshTile tile = null;
	        dtPoly poly = null;
	        if (m_NavMesh.getTileAndPolyByRef(_ref, ref tile, ref poly) != dtStatus.DT_SUCCESS)
		        return dtStatus.DT_FAILURE;
	
	        if (poly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
	        {
                LogicVector3 v0;
                LogicVector3 v1;
                v0.x = tile.verts[poly.verts[0]*3];
                v0.y = tile.verts[poly.verts[0]*3 + 1];
                v0.z = tile.verts[poly.verts[0]*3 + 2];

                v1.x = tile.verts[poly.verts[1]*3];
                v1.y = tile.verts[poly.verts[1]*3 + 1];
                v1.z = tile.verts[poly.verts[1]*3 + 2];

		        int d0 = NavMeshMath. dtVdist2D(pos, v0);
		        int d1 = NavMeshMath.dtVdist2D(pos, v1);
			    height = v0.y + (v1.y - v0.y) * d0 /(d0+d1) ;

		        return dtStatus.DT_SUCCESS;
	        }
	        else
	        {
		        uint ip = 0;
                for(uint i=0; i <tile.polys.Length; i++ )
                {
                    if(poly == tile.polys[i])
                    {
                        ip = i;
                        break;
                    }
                }

             

		        dtPolyDetail pd = tile.detailMeshes[ip];
		        for (int j = 0; j < pd.triCount; ++j)
		        {
                    m_PolyHeighByte[0] = tile.detailTris[(pd.triBase + j) * 4];
                    m_PolyHeighByte[1] = tile.detailTris[(pd.triBase + j) * 4 + 1];
                    m_PolyHeighByte[2] = tile.detailTris[(pd.triBase + j) * 4 + 2];

			        for (int k = 0; k < 3; ++k)
			        {
                        if (m_PolyHeighByte[k] < poly.vertCount)
                        {
                            m_PolyHeighVert[k].x = tile.verts[poly.verts[m_PolyHeighByte[k]] * 3];
                            m_PolyHeighVert[k].y = tile.verts[poly.verts[m_PolyHeighByte[k]] * 3 + 1];
                            m_PolyHeighVert[k].z = tile.verts[poly.verts[m_PolyHeighByte[k]] * 3 + 2];
                        }
                        else
                        {
                            m_PolyHeighVert[k].x = tile.detailVerts[(pd.vertBase + (m_PolyHeighByte[k] - poly.vertCount)) * 3];
                            m_PolyHeighVert[k].y = tile.detailVerts[(pd.vertBase + (m_PolyHeighByte[k] - poly.vertCount)) * 3 + 1];
                            m_PolyHeighVert[k].z = tile.detailVerts[(pd.vertBase + (m_PolyHeighByte[k] - poly.vertCount)) * 3 + 2];
                        }
			        }

			        int h = 0;
                    if (NavMeshMath.dtClosestHeightPointTriangle(pos, m_PolyHeighVert[0], m_PolyHeighVert[1], m_PolyHeighVert[2], ref h))
			        {
					    height = h;
				        return dtStatus.DT_SUCCESS;
			        }
		        }
	        }
	
	        return dtStatus.DT_FAILURE;
        }

        //是否孤岛
        public bool IsTilePolyIsLand(uint _ref)
        {
            dtMeshTile bestTile = null;
            dtPoly bestPoly = null;
            if(m_NavMesh.getTileAndPolyByRef(_ref, ref bestTile, ref bestPoly) != dtStatus.DT_SUCCESS)
            {
                return true;
            }

            for (int j = 0; j < bestPoly.vertCount; j++)
            {
                if (bestPoly.neis[j] == 0) 
                    continue;

                return false;
            }

            return true;
        }


#endregion

#region  寻路
        public dtStatus findPath(uint startRef, uint endRef, LogicVector3 startPos, LogicVector3 endPos, ref uint[] path,
            ref int pathCount, int maxPath) 
        {
	        pathCount = 0;
	
	        // Validate input
	        if (!m_NavMesh.isValidPolyRef(startRef) || !m_NavMesh.isValidPolyRef(endRef) ||
		       maxPath <= 0 || path == null)
		        return dtStatus.DT_FAILURE;

	        if (startRef == endRef)
	        {
		        path[0] = startRef;
		        pathCount = 1;
		        return dtStatus.DT_SUCCESS;
	        }
	
	        m_NodePool.clear();
	        m_OpenList.clear();
	
	        dtNode startNode = m_NodePool.getNode(startRef);
            startNode.pos =  startPos;
	        startNode.pidx = 0;
	        startNode.cost = 0;
	        startNode.total = NavMeshMath.Distance2D(startPos, endPos);
	        startNode.id = startRef;
	        startNode.flags = (byte)dtNodeFlags.DT_NODE_OPEN;
            m_OpenList.push(startNode);
	
	        dtNode lastBestNode = startNode;
	        int lastBestNodeCost = startNode.total;
	
	        dtStatus status = dtStatus.DT_SUCCESS;

            while (!m_OpenList.empty())
	        {
		        // Remove node from open list and put it in closed list.
                dtNode bestNode = m_OpenList.pop();
		        bestNode.flags &= ~(uint)dtNodeFlags.DT_NODE_OPEN;
		        bestNode.flags |= (uint)dtNodeFlags.DT_NODE_CLOSED;

		        // Reached the goal, stop searching.
		        if (bestNode.id == endRef)
		        {
			        lastBestNode = bestNode;
			        break;
		        }
		
		        // Get current poly and tile.
		        // The API input has been cheked already, skip checking internal data.
		         uint bestRef = bestNode.id;
		        dtMeshTile bestTile = null;
		        dtPoly bestPoly = null;
		        m_NavMesh.getTileAndPolyByRef(bestRef, ref bestTile, ref bestPoly);
		
		        // Get parent poly and tile.
		        uint parentRef = 0;
		        dtMeshTile parentTile = null;
		        dtPoly parentPoly = null;
		        if (bestNode.pidx != 0)
			        parentRef = m_NodePool.getNodeAtIdx(bestNode.pidx).id;

		        if (parentRef != 0)
			        m_NavMesh.getTileAndPolyByRef(parentRef, ref parentTile, ref parentPoly);
		
		        for (uint i = bestPoly.firstLink; i != NavMeshBuilderDefine.DT_NULL_LINK; i = bestTile.links[i].next)
		        {
			        uint neighbourRef = bestTile.links[i]._ref;
			
			        // Skip invalid ids and do not expand back to where we came from.
			        if (neighbourRef == 0 || neighbourRef == parentRef)
				        continue;
			
			        // Get neighbour poly and tile.
			        // The API input has been cheked already, skip checking internal data.
			        dtMeshTile neighbourTile = null;
			        dtPoly neighbourPoly = null;
			        m_NavMesh.getTileAndPolyByRef(neighbourRef, ref neighbourTile, ref neighbourPoly);			
		
			        // deal explicitly with crossing tile boundaries
			         byte crossSide = 0;
			        if (bestTile.links[i].side != 0xff)
				        crossSide = (byte)(bestTile.links[i].side >> 1);

			        // get the node
			        dtNode neighbourNode = m_NodePool.getNode(neighbourRef, crossSide);
			        if (neighbourNode == null)
                    {
#if UNITY_EDITOR || UNITY_SERVERS
                        Debug.LogError("findPath out of nodes,start:" + startPos + ",end:"+ endPos);
#else
                        LogUtils.LogError("findPath out of nodes,start:" + startPos + ",end:"+ endPos);
#endif
                        status = dtStatus.DT_OUT_OF_NODES;
				        continue;
			        }
			
			        // If the node is visited the first time, calculate node position.
			        if (neighbourNode.flags == 0)
			        {
				        getEdgeMidPoint(bestRef, bestPoly, bestTile,
								        neighbourRef, neighbourPoly, neighbourTile,
								         ref neighbourNode.pos);
			        }

			        // Calculate cost and heuristic.
			        int cost = 0;
                    int heuristic = 0;
			
			        // Special case for last node.
			        if (neighbourRef == endRef)
			        {
				        // Cost
                        int curCost = NavMeshMath.Distance2D(bestNode.pos, neighbourNode.pos);
				        int  endCost =NavMeshMath.Distance2D(neighbourNode.pos, endPos);
				        cost = bestNode.cost + curCost + endCost;
				        heuristic = 0;
			        }
			        else
			        {
				        // Cost
                        int curCost = NavMeshMath.Distance2D(bestNode.pos, neighbourNode.pos);
				        cost = bestNode.cost + curCost;
				        heuristic = NavMeshMath.Distance2D(neighbourNode.pos, endPos);
			        }

                    int total = cost + heuristic;
			
			        // The node is already in open list and the new result is worse, skip.
                    if (((neighbourNode.flags & (uint)dtNodeFlags.DT_NODE_OPEN) != 0) && total >= neighbourNode.total)
				        continue;
			        // The node is already visited and process, and the new result is worse, skip.
                    if ((neighbourNode.flags &  (uint)dtNodeFlags.DT_NODE_CLOSED) != 0 && total >= neighbourNode.total)
				        continue;
			
			        // Add or update the node.
			        neighbourNode.pidx = m_NodePool.getNodeIdx(bestNode);
			        neighbourNode.id = neighbourRef;
                    neighbourNode.flags = (neighbourNode.flags & ~(uint)dtNodeFlags.DT_NODE_CLOSED);
			        neighbourNode.cost = cost;
			        neighbourNode.total = total;

                    if ((neighbourNode.flags & (uint)dtNodeFlags.DT_NODE_OPEN) != 0)
			        {
				        // Already in open, update node location.
				        m_OpenList.modify(neighbourNode);
			        }
			        else
			        {
				        // Put the node in open list.
                        neighbourNode.flags |= (uint)dtNodeFlags.DT_NODE_OPEN;
                        m_OpenList.push(neighbourNode);
			        }
			
			        // Update nearest node to target so far.
			        if (heuristic < lastBestNodeCost)
			        {
				        lastBestNodeCost = heuristic;
				        lastBestNode = neighbourNode;
			        }
		        }
	        }

            status = getPathToNode(lastBestNode, ref path, ref pathCount, maxPath);

	        if (lastBestNode.id != endRef)
		        status  = dtStatus.DT_FAILURE;


            return status;
        }

        dtStatus getEdgeMidPoint(uint from, dtPoly fromPoly, dtMeshTile fromTile,
                                   uint to, dtPoly toPoly, dtMeshTile toTile,
                                   ref LogicVector3 mid)
        {
            LogicVector3 left = LogicVector3.zero;
            LogicVector3 right = LogicVector3.zero;
            if (getPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, ref left, ref right) != dtStatus.DT_SUCCESS)
                return dtStatus.DT_FAILURE;
            mid.x = (left.x + right.x) / 2;
            mid.y = (left.y + right.y) / 2;
            mid.z = (left.z + right.z) / 2;
            return dtStatus.DT_SUCCESS;
        }


        dtStatus getPathToNode(dtNode endNode, ref uint[] path, ref int pathCount, int maxPath)
        {
            // Find the length of the entire path.
            dtNode curNode = endNode;
            int length = 0;
            do
            {
                length++;
                curNode = m_NodePool.getNodeAtIdx(curNode.pidx);
            } while (curNode != null);

            // If the path cannot be fully stored then advance to the last node we will be able to store.
            curNode = endNode;
            int writeCount;
            for (writeCount = length; writeCount > maxPath; writeCount--)
            {
                curNode = m_NodePool.getNodeAtIdx(curNode.pidx);
            }

            // Write path
            for (int i = writeCount - 1; i >= 0; i--)
            {
                path[i] = curNode.id;
                curNode = m_NodePool.getNodeAtIdx(curNode.pidx);
            }

            pathCount = NavMeshMath.Min(length, maxPath);

            if (length > maxPath)
                return dtStatus.DT_SUCCESS;

            return dtStatus.DT_SUCCESS;
        }


        dtStatus closestPointOnPolyBoundary(uint _ref, LogicVector3 pos, ref  LogicVector3 closest)
        {
            dtMeshTile tile = null;
            dtPoly poly = null;
            if (m_NavMesh.getTileAndPolyByRef(_ref, ref tile, ref poly) != dtStatus.DT_SUCCESS)
                return dtStatus.DT_FAILURE;

            int nv = 0;
            for (int i = 0; i < (int)poly.vertCount; ++i)
            {
                m_TempVerts[nv * 3] = tile.verts[poly.verts[i] * 3];
                m_TempVerts[nv * 3 + 1] = tile.verts[poly.verts[i] * 3 + 1];
                m_TempVerts[nv * 3 + 2] = tile.verts[poly.verts[i] * 3 + 2];
                nv++;
            }

            bool inside = NavMeshMath.dtDistancePtPolyEdgesSqr(pos, m_TempVerts, nv, ref m_TempEdged, ref m_TempEdget);
            if (inside)
            {
                // Point is inside the polygon, return the point.
                closest = pos;
            }
            else
            {
                // Point is outside the polygon, dtClamp to nearest edge.
                long dmin = m_TempEdged[0];
                int imin = 0;
                for (int i = 1; i < nv; ++i)
                {
                    if (m_TempEdged[i] < dmin)
                    {
                        dmin = m_TempEdged[i];
                        imin = i;
                    }
                }

                closest.x = m_TempVerts[imin * 3 + 0] + (int)((long)(m_TempVerts[((imin + 1) % nv) * 3 + 0] - m_TempVerts[imin * 3 + 0]) * (long)m_TempEdget[imin] / MathUtils.iPointUnit);
                closest.y = m_TempVerts[imin * 3 + 1] + (int)((long)(m_TempVerts[((imin + 1) % nv) * 3 + 1] - m_TempVerts[imin * 3 + 1]) * (long)m_TempEdget[imin] / MathUtils.iPointUnit);
                closest.z = m_TempVerts[imin * 3 + 2] + (int)((long)(m_TempVerts[((imin + 1) % nv) * 3 + 2] - m_TempVerts[imin * 3 + 2]) * (long)m_TempEdget[imin] / MathUtils.iPointUnit);
            }

            return dtStatus.DT_SUCCESS;
        }

#endregion

#region  计算拐点

        private const int MAX_STEER_POINTS = 100;
        private int[] m_SteerPath = new int[MAX_STEER_POINTS * 3];
        private byte[] m_SteerPathFlags = new byte[MAX_STEER_POINTS];
        private uint[] m_SteerPathPolys = new uint[MAX_STEER_POINTS];

        public dtStatus CreateWayPoints(LogicVector3 startPos, LogicVector3 endPos,
            uint[] path,  int pathCount, ref List<LogicVector3> wayPoints)
        {
            LogicVector3 startTgt = startPos;
            LogicVector3 moveTgt = endPos;

            int nsteerPath = 0;
            findStraightPath(startTgt, moveTgt, path, pathCount,
                                       ref m_SteerPath, ref m_SteerPathFlags, ref m_SteerPathPolys, ref nsteerPath, MAX_STEER_POINTS, 0);

            for (int i = 0; i < nsteerPath; i++)
            {
                wayPoints.Add(new LogicVector3(m_SteerPath[i * 3], m_SteerPath[i * 3 + 1], m_SteerPath[i * 3 + 2]));
            }

            return dtStatus.DT_SUCCESS;

        }

   
        public dtStatus findStraightPath(LogicVector3 startPos, LogicVector3 endPos, uint[] path, int pathSize,
                    ref int[] straightPath, ref byte[] straightPathFlags, ref uint[] straightPathRefs,
                    ref int straightPathCount, int maxStraightPath, int options)
        {
            straightPathCount = 0;

            dtStatus stat = 0;

            // TODO: Should this be callers responsibility?
            LogicVector3 closestStartPos = LogicVector3.zero;
            if (closestPointOnPolyBoundary(path[0], startPos, ref closestStartPos) != dtStatus.DT_SUCCESS)
                return dtStatus.DT_FAILURE;

            LogicVector3 closestEndPos = LogicVector3.zero;
            if (closestPointOnPolyBoundary(path[pathSize - 1], endPos, ref closestEndPos) != dtStatus.DT_SUCCESS)
                return dtStatus.DT_FAILURE;

            // Add start point.
            stat = appendVertex(closestStartPos, (byte)dtStraightPathFlags.DT_STRAIGHTPATH_START, path[0],
                                ref straightPath, ref straightPathFlags, ref straightPathRefs,
                                ref straightPathCount, maxStraightPath);
            if (stat != dtStatus.DT_IN_PROGRESS)
                return stat;

            if (pathSize > 1)
            {
                LogicVector3 portalApex = closestStartPos;
                LogicVector3 portalLeft = portalApex;
                LogicVector3 portalRight = portalApex;

                int apexIndex = 0;
                int leftIndex = 0;
                int rightIndex = 0;

                byte leftPolyType = 0;
                byte rightPolyType = 0;

                uint leftPolyRef = path[0];
                uint rightPolyRef = path[0];

                for (int i = 0; i < pathSize; ++i)
                {
                    LogicVector3 left = LogicVector3.zero;
                    LogicVector3 right = LogicVector3.zero;
                    byte toType = 0;

                    if (i + 1 < pathSize)
                    {
                        byte fromType = 0; // fromType is ignored.

                        // Next portal.
                        if (getPortalPoints(path[i], path[i + 1], ref left, ref  right, ref fromType, ref toType) != dtStatus.DT_SUCCESS)
                        {
                            // Failed to get portal points, in practice this means that path[i+1] is invalid polygon.
                            // Clamp the end point to path[i], and return the path so far.

                            if (closestPointOnPolyBoundary(path[i], endPos, ref closestEndPos) != dtStatus.DT_SUCCESS)
                            {
                                // This should only happen when the first polygon is invalid.
                                return dtStatus.DT_FAILURE;
                            }

                            // Apeend portals along the current straight path segment.
                            if ((options & (int)(dtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS |
                                dtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                            {
                                // Ignore status return value as we're just about to return anyway.
                                appendPortals(apexIndex, i, closestEndPos, path,
                                                     ref straightPath, ref straightPathFlags, ref straightPathRefs,
                                                     ref straightPathCount, maxStraightPath, options);
                            }

                            // Ignore status return value as we're just about to return anyway.
                            appendVertex(closestEndPos, 0, path[i],
                                                ref straightPath, ref straightPathFlags, ref straightPathRefs,
                                                ref straightPathCount, maxStraightPath);

                            return dtStatus.DT_SUCCESS;
                        }

                        // If starting really close the portal, advance.
                        if (i == 0)
                        {
                            int t = 0;
                            if (NavMeshMath.dtDistancePtSegSqr2D(portalApex, left, right, ref t) <= 0)
                                continue;
                        }
                    }
                    else
                    {
                        // End of the path.
                        left =  closestEndPos;
                        right =  closestEndPos;

                        toType = (byte)dtPolyTypes.DT_POLYTYPE_GROUND;
                    }

                    // Right vertex.
                    if (NavMeshMath.dtTriArea2D(portalApex, portalRight, right) <= 0)
                    {
                        if (NavMeshMath.dtVequal(portalApex, portalRight) || NavMeshMath.dtTriArea2D(portalApex, portalLeft, right) > 0)
                        {
                            portalRight =  right;
                            rightPolyRef = (i + 1 < pathSize) ? path[i + 1] : 0;
                            rightPolyType = toType;
                            rightIndex = i;
                        }
                        else
                        {
                            // Append portals along the current straight path segment.
                            if ((options & (int)(dtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS |
                                dtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                            {
                                stat = appendPortals(apexIndex, leftIndex, portalLeft, path,
                                                     ref straightPath, ref straightPathFlags, ref straightPathRefs,
                                                     ref straightPathCount, maxStraightPath, options);
                                if (stat != dtStatus.DT_IN_PROGRESS)
                                    return stat;
                            }

                            portalApex = portalLeft;
                            apexIndex = leftIndex;

                            byte flags = 0;
                            if (leftPolyRef == 0)
                                flags = (byte)dtStraightPathFlags.DT_STRAIGHTPATH_END;
                            else if (leftPolyType == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                                flags = (byte)dtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION;
                            uint _ref = leftPolyRef;

                            // Append or update vertex
                            stat = appendVertex(portalApex, flags, _ref,
                                                ref straightPath, ref straightPathFlags, ref straightPathRefs,
                                                ref straightPathCount, maxStraightPath);
                            if (stat != dtStatus.DT_IN_PROGRESS)
                                return stat;

                            portalLeft =  portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;

                            // Restart
                            i = apexIndex;

                            continue;
                        }
                    }

                    // Left vertex.
                    if (NavMeshMath.dtTriArea2D(portalApex, portalLeft, left) >= 0)
                    {
                        if (NavMeshMath.dtVequal(portalApex, portalLeft) ||
                            NavMeshMath.dtTriArea2D(portalApex, portalRight, left) <= 0)
                        {
                            portalLeft = left;
                            leftPolyRef = (i + 1 < pathSize) ? path[i + 1] : 0;
                            leftPolyType = toType;
                            leftIndex = i;
                        }
                        else
                        {
                            // Append portals along the current straight path segment.
                            if ((options & (int)(dtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS |
                                dtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                            {
                                stat = appendPortals(apexIndex, rightIndex, portalRight, path,
                                                     ref straightPath, ref straightPathFlags, ref straightPathRefs,
                                                     ref straightPathCount, maxStraightPath, options);
                                if (stat != dtStatus.DT_IN_PROGRESS)
                                    return stat;
                            }

                            portalApex =  portalRight;
                            apexIndex = rightIndex;

                            byte flags = 0;
                            if (rightPolyRef == 0)
                                flags = (byte)dtStraightPathFlags.DT_STRAIGHTPATH_END;
                            else if (rightPolyType == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                                flags = (byte)dtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION;
                            uint _ref = rightPolyRef;

                            // Append or update vertex
                            stat = appendVertex(portalApex, flags, _ref,
                                                ref straightPath, ref straightPathFlags, ref straightPathRefs,
                                                ref straightPathCount, maxStraightPath);
                            if (stat != dtStatus.DT_IN_PROGRESS)
                                return stat;

                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;

                            // Restart
                            i = apexIndex;

                            continue;
                        }
                    }
                }

                // Append portals along the current straight path segment.
                if ((options & (int)(dtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS |
                    dtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                {
                    stat = appendPortals(apexIndex, pathSize - 1, closestEndPos, path,
                                         ref straightPath, ref straightPathFlags, ref straightPathRefs,
                                         ref straightPathCount, maxStraightPath, options);
                    if (stat != dtStatus.DT_IN_PROGRESS)
                        return stat;
                }
            }

            // Ignore status return value as we're just about to return anyway.
            appendVertex(closestEndPos, (byte)dtStraightPathFlags.DT_STRAIGHTPATH_END, 0,
                               ref  straightPath, ref straightPathFlags, ref straightPathRefs,
                                ref straightPathCount, maxStraightPath);

            return dtStatus.DT_SUCCESS;
        }


        dtStatus appendVertex(LogicVector3 pos, byte flags, uint _ref,
                                      ref int[] straightPath, ref byte[] straightPathFlags, ref uint[] straightPathRefs,
                                      ref int straightPathCount, int maxStraightPath)
        {
            LogicVector3 temV = LogicVector3.zero;
            if (straightPathCount > 0)
            {
                temV.x = straightPath[(straightPathCount - 1) * 3];
                temV.y = straightPath[(straightPathCount - 1) * 3 + 1];
                temV.z = straightPath[(straightPathCount - 1) * 3 + 2];
            }

            if ((straightPathCount) > 0 && NavMeshMath.dtVequal(temV, pos))
            {
                // The vertices are equal, update flags and poly.
                straightPathFlags[straightPathCount - 1] = flags;
                straightPathRefs[straightPathCount - 1] = _ref;
            }
            else
            {
                straightPath[straightPathCount * 3] = pos.x;
                straightPath[straightPathCount * 3 + 1] = pos.y;
                straightPath[straightPathCount * 3 + 2] = pos.z;

                straightPathFlags[(straightPathCount)] = flags;
                straightPathRefs[(straightPathCount)] = _ref;

                straightPathCount++;

                // If there is no space to append more vertices, return.
                if ((straightPathCount) >= maxStraightPath)
                {
                    return dtStatus.DT_SUCCESS;
                }

                // If reached end of path, return.
                if (flags == (byte)dtStraightPathFlags.DT_STRAIGHTPATH_END)
                {
                    return dtStatus.DT_SUCCESS;
                }
            }
            return dtStatus.DT_IN_PROGRESS;
        }


        dtStatus appendPortals(int startIdx, int endIdx, LogicVector3 endPos, uint[] path,
                                  ref int[] straightPath, ref  byte[] straightPathFlags, ref uint[] straightPathRefs,
                                 ref  int straightPathCount, int maxStraightPath, int options)
        {
            LogicVector3 startPos;
            startPos.x = straightPath[(straightPathCount - 1) * 3];
            startPos.y = straightPath[(straightPathCount - 1) * 3 + 1];
            startPos.z = straightPath[(straightPathCount - 1) * 3 + 2];

            // Append or update last vertex
            dtStatus stat = 0;
            for (int i = startIdx; i < endIdx; i++)
            {
                // Calculate portal
                uint from = path[i];
                dtMeshTile fromTile = null;
                dtPoly fromPoly = null;
                if (m_NavMesh.getTileAndPolyByRef(from, ref fromTile, ref fromPoly) != dtStatus.DT_SUCCESS)
                    return dtStatus.DT_FAILURE;

                uint to = path[i + 1];
                dtMeshTile toTile = null;
                dtPoly toPoly = null;
                if (m_NavMesh.getTileAndPolyByRef(to, ref toTile, ref toPoly) != dtStatus.DT_SUCCESS)
                    return dtStatus.DT_FAILURE;

                LogicVector3 left = LogicVector3.zero;
                LogicVector3 right = LogicVector3.zero;
                if (getPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, ref left, ref right) != dtStatus.DT_SUCCESS)
                    break;

                if ((options & (int)dtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS) != 0)
                {
                    // Skip intersection if only area crossings are requested.
                    if (fromPoly.getArea() == toPoly.getArea())
                        continue;
                }

                // Append intersection
                int s = 0, t = 0;
                if (NavMeshMath.dtIntersectSegSeg2D(startPos, endPos, left, right, ref s, ref t))
                {
                    LogicVector3 pt;
                    pt.x = left.x + (int)((long)(right.x - left.x) * (long)t / MathUtils.iPointUnit);
                    pt.y = left.y + (int)((long)(right.y - left.y) * (long)t / MathUtils.iPointUnit);
                    pt.z = left.z + (int)((long)(right.z - left.z) * (long)t / MathUtils.iPointUnit);

                    stat = appendVertex(pt, 0, path[i + 1],
                                        ref straightPath, ref straightPathFlags, ref straightPathRefs,
                                        ref straightPathCount, maxStraightPath);
                    if (stat != dtStatus.DT_IN_PROGRESS)
                        return stat;
                }
            }
            return dtStatus.DT_IN_PROGRESS;
        }

        dtStatus getPortalPoints(uint from, uint to, ref LogicVector3 left, ref LogicVector3 right,
                                        ref byte fromType, ref byte toType)
        {
            dtMeshTile fromTile = null;
            dtPoly fromPoly = null;
            if (m_NavMesh.getTileAndPolyByRef(from, ref fromTile, ref fromPoly) != dtStatus.DT_SUCCESS)
                return dtStatus.DT_FAILURE;
            fromType = fromPoly.getType();

            dtMeshTile toTile = null;
            dtPoly toPoly = null;
            if (m_NavMesh.getTileAndPolyByRef(to, ref toTile, ref  toPoly) != dtStatus.DT_SUCCESS)
                return dtStatus.DT_FAILURE;
            toType = toPoly.getType();

            return getPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, ref left, ref right);
        }

        dtStatus getPortalPoints(uint from, dtPoly fromPoly, dtMeshTile fromTile,
                                         uint to, dtPoly toPoly, dtMeshTile toTile,
                                         ref LogicVector3 left, ref LogicVector3 right)
        {
            // Find the link that points to the 'to' polygon.
            dtLink link = null;
            for (uint i = fromPoly.firstLink; i != NavMeshBuilderDefine.DT_NULL_LINK; i = fromTile.links[i].next)
            {
                if (fromTile.links[i]._ref == to)
                {
                    link = fromTile.links[i];
                    break;
                }
            }
            if (link == null)
                return dtStatus.DT_FAILURE;

            // Handle off-mesh connections.
            if (fromPoly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                // Find link that points to first vertex.
                for (uint i = fromPoly.firstLink; i != NavMeshBuilderDefine.DT_NULL_LINK; i = fromTile.links[i].next)
                {
                    if (fromTile.links[i]._ref == to)
                    {
                        int v = fromTile.links[i].edge;
                        left.x = fromTile.verts[fromPoly.verts[v] * 3];
                        left.y = fromTile.verts[fromPoly.verts[v] * 3 + 1];
                        left.z = fromTile.verts[fromPoly.verts[v] * 3 + 2];
                        right.x = fromTile.verts[fromPoly.verts[v] * 3];
                        right.y = fromTile.verts[fromPoly.verts[v] * 3 + 1];
                        right.z = fromTile.verts[fromPoly.verts[v] * 3 + 2];
                        return dtStatus.DT_SUCCESS;
                    }
                }
                return dtStatus.DT_FAILURE;
            }

            if (toPoly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                for (uint i = toPoly.firstLink; i != NavMeshBuilderDefine.DT_NULL_LINK; i = toTile.links[i].next)
                {
                    if (toTile.links[i]._ref == from)
                    {
                        int v = toTile.links[i].edge;
                        left.x = toTile.verts[toPoly.verts[v] * 3];
                        left.y = toTile.verts[toPoly.verts[v] * 3 + 1];
                        left.z = toTile.verts[toPoly.verts[v] * 3 + 2];
                        right.x = toTile.verts[toPoly.verts[v] * 3];
                        right.y = toTile.verts[toPoly.verts[v] * 3 + 1];
                        right.z = toTile.verts[toPoly.verts[v] * 3 + 2];
                        return dtStatus.DT_SUCCESS;
                    }
                }
                return dtStatus.DT_FAILURE;
            }

            // Find portal vertices.
            int v0 = fromPoly.verts[link.edge];
            int v1 = fromPoly.verts[(link.edge + 1) % (int)fromPoly.vertCount];

            left.x = fromTile.verts[v0 * 3];
            left.y = fromTile.verts[v0 * 3 + 1];
            left.z = fromTile.verts[v0 * 3 + 2];
            right.x = fromTile.verts[v1 * 3];
            right.y = fromTile.verts[v1 * 3 + 1];
            right.z = fromTile.verts[v1 * 3 + 2];


            // If the link is at tile boundary, dtClamp the vertices to
            // the link width.
            if (link.side != 0xff)
            {
                // Unpack portal limits.
                if (link.bmin != 0 || link.bmax != 255)
                {
                    left.x = fromTile.verts[v0 * 3] + (int)((long)(fromTile.verts[v1 * 3] - fromTile.verts[v0 * 3]) * (long)link.bmin / 255);
                    left.y = fromTile.verts[v0 * 3 + 1] + (int)((long)(fromTile.verts[v1 * 3 + 1] - fromTile.verts[v0 * 3 + 1]) * (long)link.bmin / 255);
                    left.z = fromTile.verts[v0 * 3 + 2] + (int)((long)(fromTile.verts[v1 * 3 + 2] - fromTile.verts[v0 * 3 + 2]) * (long)link.bmin / 255);

                    right.x = fromTile.verts[v0 * 3] + (int)((long)(fromTile.verts[v1 * 3] - fromTile.verts[v0 * 3]) * (long)link.bmax / 255);
                    right.y = fromTile.verts[v0 * 3 + 1] + (int)((long)(fromTile.verts[v1 * 3 + 1] - fromTile.verts[v0 * 3 + 1]) * (long)link.bmax / 255);
                    right.z = fromTile.verts[v0 * 3 + 2] + (int)((long)(fromTile.verts[v1 * 3 + 2] - fromTile.verts[v0 * 3 + 2]) * (long)link.bmax / 255);

                }
            }

            return dtStatus.DT_SUCCESS;
        }

#endregion


        private int[] m_RayCastVerts = new int[NavMeshBuilderDefine.DT_VERTS_PER_POLYGON * 3 + 3];	

        public dtStatus raycast(uint startRef, LogicVector3 startPos, LogicVector3 endPos, uint options, 
            ref dtRaycastHit hit, ref uint prevRef) 
        {
	        hit.t = 0;
	        hit.pathCount = 0;
	        hit.pathCost = 0;

	        // Validate input
	        if (startRef == 0 || !m_NavMesh.isValidPolyRef(startRef))
		        return dtStatus.DT_FAILURE ;
	        if (prevRef !=0 && !m_NavMesh.isValidPolyRef(prevRef))
		        return dtStatus.DT_FAILURE ;

            LogicVector3 dir;
            LogicVector3 curPos;
            LogicVector3 lastPos;

	       
	        int n = 0;
         
            curPos = startPos;
            dir = endPos - startPos;

	        hit.hitNormal = LogicVector3.zero;
            hit.hitPos = startPos;

	        dtStatus status = dtStatus.DT_SUCCESS;

	        dtMeshTile prevTile = null, tile = null, nextTile = null;
	         dtPoly prevPoly = null, poly = null, nextPoly = null;
	        uint curRef;

	        // The API input has been checked already, skip checking internal data.
	        curRef = startRef;
	        m_NavMesh.getTileAndPolyByRef(curRef, ref tile, ref poly);
	        nextTile = prevTile = tile;
	        nextPoly = prevPoly = poly;
	        if (prevRef != 0)
		        m_NavMesh.getTileAndPolyByRef(prevRef, ref prevTile, ref prevPoly);

	        while (curRef != 0)
	        {
		        // Cast ray against current polygon.
		
		        // Collect vertices.
		        int nv = 0;
		        for (int i = 0; i < (int)poly.vertCount; ++i)
		        {
                    m_RayCastVerts[nv * 3] = tile.verts[poly.verts[i] * 3];
                    m_RayCastVerts[nv * 3 + 1] = tile.verts[poly.verts[i] * 3 + 1];
                    m_RayCastVerts[nv * 3 + 2] = tile.verts[poly.verts[i] * 3 + 2];

			        nv++;
		        }
		
		        int tmin = 0, tmax = 0;
		        int segMin = 0, segMax = 0;
                if (!NavMeshMath.dtIntersectSegmentPoly2D(startPos, endPos, m_RayCastVerts, nv, ref tmin, ref tmax, 
                    ref segMin, ref segMax))
		        {
			        // Could not hit the polygon, keep the old t and report hit.
			        hit.pathCount = n;
			        return status;
		        }

		        hit.hitEdgeIndex = segMax;

		        // Keep track of furthest t so far.
		        if (tmax > hit.t)
			        hit.t = tmax;
		
		        // Store visited polygons.
		        if (n < hit.maxPath)
			        hit.path[n++] = curRef;
		        else
			        status |= dtStatus.DT_BUFFER_TOO_SMALL;

		        // Ray end is completely inside the polygon.
		        if (segMax == -1)
		        {
			        hit.t = int.MaxValue;
                    hit.hitPos = endPos;
			        hit.pathCount = n;
			
			        // add the cost
			        if ((options & (uint)dtRaycastOptions.DT_RAYCAST_USE_COSTS) != 0)
                    {
                        hit.pathCost += NavMeshMath.Distance2D(curPos, endPos);
                    }
			        return status;
		        }

		        // Follow neighbours.
		        uint nextRef = 0;

                for (uint i = poly.firstLink; i != NavMeshBuilderDefine.DT_NULL_LINK; i = tile.links[i].next)
		        {
			         dtLink link = tile.links[i];
			
			        // Find link which contains this edge.
			        if ((int)link.edge != segMax)
				        continue;
			
			        // Get pointer to the next polygon.
			        nextTile = null;
			        nextPoly = null;
			        m_NavMesh.getTileAndPolyByRef(link._ref, ref nextTile, ref nextPoly);
			
			        // Skip off-mesh connections.
			        if (nextPoly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
				        continue;
			
			        // Skip links based on filter.
			      //  if (!filter.passFilter(link.ref, nextTile, nextPoly))
				  //      continue;
			
			        // If the link is internal, just return the ref.
			        if (link.side == 0xff)
			        {
				        nextRef = link._ref;
				        break;
			        }
			
			        // If the link is at tile boundary,
			
			        // Check if the link spans the whole edge, and accept.
			        if (link.bmin == 0 && link.bmax == 255)
			        {
				        nextRef = link._ref;
				        break;
			        }
			
			        // Check for partial edge links.
			        int v0 = poly.verts[link.edge];
			        int v1 = poly.verts[(link.edge+1) % poly.vertCount];
			      //  float* left = &tile.verts[v0*3];
			      //  float* right = &tile.verts[v1*3];
			
			        // Check that the intersection lies inside the link portal.
			        if (link.side == 0 || link.side == 4)
			        {
				        // Calculate link size.
                        int lmin = tile.verts[v0 * 3+2] + (tile.verts[v1 * 3+2] - tile.verts[v0 * 3+2]) * link.bmin /255;
                        int lmax = tile.verts[v0 * 3 + 2] + (tile.verts[v1 * 3 + 2] - tile.verts[v0 * 3 + 2]) * link.bmax /255;
				        if (lmin > lmax)
                            NavMeshMath.Swap(ref lmin, ref lmax);
				
				        // Find Z intersection.
				        int z = startPos.z + (int)((long)(endPos.z-startPos.z)*(long)tmax / MathUtils.iPointUnit);
				        if (z >= lmin && z <= lmax)
				        {
					        nextRef = link._ref;
					        break;
				        }
			        }
			        else if (link.side == 2 || link.side == 6)
			        {
				        // Calculate link size.
                        int lmin = tile.verts[v0 * 3+0] + (tile.verts[v1 * 3+0] - tile.verts[v0 * 3+0]) * link.bmin /255;
                        int lmax = tile.verts[v0 * 3+0] + (tile.verts[v1 * 3+0] - tile.verts[v0 * 3+0]) * link.bmax/255;
				        if (lmin > lmax)
                           NavMeshMath.Swap(ref lmin, ref lmax);
				
				        // Find X intersection.
				        int x = startPos.x + (int)((long)(endPos.x-startPos.x)*(long)tmax / MathUtils.iPointUnit);
				        if (x >= lmin && x <= lmax)
				        {
					        nextRef = link._ref;
					        break;
				        }
			        }
		        }
		
		        // add the cost
		        if ((options & (uint)dtRaycastOptions.DT_RAYCAST_USE_COSTS) != 0)
		        {
			        // compute the intersection point at the furthest end of the polygon
			        // and correct the height (since the raycast moves in 2d)
			        lastPos =  curPos;
			        NavMeshMath.Vmad(ref curPos, startPos, dir, hit.t);
                    LogicVector3 e1;
                    LogicVector3 e2;
                    e1.x = m_RayCastVerts[segMax * 3];
                    e1.y = m_RayCastVerts[segMax * 3 + 1];
                    e1.z = m_RayCastVerts[segMax * 3 + 2];

                    e2.x = m_RayCastVerts[((segMax + 1) % nv) * 3];
                    e2.y = m_RayCastVerts[((segMax + 1) % nv) * 3 + 1];
                    e2.z = m_RayCastVerts[((segMax + 1) % nv) * 3 + 2];

                    LogicVector3 eDir = e2 - e1;
                    LogicVector3 diff = curPos - e1;

                    long s = (long)eDir.x * (long)eDir.x > (long)eDir.z * (long)eDir.z ?
                        (long)diff.x * MathUtils.lPointUnit / eDir.x : (long)diff.z * MathUtils.lPointUnit / eDir.z;
			        curPos.y = e1.y + (int)((long)eDir.y * s / MathUtils.iPointUnit);

                    hit.pathCost += NavMeshMath.Distance2D(lastPos, curPos);
                }

		        if (nextRef == 0)
		        {
			        // No neighbour, we hit a wall.
			
			        // Calculate hit normal.
			        int a = segMax;
			        int b = segMax+1 < nv ? segMax+1 : 0;
                    int dx = m_RayCastVerts[b * 3 + 0] - m_RayCastVerts[a * 3 + 0];
                    int dz = m_RayCastVerts[b * 3 + 2] - m_RayCastVerts[a * 3 + 2];
			        //hit.hitNormal.x = dz;
			        //hit.hitNormal.y = 0;
			        //hit.hitNormal.z = -dx;
                    hit.hitNormal.x = dx;
                    hit.hitNormal.y = 0;
                    hit.hitNormal.z = dz;
                    hit.hitNormal.NormalizeD4();

                    hit.hitPos.x = startPos.x + (int)((long)(endPos.x - startPos.x) * (long)hit.t / MathUtils.iPointUnit);
                    hit.hitPos.y = startPos.y + (int)((long)(endPos.y - startPos.y) * (long)hit.t / MathUtils.iPointUnit);
                    hit.hitPos.z = startPos.z + (int)((long)(endPos.z - startPos.z) * (long)hit.t / MathUtils.iPointUnit);

			        hit.pathCount = n;
			        return status;
		        }

		        // No hit, advance to neighbour polygon.
		        prevRef = curRef;
		        curRef = nextRef;
		        prevTile = tile;
		        tile = nextTile;
		        prevPoly = poly;
		        poly = nextPoly;
	        }
	
	        hit.pathCount = n;
	
	        return status;
        }




    }
}
