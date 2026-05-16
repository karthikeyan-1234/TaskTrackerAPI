using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using TaskTrackerAPI.Services;

namespace TaskTrackerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        public ITaskService TaskService { get; set; }

        public TasksController(ITaskService taskService)
        {
            TaskService = taskService;
        }

        [HttpGet("GetAllTasks")]
        public async Task<ActionResult<List<Models.Task>>> GetAllTasks()
        {
            var tasks = await TaskService.GetTasks();
            return Ok(tasks);
        }

        [HttpPost("AddTask")]
        public async Task<ActionResult<Models.Task>> AddTask(DTOs.newTaskDTO task)
        {
            var addedTask = await TaskService.AddTask(task);
            return Ok(addedTask);
        }

        [HttpPut("UpdateTask/{id}")]
        public async Task<ActionResult<Models.Task>> UpdateTask(int id, Models.Task task)
        {
            var updatedTask = await TaskService.UpdateTask(id, task);
            if (updatedTask == null)
            {
                return NotFound();
            }
            return Ok(updatedTask);
        }

        [HttpDelete("DeleteTask/{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            var deleted = await TaskService.DeleteTask(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
