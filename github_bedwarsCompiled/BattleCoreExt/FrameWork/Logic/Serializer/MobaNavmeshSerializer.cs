using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using UnityEngine;
using System.Reflection;
using MOBA;

public class MobaNavmeshSerializer
{
    public static bool LoadNavMeshTileCacheForBin(string strName, ref TileCacheSetParam tileCacheSetParam)
    {
        string FilePath =  strName;

#if UNITY_EDITOR
        TextAsset configFile = Resources.Load(FilePath) as TextAsset;
        if (configFile == null)
        {
            Debug.LogError("Resources.LoadPVPMobaConfigSerializer::LoadNavMeshTileCacheForBin [Resources.Load] not find file:" + FilePath);
            return false;
        }

        byte[] bytes = configFile.bytes;

#elif UNITY_SERVERS

        string strPath = strName + ".bytes";
        if (!File.Exists(strPath))
        {
            Debug.LogError("Resources.LoadPVPMobaConfigSerializer::LoadNavMeshTileCacheForBin  UNITY_SERVERS not find file:" + strPath);
            return false;
        }

        Stream fileStream = new FileStream(strPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] bytes = new byte[fileStream.Length];
        fileStream.Read(bytes, 0, (int)fileStream.Length);
        fileStream.Close();
#else
        TextAsset configFile = ResourceMgr.instance.GetResPrefab(FilePath) as TextAsset;
        if (configFile == null)
        {
            LogUtils.LogError("PVPMobaConfigSerializer::LoadBin.xml not find file:" + FilePath);
             return false;
        }     

          byte[] bytes = configFile.bytes;
#endif

        tileCacheSetParam.header = new TileCacheSetHeader();
        tileCacheSetParam.header.meshParams = new dtNavMeshParams();
        tileCacheSetParam.header.cacheParams = new dtTileCacheParams();

        TileCacheSetHeader header = tileCacheSetParam.header;

        MemoryStream memstream = new MemoryStream(bytes);
        BinaryReader br = new BinaryReader(memstream);

        header.magic =  br.ReadInt32();
        header.version = br.ReadInt32();
        header.numTiles = br.ReadInt32();
        header.cacheParams.orig[0] = br.ReadInt32();
        header.cacheParams.orig[1] = br.ReadInt32();
        header.cacheParams.orig[2] = br.ReadInt32();

        header.cacheParams.cs = br.ReadInt32();
        header.cacheParams.ch = br.ReadInt32();
        header.cacheParams.width = br.ReadInt32();
        header.cacheParams.height = br.ReadInt32();

        header.cacheParams.walkableHeight = br.ReadInt32();
        header.cacheParams.walkableRadius = br.ReadInt32();
        header.cacheParams.walkableClimb = br.ReadInt32();
        header.cacheParams.maxSimplificationError = br.ReadInt32();

        header.cacheParams.maxTiles = br.ReadInt32();
        header.cacheParams.maxObstacles = br.ReadInt32();

        header.meshParams.orig[0] = br.ReadInt32();
        header.meshParams.orig[1] = br.ReadInt32();
        header.meshParams.orig[2] = br.ReadInt32();
        header.meshParams.tileWidth = br.ReadInt32();
        header.meshParams.tileHeight = br.ReadInt32();
        header.meshParams.maxTiles = br.ReadInt32();
        header.meshParams.maxPolys = br.ReadInt32();

        tileCacheSetParam.cacheLayers = new TileCacheLayer[header.numTiles];

        for (int tileIndex = 0; tileIndex < header.numTiles; tileIndex++)
        {
            tileCacheSetParam.cacheLayers[tileIndex] = new TileCacheLayer();
            tileCacheSetParam.cacheLayers[tileIndex].layer = new dtTileCacheLayer();
            tileCacheSetParam.cacheLayers[tileIndex].layer.header = new dtTileCacheLayerHeader();
            TileCacheLayer cacheLayer = tileCacheSetParam.cacheLayers[tileIndex];

            cacheLayer.tileRef = br.ReadInt32();
            cacheLayer.layer.header.magic = br.ReadInt32();
            cacheLayer.layer.header.version = br.ReadInt32();

            cacheLayer.layer.header.tx = br.ReadInt32();
            cacheLayer.layer.header.ty = br.ReadInt32();
            cacheLayer.layer.header.tlayer = br.ReadInt32();

            cacheLayer.layer.header.bmin[0] = br.ReadInt32();
            cacheLayer.layer.header.bmin[1] = br.ReadInt32();
            cacheLayer.layer.header.bmin[2] = br.ReadInt32();

            cacheLayer.layer.header.bmax[0] = br.ReadInt32();
            cacheLayer.layer.header.bmax[1] = br.ReadInt32();
            cacheLayer.layer.header.bmax[2] = br.ReadInt32();

            cacheLayer.layer.header.hmin = br.ReadInt16();
            cacheLayer.layer.header.hmax = br.ReadInt16();

            cacheLayer.layer.header.width = br.ReadByte();
            cacheLayer.layer.header.height = br.ReadByte();

            cacheLayer.layer.header.minx = br.ReadByte();
            cacheLayer.layer.header.maxx = br.ReadByte();
            cacheLayer.layer.header.miny = br.ReadByte();
            cacheLayer.layer.header.maxy = br.ReadByte();
            cacheLayer.layer.regCount = br.ReadByte();

            //int gridSize = (int)cacheLayer.layer.header.width * (int)cacheLayer.layer.header.height;
            int gridSize = br.ReadInt32();

            cacheLayer.layer.heights = new byte[gridSize];
            cacheLayer.layer.areas = new byte[gridSize];
            cacheLayer.layer.cons = new byte[gridSize];
            cacheLayer.layer.regs = new byte[gridSize];

            for (int j = 0; j < gridSize; j++)
            {
                cacheLayer.layer.heights[j] = br.ReadByte();
            }

            for (int j = 0; j < gridSize; j++)
            {
                cacheLayer.layer.areas[j] = br.ReadByte();
            }

            for (int j = 0; j < gridSize; j++)
            {
                cacheLayer.layer.cons[j] = br.ReadByte();
            }

            for (int j = 0; j < gridSize; j++)
            {
                cacheLayer.layer.regs[j] = br.ReadByte();
            }
        }

        br.Close();

        br = null;
        memstream = null;
        bytes = null;

        return true;
    }


