using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOBA
{
 
    public class NavMeshBuilderDefine
    {
        public const int DT_MAX_TOUCHED_TILES = 8;
        public const int DT_VERTS_PER_POLYGON = 6;

        public const int DT_TILECACHE_MAGIC = 'D' << 24 | 'T' << 16 | 'L' << 8 | 'R'; ///< 'DTLR';
        public const int DT_TILECACHE_VERSION = 1;

        public const byte DT_TILECACHE_NULL_AREA = 0;
        public const byte DT_TILECACHE_WALKABLE_AREA = 63;
        public const ushort DT_TILECACHE_NULL_IDX = ushort.MaxValue;

        public const ushort DT_EXT_LINK = 0x8000;
        public const uint DT_NULL_LINK = 0xffffffff;
     
        public const int DT_LAYER_MAX_NEIS = 16;
    }

    public enum dtNodeFlags
    {
        DT_NODE_OPEN = 0x01,
        DT_NODE_CLOSED = 0x02,
        DT_NODE_PARENT_DETACHED = 0x04, // parent of the node is not adjacent. Found using raycast.
    };

    public enum dtTileFlags
    {
        DT_TILE_FREE_DATA = 0x01,
    };

    public enum dtStatus
    {
        DT_SUCCESS,
        DT_FAILURE,

        DT_OUT_OF_NODES,
        DT_IN_PROGRESS,
        DT_BUFFER_TOO_SMALL,

    };

    public enum dtRaycastOptions
    {
        DT_RAYCAST_USE_COSTS = 0x01,
    };

    public enum SamplePolyAreas
    {
        SAMPLE_POLYAREA_GROUND,
        SAMPLE_POLYAREA_WATER,
        SAMPLE_POLYAREA_ROAD,
        SAMPLE_POLYAREA_DOOR,
        SAMPLE_POLYAREA_GRASS,
        SAMPLE_POLYAREA_JUMP,
    };

    public enum SamplePolyFlags
    {
        SAMPLE_POLYFLAGS_WALK = 0x01,		// Ability to walk (ground, grass, road)
        SAMPLE_POLYFLAGS_SWIM = 0x02,		// Ability to swim (water).
        SAMPLE_POLYFLAGS_DOOR = 0x04,		// Ability to move through doors.
        SAMPLE_POLYFLAGS_JUMP = 0x08,		// Ability to jump.
        SAMPLE_POLYFLAGS_DISABLED = 0x10,		// Disabled polygon
        SAMPLE_POLYFLAGS_ALL = 0xffff	// All abilities.
    };

    public enum dtStraightPathFlags
    {
        DT_STRAIGHTPATH_START = 0x01,				///< The vertex is the start position in the path.
        DT_STRAIGHTPATH_END = 0x02,					///< The vertex is the end position in the path.
        DT_STRAIGHTPATH_OFFMESH_CONNECTION = 0x04,	///< The vertex is the start of an off-mesh connection.
    };

    public  enum dtStraightPathOptions
    {
        DT_STRAIGHTPATH_AREA_CROSSINGS = 0x01,	///< Add a vertex at every polygon edge crossing where area changes.
        DT_STRAIGHTPATH_ALL_CROSSINGS = 0x02,	///< Add a vertex at every polygon edge crossing.
    };


    public class dtRaycastHit
    {
	    /// The hit parameter. (FLT_MAX if no wall hit.)
        public int t; 
	
	    /// hitNormal	The normal of the nearest wall hit. [(x, y, z)]
        public LogicVector3 hitNormal;
        public LogicVector3 hitPos;

	    /// The index of the edge on the final polygon where the wall was hit.
        public int hitEdgeIndex;
	
	    /// Pointer to an array of reference ids of the visited polygons. [opt]
        public uint[] path;
	
	    /// The number of visited polygons. [opt]
        public int pathCount;

	    /// The maximum number of polygons the @p path array can hold.
        public int maxPath;

	    ///  The cost of the path until hit.
        public int pathCost;
    };

    public enum dtPolyTypes
    {
        /// The polygon is a standard convex polygon that is part of the surface of the mesh.
        DT_POLYTYPE_GROUND = 0,
        /// The polygon is an off-mesh connection consisting of two vertices.
        DT_POLYTYPE_OFFMESH_CONNECTION = 1,
    };
}
