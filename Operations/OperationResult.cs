namespace Systems.SimpleCore.Operations
{
    public readonly ref struct OperationResult<TData>
    {
        /// <summary>
        ///     Internal result
        /// </summary>
        public readonly OperationResult result;

        /// <summary>
        ///     Data associated with the result
        /// </summary>
        public readonly TData data;

        public OperationResult(int resultCode, int userCode = 0, TData data = default)
            : this(new OperationResult(resultCode, userCode), data)
        {
        }
        
        public OperationResult(OperationResult result, TData data)
        {
            this.result = result;
            this.data = data;
        }
        
        public static implicit operator OperationResult(OperationResult<TData> result) => result.result;

        public static implicit operator bool(OperationResult<TData> result) => result.result.resultCode == 0;

        public static explicit operator OperationResult<TData>(bool result)
            => result
                ? new OperationResult<TData>(OperationResult.GenericSuccess, default)
                : new OperationResult<TData>(OperationResult.Undefined, default);
        
        public static explicit operator TData(OperationResult<TData> result) => result.data;
    }

    /// <summary>
    ///     Represents result of a generic operation
    /// </summary>
    /// <remarks>
    ///     Values of Max, Max are considered as undefined result
    /// </remarks>
    public readonly ref struct OperationResult
    {
        public const int SUCCESS_CODE = 0;
        public const int UNDEFINED_CODE = int.MaxValue;
        
        public static OperationResult GenericSuccess => new(SUCCESS_CODE);

        public static OperationResult Undefined => new(UNDEFINED_CODE, UNDEFINED_CODE);

        /// <summary>
        ///     Result code, 0 for success, non-zero for failure
        ///     (to be used by SimpleKit)
        /// </summary>
        public readonly int resultCode;

        /// <summary>
        ///     User result code, here external codes can be used to further specify the result
        /// </summary>
        public readonly int userCode;

        public OperationResult<TData> WithData<TData>(TData data) => new(this, data);
        
        public OperationResult(int resultCode, int userCode = 0)
        {
            this.resultCode = resultCode;
            this.userCode = userCode;
        }

        public static implicit operator bool(OperationResult result) => result.resultCode == 0;

        public static explicit operator OperationResult(bool result) => result ? GenericSuccess : Undefined;
    }
}