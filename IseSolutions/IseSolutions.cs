using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using Microsoft.Windows.PowerShell.Gui.Internal;
using Microsoft.PowerShell.Host.ISE;
using IseHelpers;

namespace IseStudio
{
    public partial class SolutionManager
    {
        MainWindow window = null;
        PSModuleInfo module = null;
        PowerShellTabCollection tabCollection = null;

        public CommandParameter SolutionParameter { get; set; }
        public CommandParameter AllParameter { get; set; }

        public static RoutedUICommand OpenSolution = new RoutedUICommand("OpenSolution", "OpenSolution", typeof(SolutionManager));
        public static RoutedUICommand SaveSolution = new RoutedUICommand("SaveSolution", "SaveSolution", typeof(SolutionManager));
        public static RoutedUICommand CloseSolution = new RoutedUICommand("CloseSolution", "CloseSolution", typeof(SolutionManager));
        public static RoutedUICommand CloseAll = new RoutedUICommand("CloseAll", "CloseAll", typeof(SolutionManager));

        public SolutionManager(MainWindow window, string filesXmlFilePath)
        {
            this.window = window;
            tabCollection = window.GetPowerShellTabCollection();
            this.openFilesFileName = filesXmlFilePath;

            SolutionParameter = new CommandParameter();

            RegisterCommands();
        }

        private void RegisterCommands()
        {
            window.Dispatcher.Invoke((Action)(() =>
            {
                window.CommandBindings.Add(new CommandBinding(OpenSolution, OpenSolution_Executed, OpenSolution_CanExecute));
                window.CommandBindings.Add(new CommandBinding(SaveSolution, SaveSolution_Executed, SaveSolution_CanExecute));
                window.CommandBindings.Add(new CommandBinding(CloseSolution, CloseSolution_Executed, CloseSolution_CanExecute));
                window.CommandBindings.Add(new CommandBinding(CloseAll, CloseAll_Executed, CloseAll_CanExecute));
            }));
        }

        public void OpenSolution_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "PowerShell Data Files|*.psd1",
                    Title = "Select a PowerShell Data File"
                };
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    var cmd = string.Format("Test-ModuleManifest -Path '{0}'", dialog.FileName);
                    module = (PSModuleInfo)IseHelpers.PowerShellInvoker.Invoke(cmd).FirstOrDefault().BaseObject;

                    window.Title = module.Name + " Windows PowerShell ISE";

                    foreach (var file in module.IseFileList())
                    {
                        tabCollection.SelectedPowerShellTab.Files.Add(file);
                    }

                    CommandParameter parameter = e.Parameter as CommandParameter;
                    if (parameter != null)
                        parameter.CanBeExecuted = true;
                }
            }
            catch
            {
                MessageBox.Show("Error opening solution file", "Error opening solution file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenSolution_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        public void SaveSolution_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (module == null) { return; }

            FieldInfo tabControlField = window.GetType().GetField("runspaceTabControl", BindingFlags.Instance | BindingFlags.NonPublic);
            RunspaceTabControl tabControl = (RunspaceTabControl)tabControlField.GetValue(window);
            PowerShellTabCollection tabCollection = tabControl.ItemsSource as PowerShellTabCollection;

            foreach (var file in module.IseFileList())
            {
                tabCollection.SelectedPowerShellTab.Files.Where(f => f.FullPath == file).FirstOrDefault().Save();
            }
        }

        public void SaveSolution_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            CommandParameter parameter = e.Parameter as CommandParameter;

            if (parameter != null)
                e.CanExecute = parameter.CanBeExecuted;
            else
                e.CanExecute = false;
        }


        public void CloseSolution_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            window.Title = "Windows PowerShell ISE";
            if (module == null) { return; }

            FieldInfo tabControlField = window.GetType().GetField("runspaceTabControl", BindingFlags.Instance | BindingFlags.NonPublic);
            RunspaceTabControl tabControl = (RunspaceTabControl)tabControlField.GetValue(window);
            PowerShellTabCollection tabCollection = tabControl.ItemsSource as PowerShellTabCollection;

            var solutionIseFiles = tabCollection.SelectedPowerShellTab.Files.Where(f => module.IseFileList().Contains(f.FullPath));

            if (solutionIseFiles.Where(f => f.IsSaved == false).Count() > 0)
            {
                var answer = MessageBox.Show("There are unsaved files in the solution. Do you want to save them before closing?", "Save Changes?",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (answer == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if (answer == MessageBoxResult.Yes)
                {
                    SaveSolution_Executed(sender, e);
                }
            }

            foreach (var file in module.IseFileList())
            {
                tabCollection.SelectedPowerShellTab.Files.Remove(tabCollection.SelectedPowerShellTab.Files.Where(f => f.FullPath == file).FirstOrDefault(), true);
            }

            CommandParameter parameter = e.Parameter as CommandParameter;
            if (parameter != null)
                parameter.CanBeExecuted = false;
        }

        public void CloseSolution_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            CommandParameter parameter = e.Parameter as CommandParameter;

            if (parameter != null)
                e.CanExecute = parameter.CanBeExecuted;
            else
                e.CanExecute = false;
        }
    }
}