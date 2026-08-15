namespace OtakuQuest.Server.Services
{
    public class ServiceResult<T>
    {
        public bool Succeeded { get; }
        public T? Data { get; }
        public string? Error { get; }
        public int ErrorStatusCode { get; }

        private ServiceResult(bool succeeded, T? data, string? error, int errorStatusCode)
        {
            Succeeded = succeeded;
            Data = data;
            Error = error;
            ErrorStatusCode = errorStatusCode;
        }

        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T>(true, data, null, 0);
        }

        public static ServiceResult<T> Failure(string error, int errorStatusCode = 400)
        {
            return new ServiceResult<T>(false, default, error, errorStatusCode);
        }
    }
}
