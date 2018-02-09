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
    public class MenuManager
    {
        Window iseWindow;
        IseStudio ise;

        private MenuItemDefinition lastMenuItemDefinition;

        public MenuItemDefinition LastMenuItemDefinition
        {
            get { return lastMenuItemDefinition; }
        }

        public MenuManager(IseStudio ise)
        {
            this.ise = ise;

            Func<MainWindow> fnc = delegate() { return Application.Current.Windows.Cast<MainWindow>().FirstOrDefault(wnd => wnd is MainWindow) as MainWindow; };
            iseWindow = Application.Current.Dispatcher.Invoke(fnc) as MainWindow;

            FieldInfo tabControlField = iseWindow.GetType().GetField("runspaceTabControl", BindingFlags.Instance | BindingFlags.NonPublic);
            var tabControl = (RunspaceTabControl)tabControlField.GetValue(iseWindow);
        }

        public void Add(string path, MenuItemDefinition def, string iconPath = "")
        {
            iseWindow.Dispatcher.Invoke(new Action<string, MenuItemDefinition, ItemCollection>(this.AddItem), path, def, null);
        }

        public void Set(string path, MenuItemDefinition def)
        {
            iseWindow.Dispatcher.Invoke(new Action<string, MenuItemDefinition, ItemCollection>(this.SetItem), path, def, null);
        }

        public MenuItem Get(string Name)
        {
            var itemXaml = iseWindow.Dispatcher.Invoke(new Func<string, ItemCollection, string>(this.GetItem), Name, null) as string;

            StringReader stringReader = new StringReader(itemXaml);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            return (MenuItem)XamlReader.Load(xmlReader);
        }

        private void AddItem(string path, MenuItemDefinition def, ItemCollection items = null)
        {
            Menu mainMenu = null;

            //if there path is empty, add the item directory to the menu
            if (string.IsNullOrEmpty(path))
            {
                var mainMenuField = iseWindow.GetType().GetField("mainMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mainMenu = (Menu)mainMenuField.GetValue(iseWindow);

                mainMenu.Items.Add(new MenuItem() { Header = def.Header });

                return;
            }

            //if we have no item collection we assume that we start in the main menu
            if (items == null)
            {
                //as this is a private field we use replections to get to the property
                var mainMenuField = iseWindow.GetType().GetField("mainMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mainMenu = (Menu)mainMenuField.GetValue(iseWindow);
                items = ((Menu)mainMenuField.GetValue(iseWindow)).Items;
            }

            //we split the path
            var pathElements = path.Split('/');

            //and get the first element of the path
            items = items.OfType<MenuItem>().Where(i => i.Header.ToString() == pathElements[0]).FirstOrDefault().Items;

            //if that first element has sub items and the path count after split is greater 1
            if (items.Count > 1 && pathElements.Count() > 1)
            {
                //we go into the next menu level and passing the path without the first element and the sub items
                AddItem(string.Join("/", pathElements.Skip(1)), def, items);
            }
            else
            {
                //if there are no subitems or no further path elements we add the new menu item here

                Control newItem = null;
                if (def.Header == "Separator")
                {
                    newItem = new Separator();
                }
                else
                {
                    newItem = new MenuItem();

                    ((MenuItem)newItem).Header = def.Header;
                    ((MenuItem)newItem).Command = def.Command;
                    if (def.CommandBinding != null) { ((MenuItem)newItem).CommandBindings.Add(def.CommandBinding); }
                    if (def.CommandParameter != null) { ((MenuItem)newItem).CommandParameter = def.CommandParameter; }

                    if (!string.IsNullOrEmpty(def.IconPath))
                    {
                        ((MenuItem)newItem).Icon = new System.Windows.Controls.Image
                        {
                            Source = new BitmapImage(new Uri(def.IconPath))
                        };
                    }
                    ((MenuItem)newItem).IsCheckable = def.IsCheckable;
                }
                var referenceNodeIndex = 0;
                if (!string.IsNullOrEmpty(def.ReferenceNode))
                {
                    var referenceMenuItem = items.OfType<MenuItem>().Where(mi => mi.Header.ToString() == def.ReferenceNode).FirstOrDefault();
                    referenceNodeIndex = items.IndexOf(referenceMenuItem);

                    if (def.Position == MenuItemPosition.After)
                    {
                        items.Insert(referenceNodeIndex + 1, newItem);
                    }
                    else if (def.Position == MenuItemPosition.Before)
                    {
                        if (referenceNodeIndex == 0)
                        {
                            items.Insert(0, newItem);
                        }
                        else
                        {
                            items.Insert(referenceNodeIndex - 1, newItem);
                        }
                    }
                }
                else
                {
                    if (def.Position == MenuItemPosition.Top)
                    {
                        items.Insert(0, newItem);
                    }
                    else
                    {
                        items.Add(newItem);
                    }
                }
            }
        }

        private string GetItem(string path, ItemCollection items = null)
        {
            //if we have no item collection we assume that we start in the main menu
            if (items == null)
            {
                //as this is a private field we use replections to get to the property
                var mainMenuField = iseWindow.GetType().GetField("mainMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                items = ((Menu)mainMenuField.GetValue(iseWindow)).Items;
            }

            //we split the path
            var pathElements = path.Split('/');

            //and get the first element of the path
            var item = (MenuItem)items.OfType<MenuItem>().Where(i => i.Header.ToString() == pathElements[0]).FirstOrDefault();

            //if that first element has sub items and the path count after split is greater 1
            if (item.Items.Count > 1 && pathElements.Count() > 1)
            {
                //we go into the next menu level and passing the path without the first element and the sub items
                var itemXaml = (string)GetItem(string.Join("/", pathElements.Skip(1)), item.Items);

                //we return what we have received by the recursion
                return itemXaml;
            }
            else
            {
                //if there are no subitems or no further path elements we return what we have
                lastMenuItemDefinition = new MenuItemDefinition()
                {
                    Header = item.Header.ToString(),
                    IconPath = item.Icon.ToString(),
                    IsCheckable = item.IsCheckable,
                    //ShortCut = item.???

                };
                return XamlWriter.Save(item);
            }
        }

        private void SetItem(string path, MenuItemDefinition def, ItemCollection items = null)
        {
            //if we have no item collection we assume that we start in the main menu
            if (items == null)
            {
                //as this is a private field we use replections to get to the property
                var mainMenuField = iseWindow.GetType().GetField("mainMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                items = ((Menu)mainMenuField.GetValue(iseWindow)).Items;
            }

            //we split the path
            var pathElements = path.Split('/');

            //and get the first element of the path
            var item = (MenuItem)items.OfType<MenuItem>().Where(i => i.Header.ToString() == pathElements[0]).FirstOrDefault();

            //if that first element has sub items and the path count after split is greater 1
            if (item.Items.Count > 1 && pathElements.Count() > 1)
            {
                //we go into the next menu level and passing the path without the first element and the sub items
                SetItem(string.Join("/", pathElements.Skip(1)), def, item.Items);
            }
            else
            {
                //if there are no subitems or no further path elements we add the new menu item here
                item.Header = def.Header;
                if (def.Command != null) { ((MenuItem)item).Command = def.Command; }
                if (def.CommandBinding != null) { ((MenuItem)item).CommandBindings.Add(def.CommandBinding); }
                if (def.CommandParameter != null) { ((MenuItem)item).CommandParameter = def.CommandParameter; }

                if (def.IconPath != "")
                {
                    item.Icon = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri(def.IconPath))
                    };
                }
            }
        }
    }

    public class MenuItemDefinition
    {
        public MenuItemDefinition()
        {
            Position = MenuItemPosition.End;
        }

        public string Header { get; set; }
        public string IconPath { get; set; }
        public bool IsCheckable { get; set; }
        public string ShortCut { get; set; }
        public MenuItemPosition Position { get; set; }
        public RoutedUICommand Command { get; set; }
        public CommandBinding CommandBinding { get; set; }
        public object CommandParameter { get; set; }

        private string referenceNode;

        public string ReferenceNode
        {
            get { return referenceNode; }
            set
            {
                referenceNode = value;
                Position = MenuItemPosition.After;
            }
        }

    }

    public enum MenuItemPosition
    {
        Top,
        End,
        After,
        Before
    }
}