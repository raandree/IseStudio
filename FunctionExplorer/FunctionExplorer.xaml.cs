using Microsoft.PowerShell.Host.ISE;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace IseStudio
{
    #region FunctionExplorerData
    public class FunctionExplorerData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public FunctionContainer Files { get; set; }
        private string statusMessage;
        private bool autoUpdate = true;

        public string StatusMessage
        {
            get { return statusMessage; }
            set
            {
                statusMessage = value;
                OnPropertyChanged("StatusMessage");
            }
        }

        public bool AutoUpdate
        {
            get { return autoUpdate; }
            set
            {
                autoUpdate = value;
                OnPropertyChanged("AutoUpdate");
            }
        }

        public FunctionExplorerData()
        {
            Files = new FunctionContainer();
        }

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
    #endregion FunctionExplorerData

    public partial class FunctionExplorer : UserControl, IAddOnToolHostObject
    {
        ObjectModelRoot hostObject;
        FunctionExplorerData data;

        //public IEnumerable<FunctionDefinition> Functions
        //{
        //    get { return data.Functions.Values; }
        //}

        public int FunctionCount
        {
            get { return data.Files.FunctionCount; }
        }

        public DateTime LastUpdate
        {
            get { return data.Files.LastUpdate; }
        }

        public bool AutoUpdate
        {
            get { return data.AutoUpdate; }
        }

        public FunctionExplorer()
        {
            InitializeComponent();



            data = new FunctionExplorerData();

            Binding b = new Binding()
            {
                Source = data,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Path = new PropertyPath("StatusMessage")
            };
            this.lblStatusMessage.SetBinding(TextBlock.TextProperty, b);

            b = new Binding()
            {
                Source = data,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Path = new PropertyPath("Functions.FunctionCount")
            };
            this.lblFunctionCount.SetBinding(TextBlock.TextProperty, b);

            b = new Binding()
            {
                Source = data,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Path = new PropertyPath("AutoUpdate")
            };
            this.chkAutoUpdate.SetBinding(CheckBox.IsCheckedProperty, b);

            //b = new Binding()
            //{
            //    Source = data,
            //    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            //    Path = new PropertyPath("Functions")
            //};
            //this.trvFunctions.SetBinding(TreeView.ItemsSourceProperty, b);

        }

        public ObjectModelRoot HostObject
        {
            get
            {
                return this.hostObject;
            }
            set
            {
                this.hostObject = value;
            }
        }

        public void UpdateFunction()
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif

            var filesToScan = new List<ISEFile>();

            foreach (var iseFile in hostObject.CurrentPowerShellTab.Files)
            {
                var iseFileHash = iseFile.GetContentHash();
                if (data.Files.ContainsKey(iseFile.FullPath) && data.Files[iseFile.FullPath].Hash == iseFileHash)
                {
                    //file is known and has not changed
                    continue;
                }
                else if (data.Files.ContainsKey(iseFile.FullPath) && data.Files[iseFile.FullPath].Hash != iseFileHash)
                {
                    data.Files.Remove(iseFile.FullPath);

                    filesToScan.Add(iseFile);
                }
                else
                {
                    filesToScan.Add(iseFile);
                }
            }

            foreach (var fileToScan in filesToScan)
            {
                var file = new FileDefinition(fileToScan.FullPath, fileToScan.GetContentHash());

                var functionFound = false;
                System.Collections.ObjectModel.Collection<PSParseError> errors = new System.Collections.ObjectModel.Collection<PSParseError>();
                var tokens = PSParser.Tokenize(fileToScan.Editor.Text, out errors).Where(t => t.Type == PSTokenType.Keyword | t.Type == PSTokenType.CommandArgument);

                foreach (var token in tokens)
                {
                    if ((token.Content.ToLower() == "function" | token.Content.ToLower() == "workflow"))
                    {
                        functionFound = true;
                        continue;
                    }

                    if (functionFound && token.Type == PSTokenType.CommandArgument)
                    {
                        FunctionDefinition function = new FunctionDefinition(fileToScan.FullPath, token.Content, token.StartLine);
                        file.Functions.Add(function);

                        functionFound = false;
                    }
                }

                data.Files.Add(fileToScan.FullPath, file);
            }

#if DEBUG
            sw.Stop();
            data.StatusMessage = string.Format("Function update took {0}", sw.Elapsed.ToString());
#endif
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateFunction();

            trvFunctions.ItemsSource = data.Files.Values;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            data.Files.Clear();
        }
    }

    //[ValueConversion(typeof(bool), typeof(bool))]
    //public class NegateBoolenValueConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return !(bool)value;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return !(bool)value;
    //    }
    //}
}
