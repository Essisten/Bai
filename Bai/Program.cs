using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bai
{
    class Program
    {
        static readonly ITelegramBotClient bot = new TelegramBotClient("5439131220:AAG-YRDPD-H17PYpQ9Jqzz7WuMzYZXIRH6E");
        static readonly JsonSerializerOptions options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
        };
        static readonly int MaxGroup = 2, MaxAccess = 2;
        static readonly Random rng = new Random();
        static readonly string SavePath = "./data.json", BackupPath = "./backup.json", LogPath = "./log.txt";
        static readonly string[] UnknownUserReply = new string[] { "Мы знакомы?", "Я тебя не знаю", "Ты кто?", "Попроси админов добавить тебя в мой список", },
                        group_name = new string[] { "Основной", "Основной+Дополнительный" },
                        acces_name = new string[] { "Обычный", "Администратор" };
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Message message = update.Message;
            long senderID, id;
            int num = 1, access;
            string final_message = "";
            if (message == null)
            {
                if (update.CallbackQuery != null)
                    senderID = update.CallbackQuery.From.Id;
                else
                {
                    Console.WriteLine("Не могу определить ID отправителя...");
                    return;
                }
            }
            else
                senderID = message.Chat.Id;
            User sender = User.Users.Find(_ => _.ID == senderID), target_user = null;
            List<UserEntry> clones = sender.GetEntries();
            if (clones.Count == 0)
            {
                await botClient.SendTextMessageAsync(message.Chat, UnknownUserReply[rng.Next(0, UnknownUserReply.Length - 1)] + "\nТвой ID: " + senderID);
                return;
            }
            Organization target_organization = null;
            UserEntry ActiveEntry = ChooseEntry(sender, clones), target_entry = null;
            if (sender.LastAction[1] != "" && int.TryParse(sender.LastAction[1], out num))
            {
                if (sender.LastAction[0].StartsWith("EditUser") && num <= User.Users.Count)
                {
                    target_user = User.Users[num - 1];
                }
                else if (sender.LastAction[0].StartsWith("EditOrg") && num <= Organization.Orgs.Count)
                {
                    target_organization = Organization.Orgs[num - 1];
                }
                else if (sender.LastAction[0].StartsWith("EditEntry") && num <= UserEntry.Entries.Count)
                {
                    target_entry = UserEntry.Entries[num - 1];
                }
            }    
            switch (update.Type)
            {
                case UpdateType.Message:
                    switch (message.Text.ToLower())
                    {
                        #region commands
                        case "/start":
                            GetCommandList(botClient, message, sender);
                            break;
                        case "/info":
                            ShowUserProfile(botClient, message, sender, clones);
                            break;
                        case "/vacation":
                            await botClient.SendTextMessageAsync(message.Chat, "Кажется, ты числишься в нескольких организациях..." +
                                "К какой из перечисленных стоит применить твою команду?\n" + sender.GetOrgList());
                            sender.LastAction[0] = "VacationCommand";
                            break;
                        case "/endvacation":
                            await botClient.SendTextMessageAsync(message.Chat, "Кажется, ты числишься в нескольких организациях..." +
                                "К какой из перечисленных стоит применить твою команду?\n" + sender.GetOrgList());
                            sender.LastAction[0] = "EndVacationCommand";
                            break;
                        case "/userlist":
                            if (sender.AccessLevel < 2)
                            {
                                RejectCommand(botClient, message);
                                return;
                            }
                            await botClient.SendTextMessageAsync(message.Chat, User.GetGlobalUserList());
                            break;
                        case "/adduser":
                            if (sender.AccessLevel < 2)
                            {
                                RejectCommand(botClient, message);
                                return;
                            }
                            sender.LastAction[0] = "AddUser";
                            await botClient.SendTextMessageAsync(message.Chat, "В следующем сообщении введи данные сотрудника в следующем формате:\n" +
                                "ID аккаунта сотрудника" +
                                "\nФИО сотрудника" +
                                "\nПрава доступа (1 - обычный, 2 - админ)");
                            break;
                        case "/deleteuser":
                            if (sender.AccessLevel < 2)
                            {
                                RejectCommand(botClient, message);
                                return;
                            }
                            sender.LastAction[0] = "DeleteUser";
                            await botClient.SendTextMessageAsync(message.Chat, "Введи порядковый номер сотрудника из списка");
                            break;
                        case "/edituser":
                            if (sender.AccessLevel < 2)
                            {
                                RejectCommand(botClient, message);
                                return;
                            }
                            sender.LastAction[0] = "EditUser";
                            await botClient.SendTextMessageAsync(message.Chat, "Введи порядковый номер сотрудника из списка");
                            break;
                        case "/addorg":
                            if (sender.AccessLevel < 2)
                            {
                                RejectCommand(botClient, message);
                                return;
                            }
                            sender.LastAction[0] = "AddOrg";
                            await botClient.SendTextMessageAsync(message.Chat, "Введи название добавляемой организации");
                            break;
                        case "/deleteorg":
                            if (sender.AccessLevel < 2)
                            {
                                RejectCommand(botClient, message);
                                return;
                            }
                            sender.LastAction[0] = "DeleteOrg";
                            await botClient.SendTextMessageAsync(message.Chat, "Введи порядковый номер организации");
                            break;
                        case "/orglist":
                            await botClient.SendTextMessageAsync(message.Chat, Organization.GetGlobalOrgList());
                            break;
                        case "/editorg":
                            if (sender.AccessLevel < 2)
                            {
                                RejectCommand(botClient, message);
                                return;
                            }
                            sender.LastAction[0] = "EditOrg";
                            await botClient.SendTextMessageAsync(message.Chat, "Введи порядковый номер организации");
                            break;
                        case "/entrylist":
                            await botClient.SendTextMessageAsync(message.Chat, UserEntry.GetGlobalEntryList());
                            break;
                        case "/editentry":
                            await botClient.SendTextMessageAsync(message.Chat, "Введи порядковый номер учётной записи, которую нужно отредактировать");
                            sender.LastAction[0] = "EditEntry";
                            break;
                        #endregion
                        default:
                            switch (sender.LastAction[0])   //преддействие
                            {
                                case "EditUserID":
                                case "EditUserName":
                                case "EditUserAccess":
                                case "EditMain":
                                case "EditExtra":
                                    if (!int.TryParse(sender.LastAction[1], out num) || num > User.Users.Count)
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat, "Не могу найти сотрудника по указанному номеру... Проверь список ещё раз.\n\n" + User.GetGlobalUserList());
                                        return;
                                    }
                                    target_user = User.Users[num - 1];
                                    break;
                                case "VacationCommand":
                                case "EndVacationCommand":
                                    sender.LastAction[2] = message.Text;
                                    ActiveEntry = ChooseEntry(sender, clones);
                                    if (ActiveEntry == null)
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat, "Нужно указать порядковый номер организации, " +
                                            "по отношении к которой нужно применить твою команду.\n" + sender.GetOrgList());
                                        return;
                                    }
                                    break;
                            }
                            switch (sender.LastAction[0])   //действие
                            {
                                case "VacationCommand":
                                    await botClient.SendTextMessageAsync(message.Chat, "Какого числа начнётся отпуск?");
                                    sender.LastAction[0] = "StartVacation";
                                    break;
                                case "StartVacation":
                                    StartVacation(botClient, message, ActiveEntry, sender);
                                    break;
                                case "MainV":
                                case "ExtraV":
                                    MainOrExtraVacation(botClient, message, ActiveEntry, sender);
                                    break;
                                case "EndVacationCommand":
                                    if (ActiveEntry.LastDaysSync <= ActiveEntry.StartVacation)
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat, "Нет активных отпусков");
                                        return;
                                    }
                                    await botClient.SendTextMessageAsync(message.Chat, "Какого числа закончился отпуск?");
                                    sender.LastAction[0] = "EndVacation";
                                    break;
                                case "EndVacation":
                                    EndVacation(botClient, message, ActiveEntry, sender);
                                    break;
                                case "AddUser":
                                    AddUser(botClient, message, sender);
                                    break;
                                case "DeleteUser":
                                    DeleteUser(botClient, message, sender);
                                    break;
                                case "EditUser":
                                    EditUser(botClient, message, sender);
                                    break;
                                case "EditUserID":
                                    if (!long.TryParse(message.Text, out id))
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat, "Пользователя телеграм с таким ID не существует. Если он лично напишет мне, я подскажу ему какой у него ID, чтобы тот мог передать его тебе");
                                        return;
                                    }
                                    target_user.ID = id;
                                    break;
                                case "EditUserName":
                                    target_user.UserName = message.Text.Replace("\n", " ");
                                    break;
                                case "EditUserAccess":
                                    if (!int.TryParse(message.Text, out access) || access > MaxAccess || access < 1)
                                    {
                                        string msg = "";
                                        for (int k = 1; k <= MaxAccess; k++)
                                        {
                                            msg += $"{k}) {acces_name[k - 1]}\n";
                                        }
                                        await botClient.SendTextMessageAsync(message.Chat, msg);
                                        return;
                                    }
                                    target_user.AccessLevel = access;
                                    break;
                                case "CleanLastAction":
                                    for (int i = 0; i < sender.LastAction.Length; i++)
                                        sender.LastAction[i] = "";
                                    await botClient.SendTextMessageAsync(message.Chat, $"Действия {sender.UserName} очищены...");
                                    break;
                                case "AddOrg":
                                    AddOrg(botClient, message);
                                    break;
                                case "DeleteOrg":
                                    DeleteOrg(botClient, message);
                                    break;
                                case "EditOrg":
                                    EditOrg(botClient, message, sender);
                                    break;
                                case "EditOrgID":
                                    EditOrgID(botClient, message, target_organization);
                                    break;
                                case "EditOrgName":
                                    target_organization.Name = message.Text.Replace("\n", " ");
                                    break;
                                case "EditOrgAddEntry":
                                    AddEntry(botClient, message, target_organization, sender);
                                    break;
                                case "EditOrgRemoveEntry":
                                    RemoveEntry(botClient, message, target_organization);
                                    break;
                                case "EditEntry":
                                    EditEntry(botClient, message, sender);
                                    break;
                                case "EditEntryID":
                                    EditEntryID(botClient, message, target_entry);
                                    break;
                                case "EditEntryUserID":

                                    break;
                            }
                            switch (sender.LastAction[0])   //последействие
                            {
                                case "EditUserID":
                                case "EditUserName":
                                case "EditUserAccess":
                                case "EditMain":
                                case "EditExtra":
                                    Save();
                                    sender.LastAction[0] = "";
                                    await botClient.SendTextMessageAsync(message.Chat, $"Данные сотрудника {target_user.UserName} сохранены.");
                                    break;
                                case "EditOrgID":
                                case "EditOrgName":
                                case "EditOrgAddEntry":
                                case "EditOrgRemoveEntry":
                                    Save();
                                    sender.LastAction[0] = "";
                                    await botClient.SendTextMessageAsync(message.Chat, $"Данные организации {target_organization.Name} сохранены.");
                                    break;
                            }
                            break;
                    }
                    break;
                case UpdateType.CallbackQuery:
                    CallbackHandler(botClient, update.CallbackQuery, sender);
                    break;
            }
        }
        private static async void CallbackHandler(ITelegramBotClient botClient, CallbackQuery callback, User sender)
        {
            string response = callback.Data;
            Chat chat = callback.Message.Chat;
            switch (response)
            {
                case "EditUserID":
                case "EditUserName":
                case "EditUserAccess":
                    if (!sender.LastAction[1].StartsWith("EditUser"))
                    {
                        await botClient.SendTextMessageAsync(chat, "Нельзя использовать старые кнопки для редактирования");
                        return;
                    }
                    break;
                case "EditOrgID":
                case "EditOrgName":
                    if (!sender.LastAction[1].StartsWith("EditOrg"))
                    {
                        await botClient.SendTextMessageAsync(chat, "Нельзя использовать старые кнопки для редактирования");
                        return;
                    }
                    break;
            }
            switch (response)
            {
                case "MainV":
                case "ExtraV":
                    await botClient.SendTextMessageAsync(chat, "На сколько дней уйдёшь в отпуск?");
                    break;
                case "EditUserID":
                case "EditUserName":
                case "EditUserAccess":
                case "EditMain":
                case "EditExtra":
                case "EditOrgID":
                case "EditOrgName":
                    await botClient.SendTextMessageAsync(chat, "Отправь значение в следующем сообщении.");
                    break;
                case "EditOrgAddEntry":
                    await botClient.SendTextMessageAsync(chat, "В следующем сообщении отправь данные для создания учётной записи в следующем формате:" +
                        "\nПорядковый номер пользователя (для просмотра списка введи /userlist)" +
                        $"\nДата трудоустройства (например: {DateTime.Today.ToShortDateString()})" +
                        $"\nГруппа для расчёта отпуска (1 - основной, 2 - основной+дополнительный)" +
                        $"\nОсновные отпускные (необязательно)" +
                        $"\nДополнительные отпускные (необязательно)");
                    break;
                case "EditOrgRemoveEntry":
                    CallbackRemoveEntry(botClient, chat, sender);
                    break;
            }
            sender.LastAction[0] = response;
        }
        private static async void GetCommandList(ITelegramBotClient botClient, Message message, User sender)
        {
            string final_message = "Привет! Меня создали для учёта дней отпусков, которые ты получаешь каждый полный рабочий месяц. Можешь попробовать эти команды:\n" +
                "\n/info - Информация о тебе" +
                "\n/vacation - Выйти в отпуск или продлить его" +
                "\n/endvacation - Досрочно выйти из отпуска";
            if (sender.AccessLevel == 2)
                final_message += "\n\nКоманды для администраторов:" +
                "\n/userlist - Список сотрудников" +
                "\n/adduser - Добавить сотрудника" +
                "\n/deleteuser - Удалить сотрудника" +
                "\n/edituser - Редактировать данные сотрудника" +
                "\n\n/orglist - Список организаций" +
                "\n/addorg - Добавить организацию" +
                "\n/deleteorg - Удалить организацию" +
                "\n/editorg - Редактировать организацию";
            await botClient.SendTextMessageAsync(message.Chat, final_message);
        }
        private static async void ShowUserProfile(ITelegramBotClient botClient, Message message, User sender, List<UserEntry> clones)
        {
            string final_message = $"\n\nИмя: {sender.UserName}" +
                $"\nПрава: {acces_name[sender.AccessLevel - 1]}";
            for (int i = 0; i < clones.Count; i++)
            {
                UserEntry entry = clones[i];
                entry.RestoreVacation();
                final_message += $"\n\nОрагнизация: {Organization.Orgs.Find(_ => _.ID == entry.OrgID).Name}" +
                    $"\nДата трудоустройства: {entry.AppearanceDate.ToLongDateString()}" +
                    $"\nВид отпуска: {group_name[entry.Group - 1]}" +
                    $"\n\nОтпускных дней в запасе:" +
                    $"\nОсновных: {Math.Floor(entry.MainVacation)}";
                if (entry.Group == 2)
                    final_message += "\nДополнительных: " + Math.Floor(entry.ExtraVacation);
                final_message += "\n\nСтатус: ";
                if (DateTime.Today < entry.LastDaysSync && DateTime.Today >= entry.StartVacation)
                    final_message += $"в отпуске";
                else
                    final_message += "работает";
                if (entry.LastDaysSync > DateTime.Today && entry.StartVacation < entry.LastDaysSync)
                    final_message += $"\nОтпуск начнётся: {entry.StartVacation.ToLongDateString()}" +
                        $"\nЗакончится: {entry.LastDaysSync.AddDays(-1).ToLongDateString()}";
            }
            Save();
            await botClient.SendTextMessageAsync(message.Chat, final_message);
        }
        private static async void StartVacation(ITelegramBotClient botClient, Message message, UserEntry entry, User sender)
        {
            if (!DateTime.TryParse(message.Text, out DateTime appear))
            {
                await botClient.SendTextMessageAsync(message.Chat, "Правильный ли формат даты? (dd-mm-yyyy)");
                return;
            }
            sender.LastAction[1] = appear.ToString();
            if (entry.ExtraVacation > 0 && entry.MainVacation > 0)
                await botClient.SendTextMessageAsync(message.Chat, "Какие отпускные дни будут использоваться?", replyMarkup: new InlineKeyboardMarkup(
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton("Основные") { CallbackData = "MainV"},
                        new InlineKeyboardButton("Дополнительные") {CallbackData = "ExtraV"}
                    }
                ));
            else
            {
                await botClient.SendTextMessageAsync(message.Chat, "На сколько дней?");
                if (entry.MainVacation > 0)
                    sender.LastAction[0] = "MainV";
                else
                    sender.LastAction[0] = "ExtraV";
            }

        }
        private static async void EndVacation(ITelegramBotClient botClient, Message message, UserEntry ActiveEntry, User sender)
        {
            if (!DateTime.TryParse(message.Text, out DateTime appear))
            {
                await botClient.SendTextMessageAsync(message.Chat, "Правильный ли формат даты? (dd-mm-yyyy)");
                return;
            }
            if (appear < ActiveEntry.StartVacation)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Тогда отпуск ещё даже и не начался");
                return;
            }
            ActiveEntry.MainVacation += (ActiveEntry.LastDaysSync - appear).TotalDays;
            ActiveEntry.LastDaysSync = appear;
            ActiveEntry.StartVacation = appear;
            Save();
            sender.LastAction[0] = "";
            sender.LastAction[2] = "";
            await botClient.SendTextMessageAsync(message.Chat, "Вот и конец отпуску... Хорошо отдыхалось?");
        }
        private static async void MainOrExtraVacation(ITelegramBotClient botClient, Message message, UserEntry ActiveEntry, User sender)
        {
            if (sender == null)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Нельзя использовать старые кнопки для редактирования.");
                return;
            }
            if (!int.TryParse(message.Text, out int d))
            {
                await botClient.SendTextMessageAsync(message.Chat, "Отправь число, пожалуйста");
                return;
            }
            if (d < 1)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Хочешь работать без выходных?");
                return;
            }
            ActiveEntry.UseVacation(d);
            await botClient.SendTextMessageAsync(message.Chat, "Процесс завершён. Твой отпуск продлится до " +
                ActiveEntry.LastDaysSync.AddDays(-1).ToLongDateString());
            sender.LastAction[0] = "";
            sender.LastAction[2] = "";
            Save();
        }
        private static async void AddUser(ITelegramBotClient botClient, Message message, User sender)
        {
            string[] lines = message.Text.Split('\n');
            if (lines.Length != 3)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Пример:\n\n5538431641\nИван Иваныч\n1");
                return;
            }
            if (!long.TryParse(lines[0], out long id))
            {
                await botClient.SendTextMessageAsync(message.Chat, "Мне нужен ID телеграмм аккаунта сотрудника.");
                return;
            }
            if (!int.TryParse(lines[2], out int access) || access > MaxAccess || access < 1)
            {
                string msg = "";
                for (int k = 1; k <= MaxAccess; k++)
                {
                    msg += $"{k}) {acces_name[k - 1]}\n";
                }
                await botClient.SendTextMessageAsync(message.Chat, msg);
                return;
            }
            List<User> clones = User.Users.Where(_ => _.ID == id).ToList();
            if (clones.Count() > 0)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Этот сотрудник уже существует.");
                return;
            }
            User u = new User()
            {
                ID = id,
                UserName = lines[1],
                AccessLevel = access
            };
            User.Users.Add(u);
            Save();
            sender.LastAction[0] = "";
            await botClient.SendTextMessageAsync(message.Chat, $"Сотрудник {lines[1]} успешно добавлен");

        }
        private static async void EditUser(ITelegramBotClient botClient, Message message, User sender)
        {
            if (!int.TryParse(message.Text, out int num) || num > User.Users.Count || num < 1)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Не могу найти сотрудника по указанному номеру... Введи /userlist для просмотра списка всех существующих пользователей.");
                return;
            }
            sender.LastAction[1] = num.ToString();
            User target = User.Users[num - 1];
            string final_message = $"ID: {target.ID}\n" +
                $"Имя: {target.UserName}\n";
            final_message += "\nДоступ: " + acces_name[target.AccessLevel - 1];
            if (target.LastAction[0] != null)
                final_message += "\nПоследние действия: " + target.LastAction.ToString();
            await botClient.SendTextMessageAsync(message.Chat, final_message, replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton[][]
                {
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton("ID") { CallbackData = "EditUserID"},
                        new InlineKeyboardButton("Имя") {CallbackData = "EditUserName"},
                        new InlineKeyboardButton("Доступ") { CallbackData = "EditUserAccess"}
                    },
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton("Очистить последние действия") { CallbackData = "CleanLastAction"}
                    }
                }
            ));
        }
        private static async void DeleteUser(ITelegramBotClient botClient, Message message, User sender)
        {
            if (!int.TryParse(message.Text, out int i) || i < 1 || i > User.Users.Count)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Не могу найти сотрудника по указанному номеру... Проверь список ещё раз.\n\n" + User.GetGlobalUserList());
                return;
            }
            User tmp = User.Users[i - 1];
            List<UserEntry> entries = tmp.GetEntries();
            foreach (UserEntry entry in entries)
            {
                UserEntry.Entries.Remove(entry);
            }
            User.Users.Remove(tmp);
            Save();
            await botClient.SendTextMessageAsync(message.Chat, $"Сотрудник {tmp.UserName} успешно удалён");
            sender.LastAction[0] = "";
        }
        private static async void AddOrg(ITelegramBotClient botClient, Message message)
        {
            Organization org = new Organization()
            {
                ID = Organization.MaxID,
                Name = message.Text.Replace("\n", " ")
            };
            if (Organization.Orgs.Any(_ => _.Name == org.Name))
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Я уже знаю об этой организации!");
                return;
            }
            Organization.Orgs.Add(org);
            Organization.MaxID++;
            if (Organization.MaxID == int.MaxValue)
                Organization.MaxID = int.MinValue;
            await botClient.SendTextMessageAsync(message.Chat, $"Организация \"{org.Name}\" добавлена");
        }
        private static async void DeleteOrg(ITelegramBotClient botClient, Message message)
        {
            if (!int.TryParse(message.Text, out int num) || num >= Organization.Orgs.Count)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Неверный номер организации...\n\n" + Organization.GetGlobalOrgList());
                return;
            }
            Organization temp = Organization.Orgs[num - 1];
            foreach (int eID in temp.Entries)
            {
                UserEntry.Entries.Remove(UserEntry.Entries.Find(_ => _.ID == eID));
            }
            Organization.Orgs.Remove(temp);
            await botClient.SendTextMessageAsync(message.Chat, $"Организация \"{temp.Name}\" удалена...");
        }
        private static async void EditOrg(ITelegramBotClient botClient, Message message, User sender)
        {
            if (!int.TryParse(message.Text, out int num) || num >= Organization.Orgs.Count)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Неверный номер организации... Введи /orglist для просмотра списка всех существующих организаций.");
                return;
            }
            sender.LastAction[1] = num.ToString();
            Organization target = Organization.Orgs[num - 1];
            string final_message = $"ID: {target.ID}\n" +
                $"Название: {target.Name}\n";
            await botClient.SendTextMessageAsync(message.Chat, final_message, replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton[][]
                {
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton("ID") { CallbackData = "EditOrgID"},
                        new InlineKeyboardButton("Название") {CallbackData = "EditOrgName"}
                    },
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton("Добавить сотрудника") { CallbackData = "EditOrgAddEntry"}
                    },
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton("Удалить сотрудника") { CallbackData = "EditOrgRemoveEntry"}
                    }
                }
            ));
        }
        private static async void EditOrgID(ITelegramBotClient botClient, Message message, Organization target_organization)
        {
            if (!int.TryParse(message.Text, out int orgID))
            {
                await botClient.SendTextMessageAsync(message.Chat, "ID должен быть числом");
                return;
            }
            List<Organization> cops = Organization.Orgs.Where(_ => _.ID == orgID).ToList();
            if (cops.Count() > 0)
            {
                string final_message = $"ВНИМАНИЕ! Найдена уже существующая организация с указанным ID. Действуйте на свой страх и риск.";
                if (cops.Count() > 1)
                {
                    final_message += $"\nНет, подождите... Их несколько... Вот полный список:";
                    foreach (Organization o in cops)
                    {
                        final_message += $"\n{o.Name}";
                    }
                }
                await botClient.SendTextMessageAsync(message.Chat, final_message);
            }
            if (orgID >= Organization.MaxID)
                Organization.MaxID = orgID + 1;
            target_organization.ID = orgID;
        }
        private static async void AddEntry(ITelegramBotClient botClient, Message message, Organization target_organization, User sender)
        {
            string[] lines = message.Text.Split('\n');
            if (lines.Length < 3)
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Пример:\n\n1\n{DateTime.Today.ToShortDateString()}\n1");
                return;
            }
            if (!int.TryParse(lines[0], out int id) || id < 1 || id > User.Users.Count)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Порядковый номер сотрудника указан неверно. Для просмотра списка введи /userlist.");
                return;
            }
            if (!DateTime.TryParse(lines[1], out DateTime appear) || appear >= DateTime.Today)
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Неверно указана дата трудойстройства. Пример формата: {DateTime.Today.ToShortDateString()}");
                return;
            }
            if (!int.TryParse(lines[2], out int groupID) || groupID > MaxGroup || groupID < 1)
            {
                string msg = "Неверно указан тип отпускных. Вот список доступных:\n\n";
                for (int k = 1; k <= MaxGroup; k++)
                {
                    msg += $"{k}) {group_name[k - 1]}\n";
                }
                await botClient.SendTextMessageAsync(message.Chat, msg);
                return;
            }
            if (target_organization.GetEntries().Any(_ => _.UserID == sender.ID))
            {
                await botClient.SendTextMessageAsync(message.Chat, "Этот сотрудник уже существует.");
                return;
            }
            UserEntry u = new UserEntry()
            {
                ID = UserEntry.MaxID++,
                OrgID = target_organization.ID,
                UserID = id,
                StartVacation = appear,
                LastDaysSync = appear,
                Group = groupID,
                MainVacation = 0,
                ExtraVacation = 0
            };
            if (lines.Length > 3)
            {
                if (double.TryParse(lines[4], out double main_v))
                    u.MainVacation = main_v;
            }
            if (lines.Length > 4)
            {
                if (double.TryParse(lines[5], out double extra_v))
                    u.MainVacation = extra_v;
            }
            UserEntry.Entries.Add(u);
            sender.Entries.Add(u.ID);
            target_organization.Entries.Add(u.ID);
            Save();
            sender.LastAction[0] = "";
            sender.LastAction[1] = "";
            await botClient.SendTextMessageAsync(message.Chat, $"Учётная запись успешно добавлена.");
        }
        private static async void CallbackRemoveEntry(ITelegramBotClient botClient, Chat chat, User sender)
        {
            Organization target_organization = null;
            if (sender.LastAction[1] != "" && int.TryParse(sender.LastAction[1], out int num))
            {
                if (sender.LastAction[0].StartsWith("EditOrg") && num <= Organization.Orgs.Count)
                {
                    target_organization = Organization.Orgs[num - 1];
                }
            }
            if (target_organization == null)
            {
                await botClient.SendTextMessageAsync(chat, "Не удалось определить редактируемую организацию. Не используйте старые сообщения с кнопками.");
                return;
            }
            await botClient.SendTextMessageAsync(chat, "Введи в следующем сообщении номер учётной записи, которую нужно отредактировать. " + target_organization.GetEntriesList());
        }
        private static async void RemoveEntry(ITelegramBotClient botClient, Message message, Organization org)
        {
            List<UserEntry> entries = org.GetEntries();
            if (!int.TryParse(message.Text, out int id) || id < 1 || id > entries.Count)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Введён неверный порядковый номер учётной записи. " + org.GetEntriesList());
                return;
            }
            id--;
            await botClient.SendTextMessageAsync(message.Chat, $"Учётная запись \"{org.GetEntries()[id].GetUser().UserName} ({org.Name})\" удалена.");
            org.Entries.RemoveAt(id);
        }
        private static async void EditEntry(ITelegramBotClient botClient, Message message, User sender)
        {
            if (!int.TryParse(message.Text, out int num) || num >= UserEntry.Entries.Count)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Неверный номер учётной записи... Введи /entrylist для просмотра списка всех существующих учётных записей.");
                return;
            }
            sender.LastAction[1] = num.ToString();
            UserEntry target = UserEntry.Entries[num - 1];
            string final_message = $"ID: {target.ID}\n" +
                $"Имя: {target.GetUser().UserName}\n" +
                $"Организация: {target.GetOrganization().Name}\n" +
                $"Дата трудоустройства: {target.AppearanceDate.ToShortDateString()}\n" +
                $"Тип отпускных: {group_name[target.Group - 1]}\n" +
                $"Основные отпускные: {target.MainVacation}\n";
            if (target.Group > 1)
                final_message += $"Дополонительные отпускные: {target.ExtraVacation}";
            final_message += "\n\nСтатус: " + (target.LastDaysSync > DateTime.Today ? "В отпуске" : "Работает");
            if (target.StartVacation != target.LastDaysSync)
            {
                final_message += $"Начало отпуска: {target.StartVacation.ToShortDateString()}\n" +
                $"Конец отпуска: {target.LastDaysSync}\n";
            }
            await botClient.SendTextMessageAsync(message.Chat, final_message, replyMarkup: new InlineKeyboardMarkup(
                new InlineKeyboardButton[][]
                {
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton("ID") { CallbackData = "EditEntryID"},
                        new InlineKeyboardButton("Пользователь") {CallbackData = "EditEntryUserID"},
                        new InlineKeyboardButton("Организация") { CallbackData = "EditEntryOrgID"}
                    },
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton("Дата трудоустройства") { CallbackData = "EditEntryAppearance"},
                        new InlineKeyboardButton("Тип отпускных") { CallbackData = "EditEntryGroup"}
                    },
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton("Осн. отпускные") { CallbackData = "EditEntryMainV"},
                        new InlineKeyboardButton("Доп. отпускные") { CallbackData = "EditEntryExtraV"}
                    },
                    new InlineKeyboardButton[]
                    {
                        new InlineKeyboardButton("Начало отпуска") { CallbackData = "EditEntryStartV"},
                        new InlineKeyboardButton("Конец отпуска") { CallbackData = "EditEntryLastSync"}
                    }
                }
            ));
        }
        private static async void EditEntryID(ITelegramBotClient botClient, Message message, UserEntry entry)
        {
            if (!int.TryParse(message.Text, out int entryID))
            {
                await botClient.SendTextMessageAsync(message.Chat, "ID должен быть числом");
                return;
            }
            List<UserEntry> cops = UserEntry.Entries.Where(_ => _.ID == entryID).ToList();
            if (cops.Count() > 0)
            {
                string final_message = $"ВНИМАНИЕ! Найдена уже существующая организация с указанным ID. Действуйте на свой страх и риск.";
                if (cops.Count() > 1)
                {
                    final_message += $"\nНет, подождите... Их несколько... Вот полный список:";
                    foreach (UserEntry o in cops)
                    {
                        final_message += $"\n{o.GetUser().UserName} ({o.GetOrganization().Name})";
                    }
                }
                await botClient.SendTextMessageAsync(message.Chat, final_message);
            }
            if (entryID >= UserEntry.MaxID)
                UserEntry.MaxID = entryID + 1;
            entry.ID = entryID;

        }
        private static async void EditEntryUserID(ITelegramBotClient botClient, Message message, UserEntry entry)
        {

        }
        private static async void RejectCommand(ITelegramBotClient botClient, Message message)
        {
            await botClient.SendTextMessageAsync(message.Chat, "Тебе не дозволено...");
        }

        private static UserEntry ChooseEntry(User sender, List<UserEntry> entries)
        {
            if (entries.Count == 1)
                return entries[0];
            if (!int.TryParse(sender.LastAction[2], out int index) || index > entries.Count)
                return null;
            return entries[index - 1];
        }
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            await System.IO.File.AppendAllTextAsync(LogPath, exception.Message + "\n\n");
            Console.WriteLine(exception.Message);
        }
        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new Telegram.Bot.Extensions.Polling.ReceiverOptions
            {
                AllowedUpdates = new UpdateType[] { UpdateType.Message, UpdateType.CallbackQuery }
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Load();
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);
            Console.ReadLine();
        }
        /// <summary>
        /// Сохраняет данные в файл формата JSON
        /// </summary>
        public static void Save()
        {
            System.IO.File.WriteAllText(SavePath, JsonSerializer.Serialize(User.Users, options));
            if (rng.Next(1, 10) == 1)
                System.IO.File.WriteAllText(BackupPath, JsonSerializer.Serialize(User.Users, options));
        }
        /// <summary>
        /// Загружает данные из файла формата JSON, если он существует
        /// </summary>
        public static void Load()
        {
            try
            {
                User.Users = JsonSerializer.Deserialize<List<User>>(System.IO.File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Console.WriteLine("Файл не найден. Попробую загрузить резерв...");
                System.IO.File.AppendAllText(LogPath, e.Message + "\n\n");
                try
                {
                    User.Users = JsonSerializer.Deserialize<List<User>>(System.IO.File.ReadAllText(BackupPath));
                    Console.WriteLine("Я нашёл резерв и загрузил его. Интересно, что произошло?");
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Не вижу резерв... Видимо, придётся начать жизнь с чистого листа.");
                    System.IO.File.AppendAllText(LogPath, exception.Message + "\n\n");
                }
            }
        }
    }
}