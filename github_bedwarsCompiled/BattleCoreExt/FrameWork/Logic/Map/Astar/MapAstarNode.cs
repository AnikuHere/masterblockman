using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOBA
{
    public interface IPriorityQueueNode
    {
        long Priority();
    }

    public class PriorityQueue<T> where T : IPriorityQueueNode
    {
        private T[] m_heap;
        private int m_capacity;
        private int m_size;

        public PriorityQueue(int n)
        {
            m_capacity = n;
            m_size = 0;

            m_heap = new T[m_capacity + 1];
        }

        void bubbleUp(int i, T node)
        {
            int parent = (i - 1) / 2;
            // note: (index > 0) means there is a parent
            while ((i > 0) && (m_heap[parent].Priority() > node.Priority()))
            {
                m_heap[i] = m_heap[parent];
                i = parent;
                parent = (i - 1) / 2;
            }
            m_heap[i] = node;
        }

        void trickleDown(int i, T node)
        {
            int child = (i * 2) + 1;
            while (child < m_size)
            {
                if (((child + 1) < m_size) &&
                    (m_heap[child].Priority() > m_heap[child + 1].Priority()))
                {
                    child++;
                }
                m_heap[i] = m_heap[child];
                i = child;
                child = (i * 2) + 1;
            }
            bubbleUp(i, node);
        }

        public void Release()
        {
            for (int i = 0; i < m_heap.Length; i++)
                m_heap[i] = default(T);

            m_heap = null;
        }

        public void clear() { m_size = 0; }

        public T top() { return m_heap[0]; }

        public T GetAt(int index) { return m_heap[index]; }

        public T pop()
        {
            if (m_size == 0)
                return default(T);

            T result = m_heap[0];
            m_size--;
            trickleDown(0, m_heap[m_size]);
            return result;
        }

        public void push(T node)
        {
            m_size++;
            bubbleUp(m_size - 1, node);
        }

        public void modify(T node)
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

        public bool empty() { return m_size == 0; }
        public bool IsFull() { return m_size >= m_capacity; }

        public int getCapacity() { return m_capacity; }

        public int getSize() { return m_size; }
    };

    public class AstarNode : IPriorityQueueNode
    {
        public int centerX;
        public int centerZ;

        public long H;
        public long G;

        public int parentX;
        public int parentZ;

        public AstarNodeStatus status;

        public void Clear()
        {
            status = AstarNodeStatus.NONE;
            H = 0;
            G = 0;
            parentX = -1;
            parentZ = -1;
        }

        public long Priority()
        {
            return (H + G);
        }
    };

    public class BfsNode
    {
        public int x;
        public int z;
        public long step;
        public bool pass;

        public BfsNode next;
        public AstarNodeStatus status;

        public void Clear()
        {
            x = 0;
            z = 0;
            step = 0;
            pass = false;
            next = null;
            status = AstarNodeStatus.NONE;
        }
    }

    public class BfsNodeFIFO
    {
        private int m_nMaxSize = 0;
        private int m_Mask = 0;
        private BfsNode[] m_Array = null;
        private int m_In = 0;
        private int m_Out = 0;

        public BfsNodeFIFO(int nMaxSizeBits)
        {
            m_nMaxSize = 1 << nMaxSizeBits;
            m_Mask = m_nMaxSize - 1;
            m_Array = new BfsNode[m_nMaxSize];
        }

        public void Clear()
        {
            for (int i = 0; i < m_Array.Length; i++)
                m_Array[i] = null;
            m_In = 0;
            m_Out = 0;
        }

        public void Release()
        {
            m_Array = null;
        }

        public bool Push(BfsNode node)
        {
            if (m_In - m_Out >= m_nMaxSize)
                return false;

            int nOffset = m_In & m_Mask;

            m_Array[nOffset] = node;
            m_In++;
            return true;
        }

        public BfsNode Pop()
        {
            if (m_In <= m_Out)
                return null;

            int nOffset = m_Out & m_Mask;
            BfsNode node = m_Array[nOffset];

            m_Array[nOffset] = null;
            m_Out++;
            return node;
        }
    }
}
