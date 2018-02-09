using IseHelpers;
using Microsoft.PowerShell.Commands.ShowCommandInternal;
using Microsoft.PowerShell.Host.ISE;
using Microsoft.Windows.PowerShell.Gui.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Xml;

namespace IseStudio
{
    public partial class IseStudio : IModuleAssemblyInitializer
    {
        MainWindow window = null;
        PowerShellTabCollection tabCollection;

        MenuManager menuManager;
        ToolbarManager toolbarManager;
        SolutionManager solutionManager;
        AddOnManager addonManager;
        AppFileManager appFileManager;

        string openFilesFileName = "openFiles.xml";
        string breakPointsFileName = "breakpoints.xml";
        string debugLogfileName = "log.txt";

        public void OnImport()
        {
            //get ISE window
            Func<MainWindow> fnc = delegate() { return Application.Current.Windows.Cast<MainWindow>().FirstOrDefault(wnd => wnd is MainWindow) as MainWindow; };
            window = Application.Current.Dispatcher.Invoke(fnc) as MainWindow;

            tabCollection = window.GetPowerShellTabCollection();

            appFileManager = new AppFileManager();
            appFileManager.Add(breakPointsFileName);
            appFileManager.Add(openFilesFileName);
            appFileManager.Add(debugLogfileName);

            solutionManager = new SolutionManager(window, appFileManager.Get(openFilesFileName).FullName);
            addonManager = new AddOnManager(window, tabCollection);

            SetupMenu();
            SetupToolbar();

            //add handler to Event StateChanged
            var executionStateChangedEvent = tabCollection.SelectedPowerShellTab.GetType().GetEvent("ExecutionStateChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            var handler = new EventHandler<PSInvocationStateChangedEventArgs>(StateChanged);
            executionStateChangedEvent.GetAddMethod(true).Invoke(tabCollection.SelectedPowerShellTab, new[] { handler });

            tabCollection.SelectedPowerShellTab.PropertyChanged += SelectedPowerShellTab_PropertyChanged_ImportBreakpoints; //executed once
            tabCollection.SelectedPowerShellTab.PropertyChanged += solutionManager.SelectedPowerShellTab_PropertyChanged_OpenFiles; //executed once

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit_ExportBreakpoints; //executed once
            AppDomain.CurrentDomain.ProcessExit += solutionManager.CurrentDomain_ProcessExit_SaveFiles; //executed once

            tabCollection.SelectedPowerShellTab.PropertyChanged += SelectedPowerShellTab_PropertyChanged;
        }

        void SelectedPowerShellTab_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CanInvoke")
            {
                UpdateBreakpointsFromIse();
                solutionManager.UpdateFilesFromIse();
            }
        }

        void StateChanged(object sender, PSInvocationStateChangedEventArgs e)
        {
            var x = e.InvocationStateInfo.State;
        }

        public void SetupMenu()
        {
            menuManager = new MenuManager(this);
            menuManager.Add("", new MenuItemDefinition() { Header = "_IseSolution" });
            menuManager.Add("_IseSolution", new MenuItemDefinition() { Header = "FunctionExplorer", Command = AddOnManager.ToggleFunctionExplorer, CommandParameter = addonManager.FunctionExplorerParameter, IsCheckable = true });

            menuManager.Add("_File", new MenuItemDefinition() { Header = "Open So_lution", Command = SolutionManager.OpenSolution, CommandParameter = solutionManager.SolutionParameter, ReferenceNode = "_Close", IconPath = "pack://application:,,,/IseStudio;component/Resources/OpenSolution.png" });
            menuManager.Add("_File", new MenuItemDefinition() { Header = "Close So_lution", ReferenceNode = "_Close", Command = SolutionManager.CloseSolution, CommandParameter = solutionManager.SolutionParameter, IconPath = "pack://application:,,,/IseStudio;component/Resources/CloseSolution.png" });
            menuManager.Add("_File", new MenuItemDefinition() { Header = "Save So_lution", ReferenceNode = "_Close", Command = SolutionManager.SaveSolution, CommandParameter = solutionManager.SolutionParameter, IconPath = "pack://application:,,,/IseStudio;component/Resources/SaveSolution.png" });

            menuManager.Add("_File", new MenuItemDefinition() { Header = "Separator", ReferenceNode = "_Close" });

            menuManager.Add("_File", new MenuItemDefinition() { Header = "Close _All", ReferenceNode = "_Close", Command = SolutionManager.CloseAll, CommandParameter = solutionManager.AllParameter });

            menuManager.Set("_Debug/To_ggle Breakpoint", new MenuItemDefinition() { Header = "To_ggle Breakpoint", IconPath = "pack://application:,,,/IseStudio;component/Resources/BreakPointEnabled.png" });
        }

