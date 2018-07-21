using System.Windows;
using System.Windows.Forms;

namespace Voto.Consolidate
{
    /// <summary>
    ///     Interaction logic for WindowPopUp.xaml
    /// </summary>
    public partial class WindowPopUp : Window
    {
        public WindowPopUp()
        {
            InitializeComponent();
            DirectoryPath = "";
            SubDirectorySearch = false;
            Valid = false;
        }

        public string DirectoryPath { get; set; }
        public bool SubDirectorySearch { get; set; }
        public bool Valid { get; set; }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog();

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DirectoryPath = fbd.SelectedPath;
                if (SubDirectorySearch)
                {
                    DirectoryPath += "*";
                }

                textBlock.Text = DirectoryPath;
            }
        }

        private void checkBoxSubDirectory_Click(object sender, RoutedEventArgs e)
        {
            SubDirectorySearch = checkBoxSubDirectory.IsChecked ?? false;

            if (SubDirectorySearch)
            {
                DirectoryPath += "*";
            }
            else
            {
                if (DirectoryPath[DirectoryPath.Length - 1] == '*')
                {
                    DirectoryPath = DirectoryPath.Remove(DirectoryPath.Length - 1);
                }
            }

            textBlock.Text = DirectoryPath;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            Valid = true;
            Close();
        }
    }
}