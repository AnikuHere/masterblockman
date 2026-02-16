
namespace MOBA
{
  
    public class dtNavMeshParams
    {
        public int[] orig = new int[3];					///< The world space origin of the navigation mesh's tile space. [(x, y, z)]
        public int tileWidth;				///< The width of each tile. (Along the x-axis.)
        public int tileHeight;				///< The height of each tile. (Along the z-axis.)
        public int maxTiles;					///< The maximum number of tiles the navigation mesh can contain.
        public int maxPolys;					///< The maximum number of polygons each tile can contain.
    };

    public class TileCacheSetHeader
    {
        public int magic;
        public int version;
        public int numTiles;
        public dtNavMeshParams meshParams;
        public dtTileCacheParams cacheParams;
    };

    public class TileCacheLayer
    {
        public int tileRef;
        public dtTileCacheLayer layer;
    };

    public class TileCacheSetParam
    {
        public TileCacheSetHeader header;
        public TileCacheLayer[] cacheLayers;
    }


    public class dtNavMeshCreateParams
    {

        /// @name Polygon Mesh Attributes
        /// Used to create the base navigation graph.
        /// See #rcPolyMesh for details related to these attributes.
        /// @{

        public ushort[] verts; ///< The polygon mesh vertices. [(x, y, z) * #vertCount] [Unit: vx]
        public int vertCount; ///< The number vertices in the polygon mesh. [Limit: >= 3]
        public ushort[] polys; ///< The polygon data. [Size: #polyCount * 2 * #nvp]
        public ushort[] polyFlags; ///< The user defined flags assigned to each polygon. [Size: #polyCount]
        public byte[] polyAreas; ///< The user defined area ids assigned to each polygon. [Size: #polyCount]
        public int polyCount; ///< Number of polygons in the mesh. [Limit: >= 1]
        public int nvp; ///< Number maximum number of vertices per polygon. [Limit: >= 3]

        /// @}
        /// @name Height Detail Attributes (Optional)
        /// See #rcPolyMeshDetail for details related to these attributes.
        /// @{

        public uint[] detailMeshes; ///< The height detail sub-mesh data. [Size: 4 * #polyCount]
        public int[] detailVerts; ///< The detail mesh vertices. [Size: 3 * #detailVertsCount] [Unit: wu]
        public int detailVertsCount; ///< The number of vertices in the detail mesh.
        public byte[] detailTris; ///< The detail mesh triangles. [Size: 4 * #detailTriCount]
        public int detailTriCount; ///< The number of triangles in the detail mesh.

        /// @}
        /// @name Off-Mesh Connections Attributes (Optional)
        /// Used to define a custom point-to-point edge within the navigation graph, an 
        /// off-mesh connection is a user defined traversable connection made up to two vertices, 
        /// at least one of which resides within a navigation mesh polygon.
        /// @{

        /// Off-mesh connection vertices. [(ax, ay, az, bx, by, bz) * #offMeshConCount] [Unit: wu]
      //  public float[] offMeshConVerts;
        /// Off-mesh connection radii. [Size: #offMeshConCount] [Unit: wu]
     //   public float[] offMeshConRad;
        /// User defined flags assigned to the off-mesh connections. [Size: #offMeshConCount]
   //     public ushort[] offMeshConFlags;
        /// User defined area ids assigned to the off-mesh connections. [Size: #offMeshConCount]
 //       public byte[] offMeshConAreas;
        /// The permitted travel direction of the off-mesh connections. [Size: #offMeshConCount]
        ///
        /// 0 = Travel only from endpoint A to endpoint B.<br/>
        /// #DT_OFFMESH_CON_BIDIR = Bidirectional travel.
    //    public byte[] offMeshConDir;
        /// The user defined ids of the off-mesh connection. [Size: #offMeshConCount]
    //    public uint[] offMeshConUserID;
        /// The number of off-mesh connections. [Limit: >= 0]
  //      public int offMeshConCount;

        /// @}
        /// @name Tile Attributes
        /// @note The tile grid/layer data can be left at zero if the destination is a single tile mesh.
        /// @{

        public uint userId; ///< The user defined id of the tile.
        public int tileX; ///< The tile's x-grid location within the multi-tile destination mesh. (Along the x-axis.)
        public int tileY; ///< The tile's y-grid location within the multi-tile desitation mesh. (Along the z-axis.)
        public int tileLayer; ///< The tile's layer within the layered destination mesh. [Limit: >= 0] (Along the y-axis.)
        public int[] bmin = new int[3]; ///< The minimum bounds of the tile. [(x, y, z)] [Unit: wu]
        public int[] bmax = new int[3]; ///< The maximum bounds of the tile. [(x, y, z)] [Unit: wu]

