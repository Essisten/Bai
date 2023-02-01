using System.Collections.Generic;

namespace Bai
{
    class User
    {
        public static List<User> Users = new List<User>();
        public long ID { get; set; }
        public string UserName { get; set; }
        public int AccessLevel { get; set; }    //1 - обычный, 2 - админ
        public List<int> Entries { get; set; }
        public string[] LastAction = new string[3];
        /*LastAction создан для хранения временных данных.
        Например: следующую команду, которую нужно вызвать после следующего сообщения от пользователя(индекс 0) или введёные пользователем значения.*/
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
        public List<Organization> GetOrganizations()
        {
            List<Organization> tmp = new List<Organization>();
            List<UserEntry> entries = GetEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                Organization org = Organization.Orgs.Find(_ => _.ID == entries[i].OrgID);
                if (org == null)
                {
                    entries.RemoveAt(i);
                    i--;
                    entries = GetEntries();
                    continue;
                }
                tmp.Add(org);
            }
            return tmp;
        }
        /// <summary>
        /// Форматирует список организаций, в которых участвует сотрудник
        /// </summary>
        /// <param name="sender">Сотрудник</param>
        /// <returns>Список организаций</returns>
        public string GetOrgList()
        {
            List<Organization> orgs = GetOrganizations();
            string final_message = "\n";
            for (int i = 0; i < orgs.Count; i++)
            {
                final_message += $"\n{i}. {orgs[i].Name}";
            }
            return final_message;
        }
        /// <summary>
        /// Форматирует общий список сотрудников
        /// </summary>
        /// <returns>Список всех существующих сотрудников</returns>
        public static string GetGlobalUserList()
        {
            string fin = "Список пользователей:";
            for (int k = 0; k < Users.Count; k++)
            {
                fin += $"\n{k + 1}) {Users[k].UserName}.";
            }
            return fin;
        }
    }
}