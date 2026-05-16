using TaskTrackerAPI.DTOs;

namespace TaskTrackerAPI.Services
{
    public interface ITaskService
    {
        Task<Models.Task> AddTask(newTaskDTO task);
        Task<bool> DeleteTask(int id);
        Task<List<Models.Task>> GetTasks();
        Task<Models.Task?> UpdateTask(int id, Models.Task task);
    }
}