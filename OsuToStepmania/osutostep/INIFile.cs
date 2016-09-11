using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace osutostep
{
    class INIFile
    {
        public class Group
        {
            public string Name { get; private set; }
            public Dictionary<string, string> KeyMappings { get; set; }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[{Name}]");
                foreach (KeyValuePair<string, string> kv in KeyMappings)
                {
                    sb.AppendLine($"{kv.Key}={kv.Value}");
                }

                return sb.ToString();
            }

            public Group(string name)
            {
                Name = name;
                KeyMappings = new Dictionary<string, string>();
            }
        }

        public bool LoadedSuccessfully { get; private set; }

        private List<Group> groups = new List<Group>();

        public INIFile(string path)
        {
            DeserializeINI(path);
        }

        public INIFile()
        { }

        public void DeserializeINI(string path)
        {
            try
            {
                DeserializeINI_Unsafe(path);
            }
            catch
            {
                
            }
        }

        public void DeserializeINI_Unsafe(string path)
        {
            LoadedSuccessfully = false;
            string text = File.ReadAllText(path);

            Group currentGroup = null;

            using (StringReader r = new StringReader(text))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    line.Trim();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (line.StartsWith("["))
                    {
                        currentGroup = new Group(line.Replace("[", "").Replace("]", ""));
                        groups.Add(currentGroup);
                    }
                    else
                    {
                        if (currentGroup == null)
                        {
                            return;
                        }

                        string[] keyValPair = line.Split('=');

                        if (keyValPair.Length != 2)
                        {
                            continue;
                        }

                        currentGroup.KeyMappings.Add(keyValPair[0], keyValPair[1]);
                    }
                }
            }
            LoadedSuccessfully = true;
        }

        public void SerializeINI(string outPath)
        {
            StringBuilder serializedString = new StringBuilder();
            foreach(Group g in groups)
            {
                serializedString.AppendLine(g.ToString());
            }

            File.WriteAllText(outPath, serializedString.ToString());
        }
        
        public bool GroupExists(string groupName)
        {
            bool ret = false;

            foreach (Group g in groups)
            {
                if (ret |= g.Name.Equals(groupName)) break;
            }

            return ret;
        }

        public Group GetGroup(string groupName)
        {
            foreach (Group g in groups)
            {
                if (g.Name.Equals(groupName)) return g;
            }
            return null;
        }

        public void AddValue(string key, string value, string group)
        {
            Group g = GetGroup(group);

            if(g == null)
            {
                g = new Group(group);
                groups.Add(g);
            }

            g.KeyMappings.Add(key, value);
        }

        public void RemoveKey(string key, string group)
        {
            Group g = GetGroup(group);

            if(g != null)
            {
                g.KeyMappings.Remove(key);
            }
        }

        public string GetValue(string key, string group)
        {
            string value;
            Group g = GetGroup(group);

            if(g == null || !g.KeyMappings.TryGetValue(key, out value))
            {
                return null;
            }
            
            return value;
        }
        
    }
}
