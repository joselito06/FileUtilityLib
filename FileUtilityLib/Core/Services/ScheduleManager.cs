using FileUtilityLib.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileUtilityLib.Core.Services
{
    public class ScheduleManager
    {
        //private readonly ILogger<ScheduleManager> _logger;
        private readonly ILogger _logger;
        private readonly Dictionary<string, ScheduleConfiguration> _schedules;
        private readonly string _configFilePath;

        public ScheduleManager(ILogger logger, string? configDirectory = null)
        {
            _logger = logger;
            _schedules = new Dictionary<string, ScheduleConfiguration>();

            var configDir = configDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileUtilityLib");
            Directory.CreateDirectory(configDir);
            _configFilePath = Path.Combine(configDir, "schedules.json");
        }

        // ✅ CONSTRUCTOR NUEVO - SIN LOGGER ESPECÍFICO
        public ScheduleManager(string? configDirectory = null)
            : this(NullLogger.Instance, configDirectory)
        {
        }

        public void AddOrUpdateSchedule(ScheduleConfiguration schedule)
        {
            _schedules[schedule.TaskId] = schedule;
            _logger.LogInformation("Programa actualizado para tarea: {TaskId}", schedule.TaskId);
        }

        public bool RemoveSchedule(string taskId)
        {
            if (_schedules.Remove(taskId))
            {
                _logger.LogInformation("Programa eliminado para tarea: {TaskId}", taskId);
                return true;
            }
            return false;
        }

        public ScheduleConfiguration? GetSchedule(string taskId)
        {
            _schedules.TryGetValue(taskId, out var schedule);
            return schedule;
        }

        public List<ScheduleConfiguration> GetAllSchedules()
        {
            return _schedules.Values.ToList();
        }

        public List<ScheduleConfiguration> GetEnabledSchedules()
        {
            return _schedules.Values.Where(s => s.IsEnabled).ToList();
        }

        public async Task SaveSchedulesAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_schedules.Values, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_configFilePath, json);

                _logger.LogDebug("Programas guardados en: {ConfigFile}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando programas en: {ConfigFile}", _configFilePath);
                throw;
            }
        }

        public async Task LoadSchedulesAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogDebug("Archivo de programas no existe: {ConfigFile}", _configFilePath);
                    return;
                }

                var json = await File.ReadAllTextAsync(_configFilePath);
                var schedules = JsonSerializer.Deserialize<List<ScheduleConfiguration>>(json);

                _schedules.Clear();

                if (schedules != null)
                {
                    foreach (var schedule in schedules)
                    {
                        _schedules[schedule.TaskId] = schedule;
                    }

                    _logger.LogInformation("Cargados {ScheduleCount} programas desde: {ConfigFile}", schedules.Count, _configFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando programas desde: {ConfigFile}", _configFilePath);
                throw;
            }
        }

        public List<DateTime> GetNextExecutionTimes(string taskId, int count = 5)
        {
            var schedule = GetSchedule(taskId);
            if (schedule == null || !schedule.IsEnabled)
                return new List<DateTime>();

            var times = new List<DateTime>();
            var currentDate = DateTime.Today;
            var maxDate = currentDate.AddDays(365); // Buscar hasta un año adelante

            while (times.Count < count && currentDate <= maxDate)
            {
                if (IsExecutionDay(currentDate, schedule))
                {
                    foreach (var time in schedule.ExecutionTimes)
                    {
                        var executionTime = currentDate.Add(time);

                        // Solo incluir tiempos futuros
                        if (executionTime > DateTime.Now)
                        {
                            if (schedule.StartDate.HasValue && executionTime < schedule.StartDate.Value)
                                continue;

                            if (schedule.EndDate.HasValue && executionTime > schedule.EndDate.Value)
                                continue;

                            times.Add(executionTime);

                            if (times.Count >= count)
                                break;
                        }
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            return times.OrderBy(t => t).Take(count).ToList();
        }

        private static bool IsExecutionDay(DateTime date, ScheduleConfiguration schedule)
        {
            return schedule.Type switch
            {
                ScheduleType.Daily => true,
                ScheduleType.Weekly => schedule.DaysOfWeek.Contains(date.DayOfWeek),
                ScheduleType.Monthly => date.Day == 1, // Primer día del mes, puedes personalizar
                ScheduleType.Interval => true,
                _ => false
            };
        }
    }
}
