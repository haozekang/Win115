using System;
using System.IO;
using System.Threading.Tasks;
using Tanovo.ExtensionMethods;

namespace Win115.Helpers
{
    public static class LogHelper
    {
        private static string LogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Win115", "Logs");

        public static async Task Error(string trae) 
        {
            if (!Directory.Exists(LogPath))
            {
                try
                {
                    Directory.CreateDirectory(LogPath);
                }
                catch
                {
                    return;
                }
            }
            try
            {
                var t = DateTime.Now;
                await using var fs = new FileStream(@$"{LogPath}/Log-ERR-{t:yyyy-MM-dd}.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                fs.Position = fs.Length;
                await fs.WriteAsync($"{t:yyyy-MM-dd HH:mm:ss.ffff}|ERR|{trae}{Environment.NewLine}".GetBytes());
                await fs.FlushAsync();
            }
            catch
            {
                // ignored
            }
        }

        public static async Task Error(Exception err) 
        {
            await Error(err.Message);
            if (err.StackTrace.IsNotBlank())
            {
                await Error(err.StackTrace);
            }
        }

        public static async Task Trace(string trace) 
        {
            if (!Directory.Exists(LogPath))
            {
                try
                {
                    Directory.CreateDirectory(LogPath);
                }
                catch
                {
                    return;
                }
            }
            try
            {
                var t = DateTime.Now;
                await using var fs = new FileStream(@$"{LogPath}/Log-TRA-{t:yyyy-MM-dd}.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                fs.Position = fs.Length;
                await fs.WriteAsync($"{t:yyyy-MM-dd HH:mm:ss.ffff}|TRA|{trace}{Environment.NewLine}".GetBytes());
                await fs.FlushAsync();
            }
            catch
            {
                // ignored
            }
        }

        public static async Task Trace(Exception err) 
        {
            await Trace(err.Message);
            if (err.StackTrace.IsNotBlank())
            {
                await Trace(err.StackTrace);
            }
        }
    }
}
