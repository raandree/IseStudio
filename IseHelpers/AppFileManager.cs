using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IseHelpers
{
    public class AppFileManager
    {
        private string appName;
        private string appFilePath;
        private Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();

        public string AppName
        {
            get { return appName; }
        }

        public AppFileManager(string appName)
        {
            this.appName = this.appName = appName;
            this.appFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);

            if (!Directory.Exists(appFilePath))
            {
                Directory.CreateDirectory(appFilePath);
            }
        }

        public AppFileManager()
            : this(Path.GetFileNameWithoutExtension(Assembly.GetCallingAssembly().ManifestModule.Name))
        { }

        public void Add(string name)
        {
            var fullName = Path.Combine(this.appFilePath, name);

            var file = new FileInfo(fullName);

            this.files.Add(name, file);
        }

        public void Remove(string name)
        {
            this.files.Remove(name);
        }

        public void Delete(string name, bool remove = false)
        {
            this.files[name].Delete();

            if (remove)
            {
                this.Remove(name);
            }
        }

        public FileInfo Get(string name)
        {
            return this.files[name];
        }

        public IEnumerable<FileInfo> Get()
        {
            return this.files.Values;
        }
    }
}