// Copyright 2023 Aleksei Smirnov
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gimpo.Memory
{
    /// <summary>
    /// Represents the wrapper over a contiguous memory block.
    /// </summary>
    /// <typeparam name="T">The type of the elements strored inside a memory block.</typeparam>
    public class MemoryStorage<T> : IDisposable
    {
        private IMemoryOwner<T> _memoryOwner;
        private Memory<T> _memory;
        private Memory<T> Memory => _memoryOwner != null ? _memoryOwner.Memory : _memory;

        /// <summary>
        /// Return true, if memory is not allocated.
        /// </summary>
        public bool IsEmpty => Memory.IsEmpty;

        /// <summary>
        /// Gets the total number of elements the internal memory can hold without reallocation.
        /// </summary>
        public int Capacity => Memory.Length;

        /// <summary>
        ///  Returns a memory span that wraps the underlying memory buffer.
        /// </summary>
        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Memory.Span;
        }

        /// <summary>
        /// Creates an instance of <see cref="MemoryStorage{T}"/> class.
        /// </summary>
        /// <param name="capacity">Number of elements that can be stored in the storage.</param>
        /// <param name="skipZeroClear">True if not required to clear allocated memory with zero values.</param>
        /// <param name="allocator">Memory allocator.</param>
        /// <returns>An instance of <see cref="MemoryStorage{T}"/> class.</returns>
        public static MemoryStorage<T> Build(int capacity, bool skipZeroClear = false, MemoryAllocator allocator = default)
        {
            MemoryAllocator memoryAllocator = allocator ?? MemoryAllocator.Default;
            IMemoryOwner<T> memoryOwner = memoryAllocator.Allocate<T>(capacity, skipZeroClear);

            return new MemoryStorage<T>(memoryOwner);
        }

        /// <summary>
        /// Contructor, creates wrapper over already allocated memory.
        /// </summary>
        /// <param name="data">Already allocated memory.</param>
        public MemoryStorage(Memory<T> data)
        {
            _memoryOwner = null;
            _memory = data;
        }

        /// <summary>
        /// Creates a new object that contains a copy of the first <paramref name="numberOfElements"/> of elements.
        /// </summary>
        /// <param name="numberOfElements">Amount of elements to copy.</param>
        /// <param name="allocator">Memory allocator.</param>
        /// <returns>New instance of <see cref="MemoryStorage{T}"/> class.</returns>
        public MemoryStorage<T> Clone(int numberOfElements = int.MaxValue, MemoryAllocator allocator = default)
        {
            int size = Math.Min(numberOfElements, this.Capacity);

            //Create new Storage
            var ret = Build(size, false, allocator);

            //Copy values
            Span.Slice(0, size).CopyTo(ret.Span);

            return ret;
        }

        /// <summary>
        /// Ensures that the capacity of this list is at least the specified capacity.
        /// If the current capacity is less than capacity, it is successively increased to twice the current capacity until it is at least the specified capacity.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        /// <param name="allocator">Allocator used for memory allocation.</param>
        public void EnsureCapacity(int capacity, MemoryAllocator allocator = default)
        {
            if (capacity <= Capacity)
                return;

            //Allocate new memory
            MemoryAllocator memoryAllocator = allocator ?? MemoryAllocator.Default;
            IMemoryOwner<T> memoryOwner = memoryAllocator.Allocate<T>(capacity);

            //Copy values
            Memory.CopyTo(memoryOwner.Memory);

            //Dispose 
            _memory = null;
            _memoryOwner?.Dispose();

            //Set new memory
            _memoryOwner = memoryOwner;
        }

        /// <summary>
        /// Fills the elements of the underlying memory with a specified value.
        /// </summary>
        /// <param name="value">The value to assign to each element inside the defined range.</param>
        /// <param name="startingIndex">The starting position in this instance where value will be assigned.</param>
        /// <param name="endingIndex">The last position in this instance where value will be assigned.</param>
        public void FillWithValue(T value, int startingIndex = 0, int endingIndex = int.MaxValue)
        {
            Debug.Assert(startingIndex > 0);
            Debug.Assert(endingIndex > startingIndex);

            endingIndex = Math.Min(Capacity, endingIndex);
            Memory.Span.Slice(startingIndex, endingIndex).Fill(value);
        }

        /// <summary>
        /// Releases internaly used unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _memoryOwner?.Dispose();
            _memoryOwner = null;
        }

        private MemoryStorage(IMemoryOwner<T> memoryOwner)
        {
            _memoryOwner = memoryOwner;
            _memory = Memory<T>.Empty;
        }
    }
}