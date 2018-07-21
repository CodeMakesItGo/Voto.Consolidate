using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace File.Consolidate
{
    public class FileBase
    {
        public delegate void ProgressDelegate(ProgressReport p);

        protected bool Cancel;
        protected Progress<ProgressReport> Progress;

        public FileBase()
        {
            Progress = new Progress<ProgressReport>();
            Progress.ProgressChanged += (sender, cp) => ProgressEvent?.Invoke(cp);
        }


        public string SourceDirectory { get; set; }
        public string DestinationDirectory { get; set; }
        public bool SubDirectoriesFlag { get; set; }
        public bool StoreByPictureDate { get; set; }
        public int DaysOld { get; set; }
        public List<string> Extensions { get; set; }
        public event ProgressDelegate ProgressEvent;

        protected List<string> GetFileList(string source, IProgress<ProgressReport> progress, bool subdirectorySearch)
        {
            if (Directory.Exists(source) == false)
            {
                progress.Report(new ProgressReport {Report = "Source does not exist."});
                return null;
            }

            if (Extensions == null || Extensions.Count == 0)
                return null;

            try
            {
                return Directory.EnumerateFiles(source, "*.*",
                        subdirectorySearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(s => Extensions.Any(ext => string.Equals(ext, Path.GetExtension(s),
                        StringComparison.CurrentCultureIgnoreCase)))
                    .ToList();
            }
            catch (Exception e)
            {
                progress.Report(new ProgressReport {Report = e.Message});
            }

            return null;
        }

        protected string CreateDestinationPath(string destination, string file)
        {
            var fileName = Path.GetFileName(file);

            return CreateDestinationPath(destination, fileName, new FileInfo(file).LastWriteTime);
        }

        protected string CreateDestinationPath(string destination, string fileName, DateTime fileLastWriteTime)
        {
            var filePath = "";

            if (fileName == null) return filePath;

            if (StoreByPictureDate)
            {
                var dt = fileLastWriteTime;

                //var bi = new BitmapImage();
                //var img = BitmapFrame.Create(new Uri(file, UriKind.RelativeOrAbsolute));
                //var m = (BitmapMetadata) img.Metadata;

                filePath = Path.Combine(destination, dt.ToString("yyyy-MM-dd"));
            }
            else
            {
                filePath = Path.Combine(destination, DateTime.Now.ToString("yyyy-MM-dd"));
            }

            if (Directory.Exists(filePath) == false)
                Directory.CreateDirectory(filePath);

            return Path.Combine(filePath, fileName);
        }

        public void CancelAll()
        {
            Cancel = true;
        }

        public class ProgressReport
        {
            public double Bps;
            public int Completed;
            public int Failed;

            public string FileName;
            public string FullPath;
            public string Report;
            public long Size;
            public int Skipped;
            public int Successful;

            public int Total;
            public long TotalBytes;

            public ProgressReport()
            {
                Total = 0;
                Completed = 0;
                Successful = 0;
                Failed = 0;
                Skipped = 0;
                Bps = 0.0;
                TotalBytes = 0;
            }

            public float PercentComplete => Completed / (float) Total;
            public float PercentSuccessful => Successful / (float) Total;
            public float PercentFailed => Failed / (float) Total;
            public float PercentSkipped => Skipped / (float) Total;
        }
    }
}