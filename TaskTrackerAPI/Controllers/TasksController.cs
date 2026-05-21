using Confluent.Kafka;

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
        public IProducer<string, string> KafkaProducer { get; set; }

        public TasksController(ITaskService taskService, IProducer<string, string> kafkaProducer)
        {
            TaskService = taskService;
            KafkaProducer = kafkaProducer;
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

            try
            {
                // 2. Await the actual cluster transmission confirmation
                var deliveryResult = await KafkaProducer.ProduceAsync("task-events", new Message<string, string>
                {
                    Key = addedTask.id.ToString(), // Better practice to pass the entity ID as the partition key
                    Value = $"Task with ID {addedTask.id} added: {addedTask.title}"
                });

                // Optional log line to verify port destination tracking via VS Code terminal console
                Console.WriteLine($"[Kafka] Message successfully delivered to topic {deliveryResult.Topic} on partition {deliveryResult.Partition.Value}");
            }
            catch (ProduceException<string, string> ex)
            {
                // Captures connectivity snags, timeouts, or partition routing problems
                Console.WriteLine($"[Kafka Error] Failed to write to broker: {ex.Error.Reason}");
            }
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