    public static bool LoadNavMeshTileCacheXml(string strName, ref TileCacheSetParam tileCacheSetParam)
    {
#if UNITY_EDITOR
        string FilePath = Application.dataPath + "/Resources/data/map/" + strName + ".xml";
        if (!File.Exists(FilePath))
        {
#if UNITY_EDITOR
            Debug.LogError("PVPMobaConfigSerializer::LoadNavMeshTileCache.xml not find file:" + FilePath);
#else
             LogUtils.LogError("PVPMobaConfigSerializer::LoadNavMeshTileCache.xml not find file:" + FilePath);
#endif
            return false;
        }

        Stream fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] bytes = new byte[fileStream.Length];
        fileStream.Read(bytes, 0, (int)fileStream.Length);
        fileStream.Close();
#else

        byte[] bytes = null;
        string FilePath = "";//AppMgr.Instance.ExtAssetBunldePath + "moba/map/" + strName + ".xml";
        if (File.Exists(FilePath))
        {
            Stream fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            bytes = new byte[fileStream.Length];
            fileStream.Read(bytes, 0, (int)fileStream.Length);
            fileStream.Close();
        }
        else
        {
            FilePath = "data/map/" + strName;
            TextAsset configFile = Resources.Load(FilePath) as TextAsset;
            if (configFile == null)
            {
              // LogUtils.LogError("PVPMobaConfigSerializer::LoadBin.xml not find file:" + FilePath);
                return false;
            }

            bytes = configFile.bytes;
        }

#endif

        string strXml = Encoding.UTF8.GetString(bytes);

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(strXml);

        if (doc.FirstChild == null)
        {
#if UNITY_EDITOR || UNITY_SERVERS
            Debug.LogError("LoadNavMeshConfig.LoadFromText XML格式异常 1001 : " + strName);
#else
             LogUtils.LogError("LoadNavMeshConfig.LoadFromText XML格式异常 1001 : " + strName);
#endif
            return false;
        }

