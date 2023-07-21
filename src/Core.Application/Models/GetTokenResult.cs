namespace Core.Application.Models
{
    public struct GetTokenResult
    {
        private readonly bool _isSuccess;
        private readonly GetTokenError? _error;
        private readonly GetTokenSuccess? _value;

        private GetTokenResult(bool isSuccess, GetTokenSuccess? value, GetTokenError? error)
        {
            _isSuccess = isSuccess;
            _value = value;
            _error = error;
        }

        public bool IsSuccess => _isSuccess;

        public bool IsFailure => !_isSuccess;

        public GetTokenError Error
        {
            get
            {
                if (_isSuccess)
                {
                    throw new InvalidOperationException("Property Error of Result cannot be accessed because Result is successful.");
                }

                return _error;
            }
        }

        public GetTokenSuccess Success
        {
            get
            {
                if (!_isSuccess)
                {
                    throw new InvalidOperationException("Property Value of Result cannot be accessed because Result is failure.");
                }
                return _value;
            }
        }

        public static GetTokenResult ToSuccess(GetTokenSuccess value)
        {
            return new GetTokenResult(true, value, default);
        }

        public static GetTokenResult ToError(GetTokenError error)
        {
            return new GetTokenResult(false, default, error);
        }
    }
}
