using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

namespace AIEdit
{
	/// <summary>
	/// Summary description for AITable.
	/// </summary>
	public abstract class AITable<T> where T : IAIObject, new()
	{
		private Hashtable names;
        private OrderedDictionary table;

		public AITable()
		{
			names = new Hashtable();
            table = new OrderedDictionary();
		}

        public OrderedDictionary Table { get { return table; } }

		public void Add(string id, T t)
		{
            table[id] = t;
			names[t.Name] = id;
		}

		public void Remove(string id)
		{
            if (!table.Contains(id)) return;
            names.Remove(((T)table[id]).Name);
            table.Remove(id);
		}
        
        public void UpdateName(T t, string newname)
        {
            if (!names.ContainsKey(t.Name)) return;
            names.Remove(t.Name);
            names[newname] = t.ID;
            t.Name = newname;
        }


        public int GetIndex(string id)
        {
            int i = 0;
            foreach (string s in table.Keys) if (s.CompareTo(id) == 0) return i;
            return -1;
        }

        public T GetByID(string id)
		{
            if (!table.Contains(id)) return default(T);
            return (T)table[id];
		}

		public T GetByName(string name)
		{
            if (!names.ContainsKey(name)) return default(T);
			return (T)table[ names[name] ];
		}

        public string[] GetNames(bool sort)
        {
            ArrayList al = new ArrayList(table.Count);
            foreach (T t in table.Values) if (t.Name != null) al.Add(t.Name);
            if(sort) al.Sort();
            return (string[])al.ToArray(typeof(string));
        }

        public void Load(string file)
		{
			StreamReader stream = new StreamReader(file);
			IniParser ip = new IniParser(stream);
            T t;

            table.Clear();
            names.Clear();
			// Parse the section that contains the id list.
			while(ip.Next())
			{
				if(ip.Section != this.TypeList)
				{
					ip.Skip();
					continue;
				}
				
				ip.Parse();
                foreach (string id in ip.Table.Values)
                {
                    if (id == null || id.Length == 0) continue;
                    if (!table.Contains(id))
                    {
                        t = new T();
                        t.ID = id;
                        table.Add(id, t);
                    }
                }
				
				break;
			}

			// Parse each section that is in the list.
            while (ip.Next()) ParseSection(ip);

			stream.Close();

            // Check for invalid entries. 
            foreach (T t2 in table.Values)
            {
                if (t2.Name == null || t2.Name.Length == 0)
                {
                    t2.Name = t2.ID;// +" (INVALID ENTRY)";
                    names[t2.Name] = t2.ID;
                }
            }
		}

        public int Count { get { return table.Count; } }
        public IDictionaryEnumerator GetEnumerator() { return table.GetEnumerator(); }

        protected void MapName(string name, string id) { names[name] = id; }
        

		public abstract void ParseSection(IniParser ip);
		public abstract string TypeList { get; }
	}
}