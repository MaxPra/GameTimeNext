namespace GameTimeNext.Core.Framework.Utils
{
    internal static class FnErrorMessage
    {
        public sealed class ErrorMessage
        {
            #region STATIC
            // Empty/Required
            public static readonly ErrorMessage CannotBeEmpty = new("&1 cannot be empty.");
            public static readonly ErrorMessage MustBeSelected = new("&1 must be selected.");
            public static readonly ErrorMessage IsRequired = new("&1 is required.");

            // Strings
            public static readonly ErrorMessage CannotExceedChars = new("&1 cannot exceed &2 characters.");
            public static readonly ErrorMessage MustExceedChars = new("&1 must contain at least &2 characters.");
            public static readonly ErrorMessage CannotStartWith = new("&1 cannot start with &2 (&3).");
            public static readonly ErrorMessage MustEditWith = new("&1 must end with &2.");

            // Numerics
            public static readonly ErrorMessage OnlyNumeric = new("Only numeric values allowed.");
            #endregion

            private string _message { get; }

            private ErrorMessage(string message)
            {
                this._message = message;
            }

            public string GetMessage(params string[] parameters)
            {
                CheckParameterCount(parameters);

                string temp = _message;

                for (int i = 0; i < parameters.Length; i++)
                {
                    string p = parameters[i];
                    temp = temp.Replace($"&{i + 1}", p);
                }

                return temp;
            }

            [Obsolete("Use GetMessage() instead.", true)]
            public override string ToString()
            {
                throw new InvalidOperationException("Use GetMessage() instead.");
            }

            private void CheckParameterCount(string[] parameters)
            {
                int countSupplied = parameters.Length;
                int countRequired = _message.Count(c => c == '&');

                if (!countSupplied.Equals(countRequired))
                    throw new ArgumentException($"Incorrect count of parameters passed. (Required: {countRequired}, Actual: {countSupplied})");
            }

            /// <summary>
            /// Used to later be able to find similar messages and combine them to standards.
            /// </summary>
            /// <param name="message"></param>
            /// <returns></returns>
            public static string GetCustomMessage(string message)
            {
                return message;
            }
        }
    }
}
