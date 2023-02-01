using System;
using System.Collections.Generic;

namespace Bai
{
    class Organization
    {
        public static List<Organization> Orgs = new List<Organization>();
        public static int MaxID { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public List<int> Entries { get; set; }
        public List<UserEntry> GetEntries()
        {
            List<UserEntry> tmp = new List<UserEntry>();
            for (int i = 0; i < Entries.Count; i++)
            {
                UserEntry entry = UserEntry.Entries.Find(_ => _.ID == Entries[i]);
                if (entry == null)
                {
                    Entries.RemoveAt(i);
                    i--;
                    continue;
                }
                tmp.Add(entry);
            }
            return tmp;
        }
        public string GetEntriesList()
        {
            string final_message = $"Список учётных записей организации \"{Name}\":\n";
            List<UserEntry> entries = GetEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                final_message += $"\n{i + 1}) {entries[i].GetUser().UserName}";
            }
            return final_message;
        }
        public static string GetGlobalOrgList()
        {
            string final_message = "Список организаций:";
            for (int i = 0; i < Orgs.Count; i++)
            {
                final_message += $"\n{i + 1}) {Orgs[i]}";
            }
            return final_message;
        }
    }
}