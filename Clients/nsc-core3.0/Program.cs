using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using theori;
using theori.Graphics;
using theori.Platform.Windows;

namespace NeuroSonic.NetCore30
{
    static class TempFileWriter
    {
        const string FILE_NAME = "nsc-log.txt";

        private static readonly List<string> lines = new List<string>();

        private static readonly object flushLock = new object();

        public static void EmptyFile()
        {
            lock (flushLock)
            {
                File.Delete(FILE_NAME);
                File.WriteAllText(FILE_NAME, "");
            }
        }

        public static void WriteLine(string line)
        {
            lines.Add(line);
        }

        public static void Flush()
        {
            if (lines.Count == 0) return;

            lock (flushLock)
            {
                int count = lines.Count;
                using (var writer = new StreamWriter(File.Open(FILE_NAME, FileMode.Append)))
                {
                    for (int i = 0; i < count; i++)
                        writer.WriteLine(lines[i]);
                }
                lines.RemoveRange(0, count);
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // TODO: now that default skins aren't a thing, the only reason for this is
            //  persistent config across builds; setting the working directory in the project or
            //  just relying on the build directory is now preferable.
#if DEBUG
            string cd = System.Reflection.Assembly.GetEntryAssembly().Location;
            while (cd != null && !Directory.Exists(Path.Combine(cd, "InstallDir")))
                cd = Directory.GetParent(cd)?.FullName;

            if (cd != null && Directory.Exists(Path.Combine(cd, "InstallDir")))
                Environment.CurrentDirectory = Path.Combine(cd, "InstallDir");
#endif

#if !DEBUG
            try
            {
#endif
                Host.Platform = new WindowsPlatform();

                TempFileWriter.EmptyFile();

                Logger.AddLogFunction(entry => Console.WriteLine($"{ entry.When.ToString(CultureInfo.InvariantCulture) } [{ entry.Priority }]: { entry.Message }"));
                Logger.AddLogFunction(entry => TempFileWriter.WriteLine($"{ entry.When.ToString(CultureInfo.InvariantCulture) } [{ entry.Priority }]: { entry.Message }"));

                Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(100);
                        TempFileWriter.Flush();
                    }
                });

                // on platforms where [app].config doesn't work and we can't do much else (windows cough)
                //  we pre-load the necessary native libraries.
                // The search order takes over from there, looking up "SDL2.dll" ignores the directory it
                //  was loaded from and selects the proper one we loaded.
                // The Linux/MacOS clients will likely assume Mono is around and use the [app].config files instead.
                // We aren't using .NET Core, so we can't use their methods for fixing this either.
                if (Environment.Is64BitProcess)
                {
                    Host.Platform.LoadLibrary("x64/SDL2.dll");
                }
                else
                {
                    Host.Platform.LoadLibrary("x86/SDL2.dll");
                }

                Host.DefaultInitialize();

                Host.OnUserQuit += TempFileWriter.Flush;
                Host.StartStandalone(NeuroSonicGameMode.Instance, args);
#if !DEBUG
            }
            catch (Exception e)
            {
                try
                {
                    Host.Quit();
                }
                catch (Exception e2)
                {
                }

                Console.WriteLine(e);

                Console.WriteLine();
                Console.Write("Press any key to exit.");
                Console.ReadKey();
            }
#endif
        }
    }
}
