using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOBA
{
    public class dtTileCacheLayerHeader
    {
        public int magic;								///< Data magic
        public int version;							///< Data version
        public int tx;
        public int ty;
        public int tlayer;
        public int[] bmin = new int[3];
        public int[] bmax = new int[3];
        public short hmin;				///< Height min/max range
        public short hmax;
        public byte width;			///< Dimension of the layer.
        public byte height;
        public byte minx;	 	///< Usable sub-region.
        public byte maxx;
        public byte miny;
        public byte maxy;

        public void CopyFrom(dtTileCacheLayerHeader other)
        {
            magic = other.magic;
            version = other.version;
            tx = other.tx;
            ty = other.ty;
            tlayer = other.tlayer;
            bmin[0] = other.bmin[0] ;
            bmin[1]  = other.bmin[1] ;
            bmin[2]  = other.bmin[2] ;
            bmax[0] = other.bmax[0] ;
            bmax[1]  = other.bmax[1] ;
            bmax[2]  = other.bmax[2] ;
            hmin = other.hmin;
            hmax = other.hmax;
            width = other.width;
            height = other.height;
            minx = other.minx;
            maxx = other.maxx;
            miny = other.miny;
            maxy = other.maxy;
        }
    };

    public class dtTileCacheLayer
    {
        public dtTileCacheLayerHeader header;
        public byte regCount;					///< Region count.
        public byte[] heights;
        public byte[] areas;
        public byte[] cons;
        public byte[] regs;

        public void Clear()
        {
            regCount = 0;
            heights = null;
            areas = null;
            cons = null;
            regs = null;
        }


        public void CopyFrom(dtTileCacheLayer other)
        {
            if (header == null)
                header = new dtTileCacheLayerHeader();

            header.CopyFrom(other.header);
            regCount = other.regCount;

            heights = new byte[other.heights.Length];
            for(int i=0;i<other.heights.Length;i++)
            {
                heights[i] = other.heights[i];
            }

            areas = new byte[other.areas.Length];
            for (int i = 0; i < other.areas.Length; i++)
            {
                areas[i] = other.areas[i];
            }

            cons = new byte[other.cons.Length];
            for (int i = 0; i < other.cons.Length; i++)
            {
                cons[i] = other.cons[i];
            }

            regs = new byte[other.regs.Length];
            for (int i = 0; i < other.regs.Length; i++)
            {
                regs[i] = other.regs[i];
            }
        }
    };

    public class dtTileCacheContour
    {
        public int nverts;
        public byte[] verts;
        public byte reg;
        public byte area;
    };

    public class dtTileCacheContourSet
    {
        public int nconts;
        public dtTileCacheContour[] conts;

        public void Clear()
        {
            nconts = 0;
            conts = null;
        }

    };

    public class dtTileCachePolyMesh
    {
        public int nvp;
        public int nverts;				///< Number of vertices.
        public int npolys;				///< Number of polygons.
        public ushort[] verts;	///< Vertices of the mesh, 3 elements per vertex.
        public ushort[] polys;	///< Polygons of the mesh, nvp*2 elements per polygon.
        public ushort[] flags;	///< Per polygon flags.
        public byte[] areas;	///< Area ID of polygons.
                               
        public void Clear()
        {
            nvp = 0;
            nverts = 0;
            npolys = 0;
            verts = null;
            polys = null;
            flags = null;
            areas = null;
        }
    };

    
    public class dtTempContour
    {
        public dtTempContour(byte[] vbuf, int nvbuf,
                             short[] pbuf, int npbuf)
        {
            verts = vbuf;
            nverts = 0;
            cverts = nvbuf;
            poly = pbuf;
            npoly = 0;
            cpoly = npbuf;

        }

        public byte[] verts;
        public int nverts;
        public int cverts;
        public short[] poly;
        public int npoly;
        public int cpoly;
    };


    public class DetourTileCacheBuilder
    {
        public const int MAX_VERTS_PER_POLY = 6;
        public const int MAX_REM_EDGES = 48;

        const int VERTEX_BUCKET_COUNT2 = (1 << 8);

        class dtLayerSweepSpan
        {
            public short ns;	// number samples
            public byte id;	// region id
            public byte nei;	// neighbour id
        };

        public class dtLayerMonotoneRegion
        {
            public int area;
            public byte[] neis = new byte[NavMeshBuilderDefine.DT_LAYER_MAX_NEIS];
            public byte nneis;
            public byte regId;
            public byte areaId;
        };

        public static dtStatus dtBuildTileCacheRegions(dtTileCacheLayer layer, int walkableClimb)
        {
	         int w = (int)layer.header.width;
	         int h = (int)layer.header.height;

            for(int i=0; i < w*h ; i++)
            {
                layer.regs[i] = 0xff;
            }
	
	         int nsweeps = w;
            dtLayerSweepSpan[] sweeps = new dtLayerSweepSpan[nsweeps];
            for(int i=0; i < sweeps.Length; i++)
            {
                sweeps[i] = new dtLayerSweepSpan();
            }
	
	        // Partition walkable area into monotone regions.
	         byte[] prevCount = new byte[256];
	         byte regId = 0;
	
	        for (int y = 0; y < h; ++y)
	        {
		        if (regId > 0)
                {
                    for(int i=0; i <regId; i++ )
                    {
                        prevCount[i] = 0;
                    }
                }

		         byte sweepId = 0;
		
		        for (int x = 0; x < w; ++x)
		        {
			         int idx = x + y*w;
                     if (layer.areas[idx] == NavMeshBuilderDefine.DT_TILECACHE_NULL_AREA) continue;
			
			         byte sid = 0xff;
			
			        // -x
			         int xidx = (x-1)+y*w;
			        if (x > 0 && isConnected(layer, idx, xidx, walkableClimb))
			        {
				        if (layer.regs[xidx] != 0xff)
					        sid = layer.regs[xidx];
			        }
			
			        if (sid == 0xff)
			        {
				        sid = sweepId++;
				        sweeps[sid].nei = 0xff;
				        sweeps[sid].ns = 0;
			        }
			
			        // -y
			         int yidx = x+(y-1)*w;
			        if (y > 0 && isConnected(layer, idx, yidx, walkableClimb))
			        {
				          byte nr = layer.regs[yidx];
				        if (nr != 0xff)
				        {
					        // Set neighbour when first valid neighbour is encoutered.
					        if (sweeps[sid].ns == 0)
						        sweeps[sid].nei = nr;
					
					        if (sweeps[sid].nei == nr)
					        {
						        // Update existing neighbour
						        sweeps[sid].ns++;
						        prevCount[nr]++;
					        }
					        else
					        {
						        // This is hit if there is nore than one neighbour.
						        // Invalidate the neighbour.
						        sweeps[sid].nei = 0xff;
					        }
				        }
			        }
			
			        layer.regs[idx] = sid;
		        }
		
		        // Create unique ID.
		        for (int i = 0; i < sweepId; ++i)
		        {
			        // If the neighbour is set and there is only one continuous connection to it,
			        // the sweep will be merged with the previous one, else new region is created.
			        if (sweeps[i].nei != 0xff && ( short)prevCount[sweeps[i].nei] == sweeps[i].ns)
			        {
				        sweeps[i].id = sweeps[i].nei;
			        }
			        else
			        {
				        if (regId == 255)
				        {
					        // Region ID's overflow.
					        return dtStatus.DT_FAILURE;
				        }
				        sweeps[i].id = regId++;
			        }
		        }
		
		        // Remap local sweep ids to region ids.
		        for (int x = 0; x < w; ++x)
		        {
			         int idx = x+y*w;
			        if (layer.regs[idx] != 0xff)
				        layer.regs[idx] = sweeps[layer.regs[idx]].id;
		        }
	        }
	
	        // Allocate and init layer regions.
	         int nregs = (int)regId;
            dtLayerMonotoneRegion[] regs = new dtLayerMonotoneRegion[nregs];
            for (int i = 0; i < nregs; ++i)
            {
                regs[i] = new dtLayerMonotoneRegion();
                regs[i].regId = 0xff;
            }
	
	        // Find region neighbours.
	        for (int y = 0; y < h; ++y)
	        {
		        for (int x = 0; x < w; ++x)
		        {
			         int idx = x+y*w;
			          byte ri = layer.regs[idx];
			        if (ri == 0xff)
				        continue;
			
			        // Update area.
			        regs[ri].area++;
			        regs[ri].areaId = layer.areas[idx];
			
			        // Update neighbours
			         int ymi = x+(y-1)*w;
			        if (y > 0 && isConnected(layer, idx, ymi, walkableClimb))
			        {
				          byte rai = layer.regs[ymi];
				        if (rai != 0xff && rai != ri)
				        {
					        addUniqueLast(ref regs[ri].neis, ref regs[ri].nneis, rai);
					        addUniqueLast(ref regs[rai].neis, ref regs[rai].nneis, ri);
				        }
			        }
		        }
	        }
	
	        for (int i = 0; i < nregs; ++i)
		        regs[i].regId = ( byte)i;
	
	        for (int i = 0; i < nregs; ++i)
	        {
		        dtLayerMonotoneRegion reg = regs[i];
		
		        int merge = -1;
		        int mergea = 0;
		        for (int j = 0; j < (int)reg.nneis; ++j)
		        {
			          byte nei = reg.neis[j];
			        dtLayerMonotoneRegion regn = regs[nei];
			        if (reg.regId == regn.regId)
				        continue;
			        if (reg.areaId != regn.areaId)
				        continue;
			        if (regn.area > mergea)
			        {
				        if (canMerge(reg.regId, regn.regId, regs, nregs))
				        {
					        mergea = regn.area;
					        merge = (int)nei;
				        }
			        }
		        }
		        if (merge != -1)
		        {
			         byte oldId = reg.regId;
			         byte newId = regs[merge].regId;
			        for (int j = 0; j < nregs; ++j)
				        if (regs[j].regId == oldId)
					        regs[j].regId = newId;
		        }
	        }
	
	        // Compact ids.
	         byte[] remap = new byte[256];
	        // Find number of unique regions.
	        regId = 0;
	        for (int i = 0; i < nregs; ++i)
		        remap[regs[i].regId] = 1;
	        for (int i = 0; i < 256; ++i)
		        if (remap[i] != 0)
			        remap[i] = regId++;
	        // Remap ids.
	        for (int i = 0; i < nregs; ++i)
		        regs[i].regId = remap[regs[i].regId];
	
	        layer.regCount = regId;
	
	        for (int i = 0; i < w*h; ++i)
	        {
		        if (layer.regs[i] != 0xff)
			        layer.regs[i] = regs[layer.regs[i]].regId;
	        }
	
	        return dtStatus.DT_SUCCESS;
        }

    // TODO: move this somewhere else, once the layer meshing is done.
     public static dtStatus dtBuildTileCacheContours(dtTileCacheLayer layer, int walkableClimb, int maxError,
                                         ref dtTileCacheContourSet lcset)
        {

            int w = (int)layer.header.width;
            int h = (int)layer.header.height;

            lcset.nconts = layer.regCount;
            lcset.conts = new dtTileCacheContour[lcset.nconts];
            for (int i = 0; i < lcset.conts.Length; i++)
            {
                lcset.conts[i] = new dtTileCacheContour();
            }

            // Allocate temp buffer for contour tracing.
            int maxTempVerts = (w + h) * 2 * 2; // Twice around the layer.

            byte[] tempVerts = new byte[maxTempVerts * 4];
            short[] tempPoly = new short[maxTempVerts];

            dtTempContour temp = new dtTempContour(tempVerts, maxTempVerts, tempPoly, maxTempVerts);

            // Find contours.
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    int idx = x + y * w;
                    byte ri = layer.regs[idx];
                    if (ri == 0xff)
                        continue;

                    dtTileCacheContour cont = lcset.conts[ri];

                    if (cont.nverts > 0)
                        continue;

                    cont.reg = ri;
                    cont.area = layer.areas[idx];

                    if (!walkContour(layer, x, y, temp))
                    {
                        // Too complex contour.
                        // Note: If you hit here ofte, try increasing 'maxTempVerts'.
                        return dtStatus.DT_FAILURE;
                    }

                    simplifyContour(temp, maxError);

                    // Store contour.
                    cont.nverts = temp.nverts;
                    if (cont.nverts > 0)
                    {
                        cont.verts = new byte[4 * temp.nverts];

                        for (int i = 0, j = temp.nverts - 1; i < temp.nverts; j = i++)
                        {
                            //  char* dst = &cont.verts[j*4];
                            // char* v = &temp.verts[j*4];
                            //  char* vn = &temp.verts[i*4];
                            byte nei = temp.verts[i * 4 + 3]; // The neighbour reg is stored at segment vertex of a segment.

                            bool shouldRemove = false;
                            byte lh = getCornerHeight(layer, (int)temp.verts[i * 4 + 0], (int)temp.verts[i * 4 + 1],
                                (int)temp.verts[i * 4 + 2], walkableClimb, ref shouldRemove);

                            cont.verts[j * 4 + 0] = temp.verts[j * 4 + 0];
                            cont.verts[j * 4 + 1] = lh;
                            cont.verts[j * 4 + 2] = temp.verts[j * 4 + 2];

                            // Store portal direction and remove status to the fourth component.
                            cont.verts[j * 4 + 3] = 0x0f;
                            if (nei != 0xff && nei >= 0xf8)
                                cont.verts[j * 4 + 3] = (byte)(nei - (byte)0xf8);
                            if (shouldRemove)
                                cont.verts[j * 4 + 3] |= 0x80;
                        }
                    }
                }
            }

            return dtStatus.DT_SUCCESS;
        }

        public static dtStatus dtBuildTileCachePolyMesh(dtTileCacheContourSet lcset, dtTileCachePolyMesh mesh)
        {
            int maxVertices = 0;
            int maxTris = 0;
            int maxVertsPerCont = 0;
            for (int i = 0; i < lcset.nconts; ++i)
            {
                // Skip null contours.
                if (lcset.conts[i].nverts < 3) continue;
                maxVertices += lcset.conts[i].nverts;
                maxTris += lcset.conts[i].nverts - 2;
                maxVertsPerCont = NavMeshMath.Max(maxVertsPerCont, lcset.conts[i].nverts);
            }

            // TODO: warn about too many vertices?

            mesh.nvp = MAX_VERTS_PER_POLY;

            byte[] vflags = new byte[maxVertices];

            mesh.verts = new ushort[maxVertices * 3];
            mesh.polys = new ushort[maxTris * MAX_VERTS_PER_POLY * 2];
            mesh.areas = new byte[maxTris];

            mesh.flags = new ushort[maxTris];
            for (int i = 0; i < mesh.flags.Length; i++)
            {
                mesh.flags[i] = 0;
            }

            mesh.nverts = 0;
            mesh.npolys = 0;

            for (int i = 0; i < maxVertices * 3; i++)
            {
                mesh.verts[i] = 0;
            }

            for (int i = 0; i < maxTris * MAX_VERTS_PER_POLY * 2; i++)
            {
                mesh.polys[i] = NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX;
            }

            for (int i = 0; i < maxTris; i++)
            {
                mesh.areas[i] = 0;
            }


            ushort[] firstVert = new ushort[VERTEX_BUCKET_COUNT2];
            for (int i = 0; i < VERTEX_BUCKET_COUNT2; ++i)
                firstVert[i] = NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX;

            ushort[] nextVert = new ushort[maxVertices];
            for (int i = 0; i < maxVertices; i++)
            {
                nextVert[i] = 0;
            }

            ushort[] indices = new ushort[maxVertsPerCont];
            ushort[] tris = new ushort[maxVertsPerCont * 3];
            ushort[] polys = new ushort[maxVertsPerCont * MAX_VERTS_PER_POLY];


            for (int i = 0; i < lcset.nconts; ++i)
            {
                dtTileCacheContour cont = lcset.conts[i];

                // Skip null contours.
                if (cont.nverts < 3)
                    continue;

                // Triangulate contour
                for (int j = 0; j < cont.nverts; ++j)
                    indices[j] = (ushort)j;

                int ntris = triangulate(cont.nverts, cont.verts, ref indices, ref tris);
                if (ntris <= 0)
                {
                    // TODO: issue warning!
                    ntris = -ntris;
                }

                // Add and merge vertices.
                for (int j = 0; j < cont.nverts; ++j)
                {
                    //char* v = &cont.verts[j*4];
                    indices[j] = addVertex((ushort)cont.verts[j * 4 + 0], (ushort)cont.verts[j * 4 + 1], (ushort)cont.verts[j * 4 + 2],
                                           ref mesh.verts, ref firstVert, ref nextVert, ref mesh.nverts);
                    if ((cont.verts[j * 4 + 3] & 0x80) != 0)
                    {
                        // This vertex should be removed.
                        vflags[indices[j]] = 1;
                    }
                }

                // Build initial polygons.
                int npolys = 0;
                for (int m = 0; m < polys.Length; m++)
                {
                    polys[m] = NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX;
                }

                for (int j = 0; j < ntris; ++j)
                {
                    // short* t = &tris[j*3];
                    if (tris[j * 3] != tris[j * 3 + 1] && tris[j * 3 + 0] != tris[j * 3 + 2] && tris[j * 3 + 1] != tris[j * 3 + 2])
                    {
                        polys[npolys * MAX_VERTS_PER_POLY + 0] = indices[tris[j * 3 + 0]];
                        polys[npolys * MAX_VERTS_PER_POLY + 1] = indices[tris[j * 3 + 1]];
                        polys[npolys * MAX_VERTS_PER_POLY + 2] = indices[tris[j * 3 + 2]];
                        npolys++;
                    }
                }

                if (npolys == 0)
                    continue;

                // Merge polygons.
                int maxVertsPerPoly = MAX_VERTS_PER_POLY;
                if (maxVertsPerPoly > 3)
                {
                    for (; ; )
                    {
                        // Find best polygons to merge.
                        int bestMergeVal = 0;
                        int bestPa = 0, bestPb = 0, bestEa = 0, bestEb = 0;

                        for (int j = 0; j < npolys - 1; ++j)
                        {
                            int pj = j * MAX_VERTS_PER_POLY;

                            for (int k = j + 1; k < npolys; ++k)
                            {
                                int pk = k * MAX_VERTS_PER_POLY;

                                int ea = 0;
                                int eb = 0;
                                int v = getPolyMergeValue(ref polys, pj, ref polys, pk, mesh.verts, ref ea, ref eb);
                                if (v > bestMergeVal)
                                {
                                    bestMergeVal = v;
                                    bestPa = j;
                                    bestPb = k;
                                    bestEa = ea;
                                    bestEb = eb;
                                }
                            }
                        }

                        if (bestMergeVal > 0)
                        {
                            // Found best, merge.
                            int pa = bestPa * MAX_VERTS_PER_POLY;
                            int pb = bestPb * MAX_VERTS_PER_POLY;
                            //  mergePolys(pa, pb, bestEa, bestEb);
                            //  memcpy(pb, &polys[(npolys-1)*MAX_VERTS_PER_POLY], sizeof( short)*MAX_VERTS_PER_POLY);

                            mergePolys(ref polys, pa, polys, pb, bestEa, bestEb);

                            for (int m = 0; m < MAX_VERTS_PER_POLY; m++)
                            {
                                polys[bestPb * MAX_VERTS_PER_POLY + m] = polys[(npolys - 1) * MAX_VERTS_PER_POLY + m];
                            }

                            npolys--;
                        }
                        else
                        {
                            // Could not merge any polygons, stop.
                            break;
                        }
                    }
                }

                // Store polygons.
                for (int j = 0; j < npolys; ++j)
                {
                    // short* p = &mesh.polys[mesh.npolys*MAX_VERTS_PER_POLY*2];
                    //  short* q = &polys[j*MAX_VERTS_PER_POLY];
                    for (int k = 0; k < MAX_VERTS_PER_POLY; ++k)
                        mesh.polys[mesh.npolys * MAX_VERTS_PER_POLY * 2 + k] = polys[j * MAX_VERTS_PER_POLY + k];

                    mesh.areas[mesh.npolys] = cont.area;
                    mesh.npolys++;
                    if (mesh.npolys > maxTris)
                        return dtStatus.DT_FAILURE;
                }
            }


            // Remove edge vertices.
            for (int i = 0; i < mesh.nverts; ++i)
            {
                if (vflags[i] != 0)
                {
                    if (!canRemoveVertex(mesh, (short)i))
                        continue;
                    dtStatus status = removeVertex(mesh, (short)i, maxTris);
                    if (status != dtStatus.DT_SUCCESS)
                        return status;
                    // Remove vertex
                    // Note: mesh.nverts is already decremented inside removeVertex()!
                    for (int j = i; j < mesh.nverts; ++j)
                        vflags[j] = vflags[j + 1];
                    --i;
                }
            }

            // Calculate adjacency.
            if (!buildMeshAdjacency(mesh.polys, mesh.npolys, mesh.verts, mesh.nverts, lcset))
                return dtStatus.DT_FAILURE;

            return dtStatus.DT_SUCCESS;
        }

        public static dtStatus dtMarkCylinderArea(dtTileCacheLayer layer, int[] orig, int cs, int ch,
                                      int[] pos, int radius, int height, byte areaId)
        {
            int[] bmin = new int[3];
            int[] bmax = new int[3];

            bmin[0] = pos[0] - radius;
            bmin[1] = pos[1];
            bmin[2] = pos[2] - radius;
            bmax[0] = pos[0] + radius;
            bmax[1] = pos[1] + height;
            bmax[2] = pos[2] + radius;
            long r2 = (long)(radius * MathUtils.iPointUnit / cs  + 0) * (long)(radius * MathUtils.iPointUnit / cs + 0);

            int w = (int)layer.header.width;
            int h = (int)layer.header.height;
         //   int ics =  * MathUtils.iPointUnit / cs;
         //   int ich = *MathUtils.iPointUni / ch;

            int px = (pos[0] - orig[0]) / cs;
            int pz = (pos[2] - orig[2]) / cs;

            int minx = (bmin[0] - orig[0])  / cs;
            int miny = (bmin[1] - orig[1])  / ch;
            int minz = (bmin[2] - orig[2])  / cs;
            int maxx = (bmax[0] - orig[0])  / cs;
            int maxy = (bmax[1] - orig[1])  / ch;
            int maxz = (bmax[2] - orig[2])   / cs;

            if (maxx < 0) return dtStatus.DT_SUCCESS;
            if (minx >= w) return dtStatus.DT_SUCCESS;
            if (maxz < 0) return dtStatus.DT_SUCCESS;
            if (minz >= h) return dtStatus.DT_SUCCESS;

            if (minx < 0) minx = 0;
            if (maxx >= w) maxx = w - 1;
            if (minz < 0) minz = 0;
            if (maxz >= h) maxz = h - 1;

            for (int z = minz; z <= maxz; ++z)
            {
                for (int x = minx; x <= maxx; ++x)
                {
                    long dx = ((x + 0) - px) * MathUtils.iPointUnit;
                    long dz = ((z + 0) - pz )* MathUtils.iPointUnit; 
                    if (dx * dx + dz * dz > r2)
                        continue;

                    int y = layer.heights[x + z * w];
                    if (y < miny || y > maxy)
                        continue;

                    layer.areas[x + z * w] = areaId;
                }
            }

            return dtStatus.DT_SUCCESS;
        }

        public static dtStatus dtMarRectArea(dtTileCacheLayer layer, int[] orig, int cs, int ch,
                                      int[] pos, int radius, int height, byte areaId)
        {
            int[] bmin = new int[3];
            int[] bmax = new int[3];

            bmin[0] = pos[0] - radius;
            bmin[1] = pos[1];
            bmin[2] = pos[2] - radius;
            bmax[0] = pos[0] + radius;
            bmax[1] = pos[1] + height;
            bmax[2] = pos[2] + radius;

            int w = (int)layer.header.width;
            int h = (int)layer.header.height;

            int px = (pos[0] - orig[0]) / cs;
            int pz = (pos[2] - orig[2]) / cs;

            int minx = (bmin[0] - orig[0]) / cs;
            int miny = (bmin[1] - orig[1]) / ch;
            int minz = (bmin[2] - orig[2]) / cs;
            int maxx = (bmax[0] - orig[0]) / cs;
            int maxy = (bmax[1] - orig[1]) / ch;
            int maxz = (bmax[2] - orig[2]) / cs;

            if (maxx < 0) return dtStatus.DT_SUCCESS;
            if (minx >= w) return dtStatus.DT_SUCCESS;
            if (maxz < 0) return dtStatus.DT_SUCCESS;
            if (minz >= h) return dtStatus.DT_SUCCESS;

            if (minx < 0) minx = 0;
            if (maxx >= w) maxx = w - 1;
            if (minz < 0) minz = 0;
            if (maxz >= h) maxz = h - 1;

            long rectLength = radius * MathUtils.iPointUnit / cs;

            for (int z = minz; z <= maxz; ++z)
            {
                for (int x = minx; x <= maxx; ++x)
                {
                    long dx = ((x + 0) - px) * MathUtils.iPointUnit;
                    long dz = ((z + 0) - pz) * MathUtils.iPointUnit;

                    if (Math.Abs(dx) > rectLength || Math.Abs(dz) > rectLength)
                        continue;

                    int y = layer.heights[x + z * w];
                    if (y < miny || y > maxy)
                        continue;

                    layer.areas[x + z * w] = areaId;
                }
            }

            return dtStatus.DT_SUCCESS;
        }

        public static bool appendVertex(dtTempContour cont, int x, int y, int z, int r)
        {
            // Try to merge with existing segments.
            if (cont.nverts > 1)
            {
                //  byte* pa = cont.verts[(cont.nverts - 2) * 4];
                //  byte* pb = cont.verts[(cont.nverts - 1) * 4];
                if ((int)cont.verts[(cont.nverts - 1) * 4 +3] == r)
                {
                    if (cont.verts[(cont.nverts - 2) * 4 + 0] == cont.verts[(cont.nverts - 1) * 4 + 0] &&
                        (int)cont.verts[(cont.nverts - 1) * 4 + 0] == x)
                    {
                        // The verts are aligned aling x-axis, update z.
                        cont.verts[(cont.nverts - 1) * 4 +1] = (byte)y;
                        cont.verts[(cont.nverts - 1) * 4 + 2] = (byte)z;
                        return true;
                    }
                    else if (cont.verts[(cont.nverts - 2) * 4 + 2] == cont.verts[(cont.nverts - 1) * 4 + 2] &&
                        (int)cont.verts[(cont.nverts - 1) * 4 + 2] == z)
                    {
                        // The verts are aligned aling z-axis, update x.
                        cont.verts[(cont.nverts - 1) * 4 + 0] = (byte)x;
                        cont.verts[(cont.nverts - 1) * 4 + 1] = (byte)y;
                        return true;
                    }
                }
            }

            // Add new point.
            if (cont.nverts + 1 > cont.cverts)
                return false;

            // char* v = &cont.verts[cont.nverts * 4];
            cont.verts[cont.nverts * 4 + 0] = (byte)x;
            cont.verts[cont.nverts * 4 + 1] = (byte)y;
            cont.verts[cont.nverts * 4 + 2] = (byte)z;
            cont.verts[cont.nverts * 4 + 3] = (byte)r;
            cont.nverts++;

            return true;
        }

        public static byte getNeighbourReg(dtTileCacheLayer layer,
                                                int ax, int ay, int dir)
        {
            int w = (int)layer.header.width;
            int ia = ax + ay * w;

            byte con = (byte)(layer.cons[ia] & 0xf);
            byte portal =  (byte)(layer.cons[ia] >> 4);
            byte mask = (byte)(1 << dir);

            if ((con & mask) == 0)
            {
                // No connection, return portal or hard edge.
                if ( (portal & mask) != 0)
                    return  (byte)(0xf8 + (byte)dir);
                return 0xff;
            }

            int bx = ax + getDirOffsetX(dir);
            int by = ay + getDirOffsetY(dir);
            int ib = bx + by * w;

            return layer.regs[ib];
        }

        static bool walkContour(dtTileCacheLayer layer, int x, int y, dtTempContour cont)
        {
            int w = (int)layer.header.width;
            int h = (int)layer.header.height;

            cont.nverts = 0;

            int startX = x;
            int startY = y;
            int startDir = -1;

            for (int i = 0; i < 4; ++i)
            {
                int dir2 = (i + 3) & 3;
                byte rn = getNeighbourReg(layer, x, y, dir2);
                if (rn != layer.regs[x + y * w])
                {
                    startDir = dir2;
                    break;
                }
            }
            if (startDir == -1)
                return true;

            int dir = startDir;
            int maxIter = w * h;

            int iter = 0;
            while (iter < maxIter)
            {
                byte rn = getNeighbourReg(layer, x, y, dir);

                int nx = x;
                int ny = y;
                int ndir = dir;

                if (rn != layer.regs[x + y * w])
                {
                    // Solid edge.
                    int px = x;
                    int pz = y;
                    switch (dir)
                    {
                        case 0: pz++; break;
                        case 1: px++; pz++; break;
                        case 2: px++; break;
                    }

                    // Try to merge with previous vertex.
                    if (!appendVertex(cont, px, (int)layer.heights[x + y * w], pz, rn))
                        return false;

                    ndir = (dir + 1) & 0x3;  // Rotate CW
                }
                else
                {
                    // Move to next.
                    nx = x + getDirOffsetX(dir);
                    ny = y + getDirOffsetY(dir);
                    ndir = (dir + 3) & 0x3;	// Rotate CCW
                }

                if (iter > 0 && x == startX && y == startY && dir == startDir)
                    break;

                x = nx;
                y = ny;
                dir = ndir;

                iter++;
            }

            // Remove last vertex if it is duplicate of the first one.
            //char* pa = &cont.verts[(cont.nverts - 1) * 4];
          //  char* pb = &cont.verts[0];
            if (cont.verts[(cont.nverts - 1) * 4 + 0] == cont.verts[0 + 0] &&
                cont.verts[(cont.nverts - 1) * 4 + 2] == cont.verts[0 + 2])
                cont.nverts--;

            return true;
        }


  
        static void simplifyContour(dtTempContour cont, int maxError)
        {
            cont.npoly = 0;

            for (int i = 0; i < cont.nverts; ++i)
            {
                int j = (i + 1) % cont.nverts;
                // Check for start of a wall segment.
                byte ra = cont.verts[j * 4 + 3];
                byte rb = cont.verts[i * 4 + 3];
                if (ra != rb)
                    cont.poly[cont.npoly++] = (short)i;
            }
            if (cont.npoly < 2)
            {
                // If there is no transitions at all,
                // create some initial points for the simplification process. 
                // Find lower-left and upper-right vertices of the contour.
                int llx = cont.verts[0];
                int llz = cont.verts[2];
                int lli = 0;
                int urx = cont.verts[0];
                int urz = cont.verts[2];
                int uri = 0;
                for (int i = 1; i < cont.nverts; ++i)
                {
                    int x = cont.verts[i * 4 + 0];
                    int z = cont.verts[i * 4 + 2];
                    if (x < llx || (x == llx && z < llz))
                    {
                        llx = x;
                        llz = z;
                        lli = i;
                    }
                    if (x > urx || (x == urx && z > urz))
                    {
                        urx = x;
                        urz = z;
                        uri = i;
                    }
                }
                cont.npoly = 0;
                cont.poly[cont.npoly++] = (short)lli;
                cont.poly[cont.npoly++] = (short)uri;
            }

            // Add points until all raw points are within
            // error tolerance to the simplified shape.
            for (int i = 0; i < cont.npoly; )
            {
                int ii = (i + 1) % cont.npoly;

                int ai = (int)cont.poly[i];
                int ax = (int)cont.verts[ai * 4 + 0];
                int az = (int)cont.verts[ai * 4 + 2];

                int bi = (int)cont.poly[ii];
                int bx = (int)cont.verts[bi * 4 + 0];
                int bz = (int)cont.verts[bi * 4 + 2];

                // Find maximum deviation from the segment.
                long maxd = 0;
                int maxi = -1;
                int ci, cinc, endi;

                // Traverse the segment in lexilogical order so that the
                // max deviation is calculated similarly when traversing
                // opposite segments.
                if (bx > ax || (bx == ax && bz > az))
                {
                    cinc = 1;
                    ci = (ai + cinc) % cont.nverts;
                    endi = bi;
                }
                else
                {
                    cinc = cont.nverts - 1;
                    ci = (bi + cinc) % cont.nverts;
                    endi = ai;
                }

                // Tessellate only outer edges or edges between areas.
                while (ci != endi)
                {
                    long d = NavMeshMath.dtDistancePtSegSqr2D(cont.verts[ci * 4 + 0], cont.verts[ci * 4 + 2], ax, az, bx, bz);
                    if (d > maxd)
                    {
                        maxd = d;
                        maxi = ci;
                    }
                    ci = (ci + cinc) % cont.nverts;
                }


                // If the max deviation is larger than accepted error,
                // add new point, else continue to next segment.
                if (maxi != -1 && maxd * MathUtils.iPointUnit > ((long)maxError * (long)maxError) / MathUtils.iPointUnit)
                {
                    cont.npoly++;
                    for (int j = cont.npoly - 1; j > i; --j)
                        cont.poly[j] = cont.poly[j - 1];
                    cont.poly[i + 1] = (short)maxi;
                }
                else
                {
                    ++i;
                }
            }

            // Remap vertices
            //TODO 修改了嵌套
            int start = 0;
            for (int i = 1; i < cont.npoly; ++i)
            {
                if (cont.poly[i] < cont.poly[start])
                    start = i; 
            }

            cont.nverts = 0;
            for (int i = 0; i < cont.npoly; ++i)
            {
                int j = (start + i) % cont.npoly;
              //  byte* src = &cont.verts[cont.poly[j] * 4];
               // byte* dst = &cont.verts[cont.nverts * 4];
                cont.verts[cont.nverts * 4 + 0] = cont.verts[cont.poly[j] * 4 + 0];
                cont.verts[cont.nverts * 4 + 1] = cont.verts[cont.poly[j] * 4 + 1];
                cont.verts[cont.nverts * 4 + 2] = cont.verts[cont.poly[j] * 4 + 2];
                cont.verts[cont.nverts * 4 + 3] = cont.verts[cont.poly[j] * 4 + 3];


                cont.nverts++;
            }
        }

    

        static byte getCornerHeight(dtTileCacheLayer layer,
                                              int x, int y, int z,
                                              int walkableClimb,
                                             ref bool shouldRemove)
        {
            int w = (int)layer.header.width;
            int h = (int)layer.header.height;

            int n = 0;

            byte portal = 0xf;
            byte height = 0;
            byte preg = 0xff;
            bool allSameReg = true;

            for (int dz = -1; dz <= 0; ++dz)
            {
                for (int dx = -1; dx <= 0; ++dx)
                {
                    int px = x + dx;
                    int pz = z + dz;
                    if (px >= 0 && pz >= 0 && px < w && pz < h)
                    {
                        int idx = px + pz * w;
                        int lh = (int)layer.heights[idx];
                        if (NavMeshMath.Abs(lh - y) <= walkableClimb && layer.areas[idx] != NavMeshBuilderDefine.DT_TILECACHE_NULL_AREA)
                        {
                            height = NavMeshMath.Max(height, (byte)lh);
                            portal &= (byte)(layer.cons[idx] >> 4);
                            if (preg != 0xff && preg != layer.regs[idx])
                                allSameReg = false;
                            preg = layer.regs[idx];
                            n++;
                        }
                    }
                }
            }

            int portalCount = 0;
            for (int dir = 0; dir < 4; ++dir)
                if ( (byte)(portal & (1 << dir)) != 0)
                    portalCount++;

            shouldRemove = false;
            if (n > 1 && portalCount == 1 && allSameReg)
            {
                shouldRemove = true;
            }

            return height;
        }

        static int computeVertexHash2(int x, int y, int z)
        {
            uint h1 = 0x8da6b343; // Large multiplicative constants;
            uint h2 = 0xd8163841; // here arbitrarily chosen primes
            uint h3 = 0xcb1ab31f;
            long n = h1 * x + h2 * y + h3 * z;
            return (int)(n & (VERTEX_BUCKET_COUNT2 - 1));
        }

        static ushort addVertex(ushort x, ushort y, ushort z,
                                         ref ushort[] verts, ref ushort[] firstVert, ref ushort[] nextVert, ref int nv)
        {
            int bucket = computeVertexHash2(x, 0, z);
            ushort i = firstVert[bucket];

            while (i != NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX)
            {
               // short* v = &verts[i * 3];
                if (verts[i * 3 + 0] == x && verts[i * 3 + 2] == z && (NavMeshMath.Abs(verts[i * 3 + 1] - y) <= 2))
                    return i;
                i = nextVert[i]; // next
            }

            // Could not find, create new.
            i = (ushort)nv; nv++;
           // short* v = &verts[i * 3];
            verts[i * 3+0] = x;
            verts[i * 3+1] = y;
            verts[i * 3+2] = z;
            nextVert[i] = firstVert[bucket];
            firstVert[bucket] = i;

            return (ushort)i;
        }


        public class rcEdge
        {
            public ushort[] vert = new ushort[2];
            public ushort[] polyEdge = new ushort[2];
            public ushort[] poly = new ushort[2];
        };


        static bool buildMeshAdjacency(
                                        ushort[] polys, int npolys,
                                        ushort[] verts, int nverts,
                                        dtTileCacheContourSet lcset)
    {
	    // Based on code by Eric Lengyel from:
	    // http://www.terathon.com/code/edges.php

        int maxEdgeCount = npolys * MAX_VERTS_PER_POLY;
         ushort[] firstEdge = new ushort[nverts + maxEdgeCount]; 


	  //   short* nextEdge = firstEdge + nverts;
	    int edgeCount = 0;
	

        rcEdge[]  edges = new rcEdge[maxEdgeCount];
        for (int i = 0; i < edges.Length; i++ )
        {
            edges[i] = new rcEdge();
        }


            for (int i = 0; i < nverts; i++)
                firstEdge[i] = NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX;
	
	    for (int i = 0; i < npolys; ++i)
	    {
		  //   short* t = &polys[i*MAX_VERTS_PER_POLY*2];
		    for (int j = 0; j < MAX_VERTS_PER_POLY; ++j)
		    {
                if (polys[i * MAX_VERTS_PER_POLY * 2 + j] == NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX) break;
                ushort v0 = polys[i * MAX_VERTS_PER_POLY * 2 + j];
                ushort v1 = (j + 1 >= MAX_VERTS_PER_POLY || polys[i * MAX_VERTS_PER_POLY * 2 + j + 1] == NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX) ?
                    polys[i * MAX_VERTS_PER_POLY * 2 + 0] : polys[i * MAX_VERTS_PER_POLY * 2 + j + 1];
			    if (v0 < v1)
			    {
				    rcEdge edge = edges[edgeCount];
				    edge.vert[0] = v0;
				    edge.vert[1] = v1;
                    edge.poly[0] = (ushort)i;
                    edge.polyEdge[0] = (ushort)j;
                    edge.poly[1] = (ushort)i;
				    edge.polyEdge[1] = 0xff;
				    // Insert edge
				   // nextEdge[edgeCount] = firstEdge[v0];
                    firstEdge[nverts + edgeCount] = firstEdge[v0];

                    firstEdge[v0] = (ushort)edgeCount;
				    edgeCount++;
			    }
		    }
	    }
	
	    for (int i = 0; i < npolys; ++i)
	    {
		   //  short* t = &polys[i*MAX_VERTS_PER_POLY*2];
		    for (int j = 0; j < MAX_VERTS_PER_POLY; ++j)
		    {
                if (polys[i * MAX_VERTS_PER_POLY * 2 + j] == NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX) break;
                ushort v0 = polys[i * MAX_VERTS_PER_POLY * 2 + j];
                ushort v1 = (j + 1 >= MAX_VERTS_PER_POLY || polys[i * MAX_VERTS_PER_POLY * 2 + j + 1] == NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX) ?
                    polys[i * MAX_VERTS_PER_POLY * 2 + 0] : polys[i * MAX_VERTS_PER_POLY * 2 + j + 1];
			    if (v0 > v1)
			    {
				    bool found = false;
                    for (ushort e = firstEdge[v1]; e != NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX; e = firstEdge[nverts + e])
				    {
					    rcEdge edge = edges[e];
					    if (edge.vert[1] == v0 && edge.poly[0] == edge.poly[1])
					    {
                            edge.poly[1] = (ushort)i;
                            edge.polyEdge[1] = (ushort)j;
						    found = true;
						    break;
					    }
				    }
				    if (!found)
				    {
					    // Matching edge not found, it is an open edge, add it.
					    rcEdge edge = edges[edgeCount];
					    edge.vert[0] = v1;
					    edge.vert[1] = v0;
                        edge.poly[0] = (ushort)i;
                        edge.polyEdge[0] = (ushort)j;
                        edge.poly[1] = (ushort)i;
					    edge.polyEdge[1] = 0xff;
					    // Insert edge
					    firstEdge[nverts  + edgeCount] = firstEdge[v1];
                        firstEdge[v1] = (ushort)edgeCount;
					    edgeCount++;
				    }
			    }
		    }
	    }
	
	    // Mark portal edges.
	    for (int i = 0; i < lcset.nconts; ++i)
	    {
		    dtTileCacheContour cont = lcset.conts[i];
		    if (cont.nverts < 3)
			    continue;
		
		    for (int j = 0, k = cont.nverts-1; j < cont.nverts; k=j++)
		    {
			     // char* va = &cont.verts[k*4];
			   //   char* vb = &cont.verts[j*4];
                byte dir = (byte)(cont.verts[k * 4 + 3] & 0xf);
			    if (dir == 0xf)
				    continue;
			
			    if (dir == 0 || dir == 2)
			    {
				    // Find matching vertical edge
                     short x = (short)cont.verts[k * 4 + 0];
                     ushort zmin = (ushort)cont.verts[k * 4 + 2];
                     ushort zmax = (ushort)cont.verts[j * 4 + 2];
				    if (zmin > zmax)
                        NavMeshMath.Swap(ref zmin, ref zmax);
				
				    for (int m = 0; m < edgeCount; ++m)
				    {
					    rcEdge e = edges[m];
					    // Skip connected edges.
					    if (e.poly[0] != e.poly[1])
						    continue;
					    //  short* eva = &verts[e.vert[0]*3];
					     // short* evb = &verts[e.vert[1]*3];
                          if (verts[e.vert[0] * 3 + 0] == x && verts[e.vert[1] * 3 + 0] == x)
					    {
                            ushort ezmin = verts[e.vert[0] * 3 + 2];
                            ushort ezmax = verts[e.vert[1] * 3 + 2];
						    if (ezmin > ezmax)
                                NavMeshMath.Swap(ref ezmin, ref ezmax);

						    if (overlapRangeExl(zmin,zmax, ezmin, ezmax))
						    {
							    // Reuse the other polyedge to store dir.
							    e.polyEdge[1] = dir;
						    }
					    }
				    }
			    }
			    else
			    {
				    // Find matching vertical edge
                    ushort z = (ushort)cont.verts[k * 4 + 2];
                    ushort xmin = (ushort)cont.verts[k * 4 + 0];
                    ushort xmax = (ushort)cont.verts[j * 4 + 0];
				    if (xmin > xmax)
                        NavMeshMath.Swap(ref xmin, ref xmax);

				    for (int m = 0; m < edgeCount; ++m)
				    {
					    rcEdge e = edges[m];
					    // Skip connected edges.
					    if (e.poly[0] != e.poly[1])
						    continue;
					    //  short* eva = &verts[e.vert[0]*3];
					    //  short* evb = &verts[e.vert[1]*3];
                        if (verts[e.vert[0] * 3 + 2] == z && verts[e.vert[1] * 3 + 2] == z)
					    {
                            ushort exmin = verts[e.vert[0] * 3 + 0];
                            ushort exmax = verts[e.vert[1] * 3 + 0];
						    if (exmin > exmax)
                                NavMeshMath.Swap(ref exmin, ref exmax);

						    if (overlapRangeExl(xmin,xmax, exmin, exmax))
						    {
							    // Reuse the other polyedge to store dir.
							    e.polyEdge[1] = dir;
						    }
					    }
				    }
			    }
		    }
	    }
	
	
	    // Store adjacency
	    for (int i = 0; i < edgeCount; ++i)
	    {
		     rcEdge e = edges[i];
		    if (e.poly[0] != e.poly[1])
		    {
			     //short* p0 = &polys[e.poly[0]*MAX_VERTS_PER_POLY*2];
			     //short* p1 = &polys[e.poly[1]*MAX_VERTS_PER_POLY*2];
                 polys[e.poly[0] * MAX_VERTS_PER_POLY * 2 + MAX_VERTS_PER_POLY + e.polyEdge[0]] = e.poly[1];
                 polys[e.poly[1] * MAX_VERTS_PER_POLY * 2 + MAX_VERTS_PER_POLY + e.polyEdge[1]] = e.poly[0];
		    }
		    else if (e.polyEdge[1] != 0xff)
		    {
			     //short* p0 = &polys[e.poly[0]*MAX_VERTS_PER_POLY*2];
			    polys[e.poly[0]*MAX_VERTS_PER_POLY*2 + MAX_VERTS_PER_POLY + e.polyEdge[0]] =
                   (ushort)(0x8000 | (ushort)e.polyEdge[1]);
		    }
		
	    }
	
	    return true;
    }


    

        //	Exclusive or: true iff exactly one argument is true.
        //	The arguments are negated to ensure that they are 0/1
        //	values.  Then the bitwise Xor operator may apply.
        //	(This idea is due to Michael Baldwin.)
        static bool xorb(bool x, bool y)
        {
            return !x ^ !y;
        }

        // Last time I checked the if version got compiled using cmov, which was a lot faster than module (with idiv).
        static int prev(int i, int n) { return i - 1 >= 0 ? i - 1 : n - 1; }
        static int next(int i, int n) { return i + 1 < n ? i + 1 : 0; }

        static int area2(byte[] a, byte[] b, byte[] c, int start_a, int start_b, int start_c)
        {
            return ((int)b[start_b + 0] - (int)a[start_a + 0]) * ((int)c[start_c + 2] - (int)a[start_a + 2]) -
                ((int)c[start_c + 0] - (int)a[start_a + 0]) * ((int)b[start_b + 2] - (int)a[start_a + 2]);
        }

        // Returns true iff c is strictly to the left of the directed
        // line through a to b.
        static bool left(byte[] a, byte[] b, byte[] c, int start_a, int start_b, int start_c)
        {
            return area2(a, b, c, start_a, start_b, start_c) < 0;
        }

        static bool leftOn(byte[] a, byte[] b, byte[] c, int start_a, int start_b, int start_c)
        {
            return area2(a, b, c, start_a, start_b, start_c) <= 0;
        }

        static bool collinear(byte[] a, byte[] b, byte[] c, int start_a, int start_b, int start_c)
        {
            return area2(a, b, c, start_a, start_b, start_c) == 0;
        }

        //	Returns true iff ab properly intersects cd: they share
        //	a point interior to both segments.  The properness of the
        //	intersection is ensured by using strict leftness.
        static bool intersectProp(byte[] a, byte[] b,
                                    byte[] c, byte[] d, int start_a, int start_b, int start_c, int start_d)
        {
            // Eliminate improper cases.
            if (collinear(a, b, c, start_a, start_b, start_c) || collinear(a, b, d, start_a, start_b, start_d) ||
                collinear(c, d, a, start_c, start_d, start_a) || collinear(c, d, b, start_c, start_d, start_b))
                return false;

            return xorb(left(a, b, c, start_a, start_b, start_c), left(a, b, d, start_a, start_b, start_d)) &&
                xorb(left(c, d, a, start_c, start_d, start_a), left(c, d, b, start_c, start_d, start_b));
        }

        // Returns T iff (a,b,c) are collinear and point c lies 
        // on the closed segement ab.
        static bool between(byte[] a, byte[] b, byte[] c, int start_a, int start_b, int start_c)
        {
            if (!collinear(a, b, c, start_a, start_b, start_c))
                return false;
            // If ab not vertical, check betweenness on x; else on y.
            if (a[start_a + 0] != b[start_b + 0])
                return ((a[start_a + 0] <= c[start_c + 0]) && (c[start_c + 0] <= b[start_b + 0])) ||
                    ((a[start_a + 0] >= c[start_c + 0]) && (c[start_c + 0] >= b[start_b + 0]));
            else
                return ((a[start_a + 2] <= c[start_c + 2]) && (c[start_c + 2] <= b[start_b + 2])) ||
                    ((a[start_a + 2] >= c[start_c + 2]) && (c[start_c + 2] >= b[start_b + 2]));
        }

        // Returns true iff segments ab and cd intersect, properly or improperly.
        static bool intersect(byte[] a, byte[] b, byte[] c, byte[] d, int start_a, int start_b, int start_c, int startd)
        {
            if (intersectProp(a, b, c, d, start_a,  start_b,  start_c,  startd))
                return true;
            else if (between(a, b, c, start_a, start_b, start_c) || between(a, b, d, start_a, start_b, startd) ||
                     between(c, d, a, start_c, startd, start_a) || between(c, d, b, start_c, startd, start_b))
                return true;
            else
                return false;
        }

        static bool vequal(byte[] a, byte[] b, int start_a, int start_b)
        {
            return a[start_a + 0] == b[start_b + 0] && a[start_a + 2] == b[start_b +2];
        }

        // Returns T iff (v_i, v_j) is a proper internal *or* external
        // diagonal of P, *ignoring edges incident to v_i and v_j*.
        static bool diagonalie(int i, int j, int n, byte[] verts, ushort[] indices)
        {
           // char* d0 = &verts[(indices[i] & 0x7fff) * 4];
          //   char* d1 = &verts[(indices[j] & 0x7fff) * 4];

            // For each edge (k,k+1) of P
            for (int k = 0; k < n; k++)
            {
                int k1 = next(k, n);
                // Skip edges incident to i or j
                if (!((k == i) || (k1 == i) || (k == j) || (k1 == j)))
                {
                 //   const char* p0 = &verts[(indices[k] & 0x7fff) * 4];
                //    const char* p1 = &verts[(indices[k1] & 0x7fff) * 4];

                    if (vequal(verts, verts, (indices[i] & 0x7fff) * 4, (indices[k] & 0x7fff) * 4) ||
                        vequal(verts, verts, (indices[j] & 0x7fff) * 4, (indices[k] & 0x7fff) * 4) ||
                        vequal(verts, verts, (indices[i] & 0x7fff) * 4, (indices[k1] & 0x7fff) * 4) ||
                        vequal(verts, verts, (indices[j] & 0x7fff) * 4, (indices[k1] & 0x7fff) * 4))
                        continue;

                    if (intersect(verts, verts, verts, verts, (indices[i] & 0x7fff) * 4, (indices[j] & 0x7fff) * 4, 
                        (indices[k] & 0x7fff) * 4, (indices[k1] & 0x7fff) * 4))
                        return false;
                }
            }
            return true;
        }

        // Returns true iff the diagonal (i,j) is strictly internal to the 
        // polygon P in the neighborhood of the i endpoint.
        static bool inCone(int i, int j, int n, byte[] verts, ushort[] indices)
        {
           int pi = (indices[i] & 0x7fff) * 4;
           int pj = (indices[j] & 0x7fff) * 4;
           int pi1 = (indices[next(i, n)] & 0x7fff) * 4;
           int pin1 = (indices[prev(i, n)] & 0x7fff) * 4;

            // If P[i] is a convex vertex [ i+1 left or on (i-1,i) ].
           if (leftOn(verts, verts, verts, pin1, pi, pi1))
               return left(verts, verts, verts, pi, pj, pin1) && left(verts, verts, verts, pj, pi, pi1);
            // Assume (i-1,i,i+1) not collinear.
            // else P[i] is reflex.
           return !(leftOn(verts, verts, verts, pi, pj, pi1) && leftOn(verts, verts, verts, pj, pi, pin1));
        }

        // Returns T iff (v_i, v_j) is a proper internal
        // diagonal of P.
        static bool diagonal(int i, int j, int n, byte[] verts, ushort[] indices)
        {
            return inCone(i, j, n, verts, indices) && diagonalie(i, j, n, verts, indices);
        }

        static int triangulate(int n, byte[] verts, ref ushort[] indices, ref ushort[] tris)
        {
            int ntris = 0;
         //   short* dst = tris;
            int dst_index = 0;

            // The last bit of the index is used to indicate if the vertex can be removed.
            for (int i = 0; i < n; i++)
            {
                int i1 = next(i, n);
                int i2 = next(i1, n);
                if (diagonal(i, i2, n, verts, indices))
                    indices[i1] = (ushort)(indices[i1] | 0x8000);
            }

            while (n > 3)
            {
                int minLen = -1;
                int mini = -1;
                for (int i = 0; i < n; i++)
                {
                    int _i1 = next(i, n);
                    if ((indices[_i1] & 0x8000) != 0)
                    {
                      //   char* p0 = &verts[(indices[i] & 0x7fff) * 4];
                       //  char* p2 = &verts[(indices[next(i1, n)] & 0x7fff) * 4];

                        int dx = (int)verts[(indices[next(_i1, n)] & 0x7fff) * 4 + 0] - (int)verts[(indices[i] & 0x7fff) * 4 + 0];
                        int dz = (int)verts[(indices[next(_i1, n)] & 0x7fff) * 4 + 2] - (int)verts[(indices[i] & 0x7fff) * 4 + 2];
                         int len = dx * dx + dz * dz;
                        if (minLen < 0 || len < minLen)
                        {
                            minLen = len;
                            mini = i;
                        }
                    }
                }

                if (mini == -1)
                {
                    // Should not happen.
                    /*			printf("mini == -1 ntris=%d n=%d\n", ntris, n);
                     for (int i = 0; i < n; i++)
                     {
                     printf("%d ", indices[i] & 0x0fffffff);
                     }
                     printf("\n");*/
                    return -ntris;
                }

                int i0 = mini;
                int i1 = next(i0, n);
                int i2 = next(i1, n);

                tris[dst_index++] = (ushort)(indices[i0] & 0x7fff);
                tris[dst_index++] = (ushort)(indices[i1] & 0x7fff);
                tris[dst_index++] = (ushort)(indices[i2] & 0x7fff);
                ntris++;

                // Removes P[i1] by copying P[i+1]...P[n-1] left one index.
                n--;
                for (int k = i1; k < n; k++)
                    indices[k] = indices[k + 1];

                if (i1 >= n) i1 = 0;
                i0 = prev(i1, n);
                // Update diagonal flags.
                if (diagonal(prev(i0, n), i1, n, verts, indices))
                    indices[i0] = (ushort)(indices[i0] | 0x8000);
                else
                    indices[i0] &= 0x7fff;

                if (diagonal(i0, next(i1, n), n, verts, indices))
                    indices[i1] = (ushort)(indices[i1] | 0x8000);
                else
                    indices[i1] &= 0x7fff;
            }

            // Append the remaining triangle.
            tris[dst_index++] = (ushort)(indices[0] & 0x7fff);
            tris[dst_index++] = (ushort)(indices[1] & 0x7fff);
            tris[dst_index++] = (ushort)(indices[2] & 0x7fff);
            ntris++;

            return ntris;
        }


        static int countPolyVerts(ushort[] p, int start)
        {
            for (int i = 0; i < MAX_VERTS_PER_POLY; ++i)
                if (p[start + i] == NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX)
                    return i;
            return MAX_VERTS_PER_POLY;
        }

        static bool uleft(ushort[] a, int index_a, ushort[] b, int index_b, ushort[] c, int index_c)
        {
            return ((int)b[index_b + 0] - (int)a[index_a + 0]) * ((int)c[index_c + 2] - (int)a[index_a + 2]) -
            ((int)c[index_c + 0] - (int)a[index_a + 0]) * ((int)b[index_b + 2] - (int)a[index_a + 2]) < 0;
        }

        static int getPolyMergeValue(ref ushort[] pa, int start_pa, ref ushort[] pb, int start_pb,
                                       ushort[] verts, ref int ea, ref int eb)
        {
            int na = countPolyVerts(pa, start_pa);
            int nb = countPolyVerts(pb, start_pb);

            // If the merged polygon would be too big, do not merge.
            if (na + nb - 2 > MAX_VERTS_PER_POLY)
                return -1;

            // Check if the polygons share an edge.
            ea = -1;
            eb = -1;

            for (int i = 0; i < na; ++i)
            {
                ushort va0 = pa[start_pa + i];
                ushort va1 = pa[start_pa + (i + 1) % na];
                if (va0 > va1)
                    NavMeshMath.Swap(ref va0, ref va1);

                for (int j = 0; j < nb; ++j)
                {
                    ushort vb0 = pb[start_pb + j];
                    ushort vb1 = pb[start_pb + (j + 1) % nb];
                    if (vb0 > vb1)
                        NavMeshMath.Swap(ref vb0, ref  vb1);

                    if (va0 == vb0 && va1 == vb1)
                    {
                        ea = i;
                        eb = j;
                        break;
                    }
                }
            }

            // No common edge, cannot merge.
            if (ea == -1 || eb == -1)
                return -1;

            // Check to see if the merged polygon would be convex.
            ushort va, vb, vc;

            va = pa[start_pa + (ea + na - 1) % na];
            vb = pa[start_pa + ea];
            vc = pb[start_pb + (eb + 2) % nb];
            if (!uleft(verts, va * 3, verts, vb * 3, verts, vc * 3))
                return -1;

            va = pb[start_pb + (eb + nb - 1) % nb];
            vb = pb[start_pb+ eb];
            vc = pa[start_pa + (ea + 2) % na];
            if (!uleft(verts, va * 3, verts, vb * 3, verts, vc * 3))
                return -1;

            va = pa[start_pa + ea];
            vb = pa[start_pa + (ea + 1) % na];

            int dx = (int)verts[va * 3 + 0] - (int)verts[vb * 3 + 0];
            int dy = (int)verts[va * 3 + 2] - (int)verts[vb * 3 + 2];

            return dx * dx + dy * dy;
        }

        static void mergePolys(ref ushort[] pa, int index_pa, ushort[] pb, int index_pb, int ea, int eb)
        {
            ushort[] tmp = new ushort[MAX_VERTS_PER_POLY * 2];

            int na = countPolyVerts(pa, index_pa);
            int nb = countPolyVerts(pb, index_pb);

            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX;
            }

            int n = 0;
            // Add pa
            for (int i = 0; i < na - 1; ++i)
                tmp[n++] = pa[index_pa + (ea + 1 + i) % na];
            // Add pb
            for (int i = 0; i < nb - 1; ++i)
                tmp[n++] = pb[index_pb + (eb + 1 + i) % nb];

            for (int i = 0; i < MAX_VERTS_PER_POLY; i++)
            {
                pa[index_pa + i] = tmp[i];
            }
        }


        static void pushFront(ushort v, ref ushort[] arr, ref int an)
        {
            an++;
            for (int i = an - 1; i > 0; --i)
                arr[i] = arr[i - 1];
            arr[0] = v;
        }

        static void pushBack(ushort v, ref ushort[] arr, ref int an)
        {
            arr[an] = v;
            an++;
        }

        static bool canRemoveVertex(dtTileCachePolyMesh mesh, short rem)
        {
            // Count number of polygons to remove.
            int numRemovedVerts = 0;
            int numTouchedVerts = 0;
            int numRemainingEdges = 0;
            for (int i = 0; i < mesh.npolys; ++i)
            {
               // short* p = &mesh.polys[i * MAX_VERTS_PER_POLY * 2];
                int nv = countPolyVerts(mesh.polys, i * MAX_VERTS_PER_POLY * 2);
                int numRemoved = 0;
                int numVerts = 0;
                for (int j = 0; j < nv; ++j)
                {
                    if (mesh.polys[i * MAX_VERTS_PER_POLY * 2 + j] == rem)
                    {
                        numTouchedVerts++;
                        numRemoved++;
                    }
                    numVerts++;
                }

                if (numRemoved != 0)
                {
                    numRemovedVerts += numRemoved;
                    numRemainingEdges += numVerts - (numRemoved + 1);
                }
            }

            // There would be too few edges remaining to create a polygon.
            // This can happen for example when a tip of a triangle is marked
            // as deletion, but there are no other polys that share the vertex.
            // In this case, the vertex should not be removed.
            if (numRemainingEdges <= 2)
                return false;

            // Check that there is enough memory for the test.
             int maxEdges = numTouchedVerts * 2;
            if (maxEdges > MAX_REM_EDGES)
                return false;

            // Find edges which share the removed vertex.
            short[] edges = new short[MAX_REM_EDGES];
            int nedges = 0;

            for (int i = 0; i < mesh.npolys; ++i)
            {
              //  short* p = &mesh.polys[i * MAX_VERTS_PER_POLY * 2];
                int nv = countPolyVerts(mesh.polys, i * MAX_VERTS_PER_POLY * 2);

                // Collect edges which touches the removed vertex.
                for (int j = 0, k = nv - 1; j < nv; k = j++)
                {
                    if (mesh.polys[i * MAX_VERTS_PER_POLY * 2 + j] == rem || 
                        mesh.polys[i * MAX_VERTS_PER_POLY * 2 + k] == rem)
                    {
                        // Arrange edge so that a=rem.
                        int a = mesh.polys[i * MAX_VERTS_PER_POLY * 2 + j], 
                            b = mesh.polys[i * MAX_VERTS_PER_POLY * 2 + k];
                        if (b == rem)
                            NavMeshMath.Swap(ref a, ref b);

                        // Check if the edge exists
                        bool exists = false;
                        for (int m = 0; m < nedges; ++m)
                        {
                           // short* e = &edges[m * 3];
                            if (edges[m * 3 + 1] == b)
                            {
                                // Exists, increment vertex share count.
                                edges[m * 3 + 2]++;
                                exists = true;
                            }
                        }
                        // Add new edge.
                        if (!exists)
                        {
                            //short* e = &edges[nedges * 3];
                            edges[nedges * 3 + 0] = (short)a;
                            edges[nedges * 3 + 1] = (short)b;
                            edges[nedges * 3 + 2] = 1;
                            nedges++;
                        }
                    }
                }
            }

            // There should be no more than 2 open edges.
            // This catches the case that two non-adjacent polygons
            // share the removed vertex. In that case, do not remove the vertex.
            int numOpenEdges = 0;
            for (int i = 0; i < nedges; ++i)
            {
                if (edges[i * 3 + 2] < 2)
                    numOpenEdges++;
            }
            if (numOpenEdges > 2)
                return false;

            return true;
        }

        static dtStatus removeVertex(dtTileCachePolyMesh mesh, short rem, int maxTris)
        {
            // Count number of polygons to remove.
            int numRemovedVerts = 0;
            for (int i = 0; i < mesh.npolys; ++i)
            {
                //short* p = &mesh.polys[i * MAX_VERTS_PER_POLY * 2];
                int nv = countPolyVerts(mesh.polys, i * MAX_VERTS_PER_POLY * 2);
                for (int j = 0; j < nv; ++j)
                {
                    if (mesh.polys[i * MAX_VERTS_PER_POLY * 2 + j] == rem)
                        numRemovedVerts++;
                }
            }

            int nedges = 0;
            ushort[] edges = new ushort[MAX_REM_EDGES * 3];
            int nhole = 0;
            ushort[] hole = new ushort[MAX_REM_EDGES];
            int nharea = 0;
            ushort[] harea = new ushort[MAX_REM_EDGES];

            for (int i = 0; i < mesh.npolys; ++i)
            {
              //  short* p = &mesh.polys[i * MAX_VERTS_PER_POLY * 2];
                int nv = countPolyVerts(mesh.polys, i * MAX_VERTS_PER_POLY * 2);
                bool hasRem = false;
                for (int j = 0; j < nv; ++j)
                    if (mesh.polys[i * MAX_VERTS_PER_POLY * 2 + j] == rem) hasRem = true;
                if (hasRem)
                {
                    // Collect edges which does not touch the removed vertex.
                    for (int j = 0, k = nv - 1; j < nv; k = j++)
                    {
                        if (mesh.polys[i * MAX_VERTS_PER_POLY * 2 + j] != rem && 
                            mesh.polys[i * MAX_VERTS_PER_POLY * 2 + k] != rem)
                        {
                            if (nedges >= MAX_REM_EDGES)
                                return dtStatus.DT_FAILURE;
                            //short* e = &edges[nedges * 3];
                            edges[nedges * 3 + 0] = mesh.polys[i * MAX_VERTS_PER_POLY * 2 + k];
                            edges[nedges * 3 + 1] = mesh.polys[i * MAX_VERTS_PER_POLY * 2 + j];
                            edges[nedges * 3 + 2] = mesh.areas[i];
                            nedges++;
                        }
                    }
                    // Remove the polygon.
                 //   short* p2 = &mesh.polys[(mesh.npolys - 1) * MAX_VERTS_PER_POLY * 2];
                 //   memcpy(p, p2, sizeof(short) * MAX_VERTS_PER_POLY);

                    for (int m = 0; m< MAX_VERTS_PER_POLY; m++ )
                    {
                        mesh.polys[i * MAX_VERTS_PER_POLY * 2 + m] = mesh.polys[(mesh.npolys - 1) * MAX_VERTS_PER_POLY * 2 + m];
                    }

                   // memset(p + MAX_VERTS_PER_POLY, 0xff, sizeof(short) * MAX_VERTS_PER_POLY);

                    for (int m = 0; m < MAX_VERTS_PER_POLY; m++)
                    {
                        mesh.polys[i * MAX_VERTS_PER_POLY * 2 + MAX_VERTS_PER_POLY + m] = NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX;
                    }
                     


                    mesh.areas[i] = mesh.areas[mesh.npolys - 1];
                    mesh.npolys--;
                    --i;
                }
            }

            // Remove vertex.
            for (int i = (int)rem; i < mesh.nverts; ++i)
            {
                mesh.verts[i * 3 + 0] = mesh.verts[(i + 1) * 3 + 0];
                mesh.verts[i * 3 + 1] = mesh.verts[(i + 1) * 3 + 1];
                mesh.verts[i * 3 + 2] = mesh.verts[(i + 1) * 3 + 2];
            }
            mesh.nverts--;

            // Adjust indices to match the removed vertex layout.
            for (int i = 0; i < mesh.npolys; ++i)
            {
                //short* p = &mesh.polys[i * MAX_VERTS_PER_POLY * 2];
                int nv = countPolyVerts(mesh.polys, i * MAX_VERTS_PER_POLY * 2);
                for (int j = 0; j < nv; ++j)
                    if (mesh.polys[i * MAX_VERTS_PER_POLY * 2 + j] > rem) 
                        mesh.polys[i * MAX_VERTS_PER_POLY * 2 + j]--;
            }
            for (int i = 0; i < nedges; ++i)
            {
                if (edges[i * 3 + 0] > rem) edges[i * 3 + 0]--;
                if (edges[i * 3 + 1] > rem) edges[i * 3 + 1]--;
            }

            if (nedges == 0)
                return dtStatus.DT_SUCCESS;

            // Start with one vertex, keep appending connected
            // segments to the start and end of the hole.
            pushBack(edges[0], ref hole, ref nhole);
            pushBack(edges[2], ref harea, ref nharea);

            while (nedges != 0)
            {
                bool match = false;

                for (int i = 0; i < nedges; ++i)
                {
                    ushort ea = edges[i * 3 + 0];
                    ushort eb = edges[i * 3 + 1];
                    ushort a = edges[i * 3 + 2];
                    bool add = false;
                    if (hole[0] == eb)
                    {
                        // The segment matches the beginning of the hole boundary.
                        if (nhole >= MAX_REM_EDGES)
                            return dtStatus.DT_FAILURE;
                        pushFront(ea, ref hole, ref nhole);
                        pushFront(a, ref harea, ref nharea);
                        add = true;
                    }
                    else if (hole[nhole - 1] == ea)
                    {
                        // The segment matches the end of the hole boundary.
                        if (nhole >= MAX_REM_EDGES)
                            return dtStatus.DT_FAILURE;
                        pushBack(eb, ref hole, ref nhole);
                        pushBack(a, ref harea, ref nharea);
                        add = true;
                    }
                    if (add)
                    {
                        // The edge segment was added, remove it.
                        edges[i * 3 + 0] = edges[(nedges - 1) * 3 + 0];
                        edges[i * 3 + 1] = edges[(nedges - 1) * 3 + 1];
                        edges[i * 3 + 2] = edges[(nedges - 1) * 3 + 2];
                        --nedges;
                        match = true;
                        --i;
                    }
                }

                if (!match)
                    break;
            }


            ushort[] tris = new ushort[MAX_REM_EDGES * 3];
            byte[] tverts = new byte[MAX_REM_EDGES * 3];
            ushort[] tpoly = new ushort[MAX_REM_EDGES * 3];

            // Generate temp vertex array for triangulation.
            for (int i = 0; i < nhole; ++i)
            {
                ushort pi = hole[i];
                tverts[i * 4 + 0] = (byte)mesh.verts[pi * 3 + 0];
                tverts[i * 4 + 1] = (byte)mesh.verts[pi * 3 + 1];
                tverts[i * 4 + 2] = (byte)mesh.verts[pi * 3 + 2];
                tverts[i * 4 + 3] = 0;
                tpoly[i] = (ushort)i;
            }

            // Triangulate the hole.
            int ntris = triangulate(nhole, tverts, ref tpoly, ref tris);
            if (ntris < 0)
            {
                // TODO: issue warning!
                ntris = -ntris;
            }

            if (ntris > MAX_REM_EDGES)
                return dtStatus.DT_FAILURE;

            ushort[] polys = new ushort[MAX_REM_EDGES * MAX_VERTS_PER_POLY];
            byte[] pareas = new byte[MAX_REM_EDGES];

            // Build initial polygons.
            int npolys = 0;
            for (int i = 0; i < polys.Length; i++)
            {
                polys[i] = NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX;
            }

            for (int j = 0; j < ntris; ++j)
            {
              //  short* t = &tris[j * 3];
                if (tris[j * 3 + 0] != tris[j * 3 + 1] && tris[j * 3 + 0] != tris[j * 3 + 2] && tris[j * 3+1] != tris[j * 3+2])
                {
                    polys[npolys * MAX_VERTS_PER_POLY + 0] = hole[tris[j * 3+0]];
                    polys[npolys * MAX_VERTS_PER_POLY + 1] = hole[tris[j * 3+1]];
                    polys[npolys * MAX_VERTS_PER_POLY + 2] = hole[tris[j * 3+2]];
                    pareas[npolys] = (byte)harea[tris[j * 3 + 0]];
                    npolys++;
                }
            }
            if (npolys == 0)
                return dtStatus.DT_SUCCESS;

            // Merge polygons.
            int maxVertsPerPoly = MAX_VERTS_PER_POLY;
            if (maxVertsPerPoly > 3)
            {
                for (; ; )
                {
                    // Find best polygons to merge.
                    int bestMergeVal = 0;
                    int bestPa = 0, bestPb = 0, bestEa = 0, bestEb = 0;

                    for (int j = 0; j < npolys - 1; ++j)
                    {
                        int pj = j * MAX_VERTS_PER_POLY;
                        for (int k = j + 1; k < npolys; ++k)
                        {
                            int pk = k * MAX_VERTS_PER_POLY;
                            int ea = 0;
                            int  eb = 0;
                            int v = getPolyMergeValue(ref polys, pj, ref polys, pk, mesh.verts, ref  ea, ref eb);
                            if (v > bestMergeVal)
                            {
                                bestMergeVal = v;
                                bestPa = j;
                                bestPb = k;
                                bestEa = ea;
                                bestEb = eb;
                            }
                        }
                    }

                    if (bestMergeVal > 0)
                    {
                        // Found best, merge.
                       // short* pa = &polys[bestPa * MAX_VERTS_PER_POLY];
                      //  short* pb = &polys[bestPb * MAX_VERTS_PER_POLY];
                        mergePolys(ref polys, bestPa * MAX_VERTS_PER_POLY, polys, bestPb * MAX_VERTS_PER_POLY, bestEa, bestEb);
                       // memcpy(pb, &polys[(npolys - 1) * MAX_VERTS_PER_POLY], sizeof(short) * MAX_VERTS_PER_POLY);

                        for (int m = 0; m < MAX_VERTS_PER_POLY; m++ )
                        {
                            polys[bestPb * MAX_VERTS_PER_POLY + m] = polys[(npolys - 1) * MAX_VERTS_PER_POLY + m];
                        }


                            pareas[bestPb] = pareas[npolys - 1];
                        npolys--;
                    }
                    else
                    {
                        // Could not merge any polygons, stop.
                        break;
                    }
                }
            }

            // Store polygons.
            for (int i = 0; i < npolys; ++i)
            {
                if (mesh.npolys >= maxTris) break;

           //     short* p = &mesh.polys[mesh.npolys * MAX_VERTS_PER_POLY * 2];
          //      memset(p, 0xff, sizeof(short) * MAX_VERTS_PER_POLY * 2);

                for (int m = 0; m < MAX_VERTS_PER_POLY * 2; m++ )
                {
                    mesh.polys[mesh.npolys * MAX_VERTS_PER_POLY * 2 + m] = NavMeshBuilderDefine.DT_TILECACHE_NULL_IDX;
                }


                for (int j = 0; j < MAX_VERTS_PER_POLY; ++j)
                    mesh.polys[mesh.npolys * MAX_VERTS_PER_POLY * 2 + j] = polys[i * MAX_VERTS_PER_POLY + j];

                mesh.areas[mesh.npolys] = pareas[i];
                mesh.npolys++;
                if (mesh.npolys > maxTris)
                    return dtStatus.DT_FAILURE;
            }

            return dtStatus.DT_SUCCESS;
        }

        static int[] m_OffsetXArray = new int[] { -1, 0, 1, 0, };
        static public int getDirOffsetX(int dir)
        {
            return m_OffsetXArray[dir & 0x03];
        }

        static int[] m_OffsetYArray = new int[] { 0, 1, 0, -1 };
        static public int getDirOffsetY(int dir)
        {
            return m_OffsetYArray[dir & 0x03];
        }


        public static bool overlapRangeExl(ushort amin, ushort amax,
                                      ushort bmin, ushort bmax)
        {
            return (amin >= bmax || amax <= bmin) ? false : true;
        }

        public static void addUniqueLast(ref byte[] a, ref byte an, byte v)
        {
            int n = (int)an;
            if (n > 0 && a[n - 1] == v) return;
            a[an] = v;
            an++;
        }


        public static bool isConnected(dtTileCacheLayer layer, int ia, int ib, int walkableClimb)
        {
            if (layer.areas[ia] != layer.areas[ib]) return false;
            if (NavMeshMath.Abs((int)layer.heights[ia] - (int)layer.heights[ib]) > walkableClimb) return false;
            return true;
        }

        public static bool canMerge(byte oldRegId, byte newRegId, dtLayerMonotoneRegion[] regs, int nregs)
        {
            int count = 0;
            for (int i = 0; i < nregs; ++i)
            {
                dtLayerMonotoneRegion reg = regs[i];
                if (reg.regId != oldRegId) continue;
                int nnei = (int)reg.nneis;
                for (int j = 0; j < nnei; ++j)
                {
                    if (regs[reg.neis[j]].regId == newRegId)
                        count++;
                }
            }
            return count == 1;
        }
    }
}


