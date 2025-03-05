using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Добро пожаловать в Task Manager!");
        await Authentication.AuthenticateUser();
    }
}

class Authentication
{
    private static string usersFile = "users.txt";
    public static string CurrentUser { get; private set; }

    public static async Task AuthenticateUser()
    {
        Console.WriteLine("1. Вход\n2. Регистрация");
        string choice = Console.ReadLine();
        if (choice == "1")
            await Login();
        else if (choice == "2")
            await Register();
        else
            Console.WriteLine("Неверный ввод.");
    }

    private static async Task Register()
    {
        Console.Write("Введите имя пользователя: ");
        string username = Console.ReadLine();
        Console.Write("Введите пароль: ");
        string password = Console.ReadLine();
        var users = await ReadUsers();
        if (users.ContainsKey(username))
        {
            Console.WriteLine("Пользователь уже существует.");
            return;
        }
        users[username] = password;
        await SaveUsers(users);
        Console.WriteLine("Регистрация успешна!");
        CurrentUser = username;
        await TaskManager.ShowMenu();
    }

    private static async Task Login()
    {
        Console.Write("Введите имя пользователя: ");
        string username = Console.ReadLine();
        Console.Write("Введите пароль: ");
        string password = Console.ReadLine();
        var users = await ReadUsers();
        if (users.TryGetValue(username, out string storedPassword) && storedPassword == password)
        {
            Console.WriteLine("Вход выполнен!");
            CurrentUser = username;
            await TaskManager.ShowMenu();
        }
        else
            Console.WriteLine("Неверные данные!");
    }

    private static async Task<Dictionary<string, string>> ReadUsers()
    {
        if (!File.Exists(usersFile)) return new Dictionary<string, string>();
        string json = await File.ReadAllTextAsync(usersFile);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }

    private static async Task SaveUsers(Dictionary<string, string> users)
    {
        string json = JsonSerializer.Serialize(users, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true 
        });
        await File.WriteAllTextAsync(usersFile, json);
    }

}

class TaskManager
{
    private static string tasksFile = "tasks.txt";

    public static async Task ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("\nМеню:");
            Console.WriteLine("1. Добавить задачу");
            Console.WriteLine("2. Просмотреть задачи");
            Console.WriteLine("3. Редактировать задачу");
            Console.WriteLine("4. Удалить задачу");
            Console.WriteLine("5. Изменить статус задачи"); 
            Console.WriteLine("6. Выход");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1": await AddTask(); break;
                case "2": await ViewTasks(); break;
                case "3": await EditTask(); break;
                case "4": await DeleteTask(); break;
                case "5": await UpdateTaskStatus(); break; 
                case "6": return;
                default: Console.WriteLine("Неверный ввод."); break;
            }
        }
    }

    private static async Task UpdateTaskStatus()
    {
        await ViewTasks(); 
        Console.Write("Введите заголовок задачи, статус которой хотите изменить: ");
        string title = Console.ReadLine();

        var tasks = await ReadTasks();
        var task = tasks.FirstOrDefault(t => t.Title == title && t.User == Authentication.CurrentUser);

        if (task == null)
        {
            Console.WriteLine("Задача не найдена.");
            return;
        }

        Console.WriteLine("Выберите новый статус:");
        Console.WriteLine("1. Недоступна");
        Console.WriteLine("2. В процессе");
        Console.WriteLine("3. Завершена");

        string statusChoice = Console.ReadLine();
        switch (statusChoice)
        {
            case "1": task.Status = "Недоступна"; break;
            case "2": task.Status = "В процессе"; break;
            case "3": task.Status = "Завершена"; break;
            default: Console.WriteLine("Неверный выбор."); return;
        }

        await SaveTasks(tasks);
        Console.WriteLine($"Статус задачи \"{task.Title}\" обновлен до \"{task.Status}\".");
    }

    private static async Task<List<TaskItem>> ReadTasks()
    {
        if (!File.Exists(tasksFile)) return new List<TaskItem>();
        string json = await File.ReadAllTextAsync(tasksFile);
        return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
    }

    private static async Task SaveTasks(List<TaskItem> tasks)
    {
        string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        });
        await File.WriteAllTextAsync(tasksFile, json);
    }

    private static async Task AddTask()
    {
        Console.Write("Введите заголовок: ");
        string title = Console.ReadLine();
        Console.Write("Введите описание: ");
        string description = Console.ReadLine();
        Console.Write("Приоритет (низкий, средний, высокий): ");
        string priority = Console.ReadLine();

        var tasks = await ReadTasks();
        tasks.Add(new TaskItem(title, description, priority, Authentication.CurrentUser));
        await SaveTasks(tasks);
        Console.WriteLine("Задача добавлена.");
    }

    private static async Task ViewTasks()
    {
        var tasks = await ReadTasks();
        var userTasks = tasks.Where(t => t.User == Authentication.CurrentUser).ToList();
        if (!userTasks.Any()) Console.WriteLine("Нет задач.");
        else userTasks.ForEach(t => Console.WriteLine(t));
    }

    private static async Task EditTask()
    {
        await ViewTasks();
        Console.Write("Введите заголовок задачи для редактирования: ");
        string title = Console.ReadLine();
        var tasks = await ReadTasks();
        var task = tasks.FirstOrDefault(t => t.Title == title && t.User == Authentication.CurrentUser);
        if (task == null) { Console.WriteLine("Задача не найдена."); return; }

        Console.Write("Новое описание: ");
        task.Description = Console.ReadLine();
        Console.Write("Новый приоритет (низкий, средний, высокий): ");
        task.Priority = Console.ReadLine();
        await SaveTasks(tasks);
        Console.WriteLine("Задача обновлена.");
    }

    private static async Task DeleteTask()
    {
        await ViewTasks();
        Console.Write("Введите заголовок задачи для удаления: ");
        string title = Console.ReadLine();
        var tasks = await ReadTasks();
        tasks.RemoveAll(t => t.Title == title && t.User == Authentication.CurrentUser);
        await SaveTasks(tasks);
        Console.WriteLine("Задача удалена.");
    }
}

class TaskItem
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; } = "В процессе";
    public string User { get; set; }

    public TaskItem(string title, string description, string priority, string user)
    {
        Title = title;
        Description = description;
        Priority = priority;
        User = user;
    }

    public override string ToString()
    {
        return $"[{Status}] {Title} - {Priority} (Автор: {User})\n{Description}";
    }
}
