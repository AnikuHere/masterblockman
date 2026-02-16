using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOBA
{
    public class dtNavMesh
    {	
        private dtNavMeshParams m_params;
        private int[] m_orig = new int[3];					
        private int m_tileWidth, m_tileHeight;	
        private int m_maxTiles;						
        private int m_tileLutSize;				
        private int m_tileLutMask;					

        private dtMeshTile[] m_posLookup;			
        private dtMeshTile m_nextFree;				
	    private dtMeshTile[] m_tiles;			

        private uint m_saltBits;			
        private uint m_tileBits;		
        private uint m_polyBits;

        private const int NEIS_MAX_NEIS = 32;
        private dtMeshTile[] m_AddTileNeis = null;

        public dtStatus OnInit( dtNavMeshParams _params)
        {
	        m_params = _params;
            for (int i = 0; i < 3; i++ )
            {
                m_orig[i] = _params.orig[i];
            }
             
            m_tileWidth = _params.tileWidth;
	        m_tileHeight = _params.tileHeight;
	
	        // Init tiles
	        m_maxTiles = _params.maxTiles;
            m_tileLutSize = NavMeshMath.NextPow2(_params.maxTiles / 4);
	        if (m_tileLutSize == 0) m_tileLutSize = 1;
	        m_tileLutMask = m_tileLutSize-1;

            m_tiles = new dtMeshTile[m_maxTiles];
            for (int i = 0; i < m_maxTiles; i++ )
            {
                m_tiles[i] = new dtMeshTile();
            }

            m_posLookup = new dtMeshTile[m_tileLutSize];

	        m_nextFree = null;
	        for (int i = m_maxTiles-1; i >= 0; --i)
	        {
		        m_tiles[i].salt = 1;
		        m_tiles[i].next = m_nextFree;
		        m_nextFree = m_tiles[i];
	        }

            m_AddTileNeis = new dtMeshTile[NEIS_MAX_NEIS];

            m_tileBits = (uint)(NavMeshMath.Ilog2(NavMeshMath.NextPow2((int)_params.maxTiles)));
            m_polyBits = (uint)(NavMeshMath.Ilog2(NavMeshMath.NextPow2((int)_params.maxPolys)));
            m_saltBits = (uint)(NavMeshMath.Min(31, (int)(32 - m_tileBits - m_polyBits)));

	         if (m_saltBits < 10)
		        return dtStatus.DT_FAILURE;

            return dtStatus.DT_SUCCESS;
        }

        public void OnUnInit()
        {
            m_params = null;
            m_orig = null;

            for (int i = 0; i < m_posLookup.Length; i++)
            {
                m_posLookup[i] = null;
            }
            m_posLookup = null;

            m_nextFree = null;

            for (int i = 0; i < m_tiles.Length; i++)
            {
                m_tiles[i] =null;
            }
            m_tiles = null;

            for (int i = 0; i < m_AddTileNeis.Length; i++)
            {
                m_AddTileNeis[i] = null;
            }
            m_AddTileNeis = null;
        }

        public dtStatus addTile(dtNavMeshCreateParams _params, int flags, int lastRef, ref uint result)
        {

            // Make sure the location is free.
            //  if (getTileAt(header.x, header.y, header.layer))
            //        return DT_FAILURE;

            // Allocate a tile.
            dtMeshTile tile = null;

            if (lastRef == 0)
            {
                if (m_nextFree != null)
                {
                    tile = m_nextFree;
                    m_nextFree = tile.next;
                    tile.next = null;
                }
            }

            GlobalMembersDetourNavMeshBuilder.dtCreateNavMeshData(_params, ref tile);

            dtMeshHeader header = tile.header;

            // Insert tile into the position lut.
            int h = computeTileHash(header.x, header.y, m_tileLutMask);
            tile.next = m_posLookup[h];
            m_posLookup[h] = tile;


            // Build links freelist
            tile.linksFreeList = 0;
            tile.links[header.maxLinkCount - 1].next = NavMeshBuilderDefine.DT_NULL_LINK;
            for (int i = 0; i < header.maxLinkCount - 1; ++i)
                tile.links[i].next = (uint)(i + 1);

            // Init tile.
            tile.flags = flags;

            connectIntLinks(tile);

            // Base off-mesh connections to their starting polygons and connect connections inside the tile.
            //  baseOffMeshLinks(tile);
            //   connectExtOffMeshLinks(tile, tile, -1);

            // Create connections with neighbour tiles.
           
         
            int nneis = 0;

            dtMeshTile[] neis = m_AddTileNeis;
            for (int i = 0; i < neis.Length; i++)
                neis[i] = null;

                // Connect with layers in current tile.
            nneis = getTilesAt(header.x, header.y, ref neis, NEIS_MAX_NEIS);
            for (int j = 0; j < nneis; ++j)
            {
                if (neis[j] == tile)
                    continue;

                connectExtLinks(tile, neis[j], -1);
                connectExtLinks(neis[j], tile, -1);
                //    connectExtOffMeshLinks(tile, neis[j], -1);
                //    connectExtOffMeshLinks(neis[j], tile, -1);
            }

            // Connect with neighbour tiles.
            for (int i = 0; i < 8; ++i)
            {
                nneis = getNeighbourTilesAt(header.x, header.y, i, ref neis, NEIS_MAX_NEIS);
                for (int j = 0; j < nneis; ++j)
                {
                    connectExtLinks(tile, neis[j], i);
                    connectExtLinks(neis[j], tile, dtOppositeTile(i));
                    // connectExtOffMeshLinks(tile, neis[j], i);
                    //  connectExtOffMeshLinks(neis[j], tile, dtOppositeTile(i));
                }
            }


            result = getTileRef(tile);

            return dtStatus.DT_SUCCESS;
        }


        public dtStatus removeTile(uint _ref)
        {
            if (_ref == 0)
                return dtStatus.DT_FAILURE;
            uint tileIndex = decodePolyIdTile(_ref);
            uint tileSalt = decodePolyIdSalt(_ref);
            if ((int)tileIndex >= m_maxTiles)
                return dtStatus.DT_FAILURE;
            dtMeshTile tile = m_tiles[tileIndex];
            if (tile.salt != tileSalt)
                return dtStatus.DT_FAILURE;

            // Remove tile from hash lookup.
            int h = computeTileHash(tile.header.x, tile.header.y, m_tileLutMask);
            dtMeshTile prev = null;
            dtMeshTile cur = m_posLookup[h];
            while (cur != null)
            {
                if (cur == tile)
                {
                    if (prev != null)
                        prev.next = cur.next;
                    else
                        m_posLookup[h] = cur.next;
                    break;
                }
                prev = cur;
                cur = cur.next;
            }

            // Remove connections to neighbour tiles.
            const int MAX_NEIS = 32;
            dtMeshTile[] neis = new dtMeshTile[MAX_NEIS];
            int nneis;

            // Disconnect from other layers in current tile.
            nneis = getTilesAt(tile.header.x, tile.header.y, ref neis, MAX_NEIS);
            for (int j = 0; j < nneis; ++j)
            {
                if (neis[j] == tile) continue;
                unconnectLinks(neis[j], tile);
            }

            // Disconnect from neighbour tiles.
            for (int i = 0; i < 8; ++i)
            {
                nneis = getNeighbourTilesAt(tile.header.x, tile.header.y, i, ref neis, MAX_NEIS);
                for (int j = 0; j < nneis; ++j)
                    unconnectLinks(neis[j], tile);
            }

            tile.header = null;
            tile.flags = 0;
            tile.linksFreeList = 0;
            tile.polys = null;
            tile.verts = null;
            tile.links = null;
            //tile.detailMeshes = null;
          //  tile.detailVerts = null;
           // tile.detailTris = null;
            //  tile.bvTree = null;
            //   tile.offMeshCons = 0;

            // Update salt, salt should never be zero.

            tile.salt = (uint)((tile.salt + 1) & ((1 << (int)m_saltBits) - 1));

            if (tile.salt == 0)
                tile.salt++;

            // Add to free list.
            tile.next = m_nextFree;
            m_nextFree = tile;

            return dtStatus.DT_SUCCESS;
        }


        void connectIntLinks(dtMeshTile tile)
        {
            if (tile == null) return;

            uint _base = getPolyRefBase(tile);

            for (int i = 0; i < tile.header.polyCount; ++i)
            {
                dtPoly poly = tile.polys[i];
                poly.firstLink = NavMeshBuilderDefine.DT_NULL_LINK;

                if (poly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                    continue;

                // Build edge links backwards so that the links will be
                // in the linked list from lowest index to highest.
                for (int j = poly.vertCount - 1; j >= 0; --j)
                {
                    // Skip hard and non-internal edges.
                    if (poly.neis[j] == 0 || (poly.neis[j] & NavMeshBuilderDefine.DT_EXT_LINK) != 0) continue;

                    uint idx = allocLink(tile);
                    if (idx != NavMeshBuilderDefine.DT_NULL_LINK)
                    {
                        dtLink link = tile.links[idx];
                        link._ref = (uint)(_base | (poly.neis[j] - 1));
                        link.edge = (byte)j;
                        link.side = 0xff;
                        link.bmin = link.bmax = 0;
                        // Add to linked list.
                        link.next = poly.firstLink;
                        poly.firstLink = idx;
                    }
                }
            }
        }

        void connectExtLinks(dtMeshTile tile, dtMeshTile target, int side)
        {

            if (tile == null) return;

            // Connect border links.
            for (int i = 0; i < tile.header.polyCount; ++i)
            {
                dtPoly poly = tile.polys[i];

                int nv = poly.vertCount;
                for (int j = 0; j < nv; ++j)
                {
                    // Skip non-portal edges.
                    if ((poly.neis[j] & NavMeshBuilderDefine.DT_EXT_LINK) == 0)
                        continue;

                    int dir = (int)(poly.neis[j] & 0xff);
                    if (side != -1 && dir != side)
                        continue;

                    // Create new links
                    int va = poly.verts[j] * 3;
                    int vb = poly.verts[(j + 1) % nv] * 3;
                    uint[] nei = new uint[4];
                    int[] neia = new int[4 * 2];
                    int nnei = findConnectingPolys(tile.verts, va, tile.verts, vb, target, dtOppositeTile(dir), ref nei, neia, 4);

                    for (int k = 0; k < nnei; ++k)
                    {
                        uint idx = allocLink(tile);
                        if (idx != NavMeshBuilderDefine.DT_NULL_LINK)
                        {
                            dtLink link = tile.links[idx];
                            link._ref = nei[k];
                            link.edge = (byte)j;
                            link.side = (byte)dir;

                            link.next = poly.firstLink;
                            poly.firstLink = idx;

                            // Compress portal limits to a byte value.
                            if (dir == 0 || dir == 4)
                            {
                                int tmin = (neia[k * 2 + 0] - tile.verts[va + 2]) * MathUtils.iPointUnit/ (tile.verts[vb + 2] - tile.verts[va + 2]);
                                int tmax = (neia[k * 2 + 1] - tile.verts[va + 2]) * MathUtils.iPointUnit / (tile.verts[vb + 2] - tile.verts[va + 2]);
                                if (tmin > tmax)
                                    NavMeshMath.Swap(ref tmin, ref tmax);

                                link.bmin = (byte)(NavMeshMath.Clamp(tmin, 0, MathUtils.iPointUnit) * 255 / MathUtils.iPointUnit);
                                link.bmax = (byte)(NavMeshMath.Clamp(tmax, 0, MathUtils.iPointUnit) * 255 / MathUtils.iPointUnit);
                            }
                            else if (dir == 2 || dir == 6)
                            {
                                int tmin = (neia[k * 2 + 0] - tile.verts[va + 0]) * MathUtils.iPointUnit / (tile.verts[vb + 0] - tile.verts[va + 0]);
                                int tmax = (neia[k * 2 + 1] - tile.verts[va + 0]) * MathUtils.iPointUnit / (tile.verts[vb + 0] - tile.verts[va + 0]);
                                if (tmin > tmax)
                                    NavMeshMath.Swap(ref tmin, ref tmax);

                                link.bmin = (byte)(NavMeshMath.Clamp(tmin, 0, MathUtils.iPointUnit) * 255 / MathUtils.iPointUnit);
                                link.bmax = (byte)(NavMeshMath.Clamp(tmax, 0, MathUtils.iPointUnit) * 255 / MathUtils.iPointUnit);
                            }
                        }
                    }
                }

            }
        }

        void unconnectLinks(dtMeshTile tile, dtMeshTile target)
        {
            if (tile == null || target == null) return;

            uint targetNum = decodePolyIdTile(getTileRef(target));

            for (int i = 0; i < tile.header.polyCount; ++i)
            {
                dtPoly poly = tile.polys[i];
                uint j = poly.firstLink;
                uint pj = NavMeshBuilderDefine.DT_NULL_LINK;
                while (j != NavMeshBuilderDefine.DT_NULL_LINK)
                {
                    if (decodePolyIdTile(tile.links[j]._ref) == targetNum)
                    {
                        // Remove link.
                        uint nj = tile.links[j].next;
                        if (pj == NavMeshBuilderDefine.DT_NULL_LINK)
                            poly.firstLink = nj;
                        else
                            tile.links[pj].next = nj;
                        freeLink(tile, j);
                        j = nj;
                    }
                    else
                    {
                        // Advance
                        pj = j;
                        j = tile.links[j].next;
                    }
                }
            }
        }

        void freeLink(dtMeshTile tile, uint link)
        {
            tile.links[link].next = tile.linksFreeList;
            tile.linksFreeList = link;
        }

         int computeTileHash(int x, int y,  int mask)
        {
	         uint h1 = 0x8da6b343; // Large multiplicative constants;
	          uint h2 = 0xd8163841; // here arbitrarily chosen primes
	         long n = h1 * x + h2 * y;
	        return (int)(n & mask);
        }

         public  int getTileCount()  
         { 
             return m_params.maxTiles; 
         }

         public dtMeshTile getTile(int i) 
          {
              return m_tiles[i]; 
          }

        public dtMeshTile getTileAt( int x,  int y,  int layer) 
        {
	        // Find tile based on hash.
	        int h = computeTileHash(x,y,m_tileLutMask);
	        dtMeshTile tile = m_posLookup[h];
	        while (tile != null)
	        {
		        if (tile.header != null &&
			        tile.header.x == x &&
			        tile.header.y == y &&
			        tile.header.layer == layer)
		        {
			        return tile;
		        }
		        tile = tile.next;
	        }
	        return null;
        }

        public uint getTileRefAt( int x,  int y,  int layer) 
        {
	        // Find tile based on hash.
	        int h = computeTileHash(x,y,m_tileLutMask);
	        dtMeshTile tile = m_posLookup[h];
	        while (tile != null)
	        {
		        if (tile.header != null &&
			        tile.header.x == x &&
			        tile.header.y == y &&
			        tile.header.layer == layer)
		        {
			        return getTileRef(tile);
		        }
		        tile = tile.next;
	        }
	        return 0;
        }

        public int getTilesAt( int x,  int y, ref dtMeshTile[] tiles,  int maxTiles) 
        {
	        int n = 0;
	
	        // Find tile based on hash.
	        int h = computeTileHash(x,y,m_tileLutMask);
	        dtMeshTile tile = m_posLookup[h];
	        while (tile != null) 
	        {
		        if (tile.header != null &&
			        tile.header.x == x &&
			        tile.header.y == y)
		        {
			        if (n < maxTiles)
				        tiles[n++] = tile;
		        }
		        tile = tile.next;
	        }
	
	        return n;
        }

        
        int getNeighbourTilesAt( int x,  int y,  int side, ref dtMeshTile[] tiles,  int maxTiles) 
        {
	        int nx = x, ny = y;
	        switch (side)
	        {
		        case 0: nx++; break;
		        case 1: nx++; ny++; break;
		        case 2: ny++; break;
		        case 3: nx--; ny++; break;
		        case 4: nx--; break;
		        case 5: nx--; ny--; break;
		        case 6: ny--; break;
		        case 7: nx++; ny--; break;
	        };

	        return getTilesAt(nx, ny, ref tiles, maxTiles);
        }


        public dtStatus getTileAndPolyByRef(uint _ref, ref dtMeshTile tile, ref dtPoly poly)
        {
	         uint salt = 0, it = 0, ip = 0;
	        decodePolyId(_ref, ref salt, ref it, ref ip);

            if (it >= m_maxTiles) 
                return dtStatus.DT_FAILURE;
            if (m_tiles[it].salt != salt || m_tiles[it].header == null) 
                return dtStatus.DT_FAILURE;
            if (ip >= m_tiles[it].header.polyCount) 
                    return dtStatus.DT_FAILURE;

            tile = m_tiles[it];
	        poly = m_tiles[it].polys[ip];
        
            return dtStatus.DT_SUCCESS;
        }

        uint encodePolyId(uint salt,  uint it,  uint ip) 
	    {
		    return (uint) (((int)salt << (int)(m_polyBits+m_tileBits)) | ((int)it << (int)m_polyBits) | (int)ip);
	    }

        uint decodePolyIdTile(uint _ref) 
	    {
		    uint tileMask = (uint)( (1<<(int)m_tileBits)-1);
		    return ( uint)((_ref >> (int)m_polyBits) & tileMask);
	    }

        void decodePolyId(uint _ref,  ref uint  salt, ref uint it,  ref  uint ip)
	    {
            uint saltMask = (uint)( (1 << (int)m_saltBits) - 1);
            uint tileMask = (uint)((1 << (int)m_tileBits) - 1);
            uint polyMask = (uint)((1 << (int)m_polyBits) - 1);
            salt = (uint)((_ref >> (int)(m_polyBits + m_tileBits)) & saltMask);
            it = (uint)((_ref >> (int)m_polyBits) & tileMask);
		    ip = (uint)(_ref & polyMask);
	    }
        
        uint getTileRef( dtMeshTile tile) 
        {
	        if (tile == null) return 0;

             uint it = 0;
            for(int i=0; i < m_tiles.Length; i++)
            {
                if(ReferenceEquals(m_tiles[i], tile ))
                {
                    it = (uint)i;
                    break;
                }
            }

	        return encodePolyId(tile.salt, it, (uint)0);
        }

          uint decodePolyIdSalt(uint _ref) 
	        {
		         uint saltMask = (uint)((1<<(int)m_saltBits)-1);
		        return ( uint)(((int)_ref >> (int)(m_polyBits+m_tileBits)) & saltMask);
	        }


        public uint getPolyRefBase(dtMeshTile tile) 
        {
	        if (tile == null) return 0;
	       uint it = 0;
            for(int i=0; i < m_tiles.Length; i++)
            {
                if(ReferenceEquals(m_tiles[i], tile ))
                {
                    it = (uint)i;
                    break;
                }
            }

	        return encodePolyId(tile.salt, it, (uint)0);
        }

         int dtOppositeTile(int side) { return (side+4) & 0x7; }

        uint allocLink(dtMeshTile tile)
        {
            if (tile.linksFreeList == NavMeshBuilderDefine.DT_NULL_LINK)
                return NavMeshBuilderDefine.DT_NULL_LINK;
	        uint link = tile.linksFreeList;
	        tile.linksFreeList = tile.links[link].next;
	        return link;
        }

        int findConnectingPolys(int[] va, int va_start, int[] vb, int vb_start,
            dtMeshTile tile, int side, ref uint[] con, int[] conarea, int maxcon) 
        {
	        if (tile == null) return 0;

            int[] amin = new int[2];
            int[] amax = new int[2];
            calcSlabEndPoints(va, va_start, vb, vb_start, ref amin, ref amax, side);
            int apos = getSlabCoord(va, va_start, side);

	        // Remove links pointing to 'side' and compact the links array. 
            int[] bmin = new int[2];
            int[] bmax = new int[2];
            ushort m = (ushort)(NavMeshBuilderDefine.DT_EXT_LINK | (ushort)side);
	        int n = 0;
	
	        uint _base = getPolyRefBase(tile);
	
	        for (int i = 0; i < tile.header.polyCount; ++i)
	        {
		        dtPoly poly = tile.polys[i];
		        int nv = poly.vertCount;
		        for (int j = 0; j < nv; ++j)
		        {
			        // Skip edges which do not point to the right side.
			        if (poly.neis[j] != m) continue;
			
			         int vc = poly.verts[j]*3;
			         int vd = poly.verts[(j+1) % nv]*3;
                     int bpos = getSlabCoord(tile.verts, vc, side);
			
			        // Segments are not close enough.
                     if (NavMeshMath.Abs(apos - bpos) > 100)
				        continue;
			
			        // Check if the segments touch.
                     calcSlabEndPoints(tile.verts, vc, tile.verts, vd, ref bmin, ref bmax, side);
			
			        if (!overlapSlabs(amin,amax, bmin,bmax, 100, tile.header.walkableClimb)) continue;
			
			        // Add return value.
			        if (n < maxcon)
			        {
                        conarea[n * 2 + 0] = NavMeshMath.Max(amin[0], bmin[0]);
                        conarea[n * 2 + 1] = NavMeshMath.Min(amax[0], bmax[0]);
				        con[n] = (uint)((int)_base | (int)i);
				        n++;
			        }
			        break;
		        }
	        }
	        return n;
        }


        bool overlapSlabs(int[] amin, int[] amax, int[] bmin, int[] bmax, int px, int py)
        {
	        // Check for horizontal overlap.
	        // The segment is shrunken a little so that slabs which touch
	        // at end points are not connected.
            int minx = NavMeshMath.Max(amin[0] + px, bmin[0] + px);
            int maxx = NavMeshMath.Min(amax[0] - px, bmax[0] - px);
	        if (minx > maxx)
		        return false;
	
	        // Check vertical overlap.
            int ad = (amax[1] - amin[1]) * MathUtils.iPointUnit / (amax[0] - amin[0]);
            int ak = amin[1] - ad * amin[0] / MathUtils.iPointUnit;
            int bd = (bmax[1] - bmin[1]) * MathUtils.iPointUnit / (bmax[0] - bmin[0]);
            int bk = bmin[1] - bd * bmin[0] / MathUtils.iPointUnit;
            int aminy = ad * minx / MathUtils.iPointUnit + ak;
            int amaxy = ad * maxx / MathUtils.iPointUnit + ak;
            int bminy = bd * minx / MathUtils.iPointUnit + bk;
            int bmaxy = bd * maxx / MathUtils.iPointUnit + bk;
            int dmin = bminy - aminy;
            int dmax = bmaxy - amaxy;
		
	        // Crossing segments always overlap.
	        if (dmin*dmax < 0)
		        return true;
		
	        // Check for overlap at endpoints.
            long thr = (long)(py * 2) * (long)(py * 2) / MathUtils.iPointUnit; 
	        if (dmin*dmin <= thr || dmax*dmax <= thr)
		        return true;
		
	        return false;
        }

        static int getSlabCoord(int[] va, int va_start, int side)
        {
	        if (side == 0 || side == 4)
                return va[va_start + 0];
	        else if (side == 2 || side == 6)
                return va[va_start + 2];
	        return 0;
        }

        static void calcSlabEndPoints(int[] va, int va_start, int[] vb, int vb_start, ref int[] bmin, ref int[] bmax, int side)
        {
	        if (side == 0 || side == 4)
	        {
                if (va[va_start + 2] < vb[vb_start + 2])
		        {
                    bmin[0] = va[va_start + 2];
                    bmin[1] = va[va_start + 1];
                    bmax[0] = vb[vb_start + 2];
                    bmax[1] = vb[vb_start + 1];
		        }
		        else
		        {
                    bmin[0] = vb[vb_start + 2];
                    bmin[1] = vb[vb_start + 1];
                    bmax[0] = va[va_start + 2];
                    bmax[1] = va[va_start + 1];
		        }
	        }
	        else if (side == 2 || side == 6)
	        {
                if (va[va_start + 0] < vb[vb_start + 0])
		        {
                    bmin[0] = va[va_start + 0];
                    bmin[1] = va[va_start + 1];
                    bmax[0] = vb[vb_start + 0];
                    bmax[1] = vb[vb_start + 1];
		        }
		        else
		        {
                    bmin[0] = vb[vb_start + 0];
                    bmin[1] = vb[vb_start + 1];
                    bmax[0] = va[va_start + 0];
                    bmax[1] = va[va_start + 1];
		        }
	        }
        }


        public void calcTileLoc(LogicVector3 pos, ref int tx, ref int ty) 
        {
            tx = (pos.x-m_orig[0]) / m_tileWidth;
            ty = (pos.z-m_orig[2]) / m_tileHeight;
        }

        public bool isValidPolyRef(uint _ref) 
        {
	        if (_ref == 0) 
                return false;

	         uint salt = 0, it = 0, ip = 0;
	        decodePolyId(_ref, ref salt, ref it, ref ip);
	        if (it >= m_maxTiles) 
                return false;
	        if (m_tiles[it].salt != salt || m_tiles[it].header == null) 
                return false;
	        if (ip >= (uint)m_tiles[it].header.polyCount) 
                return false;
	        return true;
        }

        public int queryPolygonsInTile(dtMeshTile tile, LogicVector3 qmin, LogicVector3 qmax,
                                   ref uint[] polys, int maxPolys)
        {
            LogicVector3 bV;
            LogicVector3 bmin;
            LogicVector3 bmax;
            int n = 0;
            uint _base = getPolyRefBase(tile);
            for (int i = 0; i < tile.header.polyCount; ++i)
            {
                dtPoly p = tile.polys[i];
                // Do not return off-mesh connection polygons.
                if (p.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                    continue;

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
                    if (n < maxPolys)
                        polys[n++] = _base | (uint)i;
                }
            }

            return n;
        }


    }
}
