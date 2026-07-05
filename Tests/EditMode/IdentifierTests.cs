using NUnit.Framework;
using Systems.SimpleCore.Identifiers;
using Systems.SimpleCore.Identifiers.Abstract;
using Unity.Mathematics;

namespace Systems.SimpleCore.Tests
{
    public sealed class IdentifierTests
    {
        [Test]
        public void NumberIdentifiers_DefaultInstancesAreNotCreated()
        {
            ID8 id8 = default;
            ID16 id16 = default;
            ID32 id32 = default;
            ID64 id64 = default;
            ID128 id128 = default;
            ID256 id256 = default;
            ID512 id512 = default;

            Assert.IsFalse(id8.IsCreated);
            Assert.IsFalse(id16.IsCreated);
            Assert.IsFalse(id32.IsCreated);
            Assert.IsFalse(id64.IsCreated);
            Assert.IsFalse(id128.IsCreated);
            Assert.IsFalse(id256.IsCreated);
            Assert.IsFalse(id512.IsCreated);
        }

        [Test]
        public void NumberIdentifiers_ConstructorsMarkValuesAsCreated()
        {
            ID8 id8 = new ID8(0);
            ID16 id16 = new ID16(0);
            ID32 id32 = new ID32(0);
            ID64 id64 = new ID64(0);
            ID128 id128 = new ID128(new uint4(1, 2, 3, 4));
            ID256 id256 = new ID256(new uint4x2(new uint4(1, 2, 3, 4), new uint4(5, 6, 7, 8)));
            ID512 id512 = new ID512(new uint4x4(
                new uint4(1, 2, 3, 4),
                new uint4(5, 6, 7, 8),
                new uint4(9, 10, 11, 12),
                new uint4(13, 14, 15, 16)));

            Assert.IsTrue(id8.IsCreated);
            Assert.IsTrue(id16.IsCreated);
            Assert.IsTrue(id32.IsCreated);
            Assert.IsTrue(id64.IsCreated);
            Assert.IsTrue(id128.IsCreated);
            Assert.IsTrue(id256.IsCreated);
            Assert.IsTrue(id512.IsCreated);
        }

        [Test]
        public void NumberIdentifiers_EqualityIncludesCreatedState()
        {
            ID32 defaultId = default;
            ID32 zeroId = new ID32(0);
            ID32 first = new ID32(42);
            ID32 second = new ID32(42);
            ID32 different = new ID32(43);

            Assert.IsFalse(defaultId.Equals(zeroId));
            Assert.IsTrue(first.Equals(second));
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
            Assert.IsFalse(first.Equals(different));
            Assert.Less(first.CompareTo(different), 0);
            Assert.Greater(different.CompareTo(first), 0);
        }

        [Test]
        public void NumberIdentifiers_ToStringUsesFixedWidthHex()
        {
            ID8 id8 = new ID8(0x0A);
            ID16 id16 = new ID16(0x00AB);
            ID32 id32 = new ID32(0x0000ABCD);
            ID64 id64 = new ID64(0x00000000ABCDEF12);
            ID128 id128 = new ID128(new uint4(0x1, 0x2, 0x3, 0x4));

            Assert.AreEqual("0A", id8.ToString());
            Assert.AreEqual("00AB", id16.ToString());
            Assert.AreEqual("0000ABCD", id32.ToString());
            Assert.AreEqual("00000000ABCDEF12", id64.ToString());
            Assert.AreEqual("0000000100000002-0000000300000004", id128.ToString());
        }

        [Test]
        public void NumberIdentifier_DebugTooltipIncludesValueAndCreatedState()
        {
            INumberIdentifier<uint> identifier = new ID32(0xCAFE);

            string tooltip = identifier.GetDebugTooltipText();

            StringAssert.Contains("Identifier data", tooltip);
            StringAssert.Contains("0000CAFE", tooltip);
            StringAssert.Contains("green", tooltip);
        }

        [Test]
        public void HashIdentifier_IsStableForTypeWithinCurrentProcess()
        {
            ulong firstHash = HashIdentifier.ComputeTypeHash(typeof(IdentifierTests));
            ulong secondHash = HashIdentifier.ComputeTypeHash(typeof(IdentifierTests));
            HashIdentifier first = HashIdentifier.New(typeof(IdentifierTests));
            HashIdentifier second = new HashIdentifier(firstHash);

            Assert.AreEqual(firstHash, secondHash);
            Assert.AreEqual(firstHash, first.Value);
            Assert.IsTrue(first.IsCreated);
            Assert.IsTrue(first.Equals(second));
            Assert.AreEqual(first.ToString(), second.ToString());
        }

        [Test]
        public void Snowflake128_NewCreatesUniqueOrderedIdentifiers()
        {
            Snowflake128 first = Snowflake128.New();
            Snowflake128 second = Snowflake128.New();

            Assert.IsTrue(first.IsCreated);
            Assert.IsTrue(second.IsCreated);
            Assert.AreNotEqual(first, second);
            Assert.Less(first.CompareTo(second), 0);
            StringAssert.Contains("Identifier data", first.GetDebugTooltipText());
        }

        [Test]
        public void Snowflake128_ManualConstructionSupportsEqualityAndFormatting()
        {
            Snowflake128 first = new Snowflake128(100, 5);
            Snowflake128 second = new Snowflake128(100, 5);
            Snowflake128 different = new Snowflake128(100, 6);

            Assert.IsTrue(first == second);
            Assert.IsTrue(first != different);
            Assert.AreEqual("0000000000000064-0000000000000005", first.ToString());
            Assert.IsFalse(Snowflake128.Empty.IsCreated);
        }
    }
}
