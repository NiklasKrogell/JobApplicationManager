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
            ThrowIfInvalid();
            AddAutomaticActivities();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ThrowIfInvalid();
            AddAutomaticActivities();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void ThrowIfInvalid()
        {
            var errors = GetValidationErrors();
            if (errors.Count > 0)
                throw new SaveValidationException(errors);
        }

        /// <summary>
        /// Tarkistaa lisätyt ja muutetut rivit ennen tallennusta: hakemuksella pitää olla tehtävä ja yritys,
        /// yrityksellä ja toimialalla nimi. Kaikki tallennukset kulkevat tätä kautta välilehdestä riippumatta.
        /// </summary>
        public IReadOnlyList<string> GetValidationErrors()
        {
            ChangeTracker.DetectChanges();
            var errors = new List<string>();

            foreach (var entry in ChangeTracker.Entries<JobApplication>())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                    continue;

                var application = entry.Entity;
                var name = DescribeApplication(application, entry.State == EntityState.Added);

                if (string.IsNullOrWhiteSpace(application.Position))
                    errors.Add($"Hakemukselta {name} puuttuu tehtävä.");

                if (application.CompanyId is null && application.Company is null && MustHaveCompany(entry))
                    errors.Add($"Hakemukselta {name} puuttuu yritys.");
            }

            foreach (var entry in ChangeTracker.Entries<Company>())
            {
                if (entry.State is (EntityState.Added or EntityState.Modified) && string.IsNullOrWhiteSpace(entry.Entity.Name))
                    errors.Add("Yritykseltä puuttuu nimi.");
            }

            foreach (var entry in ChangeTracker.Entries<Industry>())
            {
                if (entry.State is (EntityState.Added or EntityState.Modified) && string.IsNullOrWhiteSpace(entry.Entity.Name))
                    errors.Add("Toimialalta puuttuu nimi.");
            }

            return errors;
        }

        /// <summary>
        /// Uudella hakemuksella yritys on pakollinen, samoin jos käyttäjä on tyhjentänyt sen. Poikkeus: yritys
        /// poistetaan samassa tallennuksessa (Yritykset-välilehden poisto jättää hakemukset tarkoituksella ilman
        /// yritystä), ja jo aiemmin ilman yritystä jääneitä hakemuksia saa muokata.
        /// </summary>
        private bool MustHaveCompany(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<JobApplication> entry)
        {
            if (entry.State == EntityState.Added)
                return true;

            var companyId = entry.Property(a => a.CompanyId);
            if (!companyId.IsModified || companyId.OriginalValue is not { } originalId)
                return false;

            var companyBeingDeleted = ChangeTracker.Entries<Company>()
                .Any(c => c.State == EntityState.Deleted && c.Entity.Id == originalId);
            return !companyBeingDeleted;
        }

        private static string DescribeApplication(JobApplication application, bool isNew)
        {
            var parts = new[] { application.Position, application.Company?.Name }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim());
            var text = string.Join(" – ", parts);
            return text.Length > 0 ? $"\"{text}\"" : isNew ? "(uusi, tyhjä hakemus)" : "(nimetön hakemus)";
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
