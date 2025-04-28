using Log4NetHelper;

namespace CompanioNc8
{
    /// <summary>
    /// This static class turns the non-invasive KeyboardHookManager into a singleton, accessible by all modules
    /// </summary>
    public static class LogHelper
    {
        private static Logger? _log;

        public static Logger Instance => _log ??= new Logger();
    }

}