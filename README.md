# nk7-data-structures

Lightweight custom data structures for Unity/C#, focused on predictable performance, explicit ownership, and low-allocation runtime code

## Features
- Stack-only `UnmanagedList<T>` for unmanaged values backed by `Span<T>`
- Caller-provided buffers via `stackalloc`, arrays, or other span-compatible storage
- Optional pooled storage through `ArrayPool<T>` when an initial capacity is used or the list grows
- Ref-return indexer for in-place value mutation without extra copies
- Explicit `Dispose` pattern for returning rented arrays to the shared pool, with optional clearing
- No external package dependencies

## Table of Contents
- [Installation](#installation)
- [Unity Package Manager](#unity-package-manager)
- [Manual Installation](#manual-installation)
- [Quick Start](#quick-start)
- [1. Use a caller-owned buffer](#1-use-a-caller-owned-buffer)
- [2. Use pooled storage](#2-use-pooled-storage)
- [3. Mutate items by reference](#3-mutate-items-by-reference)
- [4. Clear pooled storage on return](#4-clear-pooled-storage-on-return)
- [Allocation Model](#allocation-model)
- [Runtime API](#runtime-api)
- [Requirements](#requirements)

## Installation

### Unity Package Manager
1. Open Unity Package Manager (`Window -> Package Manager`).
2. Click `+ -> Add package from git URL...`.
3. Enter `https://github.com/s-elovikov/nk7-data-structures.git?path=src`.

Unity does not auto-update Git-based packages; update the hash manually when needed or use [UPM Git Extension](https://github.com/mob-sakai/UpmGitExtension).

### Manual Installation
Copy the `src` folder into your project and reference the `Nk7.DataStructures` asmdef. The runtime API is under the `Nk7.DataStructures` namespace.

## Quick Start

### 1. Use a caller-owned buffer
Use a stack buffer when the expected item count is known and should stay allocation-free.

```csharp
using Nk7.DataStructures;

Span<int> buffer = stackalloc int[8];
using var values = new UnmanagedList<int>(buffer);

values.Add(10);
values.Add(20);

int count = values.Count;
int first = values[0];
```

If the list grows beyond the provided buffer, it rents a larger array from `ArrayPool<T>`. Keep `using var` or call `Dispose()` manually so rented storage is returned. The caller-owned initial buffer is never cleared or returned by the list.

### 2. Use pooled storage
Use an initial capacity when the collection should grow from pooled storage immediately.

```csharp
using Nk7.DataStructures;

using var values = new UnmanagedList<float>(64);

for (int i = 0; i < 64; i++)
{
    values.Add(i * 0.5f);
}
```

### 3. Mutate items by reference
The indexer returns `ref T`, so values can be edited in place.

```csharp
using Nk7.DataStructures;

Span<int> buffer = stackalloc int[4];
using var values = new UnmanagedList<int>(buffer);

values.Add(1);
values.Add(2);

ref int first = ref values[0];
first += 10;
```

### 4. Clear pooled storage on return
Pass `clearArray: true` when pooled arrays may contain data that should be cleared before returning to `ArrayPool<T>.Shared`.

```csharp
using Nk7.DataStructures;

using var values = new UnmanagedList<int>(16, clearArray: true);

values.Add(42);
```

This only applies to arrays rented from the pool. Caller-owned buffers passed through `Span<T>` are not cleared by `Dispose()`.

## Allocation Model
- `UnmanagedList<T>(Span<T>, bool clearArray = false)` uses caller-owned storage and does not rent until capacity is exceeded
- `UnmanagedList<T>(int capacity, bool clearArray = false)` rents storage from `ArrayPool<T>.Shared`
- Growth rents a larger pooled array, copies current items, and returns the previous rented array when one exists
- Growth from a zero-length buffer starts with a default capacity of 4
- `Dispose()` is idempotent, returns rented storage, and clears the list state
- Operations after disposal throw `ObjectDisposedException`
- `T` must be `unmanaged`, which keeps the list focused on compact value-type data

## Runtime API
```csharp
public ref struct UnmanagedList<T>
    where T : unmanaged
{
    public UnmanagedList(Span<T> initialBuffer, bool clearArray = false);
    public UnmanagedList(int capacity, bool clearArray = false);

    public int Count { get; }
    public int Capacity { get; }

    public ref T this[int index] { get; }

    public void Add(T item);
    public void Dispose();
}
```

- `Add(T item)` appends an item and grows the backing storage when needed
- `this[int index]` returns a reference to the stored item and throws `ArgumentOutOfRangeException` for invalid indexes
- `Count` returns the number of stored items
- `Capacity` returns the current backing buffer length
- `Dispose()` should be called for any list that might rent pooled storage
- `capacity` must be zero or greater when using the capacity constructor

## Requirements
- Unity 2021.2+
- C# support for `ref struct`, `Span<T>`, and pattern-based `using`
