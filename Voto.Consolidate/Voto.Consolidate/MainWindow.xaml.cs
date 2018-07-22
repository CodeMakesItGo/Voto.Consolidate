using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using File.Consolidate;
using Voto.Consolidate.Properties;

namespace Voto.Consolidate
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileBase _fileOperation;
        private Thread _fileOperationthread;


        public MainWindow()
        {
            InitializeComponent();

            PageConsolidate = new PageConsolidate();
            PageSettings = new PageSettings();
            PageMedia = new PageMedia();
        }

        private bool IsCanceling { get; set; }

        private PageConsolidate PageConsolidate { get; }
        private PageSettings PageSettings { get; }
        private PageMedia PageMedia { get; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Frame.Content = PageConsolidate;
            Title += $" {Assembly.GetExecutingAssembly().GetName().Version}";
        }

        private void buttonConsolidate_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.Content is PageConsolidate) return;

            Frame.Content = PageConsolidate;
        }

        private void buttonSettings_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.Content is PageSettings) return;

            Frame.Content = PageSettings;
        }

        private void buttonMediaTypes_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.Content is PageMedia) return;

            Frame.Content = PageMedia;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.Save();
        }

        private void buttonRun_Click(object sender, RoutedEventArgs e)
        {
            if (_fileOperationthread == null || _fileOperationthread.IsAlive == false)
            {
                if (string.IsNullOrEmpty(Settings.Default.textBoxDestinationRootSetting))
                    MessageBox.Show(this, "Please goto settings and configure a destination directory.",
                        "No Destination Setting", MessageBoxButton.OK, MessageBoxImage.Stop);

                _fileOperationthread = Settings.Default.radioButtonCopySetting
                    ? new Thread(CopyFiles)
                    : new Thread(MoveFiles);

                _fileOperationthread.Start();

                IsCanceling = false;

                //switch to status page
                Frame.Content = PageConsolidate;
            }
            else
            {
                _fileOperation.CancelAll();

                IsCanceling = true;

                var count = 0;
                while (_fileOperationthread.IsAlive && count < 20)
                {
                    Thread.Sleep(250);
                    count++;

                    buttonRun.Content = "Stopping...";
                }
            }

            buttonRun.Content = _fileOperationthread.IsAlive ? "Stop" : "Run";
        }

        private void FileOperationComplete()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new FileOperationCompleteDelegate(FileOperationComplete));
                return;
            }

            buttonRun.Content = "Run";
            PageConsolidate.Complete();
        }

        private void MoveFiles()
        {
            var extensions = BuildFileExtensionList();

            if (Settings.Default.SourceDirectorySetting == null)
                return;

            foreach (var v in Settings.Default.SourceDirectorySetting)
            {
                var source = v;
                var subdirectories = false;

                if (v[v.Length - 1] == '*')
                {
                    source = v.Remove(v.Length - 1);
                    subdirectories = true;
                }

                _fileOperation = new FileMove
                {
                    DestinationDirectory = Settings.Default.textBoxDestinationRootSetting,
                    SourceDirectory = source,
                    SubDirectoriesFlag = subdirectories,
                    StoreByPictureDate = Settings.Default.radioButtonSubfolderLastWriteDateSetting,
                    DaysOld = Settings.Default.radioButtonConsolidationSelectionAllSetting
                        ? 0
                        : Settings.Default.Slider_ValueSetting,
                    Extensions = extensions,
                    DeleteDuplicateFiles = Settings.Default.checkBoxDeleteDuplicateFiles,
                    DeleteEmptyDirectories = Settings.Default.checkBoxDeleteEmptyDirectories
                };

                _fileOperation.ProgressEvent += PageConsolidate.ProgressReport;
                ((FileMove) _fileOperation).SynchronousMove();
            }

            FileOperationComplete();
        }

        private void CopyFiles()
        {
            var extensions = BuildFileExtensionList();

            if (Settings.Default.SourceDirectorySetting == null)
                return;

            foreach (var v in Settings.Default.SourceDirectorySetting)
            {
                var source = v;
                var subdirectories = false;

                if (v[v.Length - 1] == '*')
                {
                    source = v.Remove(v.Length - 1);
                    subdirectories = true;
                }

                _fileOperation = new FileCopy
                {
                    DestinationDirectory = Settings.Default.textBoxDestinationRootSetting,
                    SourceDirectory = source,
                    SubDirectoriesFlag = subdirectories,
                    StoreByPictureDate = Settings.Default.radioButtonSubfolderLastWriteDateSetting,
                    OverwriteFlag = Settings.Default.checkBoxOverwriteSetting,
                    DaysOld = Settings.Default.radioButtonConsolidationSelectionAllSetting
                        ? 0
                        : Settings.Default.Slider_ValueSetting,
                    Extensions = extensions
                };


                _fileOperation.ProgressEvent += PageConsolidate.ProgressReport;
                ((FileCopy) _fileOperation).SynchronousCopy();
            }

            var albums = PageSettings.GetGoogleAlbums();

            if (albums.Count > 0)
            {
                var cookies = PageSettings.GetGoogleCookies();

                foreach (var album in albums)
                {
                    if (album.isSelected == false) continue;

                    _fileOperation = new UrlCopy(album.photos.Count)
                    {
                        DestinationDirectory = Settings.Default.textBoxDestinationRootSetting,
                        SubDirectoriesFlag = false,
                        StoreByPictureDate = Settings.Default.radioButtonSubfolderLastWriteDateSetting,
                        OverwriteFlag = Settings.Default.checkBoxOverwriteSetting,
                        DaysOld = Settings.Default.radioButtonConsolidationSelectionAllSetting
                            ? 0
                            : Settings.Default.Slider_ValueSetting,
                        Extensions = extensions
                    };

                    _fileOperation.ProgressEvent += PageConsolidate.ProgressReport;


                    if (IsCanceling) break;

                    foreach (var photo in album.photos)
                    {
                        if (IsCanceling) break;

                        var url = PageSettings.GetGooglePhotoUrl(album.id, photo.id);
                        if (string.IsNullOrEmpty(url)) continue;

                        ((UrlCopy) _fileOperation).SynchronousDownload(url, cookies, photo.title, long.Parse(photo.size),
                            photo.timeStamp);
                    }
                }
            }

            FileOperationComplete();
        }

        private List<string> BuildFileExtensionList()
        {
            var extensions = new List<string>();

            if (Settings.Default.PicBmpSetting) extensions.Add(".bmp");
            if (Settings.Default.PicGifSetting) extensions.Add(".gif");
            if (Settings.Default.PicJpegSetting) extensions.Add(".jpeg");
            if (Settings.Default.PicJpgSetting) extensions.Add(".jpg");
            if (Settings.Default.PicPngSetting) extensions.Add(".png");
            if (Settings.Default.PicPsdSetting) extensions.Add(".psd");
            if (Settings.Default.PicRawSetting) extensions.Add(".raw");
            if (Settings.Default.PicTiffSetting) extensions.Add(".tiff");
            if (Settings.Default.VidAviSetting) extensions.Add(".avi");
            if (Settings.Default.VidFlvSetting) extensions.Add(".flv");
            if (Settings.Default.VidMovSetting) extensions.Add(".mov");
            if (Settings.Default.VidMp4Setting) extensions.Add(".mp4");
            if (Settings.Default.VidMpgSetting) extensions.Add(".mpg");
            if (Settings.Default.VidWmvSetting) extensions.Add(".wmv");
            if (Settings.Default.VidMtsSetting) extensions.Add(".mts");

            return extensions;
        }

        private delegate void FileOperationCompleteDelegate();
    }
}