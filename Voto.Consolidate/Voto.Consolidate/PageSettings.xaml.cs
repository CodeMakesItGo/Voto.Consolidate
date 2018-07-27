using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Voto.Consolidate.Properties;

namespace Voto.Consolidate
{
    /// <summary>
    ///     Interaction logic for PageSettings.xaml
    /// </summary>
    public partial class PageSettings : Page
    {
        private WindowGoogleLogin wgl;
 
        public PageSettings()
        {
            InitializeComponent();

            if (Settings.Default.SourceDirectorySetting != null)
            {
                foreach (var v in Settings.Default.SourceDirectorySetting)
                {
                    listBox.Items.Add(v);
                }
            }
            else
            {
                Settings.Default.SourceDirectorySetting = new StringCollection();
            }

            if(Settings.Default.GoogleAlbumsSetting != null &&
                Settings.Default.GoogleAlbumsSetting.Count > 0)
                LoadGoogleAlbums();
        }

       

        public List<WindowGoogleLogin.Album> GetGoogleAlbums()
        {
            return wgl?._albums;
        }

        public string GetGoogleCookies()
        {
            return wgl.cookies;
        }

        public string GetGooglePhotoUrl(string albumId, string photoId)
        {
            return wgl.GetPhotoUrl(albumId, photoId);
        }

        public List<string> GetGooglePhotoUrls()
        {
            List<string> urls = new List<string>();

            foreach (var album in wgl._albums)
            {
                if(album.isSelected == false) continue;

                urls.AddRange(wgl.GetAlbumPhotoUrls(album.id));
            }

            return urls;
        }

        private void textBoxDestinationRoot_Loaded(object sender, RoutedEventArgs e)
        {
            checkBoxOverwrite.IsEnabled = Settings.Default.radioButtonCopySetting;
            checkBoxDeleteEmptyDir.IsEnabled = Settings.Default.radioButtonMoveSetting;
            checkBoxDeleteDuplicates.IsEnabled = Settings.Default.radioButtonMoveSetting; 
            SliderDaysOlder.IsEnabled = Settings.Default.radioButtonConsolidationSelectionOldSetting;
        }

        private void buttonAddSourceDirectory_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new WindowPopUp();
            fbd.ShowDialog();

            if (fbd.Valid)
            {
                listBox.Items.Add(fbd.DirectoryPath);
                Settings.Default.SourceDirectorySetting.Add(fbd.DirectoryPath);
            }
        }

        private void buttonRemoveSourceDirectory_Click(object sender, RoutedEventArgs e)
        {
            while (listBox.SelectedItems.Count > 0)
            {
                Settings.Default.SourceDirectorySetting.Remove(listBox.SelectedItems[0].ToString());
                listBox.Items.Remove(listBox.SelectedItems[0]);
            }
        }

        private void buttonDestinationRoot_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog();
         
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                textBoxDestinationRoot.Text = fbd.SelectedPath;
                Settings.Default.textBoxDestinationRootSetting = fbd.SelectedPath;
            }
        }

        private void radioButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            checkBoxOverwrite.IsEnabled = true;
            checkBoxDeleteEmptyDir.IsEnabled = false;
            checkBoxDeleteDuplicates.IsEnabled = false;
        }

        private void radioButtonMove_Click(object sender, RoutedEventArgs e)
        {
            checkBoxOverwrite.IsEnabled = false;
            checkBoxDeleteEmptyDir.IsEnabled = true;
            checkBoxDeleteDuplicates.IsEnabled = true;
        }

        private void radioButtonConsolidationSelectionAll_Click(object sender, RoutedEventArgs e)
        {
            SliderDaysOlder.IsEnabled = false;
        }

        private void radioButtonConsolidationSelectionOld_Click(object sender, RoutedEventArgs e)
        {
            SliderDaysOlder.IsEnabled = true;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void radioButtonSubfolderConsolidationDate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void radioButtonSubfolderLastWriteDate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void buttonGetGoogleAlbums_Click(object sender, RoutedEventArgs e)
        {
            LoadGoogleAlbums();
        }

        private void LoadGoogleAlbums()
        {
            wgl = new WindowGoogleLogin();
            wgl.ShowDialog();

            listBoxGoogleAlbums.Items.Clear();

            if (Settings.Default.GoogleAlbumsSetting == null)
            {
                Settings.Default.GoogleAlbumsSetting = new StringCollection();
            }

            for (int i = 0; i < wgl._albums.Count; ++i)
            {
                WindowGoogleLogin.Album album = wgl._albums[i];

                if (album.photos != null && album.photos.Count > 0)
                {
                    foreach (var v in Settings.Default.GoogleAlbumsSetting)
                    {
                        if (album.id.Equals(v))
                        {
                            album.isSelected = true;
                        }
                    }

                    listBoxGoogleAlbums.Items.Add(album);
                }
            }
        }

        private void CheckBoxZone_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox chkZone = (System.Windows.Controls.CheckBox) sender;
            if (Settings.Default.GoogleAlbumsSetting.IndexOf((string) chkZone.Tag) == -1)
            {
                Settings.Default.GoogleAlbumsSetting.Add((string) chkZone.Tag);
                for (int i = 0; i < wgl._albums.Count; ++i)
                {
                    WindowGoogleLogin.Album album = wgl._albums[i];

                    if (album.id.Equals((string)chkZone.Tag))
                    {
                        album.isSelected = true;
                    }
                }
            }
        }

        private void CheckBoxZone_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox chkZone = (System.Windows.Controls.CheckBox)sender;
            Settings.Default.GoogleAlbumsSetting.Remove((string)chkZone.Tag);

            for (int i = 0; i < wgl._albums.Count; ++i)
            {
                WindowGoogleLogin.Album album = wgl._albums[i];

                if (album.id.Equals((string)chkZone.Tag))
                {
                    album.isSelected = false;
                }
            }
        }

        private void checkBoxDeleteEmptyDir_Click(object sender, RoutedEventArgs e)
        {

        }

        private void checkBoxDeleteDuplicates_Click(object sender, RoutedEventArgs e)
        {

        }

        private void checkBoxOverwrite_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}