using Microsoft.EntityFrameworkCore;

namespace BCPUtilityDownloadFiles.Models
{
    public class BCPUtilityDBContext: DbContext
    {
        public BCPUtilityDBContext() { }

        public BCPUtilityDBContext(DbContextOptions<BCPUtilityDBContext> options)  : base(options) { }

        //Table
        public DbSet<BCPDocData> SPM_JOB_DETAILS { get; set; }


    }
}