        tileCacheSetParam.header = new TileCacheSetHeader();
        tileCacheSetParam.header.meshParams = new dtNavMeshParams();
        tileCacheSetParam.header.cacheParams = new dtTileCacheParams();

        TileCacheSetHeader header = tileCacheSetParam.header;

     
        //TileCacheSetHeader
        XmlElement setHeaderNode = doc.FirstChild.FirstChild as XmlElement;

        string strVal = setHeaderNode.GetAttribute("magic");
        header.magic = int.Parse(strVal);
        strVal = setHeaderNode.GetAttribute("version");
        header.version = int.Parse(strVal);
        strVal = setHeaderNode.GetAttribute("numTiles");
        header.numTiles = int.Parse(strVal);

        //dtTileCacheParams
        XmlElement tileParamNode = setHeaderNode.FirstChild as XmlElement;
        strVal = tileParamNode.GetAttribute("orig");
        string[] strPos = strVal.Split(',');

        for (int i = 0; i < header.cacheParams.orig.Length; i++)
        {
             header.cacheParams.orig[i] = int.Parse(strPos[i]);
        }

        strVal = tileParamNode.GetAttribute("cs");
        header.cacheParams.cs = int.Parse(strVal);
        strVal = tileParamNode.GetAttribute("ch");
        header.cacheParams.ch = int.Parse(strVal);

        strVal = tileParamNode.GetAttribute("width");
        header.cacheParams.width = int.Parse(strVal);
        strVal = tileParamNode.GetAttribute("height");
        header.cacheParams.height = int.Parse(strVal);

        strVal = tileParamNode.GetAttribute("walkableHeight");
        header.cacheParams.walkableHeight = int.Parse(strVal);
        strVal = tileParamNode.GetAttribute("walkableRadius");
        header.cacheParams.walkableRadius = int.Parse(strVal);
        strVal = tileParamNode.GetAttribute("walkableClimb");
        header.cacheParams.walkableClimb = int.Parse(strVal);
        strVal = tileParamNode.GetAttribute("maxSimplificationError");
        header.cacheParams.maxSimplificationError = int.Parse(strVal);

        strVal = tileParamNode.GetAttribute("maxTiles");
        header.cacheParams.maxTiles = int.Parse(strVal);
        strVal = tileParamNode.GetAttribute("maxObstacles");
        header.cacheParams.maxObstacles = int.Parse(strVal);

        //dtNavMeshParams
        XmlElement meshParamNode = tileParamNode.NextSibling as XmlElement ;
        strVal = meshParamNode.GetAttribute("orig");
        strPos = strVal.Split(',');

        for (int i = 0; i < header.meshParams.orig.Length; i++)
        {
            header.meshParams.orig[i] = int.Parse(strPos[i]);
        }

        strVal = meshParamNode.GetAttribute("tileWidth");
        header.meshParams.tileWidth = int.Parse(strVal);
        strVal = meshParamNode.GetAttribute("tileHeight");
        header.meshParams.tileHeight = int.Parse(strVal);

        strVal = meshParamNode.GetAttribute("maxTiles");
        header.meshParams.maxTiles = int.Parse(strVal);
        strVal = meshParamNode.GetAttribute("maxPolys");
        header.meshParams.maxPolys = int.Parse(strVal);

        tileCacheSetParam.cacheLayers = new TileCacheLayer[header.numTiles]; 

        //dtCompressedTileList
        XmlElement TileListNode = setHeaderNode.NextSibling as XmlElement;