        public void SetupToolbar()
        {
            toolbarManager = new ToolbarManager(this);
            toolbarManager.NewToolBar("IseStudio", true);
            toolbarManager.Add("IseStudio", new ToolbarItemDefinition()
            {
                EnabledIconPath = "pack://application:,,,/IseStudio;component/Resources/FunctionExplorer.png",
                Tooltip = "Show Function Explorer",
                Command = AddOnManager.ToggleFunctionExplorer,
                CommandParameter = addonManager.FunctionExplorerParameter,
                ReferenceItemName = "Stop",
                IsTogglable = true
            });

            //add debug button to standard toolbar
            toolbarManager.Add("", new ToolbarItemDefinition()
            {
                EnabledIconPath = "pack://application:,,,/IseStudio;component/Resources/BreakPointEnabled.png",
                DisabledIconPath = "pack://application:,,,/IseStudio;component/Resources/BreakPointDisabled.png",
                Tooltip = "Toggle Breakpoint",
                Command = window.GetMenuCommand("MenuToggleBreakpoint"),
                ReferenceItemName = "Stop"
            });

            toolbarManager.Add("", new ToolbarItemDefinition()
            {
                EnabledIconPath = "pack://application:,,,/IseStudio;component/Resources/StopEnabled.png",
                DisabledIconPath = "pack://application:,,,/IseStudio;component/Resources/StopDisabled.png",
                Tooltip = "Stop debugging",
                Command = window.GetMenuCommand("MenuStopDebugger"),
                ReferenceItemName = "Stop"
            });

            toolbarManager.Add("", new ToolbarItemDefinition()
            {
                EnabledIconPath = "pack://application:,,,/IseStudio;component/Resources/OpenSolution.png",
                Tooltip = "Open solution",
                Command = SolutionManager.OpenSolution,
                CommandParameter = solutionManager.SolutionParameter,
                ReferenceItemName = "OpenScript"
            });

            toolbarManager.Add("IseStudio", new ToolbarItemDefinition()
            {
                EnabledIconPath = "pack://application:,,,/IseStudio;component/Resources/OpenSolution.png",
                DisabledIconPath = "pack://application:,,,/IseStudio;component/Resources/CloseSolution.png",
                Tooltip = "Test",
                Command = ToggleBreakPoint2,
                CommandBinding = new CommandBinding(ToggleBreakPoint2, ToggleBreakPoint2_Executed, ToggleBreakPoint2_CanExecute)
            });
        }

        public static RoutedUICommand ToggleBreakPoint2 = new RoutedUICommand("ToggleBreakPoint2", "ToggleBreakPoint2", typeof(SolutionManager));
        public void ToggleBreakPoint2_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UpdateBreakpointsFromIse();
            //FunctionExplorer.update
            
            //1..20 | ForEach-Object { $script = $psISE.CurrentPowerShellTab.Files | Get-Random; Set-PSBreakpoint -Script $script.FullPath -Line (Get-Random -Minimum 0 -Maximum $script.Editor.LineCount) }
        }

