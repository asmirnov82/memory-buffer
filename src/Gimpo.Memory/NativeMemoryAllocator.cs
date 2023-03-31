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

using Gimpo.Memory;
using System;
using System.Buffers;

namespace Gimpo.Memory
{
    /// <summary>
    /// NativeMemoryAllocator class is used for allocation of contiguous block memory blocks in native memory.
    /// </summary>
    public sealed class NativeMemoryAllocator : MemoryAllocator
    {
        private readonly int _alignment;
        
        public NativeMemoryAllocator(int alignment = 64)
            : base() 
        {
            _alignment = alignment;
        }

        protected override IMemoryOwner<T> AllocateInternal<T>(int length, bool skipZeroClear, out int allocatedBytes)
        {
            var manager = new NativeMemoryManager<T>(length, _alignment, skipZeroClear);
            allocatedBytes = (int)manager.AllocatedBytes;

            return manager;
        }
    }
}
