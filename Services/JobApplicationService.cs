using JobApplicationTracker.Data;
using JobApplicationTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Services
{
    public class JobApplicationService
    {
        public List<JobApplication> GetApplications(string searchText, string selectedStatus)
        {
            using var db = new ApplicationDbContext();
            EnsureDatabaseIsReady(db);

            var query = db.JobApplications.AsQueryable();

            searchText = searchText.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(application =>
                    application.CompanyName.ToLower().Contains(searchText) ||
                    application.PositionTitle.ToLower().Contains(searchText) ||
                    application.ContactPerson.ToLower().Contains(searchText) ||
                    application.ContactEmail.ToLower().Contains(searchText) ||
                    application.Notes.ToLower().Contains(searchText));
            }

            if (selectedStatus != "Alle")
            {
                query = query.Where(application => application.Status == selectedStatus);
            }

            return query
                .OrderByDescending(application => application.ApplicationDate)
                .ToList();
        }

        public List<JobApplication> GetAllApplications()
        {
            using var db = new ApplicationDbContext();
            EnsureDatabaseIsReady(db);

            return db.JobApplications.ToList();
        }

        public void AddApplication(JobApplication application)
        {
            using var db = new ApplicationDbContext();
            EnsureDatabaseIsReady(db);

            db.JobApplications.Add(application);
            db.SaveChanges();
        }

        public void UpdateApplication(JobApplication updatedApplication)
        {
            using var db = new ApplicationDbContext();
            EnsureDatabaseIsReady(db);

            var applicationFromDatabase = db.JobApplications
                .FirstOrDefault(application => application.Id == updatedApplication.Id);

            if (applicationFromDatabase is null)
            {
                throw new InvalidOperationException("Die Bewerbung wurde in der Datenbank nicht gefunden.");
            }

            applicationFromDatabase.CompanyName = updatedApplication.CompanyName;
            applicationFromDatabase.PositionTitle = updatedApplication.PositionTitle;
            applicationFromDatabase.ContactPerson = updatedApplication.ContactPerson;
            applicationFromDatabase.ContactEmail = updatedApplication.ContactEmail;
            applicationFromDatabase.ApplicationDate = updatedApplication.ApplicationDate;
            applicationFromDatabase.Status = updatedApplication.Status;
            applicationFromDatabase.Notes = updatedApplication.Notes;
            applicationFromDatabase.AttachmentPath = updatedApplication.AttachmentPath;

            db.SaveChanges();
        }

        public void DeleteApplication(int applicationId)
        {
            using var db = new ApplicationDbContext();
            EnsureDatabaseIsReady(db);

            var applicationFromDatabase = db.JobApplications
                .FirstOrDefault(application => application.Id == applicationId);

            if (applicationFromDatabase is null)
            {
                throw new InvalidOperationException("Die Bewerbung wurde in der Datenbank nicht gefunden.");
            }

            db.JobApplications.Remove(applicationFromDatabase);
            db.SaveChanges();
        }

        private static void EnsureDatabaseIsReady(ApplicationDbContext db)
        {
            db.Database.EnsureCreated();

            try
            {
                db.Database.ExecuteSqlRaw(
                    "ALTER TABLE JobApplications ADD COLUMN AttachmentPath TEXT NOT NULL DEFAULT ''");
            }
            catch
            {
                // Die Spalte existiert bereits.
            }
        }
    }
}