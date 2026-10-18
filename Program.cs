using System.Text.Json;

var tasks = TaskStore.Load();
var selected = 0;
var menuItems = new[] { "Управление задачами", "Приоритеты и сроки", "Поиск и фильтрация", "Выход" };

while (true)
{
    Console.Clear();
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║               TASKMASTER                 ║");
    Console.WriteLine("║       Система управления задачами        ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");
    Console.WriteLine("Выберите раздел (↑/↓, Enter):");
    Console.WriteLine("┌──────────────────────────────────────────┐");
    for (var i = 0; i < menuItems.Length; i++)
        Console.WriteLine($"│ {(i == selected ? "›" : " ")} {menuItems[i],-38}│");
    Console.WriteLine("└──────────────────────────────────────────┘");

    var key = Console.ReadKey(intercept: true).Key;
    if (key == ConsoleKey.UpArrow) selected = (selected + menuItems.Length - 1) % menuItems.Length;
    else if (key == ConsoleKey.DownArrow) selected = (selected + 1) % menuItems.Length;
    else if (key == ConsoleKey.Enter)
    {
        switch (selected)
        {
            case 0: TaskMenu.Run(tasks, TaskStore.Save); break;
            case 1: PriorityMenu.Run(tasks, TaskStore.Save); break;
            case 2: SearchMenu.Run(tasks); break;
            case 3: return;
        }
    }
}

static class TaskStore
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskMaster");
    private static readonly string DataFile = Path.Combine(DataDirectory, "tasks.json");

    public static List<TaskItem> Load()
    {
        try
        {
            if (File.Exists(DataFile))
                return JsonSerializer.Deserialize<List<TaskItem>>(File.ReadAllText(DataFile)) ?? [];
        }
        catch (JsonException) { }
        return [];
    }

    public static void Save(List<TaskItem> tasks)
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(DataFile, JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true }));
    }
}

static class TaskMenu
{
    public static void Run(List<TaskItem> tasks, Action<List<TaskItem>> save)
    {
        Console.Clear();
        Console.WriteLine("Управление задачами");
        Console.WriteLine("1. Добавить задачу");
        Console.WriteLine("2. Показать задачи");
        Console.WriteLine("3. Отметить задачу выполненной");
        Console.WriteLine("0. Назад");
        Console.Write("Выберите действие: ");
        switch (Console.ReadLine())
        {
            case "1":
                Console.Write("Название: ");
                var title = Console.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    Console.Write("Описание (необязательно): ");
                    var description = Console.ReadLine()?.Trim() ?? "";
                    tasks.Add(new TaskItem { Title = title, Description = description, CreatedAt = DateTime.Now, Status = "Новая", Priority = "Средний" });
                    save(tasks);
                    Console.WriteLine("Задача добавлена.");
                }
                else Console.WriteLine("Название не должно быть пустым.");
                break;
            case "2": Show(tasks); break;
            case "3":
                Show(tasks);
                Console.Write("Введите номер задачи: ");
                if (int.TryParse(Console.ReadLine(), out var number) && number > 0 && number <= tasks.Count)
                {
                    tasks[number - 1].Status = "Выполнена";
                    save(tasks);
                    Console.WriteLine("Статус задачи обновлён.");
                }
                else Console.WriteLine("Задача с таким номером не найдена.");
                break;
        }
        Pause();
    }

    internal static void Show(List<TaskItem> tasks)
    {
        if (tasks.Count == 0) { Console.WriteLine("Список задач пуст."); return; }
        for (var i = 0; i < tasks.Count; i++)
        {
            Console.WriteLine($"{i + 1}. [{tasks[i].Status}] {tasks[i].Title} — приоритет: {tasks[i].Priority}; срок: {tasks[i].DueDate?.ToString("dd.MM.yyyy") ?? "не задан"}");
            if (!string.IsNullOrWhiteSpace(tasks[i].Description)) Console.WriteLine($"   {tasks[i].Description}");
        }
    }

    internal static void Pause() { Console.WriteLine("\nНажмите Enter, чтобы продолжить..."); Console.ReadLine(); }
}

static class PriorityMenu
{
    public static void Run(List<TaskItem> tasks, Action<List<TaskItem>> save)
    {
        Console.Clear();
        Console.WriteLine("Приоритеты и сроки задач");
        TaskMenu.Show(tasks);
        if (tasks.Count == 0) { TaskMenu.Pause(); return; }
        Console.Write("Номер задачи для изменения (0 — назад): ");
        if (!int.TryParse(Console.ReadLine(), out var number) || number < 1 || number > tasks.Count)
        {
            TaskMenu.Pause();
            return;
        }

        var task = tasks[number - 1];
        Console.WriteLine("Приоритет: 1 — низкий, 2 — средний, 3 — высокий");
        Console.Write("Выберите приоритет: ");
        task.Priority = Console.ReadLine() switch
        {
            "1" => "Низкий",
            "3" => "Высокий",
            _ => "Средний"
        };
        Console.Write("Срок выполнения (дд.мм.гггг, Enter — без изменений): ");
        var dateText = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(dateText))
        {
            if (DateTime.TryParseExact(dateText, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var dueDate))
                task.DueDate = dueDate;
            else
                Console.WriteLine("Дата введена неверно; сохранён прежний срок.");
        }
        save(tasks);
        Console.WriteLine("Приоритет и срок сохранены.");
        TaskMenu.Pause();
    }
}

static class SearchMenu
{
    public static void Run(List<TaskItem> tasks)
    {
        Console.Clear();
        Console.WriteLine("Поиск и фильтрация задач");
        Console.Write("Фрагмент названия или описания (Enter — любой текст): ");
        var query = Console.ReadLine()?.Trim() ?? "";
        Console.WriteLine("Статус: 0 — любой, 1 — новая, 2 — выполнена");
        Console.Write("Выберите статус: ");
        var statusChoice = Console.ReadLine();
        Console.WriteLine("Приоритет: 0 — любой, 1 — низкий, 2 — средний, 3 — высокий");
        Console.Write("Выберите приоритет: ");
        var priorityChoice = Console.ReadLine();

        IEnumerable<TaskItem> results = tasks;
        if (!string.IsNullOrWhiteSpace(query))
            results = results.Where(task => task.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || task.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        if (statusChoice == "1") results = results.Where(task => task.Status == "Новая");
        if (statusChoice == "2") results = results.Where(task => task.Status == "Выполнена");
        var priority = priorityChoice switch { "1" => "Низкий", "2" => "Средний", "3" => "Высокий", _ => null };
        if (priority is not null) results = results.Where(task => task.Priority == priority);

        var found = results.ToList();
        Console.WriteLine($"\nНайдено задач: {found.Count}");
        TaskMenu.Show(found);
        TaskMenu.Pause();
    }
}

sealed class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "Новая";
    public string Priority { get; set; } = "Средний";
}