        int tileIndex = 0;
        //dtCompressedTile
        XmlElement TileNode = TileListNode.FirstChild as XmlElement;
        while(TileNode != null)
        {
            tileCacheSetParam.cacheLayers[tileIndex] = new TileCacheLayer();
            tileCacheSetParam.cacheLayers[tileIndex].layer = new dtTileCacheLayer();
            tileCacheSetParam.cacheLayers[tileIndex].layer.header = new dtTileCacheLayerHeader();
            TileCacheLayer cacheLayer = tileCacheSetParam.cacheLayers[tileIndex];


            strVal = TileNode.GetAttribute("tileRef");
            cacheLayer.tileRef = int.Parse(strVal);

            //header
            XmlElement layerHeaderNode = TileNode.FirstChild as XmlElement;

            strVal = layerHeaderNode.GetAttribute("magic");
            cacheLayer.layer.header.magic = int.Parse(strVal);
            strVal = layerHeaderNode.GetAttribute("version");
            cacheLayer.layer.header.version = int.Parse(strVal);

            strVal = layerHeaderNode.GetAttribute("tx");
            cacheLayer.layer.header.tx = int.Parse(strVal);
            strVal = layerHeaderNode.GetAttribute("ty");
            cacheLayer.layer.header.ty = int.Parse(strVal);
            strVal = layerHeaderNode.GetAttribute("tlayer");
            cacheLayer.layer.header.tlayer = int.Parse(strVal);

            strVal = layerHeaderNode.GetAttribute("bmin");
            strPos = strVal.Split(',');
            for (int i = 0; i < cacheLayer.layer.header.bmin.Length; i++)
            {
                cacheLayer.layer.header.bmin[i] = int.Parse(strPos[i]);
            }

            strVal = layerHeaderNode.GetAttribute("bmax");
            strPos = strVal.Split(',');
            for (int i = 0; i < cacheLayer.layer.header.bmin.Length; i++)
            {
                cacheLayer.layer.header.bmax[i] = int.Parse(strPos[i]);
            }

            strVal = layerHeaderNode.GetAttribute("hmin");
            cacheLayer.layer.header.hmin = short.Parse(strVal);
            strVal = layerHeaderNode.GetAttribute("hmax");
            cacheLayer.layer.header.hmax = short.Parse(strVal);

            strVal = layerHeaderNode.GetAttribute("width");
            cacheLayer.layer.header.width = byte.Parse(strVal);
            strVal = layerHeaderNode.GetAttribute("height");
            cacheLayer.layer.header.height = byte.Parse(strVal);
            strVal = layerHeaderNode.GetAttribute("minx");
            cacheLayer.layer.header.minx = byte.Parse(strVal);
            strVal = layerHeaderNode.GetAttribute("maxx");
            cacheLayer.layer.header.maxx = byte.Parse(strVal);
            strVal = layerHeaderNode.GetAttribute("miny");
            cacheLayer.layer.header.miny = byte.Parse(strVal);
            strVal = layerHeaderNode.GetAttribute("maxy");
            cacheLayer.layer.header.maxy = byte.Parse(strVal);

            //data
            XmlElement layerdataNode = layerHeaderNode.NextSibling as XmlElement;

            strVal = layerdataNode.GetAttribute("regCount");
            cacheLayer.layer.regCount = byte.Parse(strVal);

            int gridSize = (int)cacheLayer.layer.header.width * (int)cacheLayer.layer.header.height;

            cacheLayer.layer.heights = new byte[gridSize];
            cacheLayer.layer.areas = new byte[gridSize];
            cacheLayer.layer.cons = new byte[gridSize];
            cacheLayer.layer.regs = new byte[gridSize];

            strVal = layerdataNode.GetAttribute("heights");
            strPos = strVal.Split(',');
            for (int i = 0; i < cacheLayer.layer.heights.Length; i++)
            {
                cacheLayer.layer.heights[i] = byte.Parse(strPos[i]);
            }

            strVal = layerdataNode.GetAttribute("areas");
            strPos = strVal.Split(',');
            for (int i = 0; i < cacheLayer.layer.areas.Length; i++)
            {
                cacheLayer.layer.areas[i] = byte.Parse(strPos[i]);
            }

            strVal = layerdataNode.GetAttribute("cons");
            strPos = strVal.Split(',');
            for (int i = 0; i < cacheLayer.layer.cons.Length; i++)
            {
                cacheLayer.layer.cons[i] = byte.Parse(strPos[i]);
            }

            strVal = layerdataNode.GetAttribute("regs");
            strPos = strVal.Split(',');
            for (int i = 0; i < cacheLayer.layer.regs.Length; i++)
            {
                cacheLayer.layer.regs[i] = byte.Parse(strPos[i]);
            }


            tileIndex++;
            TileNode = TileNode.NextSibling as XmlElement;
        }

		bytes = null;
     

        return true;
    }
}

