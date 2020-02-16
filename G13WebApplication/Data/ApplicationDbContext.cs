using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using G13WebApplication.Models;

namespace G13WebApplication.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<G13WebApplication.Models.Notification> Notification { get; set; }
        public DbSet<G13WebApplication.Models.Student> Student { get; set; }
        public DbSet<G13WebApplication.Models.Teacher> Teacher { get; set; }
        public DbSet<G13WebApplication.Models.User> User { get; set; }

        public DbSet<G13WebApplication.Models.UserGuide> UserGuide { get; set; }

        public DbSet<G13WebApplication.Models.Students_Teachers> Students_Teachers { get; set; }

        public static string ConnectionString { get; set; }

        public DbSet<G13WebApplication.Models.WorkPlan> WorkPlan { get; set; }

        public DbSet<G13WebApplication.Models.Tfc> Tfc { get; set; }

        public DbSet<G13WebApplication.Models.Ata> Ata { get; set; }

        public DbSet<G13WebApplication.Models.ProposalDO> ProposalDO { get; set; }

        public DbSet<G13WebApplication.Models.TfcProposal> TfcProposal { get; set; }

        public DbSet<G13WebApplication.Models.Activity> Activity { get; set; }

        public DbSet<G13WebApplication.Models.Activity_Participant> Activity_Participant { get; set; }

        public DbSet<G13WebApplication.Models.TO> TO { get; set; }

        public DbSet<G13WebApplication.Models.Activity_Document> Activity_Document { get; set; }

        public DbSet<G13WebApplication.Models.Activity_Suggested_Date> Activity_Suggested_Date { get; set; }
    }
}