        /// @}
        /// @name General Configuration Attributes
        /// @{

        public int walkableHeight; ///< The agent height. [Unit: wu]
        public int walkableRadius; ///< The agent radius. [Unit: wu]
        public int walkableClimb; ///< The agent maximum traversable ledge. (Up/Down) [Unit: wu]
        public int cs; ///< The xz-plane cell size of the polygon mesh. [Limit: > 0] [Unit: wu]
        public int ch; ///< The y-axis cell height of the polygon mesh. [Limit: > 0] [Unit: wu]

        /// True if a bounding volume tree should be built for the tile.
        /// @note The BVTree is not normally needed for layered navigation meshes.
        public bool buildBvTree;

        /// @}
    }

    public class dtMeshHeader
    {
        public int magic;				///< Tile magic number. (Used to identify the data format.)
        public int version;			///< Tile data format version number.
        public int x;					///< The x-position of the tile within the dtNavMesh tile grid. (x, y, layer)
        public int y;					///< The y-position of the tile within the dtNavMesh tile grid. (x, y, layer)
        public int layer;				///< The layer of the tile within the dtNavMesh tile grid. (x, y, layer)
        public uint userId;	///< The user defined id of the tile.
        public int polyCount;			///< The number of polygons in the tile.
        public int vertCount;			///< The number of vertices in the tile.
        public int maxLinkCount;		///< The number of allocated links.
        public int detailMeshCount;	///< The number of sub-meshes in the detail mesh.
	
	    /// The number of unique vertices in the detail mesh. (In addition to the polygon vertices.)
        public int detailVertCount;

        public int detailTriCount;			///< The number of triangles in the detail mesh.
        public int bvNodeCount;			///< The number of bounding volume nodes. (Zero if bounding volumes are disabled.)
        public int offMeshConCount;		///< The number of off-mesh connections.
        public int offMeshBase;			///< The index of the first polygon which is an off-mesh connection.
        public int walkableHeight;		///< The height of the agents using the tile.
        public int walkableRadius;		///< The radius of the agents using the tile.
        public int walkableClimb;		///< The maximum climb height of the agents using the tile.
        public int[] bmin = new int[3];				///< The minimum bounds of the tile's AABB. [(x, y, z)]
        public int[] bmax = new int[3];			///< The maximum bounds of the tile's AABB. [(x, y, z)]
	
	    /// The bounding volume quantization factor. 
        public int bvQuantFactor;
    };

    public class dtPoly
    {
	    /// Index to first link in linked list. (Or #DT_NULL_LINK if there is no link.)
	    public uint firstLink;

	    /// The indices of the polygon's vertices.
	    /// The actual vertices are located in dtMeshTile::verts.
        public ushort[] verts = new ushort[NavMeshBuilderDefine.DT_VERTS_PER_POLYGON];

	    /// Packed data representing neighbor polygons references and flags for each edge.
        public ushort[] neis = new ushort[NavMeshBuilderDefine.DT_VERTS_PER_POLYGON];

	    /// The user defined polygon flags.
	    public    ushort flags;

	    /// The number of vertices in the polygon.
	    public byte vertCount;

	    /// The bit packed area id and polygon type.
	    /// @note Use the structure's set and get methods to acess this value.
	   public byte areaAndtype;

	    public void setArea(byte  a) { areaAndtype = (byte)((areaAndtype & 0xc0) | (a & 0x3f)); }
	    public void setType(byte  t) { areaAndtype =  (byte)((areaAndtype & 0x3f) | (t << 6)); }

	    public byte getArea()  { return  (byte)(areaAndtype & 0x3f); }
	    public  byte getType()  { return  (byte)(areaAndtype >> 6); }
    };
    
    public  class dtLink
    {
	     public  uint _ref;					///< Neighbour reference. (The neighbor that is linked to.)
	     public uint next;				///< Index of the next link.
	     public byte edge;				///< Index of the polygon edge that owns this link.
	     public byte side;				///< If a boundary link, defines on which side the link is.
	     public byte bmin;				///< If a boundary link, defines the minimum sub-edge area.
	     public byte bmax;				///< If a boundary link, defines the maximum sub-edge area.
    };

