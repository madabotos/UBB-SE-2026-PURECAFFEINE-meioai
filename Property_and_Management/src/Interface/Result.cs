using System;

namespace Property_and_Management.Src.Interface
{
    /// <summary>
    /// Lightweight result envelope for service operations that can fail with a
    /// domain-specific error. Avoids sentinel-int return codes.
    /// </summary>
    /// <typeparam name="TSuccess">Payload type returned on success.</typeparam>
    /// <typeparam name="TError">Error enum returned on failure.</typeparam>
    public sealed class Result<TSuccess, TError>
    {
        private readonly TSuccess value;
        private readonly TError error;

        private Result(bool isSuccess, TSuccess value, TError error)
        {
            IsSuccess = isSuccess;
            this.value = value;
            this.error = error;
        }

        /// <summary>
        /// True when the operation succeeded and <see cref="Value"/> is populated.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Success payload. Only meaningful when <see cref="IsSuccess"/> is true.
        /// Reading it on a failure is an error in the caller.
        /// </summary>
        public TSuccess Value
        {
            get
            {
                if (!IsSuccess)
                {
                    throw new InvalidOperationException("Cannot read Value on a failed Result.");
                }

                return value;
            }
        }

        /// <summary>
        /// Failure payload. Only meaningful when <see cref="IsSuccess"/> is false.
        /// Reading it on a success is an error in the caller.
        /// </summary>
        public TError Error
        {
            get
            {
                if (IsSuccess)
                {
                    throw new InvalidOperationException("Cannot read Error on a successful Result.");
                }

                return error;
            }
        }

        /// <summary>
        /// Builds a successful result carrying <paramref name="value"/>.
        /// </summary>
        public static Result<TSuccess, TError> Success(TSuccess value)
        {
            return new Result<TSuccess, TError>(true, value, default!);
        }

        /// <summary>
        /// Builds a failed result carrying <paramref name="error"/>.
        /// </summary>
        public static Result<TSuccess, TError> Failure(TError error)
        {
            return new Result<TSuccess, TError>(false, default!, error);
        }
    }
}
