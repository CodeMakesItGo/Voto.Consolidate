using System;
using System.IO;
using System.Net;

namespace File.Consolidate
{
    public class UrlCopy : FileBase
    {
        private readonly ProgressReport _report;

        public UrlCopy(int totalFiles)
        {
            _report = new ProgressReport
            {
                Total = totalFiles
            };
        }

        public bool OverwriteFlag { get; set; }

        public void CancelCopy()
        {
            Cancel = true;
        }

        public void SynchronousDownload(string source, string cookies, string fileName, long fileSize, DateTime fileTime)
        {
            if (Directory.Exists(DestinationDirectory) == false)
            {
                ((IProgress<ProgressReport>) Progress).Report(
                    new ProgressReport {Report = "Destination does not exist."});
                return;
            }

            if (string.IsNullOrEmpty(source)) return;

            try
            {
                double TotalBps = 0;

                _report.FileName = fileName;
                _report.Size = fileSize;

                //Based on creation time, not LastWrite time
                if (new TimeSpan(DateTime.Now.Ticks - fileTime.Ticks).TotalDays < DaysOld)
                {
                    _report.Skipped++;
                    return;
                }

                _report.FullPath = CreateDestinationPath(DestinationDirectory, fileName, fileTime);

                try
                {
                    if (OverwriteFlag || System.IO.File.Exists(_report.FullPath) == false)
                    {
                        var start = DateTime.Now;

                        using (var wc = new WebClient())
                        {
                            wc.Headers.Add("Cookie: " + cookies);
                            wc.DownloadFile(new Uri(source), _report.FullPath);
                        }

                        TotalBps += _report.Size / new TimeSpan(DateTime.Now.Ticks - start.Ticks).TotalSeconds;

                        _report.Successful++;
                        _report.TotalBytes += _report.Size;
                        _report.Bps = TotalBps / _report.Successful;
                        _report.Report = "Successful";
                    }
                    else
                    {
                        _report.Skipped++;
                        _report.Report = "Skipped";
                    }
                }

                catch (Exception e)
                {
                    _report.Failed++;
                    _report.Report = $"Failed: {e.Message}";
                }

                ((IProgress<ProgressReport>) Progress).Report(_report);
                _report.Completed++;
            }
            catch (Exception e)
            {
                ((IProgress<ProgressReport>) Progress).Report(new ProgressReport {Report = e.Message});
            }
        }
    }
}