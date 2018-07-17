using Microsoft.EntityFrameworkCore;

namespace Skills.Models
{
    public class SkillsContext: DbContext
    {
        public DbSet<VersionModel> VersionsApplied {get; set;}
        

        public DbSet<NodeModel> Nodes {get; set;}

        protected override void OnConfiguring (DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=skills.db");
        }
    }
}