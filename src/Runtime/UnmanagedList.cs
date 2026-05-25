using System.Runtime.CompilerServices;
using System.Buffers;
using System;

namespace Nk7.DataStructures
{
    /// <summary>
    /// Provides a stack-scoped, growable list for unmanaged values that can use caller-provided storage before renting pooled arrays.
    /// </summary>
    /// <typeparam name="T">The unmanaged value type stored in the list</typeparam>
    public ref struct UnmanagedList<T>
        where T : unmanaged
    {
        private const int CAPACITY_MULTIPLIER = 2;
        private const int DEFAULT_CAPACITY = 4;

        /// <summary>
        /// Gets a reference to the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside the current item range</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed</exception>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfDisposed();

                if (index < 0 || index >= _count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return ref _buffer[index];
            }
        }

        /// <summary>
        /// Gets the number of items the current buffer can hold without growing.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed</exception>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfDisposed();
                return _buffer.Length;
            }
        }

        /// <summary>
        /// Gets the number of items currently stored in the list.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed</exception>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfDisposed();
                return _count;
            }
        }

        private readonly bool _clearArray;

        private Span<T> _buffer;
        private T[] _array;

        private bool _isDisposed;
        private int _count;

        /// <summary>
        /// Initializes a new list backed by the provided initial buffer.
        /// </summary>
        /// <param name="initialBuffer">The caller-owned buffer used as the initial storage</param>
        /// <param name="clearArray">Whether pooled arrays rented during growth should be cleared before they are returned to the pool. The caller-owned <paramref name="initialBuffer"/> is never cleared by this list.</param>
        public UnmanagedList(Span<T> initialBuffer, bool clearArray = false)
        {
            _clearArray = clearArray;
            _buffer = initialBuffer;
            _isDisposed = false;
            _array = null;
            _count = 0;
        }

        /// <summary>
        /// Initializes a new list with storage rented from the shared array pool.
        /// </summary>
        /// <param name="capacity">The minimum number of items the initial rented storage must hold. Zero is allowed.</param>
        /// <param name="clearArray">Whether pooled arrays should be cleared before they are returned to the pool.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is negative</exception>
        public UnmanagedList(int capacity, bool clearArray = false)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _array = ArrayPool<T>.Shared.Rent(capacity);
            _clearArray = clearArray;
            _isDisposed = false;
            _buffer = _array;
            _count = 0;
        }

        /// <summary>
        /// Returns any rented storage to the shared array pool and marks the list as disposed.
        /// </summary>
        /// <remarks>
        /// Disposing is idempotent. After disposal, all operations except repeated disposal throw <see cref="ObjectDisposedException"/>.
        /// If the list still uses caller-provided initial storage, that storage is not cleared or returned.
        /// </remarks>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_array != null)
            {
                ArrayPool<T>.Shared.Return(_array, _clearArray);
            }

            _buffer = Span<T>.Empty;
            _array = null;
            _count = 0;
        }

        /// <summary>
        /// Adds an item to the end of the list, growing the backing storage when needed.
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            ThrowIfDisposed();

            if (_count >= _buffer.Length)
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
                ArrayPool<T>.Shared.Return(oldArray, _clearArray);
            }
        }

        private void ThrowIfDisposed()
        {
            if (!_isDisposed)
            {
                return;
            }

            throw new ObjectDisposedException(nameof(UnmanagedList<T>));
        }
    }
}
