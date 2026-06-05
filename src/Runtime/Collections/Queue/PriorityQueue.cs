using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Buffers;
using System;

namespace Nk7.DataStructures
{
    public sealed class PriorityQueue<TItem, TPriority> : IDisposable
    {
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfDisposed();
                return _nodes.Length;
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

        private Node[] _nodes;
        private bool _isDisposed;
        private int _count;

        public PriorityQueue(int capacity = CollectionUtils.DEFAULT_CAPACITY)
            : this(capacity, Comparer<TPriority>.Default) { }

        public PriorityQueue(int capacity = CollectionUtils.DEFAULT_CAPACITY, IComparer<TPriority> comparer = null)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _isDisposed = false;
            _nodes = ArrayPool<Node>.Shared.Rent(capacity);
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

            if (_nodes != null)
            {
                ArrayPool<Node>.Shared.Return(_nodes, RuntimeHelpers.IsReferenceOrContainsReferences<Node>());
                _nodes = Array.Empty<Node>();
            }

            _count = 0;
        }

        public void Clear()
        {
            ThrowIfDisposed();

            if (RuntimeHelpers.IsReferenceOrContainsReferences<Node>())
            {
                Array.Clear(_nodes, 0, _count);
            }

            _count = 0;
        }

        public void Enqueue(TItem item, TPriority priority)
        {
            ThrowIfDisposed();

            if (_count == _nodes.Length)
            {
                Grow();
            }

            _nodes[_count++] = new Node(item, priority);
            BubbleUp();
        }

        public TItem Dequeue()
        {
            ThrowIfDisposed();
            ThrowIfEmpty();

            var node = _nodes[0];

            _nodes[0] = _nodes[--_count];
            _nodes[_count] = default;

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

            return _nodes[0].Item;
        }

        private void Grow()
        {
            int newCapacity = _nodes.Length << CollectionUtils.GROWTH_SHIFT;
            var newNodes = ArrayPool<Node>.Shared.Rent(newCapacity);

            Array.Copy(_nodes, newNodes, _count);
            ArrayPool<Node>.Shared.Return(_nodes, RuntimeHelpers.IsReferenceOrContainsReferences<Node>());

            _nodes = newNodes;
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
            return _comparer.Compare(_nodes[leftIndex].Priority, _nodes[rightIndex].Priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(int parentIndex, int childIndex)
        {
            var temp = _nodes[parentIndex];

            _nodes[parentIndex] = _nodes[childIndex];
            _nodes[childIndex] = temp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (!_isDisposed)
            {
                return;
            }

            throw new ObjectDisposedException(nameof(PriorityQueue<TItem, TPriority>));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfEmpty()
        {
            if (_count > 0)
            {
                return;
            }

            throw new InvalidOperationException("Priority queue is empty");
        }


        private readonly struct Node
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