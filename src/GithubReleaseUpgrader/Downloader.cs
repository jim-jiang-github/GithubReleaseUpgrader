using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using Serilog;

namespace GithubReleaseUpgrader
{
    internal class Downloader
    {
        public static async Task<bool> StartDonwload(string downloadFileUrl, string downloadFileSavePath, bool needExtract)
        {
            try
            {
                var downloadFileDirectory = Path.GetDirectoryName(downloadFileSavePath);
                if (downloadFileDirectory == null)
                {
                    Log.Warning("downloadFileDirectory is null");
                    return false;
                }
                FileOperationsHelper.SafeCreateDirectory(downloadFileDirectory);
                FileOperationsHelper.SafeDeleteFile(downloadFileSavePath);
                using var client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(downloadFileUrl);
                response.EnsureSuccessStatusCode();
                using (var fileStream = await response.Content.ReadAsStreamAsync())
                using (var fileStreamOutput = new FileStream(downloadFileSavePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStreamOutput);
                }
                if (needExtract)
                {
                    ZipFile.ExtractToDirectory(downloadFileSavePath, downloadFileDirectory);
                    FileOperationsHelper.SafeDeleteFile(downloadFileSavePath);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Download file error hanppened:{ex}", ex);
                return false;
            }
        }
    }
}
