using Microsoft.PowerShell.Commands.ShowCommandInternal;
using Microsoft.PowerShell.Host.ISE;
using Microsoft.Windows.PowerShell.Gui.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class ToolbarManager
    {
        Window iseWindow;
        IseStudio ise;
        ToolBarTray toolBarTray;

        public ToolbarManager(IseStudio ise)
        {
            this.ise = ise;

            Func<MainWindow> fnc = delegate() { return Application.Current.Windows.Cast<MainWindow>().FirstOrDefault(wnd => wnd is MainWindow) as MainWindow; };
            iseWindow = Application.Current.Dispatcher.Invoke(fnc) as MainWindow;

            FieldInfo toolBarTrayField = iseWindow.GetType().GetField("toolBarTray", BindingFlags.Instance | BindingFlags.NonPublic);
            toolBarTray = (ToolBarTray)toolBarTrayField.GetValue(iseWindow);
        }

        #region Toolbar methods
        public void NewToolBar(string name, bool isEnabled = true)
        {
            iseWindow.Dispatcher.Invoke((Action)(() =>
            {
                var toolbar = new ToolBar();
                toolbar.Name = name;
                toolbar.Header = name;
                toolbar.IsEnabled = isEnabled;

                toolBarTray.ToolBars.Add(toolbar);
            }));
        }

        public void RemoveToolBar(string name)
        {
            iseWindow.Dispatcher.Invoke((Action)(() =>
            {
                var toolbar = toolBarTray.ToolBars.Where(tb => tb.Name == name).FirstOrDefault();

                if (toolbar != null)
                {
                    toolBarTray.ToolBars.Remove(toolbar);
                }
            }));
        }

        public ToolBar GetToolBar(string name)
        {
            var toolBar = iseWindow.Dispatcher.Invoke((Func<ToolBar>)(() =>
                {
                    return toolBarTray.ToolBars.Where(tb => tb.Name == name).FirstOrDefault();
                }));

            return toolBar;
        }

        public ToolBar GetToolBar()
        {
            var toolBar = iseWindow.Dispatcher.Invoke((Func<ToolBar>)(() =>
            {
                return toolBarTray.ToolBars[0];
            }));

            return toolBar;
        }
        #endregion

        #region ToolbarItem methods
        public void Add(string toolBarName, ToolbarItemDefinition def)
        {
            iseWindow.Dispatcher.Invoke(new Action<string, ToolbarItemDefinition>(this.AddItem), toolBarName, def);
        }

        public ToolbarItemDefinition Get(string commandName, string toolBarName)
        {
            var item = (ToolbarItemDefinition)iseWindow.Dispatcher.Invoke(new Func<string, string, ToolbarItemDefinition>(this.GetItem), commandName, toolBarName);

            return item;
        }

        public IEnumerable<string> GetExistingCommands(string toolBarName)
        {
            var commands = new List<string>();

            iseWindow.Dispatcher.Invoke(new Action<string, List<string>>(this.GetItemCommandNames), toolBarName, commands);

            return commands;
        }

        private void GetItemCommandNames(string toolBarName, List<string> list)
        {
            ToolBar toolBar = string.IsNullOrEmpty(toolBarName) ? GetToolBar() : GetToolBar(toolBarName);

            toolBar.Items.OfType<ImageButtonBase>().Select(btn => btn.Command.Name).ForEach(c => list.Add(c));
        }

        private void AddItem(string toolBarName, ToolbarItemDefinition def)
        {
            ToolBar toolBar = string.IsNullOrEmpty(toolBarName) ? GetToolBar() : GetToolBar(toolBarName);

            ImageButtonBase item = null;

            if (def.IsTogglable)
            {
                var button = new ImageToggleButton();

                button.EnabledImageSource = new BitmapImage(new Uri(def.EnabledIconPath));
                if (def.DisabledIconPath != null) { button.DisabledImageSource = new BitmapImage(new Uri(def.DisabledIconPath)); }
                if (def.CommandBinding != null) { button.CommandBindings.Add(def.CommandBinding); }
                if (def.CommandParameter != null) { button.SetCommandParameter(def.CommandParameter); }
                button.Command = def.Command;
                button.ToolTip = def.Tooltip;

                item = button;
            }
            else
            {
                var button = new ImageButton();

                button.EnabledImageSource = new BitmapImage(new Uri(def.EnabledIconPath));
                if (def.DisabledIconPath != null) { button.DisabledImageSource = new BitmapImage(new Uri(def.DisabledIconPath)); }
                button.CommandBindings.Add(def.CommandBinding);
                button.Command = def.Command;
                if (def.CommandParameter != null) { button.SetCommandParameter(def.CommandParameter); }
                if (!string.IsNullOrEmpty(def.Tooltip)) { button.ToolTip = def.Tooltip; }

                item = button;
            }

            if (!string.IsNullOrEmpty(def.ReferenceItemName))
            {
                var index = 0;

                var referenceItem = toolBar.Items.OfType<ImageButtonBase>().Where(element => element.Command.Name == def.ReferenceItemName).FirstOrDefault() as ImageButtonBase;
                index = toolBar.Items.IndexOf(referenceItem);

                if (def.Position == ToolbarItemPosition.After)
                {
                    toolBar.Items.Insert(index + 1, item);
                }
                else if (def.Position == ToolbarItemPosition.Before)
                {
                    if (index == 0)
                    {
                        toolBar.Items.Insert(0, item);
                    }
                    else
                    {
                        toolBar.Items.Insert(index - 1, item);
                    }
                }
            }
            else
            {
                if (def.Position == ToolbarItemPosition.Top)
                {
                    toolBar.Items.Insert(0, item);
                }
                else
                {
                    toolBar.Items.Add(item);
                }
            }
        }
        #endregion

        private ToolbarItemDefinition GetItem(string CommandName, string toolBarName)
        {
            ToolBar toolBar = string.IsNullOrEmpty(toolBarName) ? GetToolBar() : GetToolBar(toolBarName);

            var item = toolBar.Items.OfType<ImageButtonBase>().Where(element => element.Command.Name == CommandName).FirstOrDefault();

            var def = new ToolbarItemDefinition()
            {
                CommandBinding = item.CommandBindings[0],
                DisabledIconPath = item.DisabledImageSource.ToString(),
                EnabledIconPath = item.EnabledImageSource.ToString(),
                Tooltip = item.ToolTip.ToString()
            };

            return def;
        }
    }

    public class ToolbarItemDefinition
    {
        public ToolbarItemDefinition()
        {
            Position = ToolbarItemPosition.End;
            CommandBinding = new CommandBinding();
        }

        public string EnabledIconPath { get; set; }
        public string DisabledIconPath { get; set; }
        public bool IsTogglable { get; set; }
        public string Tooltip { get; set; }
        public ToolbarItemPosition Position { get; set; }
        public CommandBinding CommandBinding { get; set; }
        public RoutedUICommand Command { get; set; }
        public object CommandParameter { get; set; }

        private string referenceItemName;

        public string ReferenceItemName
        {
            get { return referenceItemName; }
            set
            {
                referenceItemName = value;
                Position = ToolbarItemPosition.After;
            }
        }

    }

    public enum ToolbarItemPosition
    {
        Top,
        End,
        After,
        Before
    }
}