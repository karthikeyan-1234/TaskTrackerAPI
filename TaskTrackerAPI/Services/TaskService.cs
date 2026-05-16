using Microsoft.EntityFrameworkCore;

using TaskTrackerAPI.DTOs;
using TaskTrackerAPI.Models;

namespace TaskTrackerAPI.Services
{
    public class TaskService : ITaskService
    {
        TaskDbContext context { get; set; }

        public TaskService(TaskDbContext context)
        {
            this.context = context;
        }

        public async Task<List<Models.Task>> GetTasks()
        {
            return await context.Tasks.ToListAsync();
        }

        public async Task<Models.Task> AddTask(newTaskDTO task)
        {
            var newTask = new Models.Task()
            {
                title = task.title,
                description = task.description,
                completed = task.completed
            };
            var addedTask = context.Tasks.Add(newTask);
            await context.SaveChangesAsync();
            return addedTask.Entity;
        }

        public async Task<Models.Task?> UpdateTask(int id, Models.Task task)
        {
            var existingTask = await context.Tasks.FindAsync(id);
            if (existingTask == null)
            {
                return null;
            }
            existingTask.title = task.title;
            existingTask.description = task.description;
            existingTask.completed = task.completed;
            await context.SaveChangesAsync();
            return existingTask;
        }

        public async Task<bool> DeleteTask(int id)
        {
            var existingTask = await context.Tasks.FindAsync(id);
            if (existingTask == null)
            {
                return false;
            }
            context.Tasks.Remove(existingTask);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
