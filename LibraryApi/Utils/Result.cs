namespace LibraryApi.Utils
{
    public enum ResultErrorType
    {
        None,
        NotFound,
        BadRequest,
        Forbidden,
        ServerError,
    }

    public class Result<T>
    {
        public T Data { get; }
        public bool IsSuccess { get; }
        public ResultErrorType ErrorType { get; }
        public string? ErrorMessage { get; }

        private Result(T data, bool isSuccess, ResultErrorType errorType, string? errorMessage)
        {
            Data = data;
            IsSuccess = isSuccess;
            ErrorType = errorType;
            ErrorMessage = errorMessage;
        }

        public static Result<T> Success(T data) => new Result<T>(data, true, ResultErrorType.None, string.Empty);
        public static Result<T> Failure(ResultErrorType errorType, string? errorMessage) => new Result<T>(default, false, errorType, errorMessage);
        public static Result<T> Failure(ResultErrorType errorType) => new Result<T>(default, false, errorType, null);
    }

    public class Result
    {
        public bool IsSuccess { get; }
        public ResultErrorType ErrorType { get; }
        public string? ErrorMessage { get; }

        private Result(bool isSuccess, ResultErrorType errorType, string? errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorType = errorType;
            ErrorMessage = errorMessage;
        }

        public static Result Success() => new Result(true, ResultErrorType.None, null);

        public static Result Failure(ResultErrorType errorType, string? errorMessage) => new Result(false, errorType, errorMessage);
        public static Result Failure(ResultErrorType errorType) => new Result(false, errorType, null);
    }
}

