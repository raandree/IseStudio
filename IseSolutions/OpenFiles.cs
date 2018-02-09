using IseHelpers;
using System.Management.Automation;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows;

namespace IseStudio
{
    public partial class SolutionManager
    {
        private string openFilesFileName = string.Empty;
        private ListXmlStore<File> files = new ListXmlStore<File>();
        private bool filesOpened = false;

        public ListXmlStore<File> Files
        {
            get { return files; }
        }

        private void OpenFiles()
        {
            if (!System.IO.File.Exists(openFilesFileName))
            {
                return;
            }

            var filesToOpen = ListXmlStore<File>.Import(openFilesFileName);

            foreach (var file in filesToOpen)
            {
                try
                {
                    tabCollection.SelectedPowerShellTab.Files.Add(file.FullName);
                }
                catch { }
            }

            var selectedFile = filesToOpen.Where(file => file.IsSelected).FirstOrDefault();
            if (selectedFile != null)
            {
                try
                {
                    tabCollection.SelectedPowerShellTab.Files.SelectedFile = tabCollection.SelectedPowerShellTab.Files.Where(iseFile => iseFile.FullPath == selectedFile.FullName).FirstOrDefault();
                }
                catch { }
            }

            files = filesToOpen;
        }

        public void UpdateFilesFromIse()
        {
            files.Clear();

            foreach (var iseFile in tabCollection.SelectedPowerShellTab.Files.Where(iseFile => !iseFile.IsUntitled))
            {
                files.Add(new File() { FullName = iseFile.FullPath });
            }

            try
            {
                var selectedFileFullPath = tabCollection.SelectedPowerShellTab.Files.SelectedFile.FullPath;
                files.Where(file => file.FullName == selectedFileFullPath).FirstOrDefault().IsSelected = true;
            }
            catch { }
        }

        private void SaveFiles()
        {
            files.Export(openFilesFileName);
        }

        public void SelectedPowerShellTab_PropertyChanged_OpenFiles(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (filesOpened) { return; }

            filesOpened = true;
            OpenFiles();
            tabCollection.SelectedPowerShellTab.PropertyChanged -= SelectedPowerShellTab_PropertyChanged_OpenFiles;

            tabCollection.SelectedPowerShellTab.Files.CollectionChanged += Files_CollectionChanged;
        }

        void Files_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateFilesFromIse();
        }

        public void CurrentDomain_ProcessExit_SaveFiles(object sender, EventArgs e)
        {
            SaveFiles();
        }

        public void CloseAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (tabCollection.SelectedPowerShellTab.Files.Count > 0)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        public void CloseAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var openFilesFullNames = tabCollection.SelectedPowerShellTab.Files.Select(f => f.FullPath).ToList();

            if (tabCollection.SelectedPowerShellTab.Files.Where(f => f.IsSaved == false).Count() > 0)
            {
                var answer = MessageBox.Show("There are unsaved files. Do you want to save them before closing?", "Save Changes?",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (answer == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if (answer == MessageBoxResult.Yes)
                {
                    foreach (var openFilesFullName in openFilesFullNames)
                    {
                        tabCollection.SelectedPowerShellTab.Files.Where(f => f.FullPath == openFilesFullName).FirstOrDefault().Save();
                    }
                }
            }

            foreach (var openFilesFullName in openFilesFullNames)
            {
                tabCollection.SelectedPowerShellTab.Files.Remove(tabCollection.SelectedPowerShellTab.Files.Where(f => f.FullPath == openFilesFullName).FirstOrDefault(), true);
            }
        }
    }

    public class File
    {
        public string FullName { get; set; }
        public bool IsSelected { get; set; }

        public override string ToString()
        {
            return FullName;
        }
    }
}