using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace File.Consolidate
{
    public class FileMove : FileBase
    {
        public bool DeleteEmptyDirectories { get; set; }
        public bool DeleteDuplicateFiles { get; set; } //deletes file from source side

        public void SyncronousMove()
        {
            MoveDirectory(SourceDirectory, DestinationDirectory, Progress, SubDirectoriesFlag);
        }

        public async void AsyncronousMove()
        {
            await Task.Run(() => MoveDirectory(SourceDirectory, DestinationDirectory, Progress, SubDirectoriesFlag));
        }

        public void CancelMove()
        {
            Cancel = true;
        }


        private void MoveDirectory(string source, string destination, IProgress<ProgressReport> progress,
            bool subdirectories = true)
        {
            if (Directory.Exists(source) == false)
            {
                progress.Report(new ProgressReport {Report = "Source does not exist."});
                return;
            }

            var files = GetFileList(source, progress, subdirectories);

            if (files == null)
            {
                progress.Report(new ProgressReport {Report = "No files exist with selected extensions."});
                return;
            }
            MoveFiles(files, destination, progress);

            var directories = Directory.EnumerateDirectories(source);

            foreach (var directory in directories)
                if (Directory.EnumerateFileSystemEntries(directory).Any() == false &&
                    DeleteEmptyDirectories)
                    try
                    {
                        Directory.Delete(directory);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
        }


        private void MoveFiles(List<string> fileList, string destination, IProgress<ProgressReport> progress)
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
                        if (System.IO.File.Exists(report.FullPath) == false)
                        {
                            var start = DateTime.Now;

                            System.IO.File.Move(file, report.FullPath);

                            TotalBps += report.Size / new TimeSpan(DateTime.Now.Ticks - start.Ticks).TotalSeconds;

                            report.Successful++;
                            report.TotalBytes += report.Size;
                            report.Bps = TotalBps / report.Successful;
                            report.Report = "Successful";
                        }
                        else
                        {
                            if (DeleteDuplicateFiles)
                            {
                                System.IO.File.Delete(file);
                                report.Report = "Deleted";
                            }
                            else
                            {
                                report.Report = "Skipped";
                            }
                            report.Skipped++;
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