        public void ToggleBreakPoint2_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }

    public class AddOnManager
    {
        Window window = null;
        PowerShellTabCollection tabCollection = null;
        FunctionExplorer functionExplorer = null;
        public AddOnParameter FunctionExplorerParameter { get; set; }

        public static RoutedUICommand ToggleFunctionExplorer = new RoutedUICommand("Show FunctionExplorer", "ShowFunctionExplorer", typeof(SolutionManager));

        public AddOnManager(Window window, PowerShellTabCollection tabCollection)
        {
            this.window = window;
            this.tabCollection = tabCollection;

            FunctionExplorerParameter = new AddOnParameter();

            RegisterCommands();
        }

        private void RegisterCommands()
        {
            window.Dispatcher.Invoke((Action)(() =>
            {
                window.CommandBindings.Add(new CommandBinding(ToggleFunctionExplorer, ToggleFunctionExplorer_Executed, ToggleFunctionExplorer_CanExecute));
            }));
        }

        public void ToggleFunctionExplorer_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var addon = tabCollection.SelectedPowerShellTab.VerticalAddOnTools.Where(element => element.Name == "FunctionExplorer").FirstOrDefault();

            if (addon == null)
            {
                functionExplorer = tabCollection.SelectedPowerShellTab.VerticalAddOnTools.Add("FunctionExplorer", typeof(FunctionExplorer), true).Control as FunctionExplorer;

                tabCollection.SelectedPowerShellTab.PropertyChanged += SelectedPowerShellTab_PropertyChanged;
            }
            else
            {
                tabCollection.SelectedPowerShellTab.VerticalAddOnTools.Remove(addon);
            }
        }

        void SelectedPowerShellTab_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (functionExplorer.AutoUpdate)
            {
                functionExplorer.UpdateFunction();
            }
        }

        public void ToggleFunctionExplorer_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }

    public class AddOnParameter
    {
        public ISEAddOnTool AddOn { get; set; }
    }
}

//            var runspaceRefField = currentTab.GetType().GetField("runspaceRef", BindingFlags.Instance | BindingFlags.NonPublic);
//var runspaceRef = runspaceRefField.GetValue(currentTab);

//var runspaceField = runspaceRef.GetType().GetProperty("Runspace", BindingFlags.Instance | BindingFlags.NonPublic);
//var runspace = runspaceField.GetValue(runspaceRef);

//var executionContextField = runspace.GetType().GetProperty("ExecutionContext", BindingFlags.Instance | BindingFlags.NonPublic);
//var executionContext = executionContextField.GetValue(runspace);

//ISEFile file = tabCollection.SelectedPowerShellTab.Files.SelectedFile;
//ISEEditor editor = file.Editor;


//var doSomething = new RoutedUICommand("DoSomething", "Name", this.GetType());
//            var GetFileHashCommand = new RoutedUICommand("GetFileHashCommand", "GetFileHashCommand", typeof(IseStudio));


//        public void FooCanExecute(object sender, CanExecuteRoutedEventArgs e)
//        {
//            e.CanExecute = true;  // can this command be executed?
//            e.Handled = true;     // has this event been handled?
//        }
//        public void FooExecute(object sender, ExecutedRoutedEventArgs e)
//        {
//            MessageBox.Show("Hey, I'm some help.");
//            e.Handled = true;

//            var btn = menuManager.Get("_Debug/To_ggle Breakpoint");
//            var command = btn.Command;
//        }



//public void DoSomething_Executed(object sender, ExecutedRoutedEventArgs e)
//        {
//            MessageBox.Show("Test");

//            //((ImageButton)sender).EnabledImageSource = new BitmapImage(new Uri("pack://application:,,,/IseStudio;component/Resources/y.png"));
//        }

//        public void DoSomething_CanExecute(object sender, CanExecuteRoutedEventArgs e)
//        {
//            e.CanExecute = true;
//        }



//tabCollection = tabControl.ItemsSource as PowerShellTabCollection;
//            var mi = tabCollection.SelectedPowerShellTab.Files.SelectedFile.Editor.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);


//PowerShellTabCollection tabCollection = hostObject.PowerShellTabs;

//var hashes = tabCollection.SelectedPowerShellTab.Files.Select(f => f.GetContentHash()).ToList();

//MessageBox.Show(string.Join("\n", hashes));