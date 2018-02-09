using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Linq;
using System.IO;
using Microsoft.PowerShell.Host.ISE;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Windows.PowerShell.Gui.Internal;
using System.Windows.Input;
using System.Reflection;
using Microsoft.PowerShell.Commands.ShowCommandInternal;
using System.Windows.Controls;

namespace IseStudio
{
    public static class Extensions
    {
        public delegate TOut Action2<TIn, TOut>(TIn element);

        public static List<string> IseFileList(this PSModuleInfo module)
        {
            List<string> validExtensions = new List<string>() { ".psm1", ".psd1", ".ps1", ".ps1xml", ".txt", ".xml" };

            return module.FileList.Where(f => validExtensions.Contains(System.IO.Path.GetExtension(f)) && File.Exists(f)).ToList();
        }

        public static string GetContentHash(this ISEFile iseFile)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(iseFile.Editor.Text);

            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(textBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static bool GetCanExecuteToggleBreakpoint(this PowerShellTab tab)
        {
            var canExecuteToggleBreakpointProperty = tab.GetType().GetProperty("CanExecuteToggleBreakpoint", BindingFlags.Instance | BindingFlags.NonPublic);
            var canExecuteToggleBreakpoint = (bool)canExecuteToggleBreakpointProperty.GetValue(tab);

            return canExecuteToggleBreakpoint;
        }

        public static List<LineBreakpoint> GetBreakpointsAtLine(this ISEFile iseFile, int currentLine)
        {
            var getBreakpointsAtLineMethod = iseFile.GetType().GetMethod("GetBreakpointsAtLine", BindingFlags.Instance | BindingFlags.NonPublic);

            var breakpoints = getBreakpointsAtLineMethod.Invoke(iseFile, new object[] { currentLine }) as List<LineBreakpoint>;

            return breakpoints;
        }

        public static void DoAsynchronousInvoke(this PowerShellTab tab, PSCommand command)
        {
            var getBreakpointsAtLineMethod = tab.GetType().GetMethod("DoAsynchronousInvoke", BindingFlags.Instance | BindingFlags.NonPublic);

            getBreakpointsAtLineMethod.Invoke(tab, new object[] { command });
        }

        public static int GetCurrentLine(this ISEFile iseFile)
        {
            var getCurrentLineMethod = iseFile.GetType().GetMethod("GetCurrentLine", BindingFlags.NonPublic | BindingFlags.Instance);

            var currentLine = (int)getCurrentLineMethod.Invoke(iseFile, null);

            return currentLine;
        }

        public static RoutedUICommand GetMenuCommand(this MainWindow iseWindow, string fieldName)
        {
            RoutedUICommand command = null;

            iseWindow.Dispatcher.Invoke((Action)(() =>
            {
                var fieldInfo = iseWindow.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                var field = fieldInfo.GetValue(iseWindow);

                var cmd = ((MenuItem)field).Command;

                command = (RoutedUICommand)cmd;
            }));

            return command;
        }

        public static PowerShellTabCollection GetPowerShellTabCollection(this MainWindow window)
        {
            FieldInfo tabControlField = window.GetType().GetField("runspaceTabControl", BindingFlags.Instance | BindingFlags.NonPublic);
            RunspaceTabControl tabControl = (RunspaceTabControl)tabControlField.GetValue(window);
            PowerShellTabCollection tabCollection = tabControl.ItemsSource as PowerShellTabCollection;

            return tabCollection;
        }

        public static List<Breakpoint> GetBreakpoints(this PowerShellTab tab)
        {
            var breakPointsField = tab.GetType().GetField("breakpoints", BindingFlags.Instance | BindingFlags.NonPublic);
            var breakpoints = breakPointsField.GetValue(tab) as List<Breakpoint>;

            return breakpoints;
        }

        public static void SetCommandParameter(this ImageButton button, object commandParameter)
        {
            var innerButtonField = button.GetType().GetField("innerButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var innerButton = innerButtonField.GetValue(button) as Button;

            innerButton.CommandParameter = commandParameter;
        }

        public static object GetCommandParameter(this ImageButton button)
        {
            var innerButtonField = button.GetType().GetField("innerButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var innerButton = innerButtonField.GetValue(button) as Button;

            return innerButton.CommandParameter;
        }

        public static void SetCommandParameter(this ImageToggleButton button, object commandParameter)
        {
            var innerButtonField = button.GetType().GetField("toggleInnerButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var innerButton = innerButtonField.GetValue(button) as System.Windows.Controls.Primitives.ToggleButton;

            innerButton.CommandParameter = commandParameter;
        }

        public static object GetCommandParameter(this ImageToggleButton button)
        {
            var innerButtonField = button.GetType().GetField("innerButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var innerButton = innerButtonField.GetValue(button) as Button;

            return innerButton.CommandParameter;
        }

        public static IEnumerable<TOut> ForEach<TIn, TOut>(this IEnumerable<TIn> source, Action2<TIn, TOut> action)
        {
            if (source == null) { throw new ArgumentException(); }
            if (action == null) { throw new ArgumentException(); }

            foreach (TIn element in source)
            {
                TOut result = action(element);
                yield return result;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) { throw new ArgumentException(); }
            if (action == null) { throw new ArgumentException(); }

            foreach (T element in source)
            {
                action(element);
            }
        }
    }
}



//public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
//{
//    if (source == null) { throw new ArgumentException(); }
//    if (action == null) { throw new ArgumentException(); }

//    foreach (T element in source)
//    {
//        action(element);
//        yield return element;
//    }
//}

//RoutedUICommand toggleBreakpointMenuItemCommand = null;
// iseWindow.Dispatcher.Invoke((Action)(() =>
// {
//     var toggleBreakpointMenuItemField = iseWindow.GetType().GetField("MenuToggleBreakpoint", BindingFlags.Instance | BindingFlags.NonPublic);
//     var toggleBreakpointMenuItem = toggleBreakpointMenuItemField.GetValue(iseWindow);

//     var cmd = ((MenuItem)toggleBreakpointMenuItem).Command;
//     var cmdBinding = ((MenuItem)toggleBreakpointMenuItem).CommandBindings;
//     var cmdTarget = ((MenuItem)toggleBreakpointMenuItem).CommandTarget;

//     toggleBreakpointMenuItemCommand = (RoutedUICommand)cmd;

//     //################################

//     var startPowerShellButtonField = iseWindow.GetType().GetField("startPowerShellButton", BindingFlags.Instance | BindingFlags.NonPublic);
//     var startPowerShellButton = startPowerShellButtonField.GetValue(iseWindow);

//     var cmd2 = ((ImageButton)startPowerShellButton).Command;
//     var cmdBinding2 = ((ImageButton)startPowerShellButton).CommandBindings;

//     //################################

//     FieldInfo toolBarTrayField = iseWindow.GetType().GetField("toolBarTray", BindingFlags.Instance | BindingFlags.NonPublic);
//     var toolBarTray = (ToolBarTray)toolBarTrayField.GetValue(iseWindow);

//     var btnTg = (ImageButton)toolBarTray.ToolBars[0].Items[11];
//     var cmd3 = btnTg.Command;
//     var cmdBinding3 = btnTg.CommandBindings;

//     toggleBreakpointMenuItemCommand = (RoutedUICommand)cmd3;
// }));