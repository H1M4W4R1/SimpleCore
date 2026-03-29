using System.Runtime.InteropServices;

namespace Systems.SimpleCore.Operations
{
    /// <summary>
    ///     Represents result of a generic operation
    /// </summary>
    [StructLayout(LayoutKind.Explicit)] public readonly ref struct OperationResult
    {
        /// <summary>
        ///     Result code for PERMITTED success data.
        /// </summary>
        /// <remarks>
        /// By design, this shares the same resultCode value as <see cref="ERROR_DENIED"/>.
        /// They are distinguished by the error bit in systemCode (bit 15), not by resultCode.
        /// <see cref="AreSimilar"/> compares both systemCode and resultCode, so a success-permitted
        /// and error-denied will only match if they also share the same systemCode (which includes
        /// the error bit), making false matches impossible in practice.
        /// </remarks>
        public const ushort SUCCESS_PERMITTED = ushort.MaxValue;

        /// <summary>
        ///     Result code for DENIED error data.
        /// </summary>
        /// <remarks>
        /// By design, this shares the same resultCode value as <see cref="SUCCESS_PERMITTED"/>.
        /// See <see cref="SUCCESS_PERMITTED"/> remarks for details on why this is safe.
        /// </remarks>
        public const ushort ERROR_DENIED = ushort.MaxValue;

        /// <summary>
        ///     Place for generic system and result codes
        /// </summary>
        public const ushort GENERIC_SPACE = 0;
        
        /// <summary>
        ///     All user system codes and result codes start from this value. Use it with offset rather
        ///     than hard-coding value as it may change in the future.
        /// </summary>
        public const ushort USER_SPACE_START = 1 << 9;
   
        /// <summary>
        ///     Vectorized result code
        /// </summary>
        [FieldOffset(0)] private readonly ulong vectorized;

        /// <summary>
        ///     Result code used in system for internal purposes1
        /// </summary>
        /// <remarks>
        ///     Top-most bit is used to determine if the result is success or error
        /// </remarks>
        [FieldOffset(0)] public readonly ushort systemCode;

        /// <summary>
        ///     Result code for precise result targeting
        /// </summary>
        [FieldOffset(sizeof(ushort))] public readonly ushort resultCode;

        /// <summary>
        ///     User result code, here external codes can be used to further specify the result
        /// </summary>
        [FieldOffset(sizeof(ushort) * 2)] public readonly uint userCode;

        /// <summary>
        ///     Creates new success result
        /// </summary>
        public static OperationResult Success(ushort systemCode, ushort resultCode, uint userCode = 0)
            => new(systemCode, resultCode, userCode);

        /// <summary>
        ///     Creates new error result
        /// </summary>
        public static OperationResult Error(ushort systemCode, ushort resultCode, uint userCode = 0)
            => new((ushort) (systemCode | (1 << 15)), resultCode, userCode);

        /// <summary>
        ///     Checks if result is success
        /// </summary>
        public static bool IsSuccess(in OperationResult operationResult)
            => (operationResult.systemCode & (1 << 15)) == 0;

        /// <summary>
        ///     Checks if result is error
        /// </summary>
        public static bool IsError(in OperationResult operationResult) => !IsSuccess(operationResult);

        /// <summary>
        ///     Checks if result is from specified system, used to determine if we are reading correct
        ///     result code
        /// </summary>
        public static bool IsFromSystem(in OperationResult operationResult, ushort systemCode)
            => (operationResult.systemCode & 0x7FFF) == (systemCode & 0x7FFF);

        /// <summary>
        ///     Checks if result is the same as other result, used to determine if we are reading correct,
        ///     however it does not check userCode to allow re-use of generic result codes
        /// </summary>
        public static bool AreSimilar(in OperationResult operationResult, in OperationResult other)
        {
            return operationResult.systemCode == other.systemCode &&
                   operationResult.resultCode == other.resultCode;
        }

        /// <summary>
        ///     Check if result is exactly the same as other result including userCode
        /// </summary>
        public static bool AreExactlySame(
            in OperationResult operationResult,
            in OperationResult other)
        {
            return operationResult.vectorized == other.vectorized;
        }


#region Operators and constructors

        public static implicit operator bool(OperationResult operationResult) => IsSuccess(operationResult);

        /// <remarks>
        /// IMPORTANT: The <c>vectorized = 0</c> assignment MUST remain first. It clears all 8 bytes
        /// of the overlapping union. The subsequent field assignments then write their values into the
        /// correct offsets. Reordering these assignments (e.g., assigning systemCode before vectorized)
        /// would cause vectorized to overwrite the previously set fields. The compiler requires all
        /// fields to be assigned in a struct constructor, so removing the vectorized assignment is not
        /// an option without restructuring the type.
        /// </remarks>
        internal OperationResult(ushort systemCode, ushort resultCode, uint userCode = 0)
        {
            vectorized = 0;
            this.resultCode = resultCode;
            this.systemCode = systemCode;
            this.userCode = userCode;
        }

#endregion
    }
}