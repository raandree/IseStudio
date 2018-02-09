using IseHelpers;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace IseStudio
{
    [Serializable]
    public class FunctionDefinition
    {
        private string fullPath;
        private int lineNumber;
        private string name;

        public string FullPath
        {
            get { return fullPath; }
        }

        public int LineNumber
        {
            get { return lineNumber; }
        }

        public string Name
        {
            get { return name; }
        }

        [XmlIgnore]
        public string UniqueFunctionName
        {
            get
            {
                return string.Format("{0} {1}", fullPath, name);
            }
        }

        public FunctionDefinition(string fullPath, string name, int lineNumber)
        {
            this.fullPath = fullPath;
            this.name = name;
            this.lineNumber = lineNumber;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    [Serializable]
    public class FileDefinition
    {
        private string fullPath;
        private string hash;
        private ObservableCollection<FunctionDefinition> functions;

        public string FullPath
        {
            get { return fullPath; }
        }

        public string Hash
        {
            get { return hash; }
        }

        public string Name
        {
            get { return System.IO.Path.GetFileName(fullPath); }
        }

        public ObservableCollection<FunctionDefinition> Functions
        {
            get { return functions; }
            set { functions = value; }
        }

        public FileDefinition(string fullPath, string hash)
        {
            functions = new ObservableCollection<FunctionDefinition>();

            this.fullPath = fullPath;
            this.hash = hash;
        }

        public override string ToString()
        {
            return fullPath;
        }
    }

    public class FunctionContainer : ObservableDictionaryXmlStore<string, FileDefinition>
    {
        private DateTime lastUpdate;

        public DateTime LastUpdate
        {
            get { return lastUpdate; }
        }

        public int FunctionCount
        {
            get { return this.Values.Select(file => file.Functions).Count(); }
        }

        public FunctionContainer()
        {
            lastUpdate = DateTime.Now;
        }
    }
}