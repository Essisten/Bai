using System;
using System.Collections.Generic;

namespace Bai
{
    class UserEntry
    {
        public static List<UserEntry> Entries = new List<UserEntry>();
        public static int MaxID { get; set; }
        public int ID { get; set; }
        public long UserID { get; set; }
        public int OrgID { get; set; }
        public DateTime AppearanceDate { get; set; }
        public DateTime LastDaysSync { get; set; }
        public DateTime StartVacation { get; set; }
        public double MainVacation { get; set; }
        public double ExtraVacation { get; set; }
        public int Group { get; set; }          //1 - 28 дней, 2 - 42 дня
        /// <summary>
        /// Тратит отпускные дни и устанавливает дату конца отпуска
        /// </summary>
        /// <param name="days">количество добавляемых отпускных дней</param>
        public void UseVacation(int days)
        {
            User owner = User.Users.Find(_ => _.ID == UserID);
            if (owner.LastAction[0] == "ExtraV")
            {
                ExtraVacation -= days;
                if (ExtraVacation < 0)
                {
                    MainVacation += ExtraVacation;
                    ExtraVacation = 0;
                }
            }
            else
            {
                MainVacation -= days;
                if (MainVacation < 0)
                {
                    ExtraVacation += MainVacation;
                    MainVacation = 0;
                }
            }
            if (StartVacation < LastDaysSync)
                LastDaysSync = LastDaysSync.AddDays(days);
            else if (DateTime.TryParse(owner.LastAction[1], out DateTime start))
            {
                StartVacation = start;
                LastDaysSync = StartVacation.AddDays(days);
            }
        }
        /// <summary>
        /// Подсчитывает, какое количество дней нужно добавить сотруднику, после чего устанавливает дату последней синхронизации
        /// </summary>
        /// <returns>Были ли добавлены дни</returns>
        public bool RestoreVacation()
        {
            int PassedMonth = (DateTime.Today.Month - LastDaysSync.Month) + (DateTime.Today.Year - LastDaysSync.Year) * 12;
            if (DateTime.Today.Day < LastDaysSync.Day)
                PassedMonth--;
            if (PassedMonth < 1)
                return false;
            LastDaysSync = LastDaysSync.AddMonths(PassedMonth);
            StartVacation = LastDaysSync;
            if (Group == 2)
                ExtraVacation += PassedMonth * 1.17;
            MainVacation += PassedMonth * 2.33;
            return true;
        }
        public User GetUser()
        {
            return User.Users.Find(_ => _.ID == UserID);
        }
        public Organization GetOrganization()
        {
            return Organization.Orgs.Find(_ => _.ID == OrgID);
        }
        public static string GetGlobalEntryList()
        {
            string fin = "Список учётных записей:\n";
            for (int k = 0; k < Entries.Count; k++)
            {
                UserEntry entry = Entries[k];
                User user = entry.GetUser();
                Organization org = entry.GetOrganization();
                fin += $"\n{k + 1}) {user.UserName} ({org.Name}).";
            }
            return fin;
        }
    }
}