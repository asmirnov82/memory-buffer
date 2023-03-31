// Licensed to the Apache Software Foundation (ASF) under one or more
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership.
// The ASF licenses this file to You under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with
// the License.  You may obtain a copy of the License at
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
using System.Threading;

namespace Gimpo.Memory
{
    /// <summary>
    /// An abstraction that is used for allocation a contiguous memory block.
    /// </summary>
    public abstract class MemoryAllocator
    {
        private static readonly Lazy<MemoryAllocator> _default = new Lazy<MemoryAllocator>(BuildDefault, true);

        private long _allocatedBytes;
        private long _allocations;
                
        public static MemoryAllocator Default => _default.Value;

        /// <summary>
        /// Returns the total amount of allocations performed by this instance.
        /// </summary>
        public long Allocations => Interlocked.Read (ref _allocations);
        
        /// <summary>
        /// Returns the total amount of bytes allocated by this instance.
        /// </summary>
        public long AllocatedBytes => Interlocked.Read (ref _allocatedBytes);
                
        protected MemoryAllocator()
        {}

        /// <summary>
        /// Allocates contiguous memory block and returns it's owner who is responsible for disposing of the underlying memory appropriately.
        /// </summary>
        /// <typeparam name="T">The type of elements to store inside an allocated memory block.</typeparam>
        /// <param name="length"></param>
        /// <param name="skipZeroClear">True if not required to clear allocated memory with zero values.</param>
        /// <returns>Memory owner.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public IMemoryOwner<T> Allocate<T>(int length, bool skipZeroClear = false)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (length == 0)
            {
                return new NullMemoryOwner<T>();
            }

            IMemoryOwner<T> memoryOwner = AllocateInternal<T>(length, skipZeroClear, out int allocatedBytes);

            Interlocked.Increment(ref _allocations);
            Interlocked.Add(ref _allocatedBytes, allocatedBytes);

            return memoryOwner;
        }

        private static MemoryAllocator BuildDefault()
        {
            return new NativeMemoryAllocator();
        }

        protected abstract IMemoryOwner<T> AllocateInternal<T>(int length, bool zeroMemory, out int bytesAllocated);
    }
}
