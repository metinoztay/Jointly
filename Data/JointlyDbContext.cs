using Microsoft.EntityFrameworkCore;
using Jointly.Models;

namespace Jointly.Data
{
    public class JointlyDbContext : DbContext
    {
        public JointlyDbContext(DbContextOptions<JointlyDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
    }
}
