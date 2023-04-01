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

using System.Buffers;
using System.Drawing;
using Xunit;
using FluentAssertions;

namespace Gimpo.Memory.Tests
{
    public class NativeMemoryManagerTest
    {
        /// <summary>
        /// Ensure allocated memory block is properly aligned.
        /// </summary>
        [Theory]
        [InlineData(1, 64)]
        [InlineData(8, 64)]
        [InlineData(64, 64)]
        [InlineData(128, 64)]
        [InlineData(256, 64)]
        [InlineData(1, 128)]
        [InlineData(8, 128)]
        [InlineData(64, 128)]
        [InlineData(128, 128)]
        [InlineData(256, 128)]
        public unsafe void ConstructionTestAllocatesAligned(int size, int aligment)
        {
            TestAllocatesAligned<byte>(size, aligment);
            TestAllocatesAligned<short>(size, aligment);
            TestAllocatesAligned<int>(size, aligment);
            TestAllocatesAligned<long>(size, aligment);
            TestAllocatesAligned<double>(size, aligment);
            TestAllocatesAligned<decimal>(size, aligment);
        }

        private unsafe void TestAllocatesAligned<T>(int size, int aligment) where T : unmanaged
        {
            //Act
            var memoryManager = new NativeMemoryManager<T>(size, aligment);
            var span = memoryManager.Memory.Span;

            //Assert
            fixed (T* ptr = &span.GetPinnableReference())
            {
                long diffWithAligmentBoundary = new IntPtr(ptr).ToInt64() % aligment;
                diffWithAligmentBoundary.Should().Be(0);
            }
        }

        /// <summary>
        /// Ensure allocated memory block is initialized with zeroes.
        /// </summary>
        [Fact]
        public void ConstructionTestIsZeroInitialized()
        {            
            //Act
            var memoryManager = new NativeMemoryManager<int>(10);
            var span = memoryManager.Memory.Span;

            //Assert
            foreach (var value in span)
            {
                value.Should().Be(0);
            }
        }

        /// <summary>
        /// Ensure allocated memory block is initialized with zeroes.
        /// </summary>
        [Theory]
        [InlineData(0, 32)]
        [InlineData(2, 32)]
        [InlineData(4, 32)]
        [InlineData(256, 32)]
        [InlineData(0, 64)]
        [InlineData(2, 64)]
        [InlineData(4, 64)]
        [InlineData(256, 64)]
        public void ConstructionTestAllocatesCorrectAmountOfMemory(int count, int aligment)
        {
            //Act
            TestAllocatesCorrectAmountOfMemory<int>(count, aligment);
            TestAllocatesCorrectAmountOfMemory<long>(count, aligment);
            TestAllocatesCorrectAmountOfMemory<double>(count, aligment);
            TestAllocatesCorrectAmountOfMemory<decimal>(count, aligment);

        }

        private unsafe void TestAllocatesCorrectAmountOfMemory<T>(int count, int aligment) where T : unmanaged
        {
            //Act
            var memoryManager = new NativeMemoryManager<T>(count, aligment);
            memoryManager.AllocatedBytes.Should()
                .BeGreaterThanOrEqualTo(count * sizeof(T))
                .And
                .BeLessThanOrEqualTo(count * sizeof(T) + aligment);
        }
    }
}