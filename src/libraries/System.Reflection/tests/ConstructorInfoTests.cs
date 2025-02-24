// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class ConstructorInfoTests
    {
        [Fact]
        public void ConstructorName()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.Equal(3, constructors.Length);
            foreach (ConstructorInfo constructorInfo in constructors)
            {
                Assert.Equal(ConstructorInfo.ConstructorName, constructorInfo.Name);
            }
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            ConstructorInfo[] methodSampleConstructors1 = GetConstructors(typeof(ClassWith3Constructors));
            ConstructorInfo[] methodSampleConstructors2 = GetConstructors(typeof(ClassWith3Constructors));
            yield return new object[] { methodSampleConstructors1[0], methodSampleConstructors2[0], true };
            yield return new object[] { methodSampleConstructors1[1], methodSampleConstructors2[1], true };
            yield return new object[] { methodSampleConstructors1[2], methodSampleConstructors2[2], true };
            yield return new object[] { methodSampleConstructors1[1], methodSampleConstructors2[2], false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(ConstructorInfo constructorInfo1, ConstructorInfo constructorInfo2, bool expected)
        {
            Assert.Equal(expected, constructorInfo1.Equals(constructorInfo2));
            Assert.NotEqual(expected, constructorInfo1 != constructorInfo2);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            foreach (ConstructorInfo constructorInfo in constructors)
            {
                Assert.NotEqual(0, constructorInfo.GetHashCode());
            }
        }

        [Fact]
        public void Invoke()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.Equal(3, constructors.Length);
            ClassWith3Constructors obj = (ClassWith3Constructors)constructors[0].Invoke(null);
            Assert.NotNull(obj);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInvokingStaticConstructorsSupported))]
        public void Invoke_StaticConstructor_NullObject_NullParameters()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWithStaticConstructor));
            Assert.Equal(1, constructors.Length);
            object obj = constructors[0].Invoke(null, new object[] { });
            Assert.Null(obj);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInvokingStaticConstructorsSupported))]
        public void Invoke_StaticConstructorMultipleTimes()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection));
            Assert.Equal(1, constructors.Length);
            // The first time the static cctor is called, it should run the cctor twice
            // Once to initialize run the cctor as a cctor
            // The second to run it as a method which is invoked.
            Assert.Equal(0, ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection.VisibleStatics.s_cctorCallCount);
            object obj = constructors[0].Invoke(null, new object[] { });
            Assert.Null(obj);
            Assert.Equal(1, ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection.VisibleStatics.s_cctorCallCount);

            // Subsequent invocations of the static cctor should not run the cctor at all, as it has already executed
            // and running multiple times opens up the possibility of modifying read only static data
            obj = constructors[0].Invoke(null, new object[] { });
            Assert.Null(obj);
            Assert.Equal(1, ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection.VisibleStatics.s_cctorCallCount);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15024", TestRuntimes.Mono)]
        public void Invoke_StaticConstructor_ThrowsMemberAccessException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWithStaticConstructor));
            Assert.Equal(1, constructors.Length);
            Assert.Throws<MemberAccessException>(() => constructors[0].Invoke(new object[0]));
        }

        [Fact]
        public void Invoke_OneDimensionalArray()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(object[]));
            int[] arraylength = { 1, 2, 99, 65535 };

            // Try to invoke Array ctors with different lengths
            foreach (int length in arraylength)
            {
                // Create big Array with elements
                object[] arr = (object[])constructors[0].Invoke(new object[] { length });
                Assert.Equal(arr.Length, length);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67457", TestRuntimes.Mono)]
        public void Invoke_OneDimensionalArray_NegativeLengths_ThrowsOverflowException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(object[]));
            int[] arraylength = new int[] { -1, -2, -99 };
            // Try to invoke Array ctors with different lengths
            foreach (int length in arraylength)
            {
                // Create big Array with elements
                Exception ex = Assert.Throws<TargetInvocationException>(() => constructors[0].Invoke(new object[] { length }));
                Assert.IsType<OverflowException>(ex.InnerException);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67531", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void Invoke_TwoDimensionalArray_CustomBinder_IncorrectTypeArguments()
        {
            var ctor = typeof(int[,]).GetConstructor(new[] { typeof(int), typeof(int) });
            var args = new object[] { "1", "2" };
            var arr = (int[,])ctor.Invoke(BindingFlags.Default, new ConvertStringToIntBinder(), args, null);
            Assert.Equal(2, arr.Length);
            Assert.True(args[0] is int);
            Assert.True(args[1] is int);
        }

        [Fact]
        public void Invoke_OneParameter()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            ClassWith3Constructors obj = (ClassWith3Constructors)constructors[1].Invoke(new object[] { 100 });
            Assert.Equal(100, obj.intValue);
        }

        [Fact]
        public void Invoke_TwoParameters()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            ClassWith3Constructors obj = (ClassWith3Constructors)constructors[2].Invoke(new object[] { 101, "hello" });
            Assert.Equal(101, obj.intValue);
            Assert.Equal("hello", obj.stringValue);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67531", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void Invoke_TwoParameters_CustomBinder_IncorrectTypeArgument()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));

            var args = new object[] { "101", "hello" };
            ClassWith3Constructors obj = (ClassWith3Constructors)constructors[2].Invoke(BindingFlags.Default, new ConvertStringToIntBinder(), args, null);
            Assert.Equal(101, obj.intValue);
            Assert.Equal("hello", obj.stringValue);
            Assert.True(args[0] is int);
            Assert.True(args[1] is string);
        }

        [Fact]
        public void Invoke_NoParameters_ThowsTargetParameterCountException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.Throws<TargetParameterCountException>(() => constructors[2].Invoke(new object[0]));
        }

        [Fact]
        public void Invoke_ParameterMismatch_ThrowsTargetParameterCountException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.Throws<TargetParameterCountException>(() => (ClassWith3Constructors)constructors[2].Invoke(new object[] { 121 }));
        }

        [Fact]
        public void Invoke_ParameterWrongType_ThrowsArgumentException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            AssertExtensions.Throws<ArgumentException>(null, () => (ClassWith3Constructors)constructors[1].Invoke(new object[] { "hello" }));
        }

        [Fact]
        public void Invoke_ExistingInstance()
        {
            // Should not produce a second object.
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            ClassWith3Constructors obj1 = new ClassWith3Constructors(100, "hello");
            ClassWith3Constructors obj2 = (ClassWith3Constructors)constructors[2].Invoke(obj1, new object[] { 999, "initialized" });
            Assert.Null(obj2);
            Assert.Equal(999, obj1.intValue);
            Assert.Equal("initialized", obj1.stringValue);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15026", TestRuntimes.Mono)]
        public void Invoke_AbstractClass_ThrowsMemberAccessException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ConstructorInfoAbstractBase));
            Assert.Throws<MemberAccessException>(() => (ConstructorInfoAbstractBase)constructors[0].Invoke(new object[0]));
        }

        [Fact]
        public void Invoke_SubClass()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ConstructorInfoDerived));
            ConstructorInfoDerived obj = null;
            obj = (ConstructorInfoDerived)constructors[0].Invoke(new object[] { });
            Assert.NotNull(obj);
        }

        [Fact]
        public void Invoke_Struct()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(StructWith1Constructor));
            StructWith1Constructor obj;
            obj = (StructWith1Constructor)constructors[0].Invoke(new object[] { 1, 2 });
            Assert.Equal(1, obj.x);
            Assert.Equal(2, obj.y);
        }

        [Fact]
        public void IsConstructor_ReturnsTrue()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.All(constructors, constructorInfo => Assert.True(constructorInfo.IsConstructor));
        }

        [Fact]
        public void IsPublic()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.True(constructors[0].IsPublic);
        }

        public static ConstructorInfo[] GetConstructors(Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors.ToArray();
        }
    }

    // Metadata for Reflection
    public abstract class ConstructorInfoAbstractBase
    {
        public ConstructorInfoAbstractBase() { }
    }

    public class ConstructorInfoDerived : ConstructorInfoAbstractBase
    {
        public ConstructorInfoDerived() { }
    }

    public class ClassWith3Constructors
    {
        public int intValue = 0;
        public string stringValue = "";

        public ClassWith3Constructors() { }

        public ClassWith3Constructors(int intValue) { this.intValue = intValue; }

        public ClassWith3Constructors(int intValue, string stringValue)
        {
            this.intValue = intValue;
            this.stringValue = stringValue;
        }
    }

    public static class ClassWithStaticConstructor
    {
        static ClassWithStaticConstructor() { }
    }

    // Use this class only from the Invoke_StaticConstructorMultipleTimes method
    public static class ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection
    {
        public static class VisibleStatics
        {
            public static int s_cctorCallCount;
        }

        static ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection()
        {
            VisibleStatics.s_cctorCallCount++;
        }
    }

    public struct StructWith1Constructor
    {
        public int x;
        public int y;

        public StructWith1Constructor(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
