using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Buffers;
using System;

namespace Nk7.DataStructures
{
    public ref struct UnmanagedPriorityQueue<TItem, TPriority>
        where TItem : unmanaged
        where TPriority : unmanaged
    {
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfDisposed();
                return _nodesBuffer.Length;
            }
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfDisposed();
                return _count;
            }
        }

        private readonly IComparer<TPriority> _comparer;

        private Span<Node> _nodesBuffer;
        private Node[] _nodesArray;
        private bool _isDisposed;
        private bool _clearArray;
        private int _count;

        public UnmanagedPriorityQueue(Span<Node> initialBuffer, bool clearArray = false, IComparer<TPriority> comparer = null)
        {
            _clearArray = clearArray;
            _isDisposed = false;

            _nodesArray = null;
            _nodesBuffer = initialBuffer;
            _comparer = comparer
                ?? Comparer<TPriority>.Default;

            _count = 0;
        }
        
        public UnmanagedPriorityQueue(int capacity = CollectionUtils.DEFAULT_CAPACITY, bool clearArray = false, IComparer<TPriority> comparer = null)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _clearArray = clearArray;
            _isDisposed = false;

            _nodesArray = ArrayPool<Node>.Shared.Rent(capacity);
            _nodesBuffer = _nodesArray;
            _comparer = comparer
                ?? Comparer<TPriority>.Default;

            _count = 0;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_nodesArray != null)
            {
                ArrayPool<Node>.Shared.Return(_nodesArray, _clearArray);
            }

            _nodesBuffer = Span<Node>.Empty;
            _nodesArray = null;
            _count = 0;
        }

        public void Clear()
        {
            ThrowIfDisposed();

            if (_clearArray)
            {
                _nodesBuffer.Slice(0, _count).Clear();
            }

            _count = 0;
        }

        public void Enqueue(TItem item, TPriority priority)
        {
            ThrowIfDisposed();

            if (_count == _nodesBuffer.Length)
            {
                Grow();
            }

            _nodesBuffer[_count++] = new Node(item, priority);
            BubbleUp();
        }

        public TItem Dequeue()
        {
            ThrowIfDisposed();
            ThrowIfEmpty();

            var node = _nodesBuffer[0];

            _nodesBuffer[0] = _nodesBuffer[--_count];

            if (_count > 0)
            {
                BubbleDown();
            }

            return node.Item;
        }

        public TItem Peek()
        {
            ThrowIfDisposed();
            ThrowIfEmpty();

            return _nodesBuffer[0].Item;
        }

        private void Grow()
        {
            int newCapacity = _nodesBuffer.Length == 0
                ? CollectionUtils.DEFAULT_CAPACITY
                : _nodesBuffer.Length << CollectionUtils.GROWTH_SHIFT;
            var newArrayNodes = ArrayPool<Node>.Shared.Rent(newCapacity);
            var oldArrayNodes = _nodesArray;

            _nodesBuffer.Slice(0, _count).CopyTo(newArrayNodes);

            _nodesArray = newArrayNodes;
            _nodesBuffer = newArrayNodes;

            if (oldArrayNodes != null)
            {
                ArrayPool<Node>.Shared.Return(oldArrayNodes, _clearArray);
            }
        }

        private void BubbleUp()
        {
            int index = _count - 1;

            while (index > 0)
            {
                int parentIndex = BinaryHeapUtils.GetParentIndex(index);

                if (Compare(index, parentIndex) >= 0)
                {
                    break;
                }

                Swap(parentIndex, index);
                index = parentIndex;
            }
        }

        private void BubbleDown()
        {
            int index = 0;

            while (true)
            {
                int leftChildIndex = BinaryHeapUtils.GetLeftChildIndex(index);
                int rightChildIndex = BinaryHeapUtils.GetRightChildIndex(index);

                if (leftChildIndex >= _count)
                {
                    break;
                }

                int bestChildIndex = leftChildIndex;

                if (rightChildIndex < _count
                    && Compare(rightChildIndex, leftChildIndex) < 0)
                {
                    bestChildIndex = rightChildIndex;
                }

                if (Compare(index, bestChildIndex) <= 0)
                {
                    break;
                }

                Swap(index, bestChildIndex);
                index = bestChildIndex;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Compare(int leftIndex, int rightIndex)
        {
            return _comparer.Compare(_nodesBuffer[leftIndex].Priority, _nodesBuffer[rightIndex].Priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(int parentIndex, int childIndex)
        {
            var temp = _nodesBuffer[parentIndex];

            _nodesBuffer[parentIndex] = _nodesBuffer[childIndex];
            _nodesBuffer[childIndex] = temp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (!_isDisposed)
            {
                return;
            }

            throw new ObjectDisposedException(nameof(UnmanagedPriorityQueue<TItem, TPriority>));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfEmpty()
        {
            if (_count > 0)
            {
                return;
            }

            throw new InvalidOperationException("Unmanaged priority queue is empty");
        }


        public readonly struct Node
        {
            public readonly TItem Item;
            public readonly TPriority Priority;

            public Node(TItem item, TPriority priority)
            {
                Item = item;
                Priority = priority;
            }
        }
    }
}