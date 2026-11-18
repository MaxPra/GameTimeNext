namespace GameTimeNext.Core.Application.General.BackgroundProcesses
{
    /// <summary>
    /// Exception of another process, known only as text: Exception.ToString() sent by the remote monitoring, or the error
    /// output of a crashed process. ToString returns that text unchanged, so the log stores it as it was written there.
    /// </summary>
    internal sealed class ForwardedException : Exception
    {
        private readonly string _text;

        public ForwardedException(string text) : base(GetFirstLine(text))
        {
            _text = text;
        }

        public override string ToString() => _text;

        private static string GetFirstLine(string text)
        {
            int lineEnd = text.IndexOfAny(['\r', '\n']);
            return lineEnd < 0 ? text : text[..lineEnd];
        }
    }
}
