using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IseHelpers
{
    [Serializable]
    public class ListXmlStore<T> : List<T>
    {
        public void AddFromFile(string path)
        {
            if (! new FileInfo(path).Exists) { return; }
            
            var serializer = new XmlSerializer(typeof(ListXmlStore<T>));
            FileStream fileStream = new FileStream(path, FileMode.Open);

            this.AddRange((ListXmlStore<T>)serializer.Deserialize(fileStream));

            fileStream.Close();
        }

        public static ListXmlStore<T> Import(string path)
        {
            var serializer = new XmlSerializer(typeof(ListXmlStore<T>));
            FileStream fileStream = new FileStream(path, FileMode.Open);

            var items = (ListXmlStore<T>)serializer.Deserialize(fileStream);

            fileStream.Close();

            return items;
        }

        public void Export(string path)
        {
            var serializer = new XmlSerializer(typeof(ListXmlStore<T>));
            File.Delete(path);
            FileStream fileStream = new FileStream(path, FileMode.CreateNew);

            serializer.Serialize(fileStream, this);

            fileStream.Close();
        }
    }

    [Serializable]
    public class DictionaryXmlStore<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private string keyProperty;
        public string KeyProperty
        {
            get { return keyProperty; }
        }

        public void AddFromFile(string path)
        {
            var serializer = new XmlSerializer(typeof(List<TValue>));
            FileStream fileStream = new FileStream(path, FileMode.Open);

            var items = (List<TValue>)serializer.Deserialize(fileStream);

            fileStream.Close();

            foreach (var item in items)
            {
                TKey key = (TKey)item.GetType().GetProperty(keyProperty).GetValue(item, null);
                this.Add(key, item);
            }
        }

        public static DictionaryXmlStore<TKey, TValue> Import(string path, string keyProperty)
        {
            var serializer = new XmlSerializer(typeof(List<TValue>));
            FileStream fileStream = new FileStream(path, FileMode.Open);
            List<TValue> items;

            try
            {
                items = (List<TValue>)serializer.Deserialize(fileStream);
            }
            catch
            {
                items = new List<TValue>();
            }

            fileStream.Close();

            var dictionary = new DictionaryXmlStore<TKey, TValue>();
            foreach (var item in items)
            {
                TKey key = (TKey)item.GetType().GetProperty(keyProperty).GetValue(item, null);
                dictionary.Add(key, item);
            }

            dictionary.keyProperty = keyProperty;

            return dictionary;
        }

        public void Export(string path)
        {
            var functions = this.Values.ToList();

            var serializer = new XmlSerializer(functions.GetType());
            File.Delete(path);
            FileStream fileStream = new FileStream(path, FileMode.CreateNew);

            serializer.Serialize(fileStream, functions);

            fileStream.Close();
        }
    }

    public partial class ObservableDictionaryXmlStore<TKey, TValue> : Dictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public ObservableDictionaryXmlStore() : base() { }
        public ObservableDictionaryXmlStore(int capacity) : base(capacity) { }
        public ObservableDictionaryXmlStore(IEqualityComparer<TKey> comparer) : base(comparer) { }
        public ObservableDictionaryXmlStore(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
        public ObservableDictionaryXmlStore(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }
        public ObservableDictionaryXmlStore(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private string keyProperty;
        public string KeyProperty
        {
            get { return keyProperty; }
        }

        public new TValue this[TKey key]
        {
            get
            {
                return base[key];
            }
            set
            {
                TValue oldValue;
                bool exist = base.TryGetValue(key, out oldValue);
                var oldItem = new KeyValuePair<TKey, TValue>(key, oldValue);
                base[key] = value;
                var newItem = new KeyValuePair<TKey, TValue>(key, value);
                if (exist)
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, base.Keys.ToList().IndexOf(key)));
                }
                else
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, base.Keys.ToList().IndexOf(key)));
                    this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                }
            }
        }

        public static ObservableDictionaryXmlStore<TKey, TValue> Import(string path, string keyProperty)
        {
            var serializer = new XmlSerializer(typeof(List<TValue>));
            FileStream fileStream = new FileStream(path, FileMode.Open);
            List<TValue> items;

            try
            {
                items = (List<TValue>)serializer.Deserialize(fileStream);
            }
            catch
            {
                items = new List<TValue>();
            }

            fileStream.Close();

            var dictionary = new ObservableDictionaryXmlStore<TKey, TValue>();

            foreach (var item in items)
            {
                TKey key = (TKey)item.GetType().GetProperty(keyProperty).GetValue(item, null);
                dictionary.Add(key, item);
            }

            dictionary.keyProperty = keyProperty;
            
            return dictionary;
        }

        public new void Add(TKey key, TValue value)
        {
            if (!base.ContainsKey(key))
            {
                var item = new KeyValuePair<TKey, TValue>(key, value);
                base.Add(key, value);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, base.Keys.ToList().IndexOf(key)));
                this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            }
        }

        public void AddFromFile(string path)
        {
            var serializer = new XmlSerializer(typeof(List<TValue>));
            FileStream fileStream = new FileStream(path, FileMode.Open);
            
            TKey lastKey = default(TKey);
            TValue lastItem = default(TValue);

            var items = (List<TValue>)serializer.Deserialize(fileStream);

            fileStream.Close();

            foreach (var item in items)
            {
                TKey key = (TKey)item.GetType().GetProperty(keyProperty).GetValue(item, null);
                this.Add(key, item);
                lastItem = item;
                lastKey = key;
            }

            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, lastItem, base.Keys.ToList().IndexOf(lastKey)));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        }

        public new bool Remove(TKey key)
        {
            TValue value;
            if (base.TryGetValue(key, out value))
            {
                var item = new KeyValuePair<TKey, TValue>(key, base[key]);
                bool result = base.Remove(key);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, base.Keys.ToList().IndexOf(key)));
                this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                return result;
            }
            return false;
        }

        public new void Clear()
        {
            base.Clear();
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, e);
            }
        }

        public void Export(string path)
        {
            var functions = this.Values.ToList();

            var serializer = new XmlSerializer(functions.GetType());
            File.Delete(path);
            FileStream fileStream = new FileStream(path, FileMode.CreateNew);

            serializer.Serialize(fileStream, functions);

            fileStream.Close();
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, e);
            }
        }
    }

    public partial class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public ObservableDictionary() : base() { }
        public ObservableDictionary(int capacity) : base(capacity) { }
        public ObservableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
        public ObservableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }
        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public new TValue this[TKey key]
        {
            get
            {
                return base[key];
            }
            set
            {
                TValue oldValue;
                bool exist = base.TryGetValue(key, out oldValue);
                var oldItem = new KeyValuePair<TKey, TValue>(key, oldValue);
                base[key] = value;
                var newItem = new KeyValuePair<TKey, TValue>(key, value);
                if (exist)
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, base.Keys.ToList().IndexOf(key)));
                }
                else
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, base.Keys.ToList().IndexOf(key)));
                    this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                }
            }
        }

        public new void Add(TKey key, TValue value)
        {
            if (!base.ContainsKey(key))
            {
                var item = new KeyValuePair<TKey, TValue>(key, value);
                base.Add(key, value);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, base.Keys.ToList().IndexOf(key)));
                this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            }
        }

        public new bool Remove(TKey key)
        {
            TValue value;
            if (base.TryGetValue(key, out value))
            {
                var item = new KeyValuePair<TKey, TValue>(key, base[key]);
                bool result = base.Remove(key);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, base.Keys.ToList().IndexOf(key)));
                this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                return result;
            }
            return false;
        }

        public new void Clear()
        {
            base.Clear();
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, e);
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, e);
            }
        }
    }
}