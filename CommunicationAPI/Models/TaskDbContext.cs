using Microsoft.EntityFrameworkCore;

namespace CommunicationAPI.Models
{
    public class TaskDbContext: DbContext
    {
        public DbSet<Task> Tasks { get; set; }

        public TaskDbContext(DbContextOptions<TaskDbContext> options):base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            var model = modelBuilder.Entity<Task>();
            model.Property(x => x.id).UseIdentityColumn();

            base.OnModelCreating(modelBuilder);
        }
    }
}
