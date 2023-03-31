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
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Gimpo.Memory
{
    /// <summary>
    /// The NativeMemoryManager<T> class is used to allocate and own a block of native memory. 
    /// Actual data stored inside allocated memory is aligned to preconfigured boundary.
    /// Data aligment allows to take advantage of the latest SIMD (Single input multiple data) operations included in modern processors for native vectorized optimization.
    /// </summary>
    /// <typeparam name="T">The type of items in the memory block managed by this memory manager.</typeparam>
    public sealed class NativeMemoryManager<T> : MemoryManager<T>
    {
        private object _lock = new object();
        private readonly int _offset;
        private readonly int _length;
        private readonly long _allocatedBytes;

        private IntPtr _ptr;
        private int _retainedCount;
        private bool _disposed;

        public long AllocatedBytes => _allocatedBytes;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="length">The number of elements in the memory buffer.</param>
        /// <param name="alignment">Aligment in bytes.</param>
        /// <param name="skipZeroClear">If True allocated bytes are not reseted to zero.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public NativeMemoryManager(int length, int alignment = 64, bool skipZeroClear = false)
        {
            long requiredBytes = (long)length * Unsafe.SizeOf<T>() + alignment;

            //Check x64 or x32 platform
            if (IntPtr.Size == 4 && requiredBytes > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(length), length, Resources.ExceededMemoryLimitOn32Bit);

            //TODO use NativeMemory.AlignedAlloc for .Net 6.0 and higher
            IntPtr ptr = Marshal.AllocHGlobal(new IntPtr(requiredBytes));
            int offset = (int)(alignment - (ptr.ToInt64() & (alignment - 1)));

            _ptr = ptr;
            _offset = offset;
            _length = length;

            GC.AddMemoryPressure(requiredBytes);

            // Ensure all allocated memory is zeroed.
            if (!skipZeroClear)
                ZeroMemory(ptr, (uint)requiredBytes);

            _allocatedBytes = requiredBytes;
        }

        ~NativeMemoryManager()
        {
            Dispose(false);
        }

        /// <summary>
        /// Returns a memory span that wraps the underlying memory buffer and is aligned to preconfigured boundary.
        /// </summary>
        /// <returns></returns>
        public override unsafe Span<T> GetSpan()
        {
            void* ptr = CalculatePointer(0);
            return new Span<T>(ptr, _length);
        }

        ///<inheritdoc/>
        public override unsafe MemoryHandle Pin(int elementIndex = 0)
        {                                    
            if ((uint)elementIndex > (uint)_length)
            {
                throw new ArgumentOutOfRangeException(nameof(elementIndex));
            }

            lock (_lock)
            {
                _retainedCount++;

                void* ptr = CalculatePointer(elementIndex);
                return new MemoryHandle(ptr, default, this);
            }
        }

        /// <summary>
        /// Returns true if underlying memory was disposed.
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                lock (_lock)
                {
                    return _disposed && _retainedCount == 0;
                }
            }
        }

        /// <summary>
        /// Returns true if underlying memory is pinned.
        /// </summary>
        public bool IsRetained
        {
            get
            {
                lock (_lock)
                {
                    return _retainedCount > 0;
                }
            }
        }

        ///<inheritdoc/>
        public override void Unpin()
        {
            lock (_lock)
            {
                if (_retainedCount > 0)
                {
                    _retainedCount--;
                    if (_retainedCount == 0 && _disposed)
                        DisposeInternal();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (_lock)
            {
                _disposed = true;
                if (_retainedCount == 0)
                    DisposeInternal();
            }
        }

        private void DisposeInternal()
        {
            // Free only once.
            if (_ptr != IntPtr.Zero)
            {
                //TODO Use NativeMemory.AlignedFree for .Net 6.0 and higher
                Marshal.FreeHGlobal(_ptr);
                Interlocked.Exchange(ref _ptr, IntPtr.Zero);
                GC.RemoveMemoryPressure(_length * Unsafe.SizeOf<T>());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void* CalculatePointer(int index) =>
            (_ptr + _offset + Unsafe.SizeOf<T>() * index).ToPointer();

        unsafe private static void ZeroMemory(IntPtr ptr, uint byteCount) =>
            Unsafe.InitBlockUnaligned(ptr.ToPointer(), 0, byteCount);
    }
}