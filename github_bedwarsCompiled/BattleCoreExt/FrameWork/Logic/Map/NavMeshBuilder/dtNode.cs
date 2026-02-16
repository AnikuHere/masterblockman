using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOBA
{
    public class dtNode
    {
        public LogicVector3 pos;								///< Position of the node.
        public int cost;									///< Cost from previous node to current node.
        public int total;								///< Cost up to the node.
        public uint pidx;	///< Index to parent node.
        public uint state;	///< extra state information. A polyRef can have multiple nodes with different extra info. see DT_MAX_STATES_PER_NODE
        public uint flags;						///< Node flags. A combination of dtNodeFlags.
        public uint id;								///< Polygon ref the node corresponds to.

        public dtNode()
        {
            cost = 0;
            total = 0;
            pidx  = 0;
            state = 0;
            flags = 3;
            id = 0;
        }

        public void Clear()
        {
            pos = LogicVector3.zero;
            cost = 0;
            total = 0;
            pidx  = 0;
            state = 0;
            flags = 3;
            id = 0;
        }
    };

    public class dtNodePool
    {
        const ushort DT_NULL_IDX = ushort.MaxValue;

        private dtNode[] m_nodes;
        private ushort[] m_first;
        private ushort[] m_next;

        private  int m_maxNodes;
        private  int m_hashSize;
        private int m_nodeCount;
 
	    public dtNodePool(int maxNodes, int hashSize)
        {
            m_maxNodes = maxNodes;
            m_hashSize = hashSize;
            m_nodeCount = 0;

            m_nodes = new dtNode[m_maxNodes];
            m_next = new ushort[m_maxNodes];
            m_first = new ushort[hashSize];

            for (int i = 0; i < m_maxNodes; i++)
            {
                m_nodes[i] = new dtNode();
            }

            for(int i=0; i < m_maxNodes; i++)
            {
                m_next[i] = ushort.MaxValue;
            }

            for(int i=0; i < hashSize; i++)
            {
                m_first[i] = ushort.MaxValue;
            }
      
        }
	
	   public  void clear()
        {
            for (int i = 0; i < m_hashSize; i++)
            {
                m_first[i] = ushort.MaxValue;
            }

           m_nodeCount = 0;
        }

        public uint  findNodes(uint id, ref dtNode[] nodes,  int maxNodes)
        {
	        int n = 0;
	        uint bucket = (uint)(dtHashRef(id) & (m_hashSize-1));
	        ushort i = m_first[bucket];
	        while (i != DT_NULL_IDX)
	        {
		        if (m_nodes[i].id == id)
		        {
			        if (n >= maxNodes)
				        return (uint)n;
			        nodes[n++] = m_nodes[i];
		        }
		        i = m_next[i];
	        }

	        return (uint)n;
        }

        public dtNode findNode(uint id, byte state)
        {
	         uint bucket = (uint)(dtHashRef(id) & (m_hashSize-1));
	        ushort i = m_first[bucket];
	        while (i != DT_NULL_IDX)
	        {
		        if (m_nodes[i].id == id && m_nodes[i].state == state)
			        return m_nodes[i];
		        i = m_next[i];
	        }
	        return null;
        }
        
        public dtNode getNode(uint id, byte state = 0)
        {
	         uint bucket = (uint)(dtHashRef(id) & (uint)(m_hashSize-1));
	        ushort i = m_first[bucket];
	        dtNode node = null;
	        while (i != DT_NULL_IDX)
	        {
		        if (m_nodes[i].id == id && m_nodes[i].state == state)
			        return m_nodes[i];
		        i = m_next[i];
	        }
	
	        if (m_nodeCount >= m_maxNodes)
		        return null;
	
	        i = (ushort)m_nodeCount;
	        m_nodeCount++;
	
	        // Init node
	        node = m_nodes[i];
	        node.pidx = 0;
	        node.cost = 0;
	        node.total = 0;
	        node.id = id;
	        node.state = state;
	        node.flags = 0;
	
	        m_next[i] = m_first[bucket];
	        m_first[bucket] = i;
	
	        return node;
        }

        public dtNode getNodeAtIdx(uint idx)
	    {
		    if (idx == 0) return null;
		    return m_nodes[idx - 1];
	    }

        public  uint getNodeIdx(dtNode node) 
        {
            if (node == null) return 0;

            for(int i=0; i < m_nodes.Length; i++)
            {
                if (ReferenceEquals(m_nodes[i], node))
                    return (uint)i +1;
            }

            return 0;
        }
        
        private  uint dtHashRef(uint a)
        {
	        a += ~(a<<15);
	        a ^=  (a>>10);
	        a +=  (a<<3);
	        a ^=  (a>>6);
	        a += ~(a<<11);
	        a ^=  (a>>16);
	        return (uint)a;
        }
	
    };


    public class dtNodeQueue
    {
        private dtNode[] m_heap;
	    private int m_capacity;
	    private int m_size;

	    public dtNodeQueue(int n)
        {
            m_capacity = n;
            m_size = 0;

            m_heap = new dtNode[m_capacity+1];
        }

        void bubbleUp(int i, dtNode node)
        {
	        int parent = (i-1)/2;
	        // note: (index > 0) means there is a parent
	        while ((i > 0) && (m_heap[parent].total > node.total))
	        {
		        m_heap[i] = m_heap[parent];
		        i = parent;
		        parent = (i-1)/2;
	        }
	        m_heap[i] = node;
        }

        void trickleDown(int i, dtNode node)
        {
	        int child = (i*2)+1;
	        while (child < m_size)
	        {
		        if (((child+1) < m_size) && 
			        (m_heap[child].total > m_heap[child+1].total))
		        {
			        child++;
		        }
		        m_heap[i] = m_heap[child];
		        i = child;
		        child = (i*2)+1;
	        }
	        bubbleUp(i, node);
        }

	     public void clear() { m_size = 0; }
	
	     public dtNode top() { return m_heap[0]; }
	
	     public dtNode pop()
	    {
		    dtNode result = m_heap[0];
		    m_size--;
		    trickleDown(0, m_heap[m_size]);
		    return result;
	    }
	
	    public void push(dtNode node)
	    {
		    m_size++;
		    bubbleUp(m_size-1, node);
	    }
	
	    public void modify(dtNode node)
	    {
		    for (int i = 0; i < m_size; ++i)
		    {
                if (ReferenceEquals(m_heap[i], node))
			    {
				    bubbleUp(i, node);
				    return;
			    }
		    }
	    }
	
	     public bool empty()  { return m_size == 0; }
	
	     public int getCapacity()  { return m_capacity; }

    };		



}
