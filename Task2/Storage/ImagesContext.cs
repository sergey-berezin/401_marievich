using Microsoft.EntityFrameworkCore;

namespace ArcFaceWPF.Storage
{
    public class ImagesContext : DbContext
    {
        public ImagesContext()
        {
            Database.EnsureCreated();
        }

        public DbSet<ImageEntry> Images { get; set; }

        public DbSet<ImageData> Details { get; set; }

        public void Clear()
        {
            Images.RemoveRange(Images);
            Details.RemoveRange(Details);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
        {
            o.UseSqlite("Data Source=images.db");
        }
    }
}
