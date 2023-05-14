using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace GithubReleaseUpgrader
{
    internal static class FileOperationsHelper
    {
        public static void SafeClearDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                var directory = new DirectoryInfo(path);

                foreach (FileInfo file in directory.GetFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                {
                    subDirectory.Delete(true);  // Recursively delete sub-directories
                }
            }
            catch (IOException ioEx)
            {
                Log.Error("Directory not found:{ex}", ioEx.Message);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                // Handle any permission exceptions here
                Log.Error("Permission error:{ex}", uaEx.Message);
            }
        }

        public static void SafeCreateDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (IOException ioEx)
            {
                Log.Error("Directory not found, or path is a file:{ex}", ioEx.Message);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Log.Error("Permission error:{ex}", uaEx.Message);
            }
        }

        public static void SafeDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException ioEx)
            {
                Log.Error("file is in use:{ex}", ioEx.Message);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Log.Error("Permission error:{ex}", uaEx.Message);
            }
        }

        public static void SafeCreateFile(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
            }
            catch (IOException ioEx)
            {
                Log.Error("file is in use or disk is full:{ex}", ioEx.Message);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Log.Error("Permission error:{ex}", uaEx.Message);
            }
        }
    }
}
