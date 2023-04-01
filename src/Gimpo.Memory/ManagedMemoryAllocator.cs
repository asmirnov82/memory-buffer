// Copyright 2016-2019 The Apache Software Foundation (Apache Arrow)
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

namespace Gimpo.Memory
{
	/// <summary>
	/// Allocates memory in the managed heap using MemoryPool to reuse previously allocated resources.
	/// Allows to minimize memory allocations and garbage collection overhead and hence improve performance.
	/// </summary>
	public sealed class ManagedMemoryAllocator : MemoryAllocator
	{
		protected override IMemoryOwner<T> AllocateInternal<T>(int length, bool skipZeroClear, out int bytesAllocated)
		{
			bytesAllocated = length;

			var memory = MemoryPool<T>.Shared.Rent(length);
			
			if (!skipZeroClear)
				ZeroMemory(memory.Memory);

            return memory;
		}

		private void ZeroMemory<T>(Memory<T> memory) 
		{
			memory.Span.Clear();
		}
	}
}