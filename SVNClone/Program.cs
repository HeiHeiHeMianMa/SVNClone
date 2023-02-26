using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using SharpSvn;
using SharpSvn.Remote;

namespace SVNClone
{
    internal class Program
    {
        static bool isPaused;

        static void Main(string[] args)
        {
            var SourceUrl = IniHelper.GetValue("SourceUrl");
            var TargetUrl = IniHelper.GetValue("TargetUrl");
            var TargetPath = IniHelper.GetValue("TargetPath");
            if (SourceUrl == null ||
                TargetUrl == null ||
                TargetPath == null)
            {
                Console.WriteLine("缺少配置项");
                return;
            }

            var client = new SvnClient();
            var repositoryUrl = new Uri(SourceUrl);
            var sourceMaxVersion = 106581;//GetLastLog(ServerUrl).Revision;
            var targetMaxVersion = GetLastVersionByLog(GetLastLog(TargetUrl));

            if (targetMaxVersion == -1)
            {
                Console.WriteLine("读取目标仓库最新版本失败");
                return;
            }

            ChckPaused();
            Console.WriteLine("启动");
            Console.WriteLine($"源库:{SourceUrl}\t 最新版本:{sourceMaxVersion}");
            Console.WriteLine($"目标库:{TargetUrl}\t 最新版本:{targetMaxVersion}");
            Console.WriteLine($"目标本地:{TargetPath}");

            var logArgs = new SvnLogArgs
            {
                Limit = 1000,
                Start = new SvnRevision(targetMaxVersion + 1),
                End = new SvnRevision(SvnRevisionType.Head)
            };

            client.GetLog(repositoryUrl, logArgs, out var logItems);

            while (logItems.Count > 0)
            {
                foreach (var item in logItems)
                {
                    if(isPaused)
                    {
                        Console.WriteLine($"[{DateTime.Now}] 已暂停");
                        while (isPaused) { }
                        Console.WriteLine($"[{DateTime.Now}] 已继续");
                    }

                    client.Update(TargetPath);
                    Console.WriteLine($"[{DateTime.Now}] 开始合并 {item.ToLogStr()}");
                    client.Merge(TargetPath, SourceUrl, new SvnRevisionRange(item.Revision - 1, item.Revision));
                    Console.WriteLine($"[{DateTime.Now}] 开始提交");
                    client.Commit(TargetPath, new SvnCommitArgs { LogMessage = item.ToLogStr() });
                    Console.WriteLine($"[{DateTime.Now}] 提交完成");
                }

                var startVersion = logItems[logItems.Count - 1].Revision + 1;
                if(startVersion > sourceMaxVersion) break;

                logArgs.Start = new SvnRevision(startVersion);
                logArgs.End = new SvnRevision(SvnRevisionType.Head); // 获取最新的版本号
                client.GetLog(repositoryUrl, logArgs, out logItems);
            }

            Console.WriteLine($"结束");
        }

        static void ChckPaused()
        {
            Thread inputThread = new Thread(() =>
            {
                while (true)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Spacebar)
                    {
                        Console.WriteLine($"[{DateTime.Now}]" + (isPaused ? "尝试继续" : "尝试暂停"));
                        isPaused = !isPaused;
                    }
                }
            });
            inputThread.Start();
        }

        static SvnLogEventArgs GetLastLog(string url)
        {
            using (var client = new SvnClient())
            {
                // 获取所有日志
                var logArgs = new SvnLogArgs
                {
                    Start = new SvnRevision(SvnRevisionType.Head),
                    End = new SvnRevision(SvnRevisionType.Head),
                    Limit = 1,                    
                };
                client.GetLog(new Uri(url), logArgs, out var logItems);
                return logItems[0];
            }
        }

        static int GetLastVersionByLog(SvnLogEventArgs log)
        {
            if (log.Revision == 0) return 0;
            if (string.IsNullOrEmpty(log.LogMessage)) return -1;

            var foo = log.LogMessage.Split('#');
            if (foo.Length < 2) return -1;

            if (int.TryParse(foo[0], out var version))
                return version;
            else
                return -1;
        }

    }

    static class Extens
    {
        public static string ToLogStr(this SvnLogEventArgs log)
        {
            return $"{log.Revision}#\t{log.Time.AddHours(8)}\t{log.Author}\t{log.LogMessage}";
        }
    }
}