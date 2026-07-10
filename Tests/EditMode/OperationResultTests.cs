using NUnit.Framework;
using Systems.SimpleCore.Operations;

namespace Systems.SimpleCore.Tests
{
    public sealed class OperationResultTests
    {
        [Test]
        public void Success_CreatesSuccessResultWithExpectedCodes()
        {
            OperationResult result = OperationResult.Success(7, 11, 13);

            Assert.IsTrue(OperationResult.IsSuccess(result));
            Assert.IsFalse(OperationResult.IsError(result));
            Assert.IsTrue(result);
            Assert.AreEqual(7, result.systemCode);
            Assert.AreEqual(11, result.resultCode);
            Assert.AreEqual(13U, result.userCode);
        }

        [Test]
        public void Error_SetsErrorBitButKeepsSystemIdentity()
        {
            OperationResult result = OperationResult.Error(7, 11, 13);

            Assert.IsTrue(OperationResult.IsError(result));
            Assert.IsFalse(OperationResult.IsSuccess(result));
            Assert.IsFalse(result);
            Assert.AreEqual((ushort)(7 | (1 << 15)), result.systemCode);
            Assert.IsTrue(OperationResult.IsFromSystem(result, 7));
            Assert.IsTrue(OperationResult.IsFromSystem(result, 7 | (1 << 15)));
        }

        [Test]
        public void AreSimilar_IgnoresUserCodeOnly()
        {
            OperationResult first = OperationResult.Success(2, 3, 100);
            OperationResult second = OperationResult.Success(2, 3, 200);
            OperationResult differentResultCode = OperationResult.Success(2, 4, 100);
            OperationResult error = OperationResult.Error(2, 3, 100);

            Assert.IsTrue(OperationResult.AreSimilar(first, second));
            Assert.IsFalse(OperationResult.AreExactlySame(first, second));
            Assert.IsFalse(OperationResult.AreSimilar(first, differentResultCode));
            Assert.IsFalse(OperationResult.AreSimilar(first, error));
        }

        [Test]
        public void AreExactlySame_ComparesAllPackedFields()
        {
            OperationResult first = OperationResult.Error(5, OperationResult.ERROR_DENIED, 77);
            OperationResult second = OperationResult.Error(5, OperationResult.ERROR_DENIED, 77);
            OperationResult differentUserCode = OperationResult.Error(5, OperationResult.ERROR_DENIED, 78);

            Assert.IsTrue(OperationResult.AreExactlySame(first, second));
            Assert.IsFalse(OperationResult.AreExactlySame(first, differentUserCode));
        }
    }
}
