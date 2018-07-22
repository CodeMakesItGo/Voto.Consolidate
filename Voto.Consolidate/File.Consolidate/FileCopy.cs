using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace File.Consolidate
{
    public class FileCopy : FileBase
    {
        public bool OverwriteFlag { get; set; }


        public void SynchronousCopy()
        {
            CopyDirectory(SourceDirectory, DestinationDirectory, Progress, SubDirectoriesFlag, OverwriteFlag);
        }

        public async void AsynchronousCopy()
        {
            await Task.Run(() => CopyDirectory(SourceDirectory, DestinationDirectory, Progress, SubDirectoriesFlag,
                OverwriteFlag));
        }

        public void CancelCopy()
        {
            Cancel = true;
        }


        private void CopyDirectory(string source, string destination, IProgress<ProgressReport> progress,
            bool subdirectories = true, bool overwrite = false)
        {
            if (Directory.Exists(destination) == false)
            {
                progress.Report(new ProgressReport {Report = "Destination does not exist."});
                return;
            }

            var files = GetFileList(source, progress, subdirectories);

            if (files == null)
            {
                progress.Report(new ProgressReport {Report = "No files exist with selected extensions."});
                return;
            }
            CopyFiles(files, destination, progress, overwrite);
        }


        private void CopyFiles(List<string> fileList, string destination, IProgress<ProgressReport> progress,
            bool overwrite = false)
        {
            Cancel = false;

            if (fileList == null) return;

            try
            {
                double TotalBps = 0;

                var report = new ProgressReport
                {
                    Total = fileList.Count
                };

                foreach (var file in fileList)
                {
                    if (Cancel) break;

                    if (System.IO.File.Exists(file) == false) continue;

                    report.FileName = Path.GetFileName(file);
                    report.Size = new FileInfo(file).Length;

                    //Based on creation time, not LastWrite time
                    if (new TimeSpan(DateTime.Now.Ticks - new FileInfo(file).CreationTime.Ticks).TotalDays < DaysOld)
                    {
                        report.Skipped++;
                        continue;
                    }

                    report.FullPath = CreateDestinationPath(destination, file);

                    try
                    {
                        if (overwrite || System.IO.File.Exists(report.FullPath) == false)
                        {
                            var start = DateTime.Now;

                            System.IO.File.Copy(file, report.FullPath, overwrite);

                            TotalBps += report.Size / new TimeSpan(DateTime.Now.Ticks - start.Ticks).TotalSeconds;


                            report.Successful++;
                            report.TotalBytes += report.Size;
                            report.Bps = TotalBps / report.Successful;
                            report.Report = "Successful";
                        }
                        else
                        {
                            report.Skipped++;
                            report.Report = "Skipped";
                        }
                    }

                    catch (Exception e)
                    {
                        report.Failed++;
                        report.Report = $"Failed: {e.Message}";
                    }

                    progress.Report(report);
                    report.Completed++;
                }
            }
            catch (Exception e)
            {
                progress.Report(new ProgressReport {Report = e.Message});
            }
        }
    }
}