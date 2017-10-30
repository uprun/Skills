using Microsoft.EntityFrameworkCore;

namespace Skills.Models
{
    public class SkillsContext: DbContext
    {
        public DbSet<SkillModel> Skills {get; set;}

        protected override void OnConfiguring (DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=skills.db");
        }
    }
}