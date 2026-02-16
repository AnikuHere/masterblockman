using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOBA
{
    public class dtCompressedTile
    {
         public int salt;					
         public dtTileCacheLayerHeader header;
         public byte[] compressed;
         public int compressedSize;
         public byte[] data;
         public int dataSize;
         public  int flags;
         public dtCompressedTile next ;
    };

    public enum ObstacleState
    {
	    DT_OBSTACLE_EMPTY,
	    DT_OBSTACLE_PROCESSING,
	    DT_OBSTACLE_PROCESSED,
	    DT_OBSTACLE_REMOVING,
    };



    public class dtTileCacheObstacle
    {
	    public int[] pos = new int[3]; 
        public int radius;
        public int height;
	    public int[] touched = new int[NavMeshBuilderDefine.DT_MAX_TOUCHED_TILES];
	    public int[] pending= new int[NavMeshBuilderDefine.DT_MAX_TOUCHED_TILES];
	    public  short salt;
	    public  byte state;
        public byte ntouched;
        public byte npending;
	    public dtTileCacheObstacle next;
    };

    public class dtTileCacheParams
    {
        public int[] orig = new int[3]; 
	    public  int cs;
        public int ch;
	    public  int width;
        public int height;
        public int walkableHeight;
        public int walkableRadius;
        public int walkableClimb;
        public int maxSimplificationError;
	    public  int maxTiles;
	    public  int maxObstacles;
    };

    public enum ObstacleRequestAction
    {
        REQUEST_ADD,
        REQUEST_REMOVE,
    };

    public class NavMeshTileBuildContext
    {
        public dtTileCacheLayer layer = new dtTileCacheLayer();
        public dtTileCacheContourSet lcset = new dtTileCacheContourSet();
        public dtTileCachePolyMesh lmesh = new dtTileCachePolyMesh();

        public void Clear()
        {
            layer.Clear();
            lcset.Clear();
            lmesh.Clear();
        }
    };

    public class dtTileCache
    {
        const int MAX_REQUESTS = 64;
        const int MAX_UPDATE = 64;

        private int m_tileLutSize;						
        private int m_tileLutMask;					

        private dtCompressedTile[] m_posLookup;			
        private dtCompressedTile m_nextFreeTile;	
        private dtCompressedTile[] m_tiles;

        private int m_saltBits;				///< Number of salt bits in the tile ID.
        private int m_tileBits;				///< Number of tile bits in the tile ID.

        private dtTileCacheParams m_params;

        private dtTileCacheObstacle[] m_obstacles;
        private dtTileCacheObstacle m_nextFreeObstacle;

        private int[] m_update = new int[MAX_UPDATE];
        private int m_nupdate;

        private dtNavMesh m_Navmesh;

        //缓存数据
        private TileCacheSetParam m_TileCacheSetParam;

        private NavMeshTileBuildContext m_BuildContext = null;

        public dtTileCache()
        {
            m_tileLutSize = 0;
            m_tileLutMask = 0;
            m_posLookup = null;
            m_nextFreeTile = null;
            m_tiles = null;
            m_saltBits = 0;
            m_tileBits = 0;
            m_obstacles = null;
            m_nextFreeObstacle = null;
            m_nupdate = 0;
        }

        public dtStatus OnInit(TileCacheSetParam tileCacheSetParam, dtNavMesh navmesh)
        {
            m_TileCacheSetParam = tileCacheSetParam;
            m_params = m_TileCacheSetParam.header.cacheParams;
            m_Navmesh = navmesh;

            m_obstacles = new dtTileCacheObstacle[m_params.maxObstacles];
            // Alloc space for obstacles.
            m_nextFreeObstacle = null;

            for (int i = m_params.maxObstacles - 1; i >= 0; --i)
            {
                m_obstacles[i] = new dtTileCacheObstacle();
                m_obstacles[i].salt = 1;
                m_obstacles[i].next = m_nextFreeObstacle;
                m_nextFreeObstacle = m_obstacles[i];
            }

            // Init tiles
            m_tileLutSize = NavMeshMath.NextPow2(m_params.maxTiles / 4);
            if (m_tileLutSize == 0) m_tileLutSize = 1;
            m_tileLutMask = m_tileLutSize - 1;

            m_tiles = new dtCompressedTile[m_params.maxTiles];
            m_posLookup = new dtCompressedTile[m_tileLutSize];
            m_nextFreeTile = null;

            for (int i = m_params.maxTiles - 1; i >= 0; --i)
            {
                m_tiles[i] = new dtCompressedTile();
                m_tiles[i].salt = 1;
                m_tiles[i].next = m_nextFreeTile;
                m_nextFreeTile = m_tiles[i];
            }

            // Init ID generator values.
            m_tileBits = NavMeshMath.Ilog2(NavMeshMath.NextPow2((int)m_params.maxTiles));
            // Only allow 31 salt bits, since the salt mask is calculated using 32bit uint and it will overflow.
            m_saltBits = NavMeshMath.Min((int)31, 32 - m_tileBits);
            if (m_saltBits < 10)
                return dtStatus.DT_FAILURE;

            m_BuildContext = new NavMeshTileBuildContext();

            return dtStatus.DT_SUCCESS;
        }

        public void OnUnInit()
        {
            m_posLookup = null;
            m_nextFreeTile = null;
            m_tiles = null;

            m_params = null;
            m_obstacles = null;
            m_nextFreeObstacle = null;

            m_update = null;
            m_Navmesh = null;
            m_TileCacheSetParam = null;

            m_BuildContext = null;
        }

        public dtStatus addTile(dtTileCacheLayerHeader header, byte flags, ref int result)
        {
            if (header.magic != NavMeshBuilderDefine.DT_TILECACHE_MAGIC)
                return dtStatus.DT_FAILURE;
            if (header.version != NavMeshBuilderDefine.DT_TILECACHE_VERSION)
                return dtStatus.DT_FAILURE;

            // Make sure the location is free.
            if (getTileAt(header.tx, header.ty, header.tlayer) != null)
                return dtStatus.DT_FAILURE;

            // Allocate a tile.
            dtCompressedTile tile = null;
            if (m_nextFreeTile != null)
            {
                tile = m_nextFreeTile;
                m_nextFreeTile = tile.next;
                tile.next = null;
            }

            // Make sure we could allocate a tile.
            if (tile == null)
                return dtStatus.DT_FAILURE ;

            // Insert tile into the position lut.
            int h = computeTileHash(header.tx, header.ty, m_tileLutMask);
            tile.next = m_posLookup[h];
            m_posLookup[h] = tile;

            // Init tile.
            tile.header = header;
            tile.data = null;
            tile.dataSize = 0;
            tile.compressed = null; ;
            tile.compressedSize = 0;
            tile.flags = flags;

            result = getTileRef(tile);

            return dtStatus.DT_SUCCESS;
        }


        public int getTilesAt(int tx, int ty, int[] tiles, int maxTiles)
        {
            int n = 0;

            // Find tile based on hash.
            int h = computeTileHash(tx, ty, m_tileLutMask);
            dtCompressedTile tile = m_posLookup[h];
            while (tile != null)
            {
                if (tile.header != null &&
                    tile.header.tx == tx &&
                    tile.header.ty == ty)
                {
                    if (n < maxTiles)
                        tiles[n++] = getTileRef(tile);
                }
                tile = tile.next;
            }

            return n;
        }

        public dtCompressedTile getTileAt(int tx, int ty, int tlayer)
        {
            // Find tile based on hash.
            int h = computeTileHash(tx, ty, m_tileLutMask);
            dtCompressedTile tile = m_posLookup[h];
            while (tile != null)
            {
                if (tile.header != null &&
                    tile.header.tx == tx &&
                    tile.header.ty == ty &&
                    tile.header.tlayer == tlayer)
                {
                    return tile;
                }
                tile = tile.next;
            }
            return null;
        }

        public int getTileRef(dtCompressedTile tile)
        {
            if (tile == null) return 0;

            int it = -1;
            for (int i = 0; i < m_tiles.Length; i++)
            {
                if (ReferenceEquals(tile, m_tiles[i]))
                {
                    it = i;
                    break;
                }
            }

            return (int)encodeTileId(tile.salt, it);
        }

        public int getObstacleRef(dtTileCacheObstacle ob)
        {
            if (ob == null) return 0;

            int idx = -1;
            for (int i = 0; i < m_obstacles.Length; i++)
            {
                if (ReferenceEquals(ob, m_obstacles[i]))
                {
                    idx = i;
                    break;
                }
            }

            return encodeObstacleId(ob.salt, idx);
        }


        public dtStatus addObstacle(LogicVector3 pos, int radius, int height, ref int result)
        {
            dtTileCacheObstacle ob = null;
            if (m_nextFreeObstacle != null)
            {
                ob = m_nextFreeObstacle;
                m_nextFreeObstacle = ob.next;
                ob.next = null;
            }
            if (ob == null)
                return dtStatus.DT_FAILURE ;

            short salt = ob.salt;
            //memset(ob, 0, sizeof(dtTileCacheObstacle));
            ob.salt = salt;
            ob.state = (byte)ObstacleState.DT_OBSTACLE_PROCESSING;
            ob.pos[0] = pos.x;
            ob.pos[1] = pos.y;
            ob.pos[2] = pos.z;
            ob.radius = radius;
            ob.height = height;

            int opRef = getObstacleRef(ob);
            updateObsRequest(opRef, ObstacleRequestAction.REQUEST_ADD);

            result = opRef;

            return dtStatus.DT_SUCCESS;
        }

        public dtStatus removeObstacle(int _ref)
        {
            if (_ref == 0)
                return dtStatus.DT_SUCCESS;
         
            updateObsRequest(_ref, ObstacleRequestAction.REQUEST_REMOVE);

            return dtStatus.DT_SUCCESS;
        }

        public dtStatus updateObsRequest(int opRef, ObstacleRequestAction action)
        {
            int idx = decodeObstacleIdObstacle(opRef);
            if ((int)idx >= m_params.maxObstacles)
                return dtStatus.DT_FAILURE;

            dtTileCacheObstacle ob = m_obstacles[idx];
            int salt = decodeObstacleIdSalt(opRef);
            if (ob.salt != salt)
                return dtStatus.DT_FAILURE;

            if (action == (int)ObstacleRequestAction.REQUEST_ADD)
            {
                // Find touched tiles.
                LogicVector3 bmin = LogicVector3.zero;
                LogicVector3 bmax = LogicVector3.zero;
                getObstacleBounds(ob, ref bmin, ref bmax);

                int ntouched = 0;
                queryTiles(bmin, bmax, ref ob.touched, ref ntouched, NavMeshBuilderDefine.DT_MAX_TOUCHED_TILES);
                ob.ntouched = (byte)ntouched;
                // Add tiles to update list.
                ob.npending = 0;
                for (int j = 0; j < ob.ntouched; ++j)
                {
                    if (m_nupdate < MAX_UPDATE)
                    {
                        if (!contains(m_update, m_nupdate, ob.touched[j]))
                            m_update[m_nupdate++] = ob.touched[j];
                        ob.pending[ob.npending++] = ob.touched[j];
                    }
                }
            }
            else if (action == ObstacleRequestAction.REQUEST_REMOVE)
            {
                // Prepare to remove obstacle.
                ob.state = (int)ObstacleState.DT_OBSTACLE_REMOVING;
                // Add tiles to update list.
                ob.npending = 0;
                for (int j = 0; j < ob.ntouched; ++j)
                {
                    if (m_nupdate < MAX_UPDATE)
                    {
                        if (!contains(m_update, m_nupdate, ob.touched[j]))
                            m_update[m_nupdate++] = ob.touched[j];
                        ob.pending[ob.npending++] = ob.touched[j];
                    }
                }
            }

            dtStatus status = dtStatus.DT_SUCCESS;
            // Process updates
            while (m_nupdate > 0)
            {
                // Build mesh
                int _ref = m_update[0];
                dtTileCacheLayer cacheLayer = GetTileCacheLayer(_ref);
                status = buildNavMeshTile(_ref, cacheLayer);
                m_nupdate--;
                if (m_nupdate > 0)
                {
                    //   memmove(m_update, m_update + 1, m_nupdate * sizeof(int));

                    for (int i = 0; i < m_nupdate; i++)
                    {
                        m_update[i] = m_update[i + 1];
                    }

                }

                // Update obstacle states.
                for (int i = 0; i < m_params.maxObstacles; ++i)
                {
                    ob = m_obstacles[i];
                    if (ob.state == (byte)ObstacleState.DT_OBSTACLE_PROCESSING || ob.state == (byte)ObstacleState.DT_OBSTACLE_REMOVING)
                    {
                        // Remove handled tile from pending list.
                        for (int j = 0; j < (int)ob.npending; j++)
                        {
                            if (ob.pending[j] == _ref)
                            {
                                ob.pending[j] = ob.pending[(int)ob.npending - 1];
                                ob.npending--;
                                break;
                            }
                        }

                        // If all pending tiles processed, change state.
                        if (ob.npending == 0)
                        {
                            if (ob.state == (byte)ObstacleState.DT_OBSTACLE_PROCESSING)
                            {
                                ob.state = (byte)ObstacleState.DT_OBSTACLE_PROCESSED;
                            }
                            else if (ob.state == (byte)ObstacleState.DT_OBSTACLE_REMOVING)
                            {
                                ob.state = (byte)ObstacleState.DT_OBSTACLE_EMPTY;
                                // Update salt, salt should never be zero.
                                ob.salt = (short)((ob.salt + 1) & ((1 << 16) - 1));
                                if (ob.salt == 0)
                                    ob.salt++;
                                // Return obstacle to free list.
                                ob.next = m_nextFreeObstacle;
                                m_nextFreeObstacle = ob;
                            }
                        }
                    }
                }
            }

            return status;
        }


        public dtStatus buildNavMeshTile(int _ref, dtTileCacheLayer layer)
        {
            int idx = decodeTileIdTile(_ref);
            if (idx > (int)m_params.maxTiles)
                return dtStatus.DT_FAILURE ;

            dtCompressedTile tile = m_tiles[idx];
            int salt = decodeTileIdSalt(_ref);
            if (tile.salt != salt)
                return dtStatus.DT_FAILURE ;

            int walkableClimbVx = (int)(m_params.walkableClimb / m_params.ch);
            dtStatus status = dtStatus.DT_SUCCESS;

            m_BuildContext.Clear();
            m_BuildContext.layer.CopyFrom(layer);
            
            // Decompress tile layer data. 
            //    status = DetourTileCacheBuilder.dtDecompressTileCacheLayer(m_tcomp, tile.data, tile.dataSize,  bc.layer);
            if (status != dtStatus.DT_SUCCESS)
                return status;

            // Rasterize obstacles.
            for (int i = 0; i < m_params.maxObstacles; ++i)
            {
                dtTileCacheObstacle ob = m_obstacles[i];
                if (ob.state == (int)ObstacleState.DT_OBSTACLE_EMPTY || ob.state == (int)ObstacleState.DT_OBSTACLE_REMOVING)
                    continue;
                if (contains(ob.touched, ob.ntouched, _ref))
                {
                    DetourTileCacheBuilder.dtMarRectArea(m_BuildContext.layer, tile.header.bmin, m_params.cs, m_params.ch,
                                       ob.pos, ob.radius, ob.height, 0);
                }
            }

            // Build navmesh
            status = DetourTileCacheBuilder.dtBuildTileCacheRegions(m_BuildContext.layer, walkableClimbVx);
            if (status != dtStatus.DT_SUCCESS)
                return status;

            status = DetourTileCacheBuilder.dtBuildTileCacheContours(m_BuildContext.layer, walkableClimbVx,
                                              m_params.maxSimplificationError, ref m_BuildContext.lcset);
            if (status != dtStatus.DT_SUCCESS)
                return status;

            status = DetourTileCacheBuilder.dtBuildTileCachePolyMesh(m_BuildContext.lcset, m_BuildContext.lmesh);
            if (status != dtStatus.DT_SUCCESS)
                return status;

            // Early out if the mesh tile is empty.
            if (m_BuildContext.lmesh.npolys == 0)
            {
                // Remove existing tile.
                //    navmesh.removeTile(navmesh.getTileRefAt(tile.header.tx, tile.header.ty, tile.header.tlayer), 0, 0);
                return dtStatus.DT_SUCCESS;
            }

            dtNavMeshCreateParams _params = new dtNavMeshCreateParams();
            _params.verts = m_BuildContext.lmesh.verts;
            _params.vertCount = m_BuildContext.lmesh.nverts;
            _params.polys = m_BuildContext.lmesh.polys;
            _params.polyAreas = m_BuildContext.lmesh.areas;
            _params.polyFlags = m_BuildContext.lmesh.flags;
            _params.polyCount = m_BuildContext.lmesh.npolys;
            _params.nvp = 6;
            _params.walkableHeight = m_params.walkableHeight;
            _params.walkableRadius = m_params.walkableRadius;
            _params.walkableClimb = m_params.walkableClimb;
            _params.tileX = tile.header.tx;
            _params.tileY = tile.header.ty;
            _params.tileLayer = tile.header.tlayer;
            _params.cs = m_params.cs;
            _params.ch = m_params.ch;
            _params.buildBvTree = false;
            _params.bmin = tile.header.bmin;
            _params.bmax = tile.header.bmax;

            processArea(_params, m_BuildContext.lmesh.areas, ref m_BuildContext.lmesh.flags);

            // Remove existing tile.
            m_Navmesh.removeTile(m_Navmesh.getTileRefAt(tile.header.tx, tile.header.ty, tile.header.tlayer));

            uint result = 0;
            m_Navmesh.addTile(_params, (int)dtTileFlags.DT_TILE_FREE_DATA, 0, ref result);

            return dtStatus.DT_SUCCESS;
        }

        void processArea(dtNavMeshCreateParams _params,byte[] polyAreas, ref ushort[] polyFlags)
        {
            // Update poly flags from areas.
            for (int i = 0; i < _params.polyCount; ++i)
            {
                if (polyAreas[i] == NavMeshBuilderDefine.DT_TILECACHE_WALKABLE_AREA)
                    polyAreas[i] = (byte)SamplePolyAreas.SAMPLE_POLYAREA_GROUND;

                if (polyAreas[i] == (byte)SamplePolyAreas.SAMPLE_POLYAREA_GROUND ||
                    polyAreas[i] == (byte)SamplePolyAreas.SAMPLE_POLYAREA_GRASS ||
                    polyAreas[i] == (byte)SamplePolyAreas.SAMPLE_POLYAREA_ROAD)
                {
                    polyFlags[i] = (byte)SamplePolyFlags.SAMPLE_POLYFLAGS_WALK;
                }
                else if (polyAreas[i] == (byte)SamplePolyAreas.SAMPLE_POLYAREA_WATER)
                {
                    polyFlags[i] = (byte)SamplePolyFlags.SAMPLE_POLYFLAGS_SWIM;
                }
                else if (polyAreas[i] == (byte)SamplePolyAreas.SAMPLE_POLYAREA_DOOR)
                {
                    polyFlags[i] = (byte)SamplePolyFlags.SAMPLE_POLYFLAGS_WALK | (byte)SamplePolyFlags.SAMPLE_POLYFLAGS_DOOR;
                }
            }
        }


        dtStatus queryTiles(LogicVector3 bmin, LogicVector3 bmax, ref int[] results, ref int resultCount, int maxResults)
        {
            const int MAX_TILES = 32;
            int[] tiles = new int[MAX_TILES];

            int n = 0;

            int tw = m_params.width * m_params.cs;
            int th = m_params.height * m_params.cs;

            //TODO 可能四舍五入
            int tx0 =(bmin.x - m_params.orig[0]) / tw;
            int tx1 =(bmax.x - m_params.orig[0]) / tw;
            int ty0 =(bmin.z - m_params.orig[2]) / th;
            int ty1 =(bmax.z - m_params.orig[2]) / th;

            for (int ty = ty0; ty <= ty1; ++ty)
            {
                for (int tx = tx0; tx <= tx1; ++tx)
                {
                    int ntiles = getTilesAt(tx, ty, tiles, MAX_TILES);

                    for (int i = 0; i < ntiles; ++i)
                    {
                        dtCompressedTile tile = m_tiles[decodeTileIdTile(tiles[i])];
                        LogicVector3 tbmin = LogicVector3.zero;
                        LogicVector3 tbmax = LogicVector3.zero;
                        calcTightTileBounds(tile.header, ref tbmin, ref tbmax);

                        if (NavMeshMath.dtOverlapBounds(bmin, bmax, tbmin, tbmax))
                        {
                            if (n < maxResults)
                                results[n++] = tiles[i];
                        }
                    }
                }
            }

            resultCount = n;

            return dtStatus.DT_SUCCESS;
        }


        public dtTileCacheLayer GetTileCacheLayer(int _ref)
        {
            for(int i=0; i < m_TileCacheSetParam.cacheLayers.Length; i++)
            {
                if(m_TileCacheSetParam.cacheLayers[i].tileRef == _ref)
                    return m_TileCacheSetParam.cacheLayers[i].layer;
            }

            return null;
        }


        void calcTightTileBounds(dtTileCacheLayerHeader header, ref LogicVector3 bmin, ref LogicVector3 bmax)
        {
            int cs = m_params.cs;
            bmin.x = header.bmin[0] + header.minx * cs;
            bmin.y = header.bmin[1];
            bmin.z = header.bmin[2] + header.miny * cs;
            bmax.x = header.bmin[0] + (header.maxx + 1) * cs;
            bmax.y = header.bmax[1];
            bmax.z = header.bmin[2] + (header.maxy + 1) * cs;
        }

        void getObstacleBounds(dtTileCacheObstacle ob, ref LogicVector3 bmin, ref  LogicVector3 bmax)
        {
            bmin.x = ob.pos[0] - ob.radius;
            bmin.y = ob.pos[1];
            bmin.z = ob.pos[2] - ob.radius;
            bmax.x = ob.pos[0] + ob.radius;
            bmax.y = ob.pos[1] + ob.height;
            bmax.z = ob.pos[2] + ob.radius;
        }

        public int getObstacleCount() { return m_params.maxObstacles; }
        public dtTileCacheObstacle getObstacle(int i) { return m_obstacles[i]; }


        /// Encodes a tile id.
        int encodeTileId(int salt, int it)
        {
            return ((int)salt << m_tileBits) | (int)it;
        }

        /// Decodes a tile salt.
        int decodeTileIdSalt(int _ref)
        {
            int saltMask = ((int)1 << m_saltBits) - 1;
            return (int)((_ref >> m_tileBits) & saltMask);
        }

        /// Decodes a tile id.
        int decodeTileIdTile(int _ref)
        {
            int tileMask = ((int)1 << m_tileBits) - 1;
            return (int)(_ref & tileMask);
        }

        /// Encodes an obstacle id.
        int encodeObstacleId(int salt, int it)
        {
            return ((int)salt << 16) | (int)it;
        }

        /// Decodes an obstacle salt.
        int decodeObstacleIdSalt(int _ref)
        {
            int saltMask = ((int)1 << 16) - 1;
            return (int)((_ref >> 16) & saltMask);
        }

        /// Decodes an obstacle id.
        int decodeObstacleIdObstacle(int _ref)
        {
            int tileMask = ((int)1 << 16) - 1;
            return (int)(_ref & tileMask);
        }





        bool contains(int[] a, int n, int v)
        {
            for (int i = 0; i < n; ++i)
                if (a[i] == v)
                    return true;
            return false;
        }

        int computeTileHash(int x, int y, int mask)
        {
            uint h1 = 0x8da6b343; // Large multiplicative constants;
            uint h2 = 0xd8163841; // here arbitrarily chosen primes
            long n = h1 * x + h2 * y;
            return (int)(n & mask);
        }

    }

};





