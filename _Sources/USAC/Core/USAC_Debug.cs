namespace USAC
{
    // 定义开发者日志开关
    public static class USAC_Debug
    {
        // 检查并执行日志输出逻辑
        public static bool EnableLog = false;

        public static void Log(string message)
        {
            if (EnableLog)
            {
                Verse.Log.Message(message);
            }
        }
    }
}
