using FileUtilityLib.Core.Interfaces;
using FileUtilityLib.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FileUtilityLib.Core.Services
{
    public class TaskManager : ITaskManager
    {
        private readonly ILogger<TaskManager> _logger;
        private readonly Dictionary<string, FileCopyTask> _tasks;
        private readonly string _configFilePath;

        public TaskManager(ILogger<TaskManager> logger, string? configDirectory = null)
        {
            _logger = logger;
            _tasks = new Dictionary<string, FileCopyTask>();

            var configDir = configDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileUtilityLib");
            Directory.CreateDirectory(configDir);
            _configFilePath = Path.Combine(configDir, "tasks.json");
        }

        public string AddTask(FileCopyTask task)
        {
            if (string.IsNullOrEmpty(task.Id))
            {
                task.Id = Guid.NewGuid().ToString();
            }

            task.CreatedAt = DateTime.Now;
            _tasks[task.Id] = task;

            _logger.LogInformation("Tarea agregada: {TaskName} (ID: {TaskId})", task.Name, task.Id);

            return task.Id;
        }

        public bool UpdateTask(FileCopyTask task)
        {
            if (string.IsNullOrEmpty(task.Id) || !_tasks.ContainsKey(task.Id))
            {
                return false;
            }

            _tasks[task.Id] = task;

            _logger.LogInformation("Tarea actualizada: {TaskName} (ID: {TaskId})", task.Name, task.Id);

            return true;
        }

        public bool RemoveTask(string taskId)
        {
            if (_tasks.Remove(taskId))
            {
                _logger.LogInformation("Tarea eliminada: {TaskId}", taskId);
                return true;
            }

            return false;
        }

        public FileCopyTask? GetTask(string taskId)
        {
            _tasks.TryGetValue(taskId, out var task);
            return task;
        }

        public List<FileCopyTask> GetAllTasks()
        {
            return _tasks.Values.ToList();
        }

        public List<FileCopyTask> GetEnabledTasks()
        {
            return _tasks.Values.Where(t => t.IsEnabled).ToList();
        }

        public async Task SaveTasksAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_tasks.Values, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_configFilePath, json);

                _logger.LogDebug("Tareas guardadas en: {ConfigFile}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando tareas en: {ConfigFile}", _configFilePath);
                throw;
            }
        }

        public async Task LoadTasksAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogDebug("Archivo de configuración no existe: {ConfigFile}", _configFilePath);
                    return;
                }

                var json = await File.ReadAllTextAsync(_configFilePath);
                var tasks = JsonSerializer.Deserialize<List<FileCopyTask>>(json);

                _tasks.Clear();

                if (tasks != null)
                {
                    foreach (var task in tasks)
                    {
                        _tasks[task.Id] = task;
                    }

                    _logger.LogInformation("Cargadas {TaskCount} tareas desde: {ConfigFile}", tasks.Count, _configFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando tareas desde: {ConfigFile}", _configFilePath);
                throw;
            }
        }
    }
}
