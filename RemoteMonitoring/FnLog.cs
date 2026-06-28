namespace RemoteMonitoring
{
    public static class FnLog
    {
        public static void AddError(object source, string message, Exception? ex = null)
        {
            if (ex is not null)
                message = message += Environment.NewLine + ex.Message;

            WriteLog(source, "Error", message);
        }

        public static void AddWarning(object source, string message, Exception? ex = null)
        {
            if (ex is not null)
                message = message += Environment.NewLine + ex.Message;

            WriteLog(source, "Warning", message);
        }

        public static void AddInfo(object source, string message)
        {
            WriteLog(source, "Info", message);
        }

        private static void WriteLog(object source, string type, string message)
        {
            ConsoleColor origin = Console.ForegroundColor;

            switch (type.ToLower())
            {
                case "error":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "warning":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            Console.Write(type);
            Console.Write(" [");
            Console.Write(source.GetType().Name);
            Console.Write("] ");

            if (!type.ToLower().Equals("error")) Console.ForegroundColor = origin;

            Console.WriteLine(message);
            
            Console.ForegroundColor = origin;
        }
    }
}