    public class dtPolyDetail
    {
         public uint vertBase;			///< The offset of the vertices in the dtMeshTile::detailVerts array.
         public uint triBase;			///< The offset of the triangles in the dtMeshTile::detailTris array.
        public  byte vertCount;		///< The number of vertices in the sub-mesh.
        public byte triCount;			///< The number of triangles in the sub-mesh.
    };


   public class  dtMeshTile
    {
        public uint salt;					///< Counter describing modifications to the tile.

        public uint linksFreeList;			///< Index to the next free link.
        public dtMeshHeader header;				///< The tile header.
        public dtPoly[] polys;						///< The tile polygons. [Size: dtMeshHeader::polyCount]
        public int[] verts;						///< The tile vertices. [Size: dtMeshHeader::vertCount]
        public dtLink[] links;						///< The tile links. [Size: dtMeshHeader::maxLinkCount]
                                                
        public dtPolyDetail[] detailMeshes;			///< The tile's detail sub-meshes. [Size: dtMeshHeader::detailMeshCount]
	
	    /// The detail mesh's unique vertices. [(x, y, z) * dtMeshHeader::detailVertCount]
        public int[] detailVerts;	

	    /// The detail mesh's triangles. [(vertA, vertB, vertC) * dtMeshHeader::detailTriCount]
        public byte[] detailTris;	

	    /// The tile bounding volume nodes. [Size: dtMeshHeader::bvNodeCount]
	    /// (Will be null if bounding volumes are disabled.)
	  //  dtBVNode* bvTree;

	  //  dtOffMeshConnection* offMeshCons;		///< The tile off-mesh connections. [Size: dtMeshHeader::offMeshConCount]
		
	  //  unsigned char* data;					///< The tile data. (Not directly accessed under normal situations.)
	  //  int dataSize;							///< Size of the tile data.
        public int flags;								///< Tile flags. (See: #dtTileFlags)
        public dtMeshTile next;						///< The next free tile, or the next tile in the spatial grid.
   // private:
	 //   dtMeshTile(const dtMeshTile&);
	  //  dtMeshTile& operator=(const dtMeshTile&);
    };

    public static class GlobalMembersDetourNavMeshBuilder
    {
        
