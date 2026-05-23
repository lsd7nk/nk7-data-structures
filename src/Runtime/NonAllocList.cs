using System.Runtime.CompilerServices;
using System.Buffers;
using System;

namespace Nk7.DataStructures
{
    /// <summary>
    /// Provides a stack-only, growable list for unmanaged values that can use a caller-provided buffer before renting pooled arrays
    /// </summary>
    /// <typeparam name="T">The unmanaged value type stored in the list</typeparam>
    public ref struct NonAllocList<T>
        where T : unmanaged
    {
        private const int CAPACITY_MULTIPLIER = 2;
        private const int DEFAULT_CAPACITY = 4;

        /// <summary>
        /// Gets a reference to the item at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the item to get</param>
        /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is outside the current item range</exception>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index < 0 || index >= _count)
                {
                    throw new IndexOutOfRangeException();
                }

                return ref _buffer[index];
            }
        }

        /// <summary>
        /// Gets the number of items the current buffer can hold without growing
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length;
        }

        /// <summary>
        /// Gets the number of items currently stored in the list
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        private Span<T> _buffer;
        private T[] _array;

        private int _count;

        /// <summary>
        /// Initializes a new list backed by the provided initial buffer
        /// </summary>
        /// <param name="initialBuffer">The caller-owned buffer used as the initial storage</param>
        public NonAllocList(Span<T> initialBuffer)
        {
            _buffer = initialBuffer;
            _array = null;
            _count = 0;
        }

        /// <summary>
        /// Initializes a new list with storage rented from the shared array pool
        /// </summary>
        /// <param name="capacity">The minimum number of items the initial rented storage must hold</param>
        public NonAllocList(int capacity)
        {
            _array = ArrayPool<T>.Shared.Rent(capacity);
            _buffer = _array;
            _count = 0;
        }

        /// <summary>
        /// Returns any rented storage to the shared array pool and clears the list state
        /// </summary>
        public void Dispose()
        {
            if (_array != null)
            {
                ArrayPool<T>.Shared.Return(_array);
            }

            _buffer = Span<T>.Empty;
            _array = null;
            _count = 0;
        }

        /// <summary>
        /// Adds an item to the end of the list, growing the backing storage when needed
        /// </summary>
        /// <param name="item">The item to add</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (_count >= Capacity)
            {
                Grow();
            }

            _buffer[_count++] = item;
        }

        private void Grow()
        {
            int newLength = _buffer.Length == 0
                ? DEFAULT_CAPACITY
                : _buffer.Length * CAPACITY_MULTIPLIER;
            var newArray = ArrayPool<T>.Shared.Rent(newLength);
            var oldArray = _array;

            _buffer.Slice(0, _count).CopyTo(newArray);

            _array = newArray;
            _buffer = newArray;

            if (oldArray != null)
            {
                ArrayPool<T>.Shared.Return(oldArray);
            }
        }
    }
}
