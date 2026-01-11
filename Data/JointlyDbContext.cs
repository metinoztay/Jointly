using Microsoft.EntityFrameworkCore;

namespace Jointly.Data
{
    public class JointlyDbContext : DbContext
    {
        public JointlyDbContext(DbContextOptions<JointlyDbContext> options) : base(options)
        {
        }
    }
}