        public static bool dtCreateNavMeshData(dtNavMeshCreateParams _params, ref dtMeshTile meshTile )
        {
            if (_params.nvp > NavMeshBuilderDefine.DT_VERTS_PER_POLYGON)
		        return false;
	        if (_params.vertCount >= 0xffff)
		        return false;
	        if (_params.vertCount == 0 || _params.verts == null)
		        return false;
	        if (_params.polyCount == 0 || _params.polys == null)
		        return false;

	         int nvp = _params.nvp;
	
	        int storedOffMeshConCount = 0;
	        int offMeshConLinkCount = 0;
	
	        // Off-mesh connectionss are stored as polygons, adjust values.
	         int totPolyCount = _params.polyCount + storedOffMeshConCount;
	         int totVertCount = _params.vertCount + storedOffMeshConCount*2;
	
	        // Find portal edges which are at tile borders.
	        int edgeCount = 0;
	        int portalCount = 0;
	        for (int i = 0; i < _params.polyCount; ++i)
	        {
		        // ushort* p = &_params.polys[i*2*nvp];
		        for (int j = 0; j < nvp; ++j)
		        {
                    if (_params.polys[i * 2 * nvp + j] == NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX) break;
			        edgeCount++;
			
			        if ((_params.polys[i*2*nvp + nvp+j] & 0x8000) != 0)
			        {
				         ushort dir = (ushort)( _params.polys[i*2*nvp + nvp+j] & 0xf);
				        if (dir != 0xf)
					        portalCount++;
			        }
		        }
	        }

	         int maxLinkCount = edgeCount + portalCount*2 + offMeshConLinkCount*2;
	
	        // Find unique detail vertices.
	        int uniqueDetailVertCount = 0;
	        int detailTriCount = 0;
	        if (_params.detailMeshes != null)
	        {
		        // Has detail mesh, count unique detail vertex count and use input detail tri count.
		        detailTriCount = _params.detailTriCount;
		        for (int i = 0; i < _params.polyCount; ++i)
		        {
			         //ushort* p = &_params.polys[i*nvp*2];
			        int ndv = (int)_params.detailMeshes[i*4+1];
			        int nv = 0;
			        for (int j = 0; j < nvp; ++j)
			        {
                        if (_params.polys[i * nvp * 2 + j] == NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX) break;
				        nv++;
			        }
			        ndv -= nv;
			        uniqueDetailVertCount += ndv;
		        }
	        }
	        else
	        {
		        // No input detail mesh, build detail mesh from nav polys.
		        uniqueDetailVertCount = 0; // No extra detail verts.
		        detailTriCount = 0;
		        for (int i = 0; i < _params.polyCount; ++i)
		        {
			      //  ushort* p = &_params.polys[i*nvp*2];
			        int nv = 0;
			        for (int j = 0; j < nvp; ++j)
			        {
                        if (_params.polys[i * nvp * 2 + j] == NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX) break;
				        nv++;
			        }
			        detailTriCount += nv-2;
		        }
	        }
	
	         int vertsSize =3*totVertCount;
	         int polysSize =totPolyCount;
	         int linksSize =maxLinkCount;
	         int detailMeshesSize = _params.polyCount;
	         int detailVertsSize = 3*uniqueDetailVertCount;
	         int detailTrisSize = 4*detailTriCount;
	    
            meshTile.header = new dtMeshHeader();
            meshTile.verts = new int[vertsSize];
            meshTile.polys = new dtPoly[polysSize];
            for (int i = 0; i < polysSize; i++ )
            {
                meshTile.polys[i] = new dtPoly();
            }

            meshTile.links = new dtLink[linksSize];
            for (int i = 0; i < linksSize; i++)
            {
                meshTile.links[i] = new dtLink();
            }

            meshTile.detailVerts = new int[detailVertsSize];
           meshTile.detailTris = new byte[detailTrisSize];
            
            meshTile.detailMeshes = new dtPolyDetail[detailMeshesSize];
            for (int i = 0; i < detailMeshesSize; i++ )
            {
                meshTile.detailMeshes[i] = new dtPolyDetail();
            }
            
             // Store header
             meshTile.header.magic = 0;
	         meshTile.header.version = 0;
	         meshTile.header.x = _params.tileX;
	         meshTile.header.y = _params.tileY;
	         meshTile.header.layer = _params.tileLayer;
	         meshTile.header.userId = _params.userId;
	         meshTile.header.polyCount = totPolyCount;
	         meshTile.header.vertCount = totVertCount;
	         meshTile.header.maxLinkCount = maxLinkCount;

            for(int i=0; i <_params.bmin.Length; i++ )
            {
                meshTile.header.bmin[i] = _params.bmin[i];
            }

            for(int i=0; i <_params.bmax.Length; i++ )
            {
                meshTile.header.bmax[i] = _params.bmax[i];
            }

	         meshTile.header.detailMeshCount = _params.polyCount;
	         meshTile.header.detailVertCount = uniqueDetailVertCount;
	         meshTile.header.detailTriCount = detailTriCount;
             meshTile.header.bvQuantFactor = MathUtils.iPointUnit * MathUtils.iPointUnit / _params.cs;
	         meshTile.header.offMeshBase = _params.polyCount;
	         meshTile.header.walkableHeight = _params.walkableHeight;
	         meshTile.header.walkableRadius = _params.walkableRadius;
	         meshTile.header.walkableClimb = _params.walkableClimb;
	         meshTile.header.offMeshConCount = storedOffMeshConCount;
	         meshTile.header.bvNodeCount = _params.buildBvTree ? _params.polyCount*2 : 0;
	
	         int offMeshVertsBase = _params.vertCount;
	         int offMeshPolyBase = _params.polyCount;
	
	        // Store vertices
	        // Mesh vertices
	        for (int i = 0; i < _params.vertCount; ++i)
	        {
		      //  ushort* iv = &_params.verts[i*3];
		      //  float* v = &navVerts[i*3];
                meshTile.verts[i * 3 + 0] = _params.bmin[0] + _params.verts[i * 3 + 0] * _params.cs;
                meshTile.verts[i * 3 + 1] = _params.bmin[1] + _params.verts[i * 3 + 1] * _params.ch;
                meshTile.verts[i * 3 + 2] = _params.bmin[2] + _params.verts[i * 3 + 2] * _params.cs;
	        }

         
	        // Store polygons
	        // Mesh polys
            int srcIndex = 0;

	        for (int i = 0; i < _params.polyCount; ++i)
	        {
		        dtPoly p = meshTile.polys[i];
		        p.vertCount = 0;
		        p.flags = _params.polyFlags[i];
		        p.setArea(_params.polyAreas[i]);
		        p.setType((byte)dtPolyTypes.DT_POLYTYPE_GROUND);

		        for (int j = 0; j < nvp; ++j)
		        {
                    if (_params.polys[srcIndex + j] == NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX) break;

			        p.verts[j] = _params.polys[srcIndex + j];
			        if ( (_params.polys[srcIndex + nvp+j] & 0x8000) != 0)
			        {
				        // Border or portal edge.
				        ushort dir =  (ushort)  (_params.polys[srcIndex + nvp+j] & 0xf);
				        if (dir == 0xf) // Border
					        p.neis[j] = 0;
				        else if (dir == 0) // Portal x-
                            p.neis[j] = NavMeshBuilderDefine.DT_EXT_LINK | 4;
				        else if (dir == 1) // Portal z+
                            p.neis[j] = NavMeshBuilderDefine.DT_EXT_LINK | 2;
				        else if (dir == 2) // Portal x+
                            p.neis[j] = NavMeshBuilderDefine.DT_EXT_LINK | 0;
				        else if (dir == 3) // Portal z-
                            p.neis[j] = NavMeshBuilderDefine.DT_EXT_LINK | 6;
			        }
			        else
			        {
				        // Normal connection
				        p.neis[j] = (ushort)( _params.polys[srcIndex + nvp+j]+1);
			        }
			
			        p.vertCount++;
		        }

		        srcIndex += nvp*2;
	        }


            // Store detail meshes and vertices.
	        // The nav polygon vertices are stored as the first vertices on each mesh.
	        // We compress the mesh data by skipping them and using the navmesh coordinates.
	        if (_params.detailMeshes != null)
	        {
		        ushort vbase = 0;
		        for (int i = 0; i < _params.polyCount; ++i)
		        {
			        dtPolyDetail dtl = meshTile.detailMeshes[i];
			        int vb = (int)_params.detailMeshes[i*4+0];
			        int ndv = (int)_params.detailMeshes[i*4+1];
			        int nv =  meshTile.polys [i].vertCount;
			        dtl.vertBase = (uint)vbase;
			        dtl.vertCount = (byte)(ndv-nv);
			        dtl.triBase = (uint)_params.detailMeshes[i*4+2];
			        dtl.triCount = (byte)_params.detailMeshes[i*4+3];
			        // Copy vertices except the first 'nv' verts which are equal to nav poly verts.
			        if ((ndv-nv)  != 0)
			        {
				        //memcpy(&navDVerts[vbase*3], &_params.detailVerts[(vb+nv)*3], sizeof(float)*3*(ndv-nv));

                         for(int j=0; j <3*(ndv-nv); j++ )
                        {
                            meshTile.detailVerts[vbase * 3 + j] = _params.detailVerts[(vb + nv) * 3 + j];
                        }

				        vbase += (ushort)(ndv-nv);
			        }
		        }
		        // Store triangles.
		     //   memcpy(navDTris, _params.detailTris, sizeof(unsigned char)*4*_params.detailTriCount);

                for(int i=0; i <_params.detailTriCount * 4; i++ )
                {
                    meshTile.detailTris[i] = _params.detailTris[i];
                }

	        }
	        else
	        {
		        // Create dummy detail mesh by triangulating polys.
		        int tbase = 0;
		        for (int i = 0; i < _params.polyCount; ++i)
		        {
			        dtPolyDetail dtl = meshTile.detailMeshes[i];
			        int nv =  meshTile.polys [i].vertCount;
			        dtl.vertBase = 0;
			        dtl.vertCount = 0;
			        dtl.triBase = (uint)tbase;
			        dtl.triCount = (byte)(nv-2);
			        // Triangulate polygon (local indices).
			        for (int j = 2; j < nv; ++j)
			        {
				      //  unsigned char* t = &navDTris[tbase*4];
                        meshTile.detailTris[tbase * 4] = 0;
                        meshTile.detailTris[tbase * 4 + 1] = (byte)(j - 1);
                        meshTile.detailTris[tbase * 4 + 2] = (byte)j;
				        // Bit for each edge that belongs to poly boundary.
                        meshTile.detailTris[tbase * 4 + 3] = (1 << 2);

				        if (j == 2)
                            meshTile.detailTris[tbase * 4 + 3] |= (1 << 0);
				        if (j == nv-1)
                            meshTile.detailTris[tbase * 4 + 3] |= (1 << 4);
				        tbase++;
			        }
		        }
	        }

	        return true;
        }

    }

}