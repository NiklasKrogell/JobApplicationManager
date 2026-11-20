using JobApplicationManager.Helpers;
using JobApplicationManager.Model;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace JobApplicationManager.Context
{
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Tietokanta sijaitsee käyttäjän LocalAppData-kansiossa, jotta data säilyy
        /// vaikka bin/obj-kansiot siivottaisiin tai sovellus buildattaisiin uudelleen.
        /// Ympäristömuuttujalla JOBAPPMANAGER_DB_PATH voi käyttää toista kantaa (esim. testaukseen).
        /// </summary>
        public static string DatabasePath { get; } =
            Environment.GetEnvironmentVariable("JOBAPPMANAGER_DB_PATH") is { Length: > 0 } customPath
                ? customPath
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "JobApplicationManager",
                    "job_applications.db");

        public DbSet<Company> Companies { get; set; }
        public DbSet<Industry> Industries { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<Activity> Activities { get; set; }

        public AppDbContext()
        {
        }

        /// <summary>
        /// Testeille: mahdollistaa erillisen (esim. väliaikaisen) kannan käytön.
        /// </summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
                optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
            }
            optionsBuilder.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Enumit tallennetaan nimenä ("Interviewing"), ei numerona, jotta
            // enumin järjestystä voi muuttaa rikkomatta olemassa olevaa dataa.
            modelBuilder.Entity<JobApplication>()
                .Property(a => a.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Activity>(activity =>
            {
                activity.Property(a => a.Type).HasConversion<string>();
                activity.Property(a => a.NewStatus).HasConversion<string>();

                activity.HasOne(a => a.JobApplication)
                    .WithMany(j => j.Activities)
                    .HasForeignKey(a => a.JobApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AddAutomaticActivities();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AddAutomaticActivities();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// Kirjaa tapahtumalokiin automaattisesti uuden hakemuksen luonnin ja jokaisen tilamuutoksen.
        /// </summary>
        private void AddAutomaticActivities()
        {
            ChangeTracker.DetectChanges();
            var now = DateTime.Now;

            // ToList(): lokirivien lisääminen muuttaa ChangeTrackeria kesken läpikäynnin.
            foreach (var entry in ChangeTracker.Entries<JobApplication>().ToList())
            {
                var application = entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    Activities.Add(new Activity
                    {
                        JobApplication = application,
                        Date = now,
                        Type = ActivityType.StatusChange,
                        NewStatus = application.Status,
                        Description = $"Hakemus luotu (tila: {application.Status.GetDescription()})"
                    });
                }
                else if (entry.State == EntityState.Modified)
                {
                    var status = entry.Property(a => a.Status);
                    if (status.IsModified && status.OriginalValue != status.CurrentValue)
                    {
                        Activities.Add(new Activity
                        {
                            JobApplication = application,
                            Date = now,
                            Type = ActivityType.StatusChange,
                            NewStatus = status.CurrentValue,
                            Description = $"{status.OriginalValue.GetDescription()} → {status.CurrentValue.GetDescription()}"
                        });
                    }
                }
            }
        }
    }
}
