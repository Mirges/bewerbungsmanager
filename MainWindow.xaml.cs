using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using JobApplicationTracker.Data;
using JobApplicationTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<JobApplication> _applications = new();

        public MainWindow()
        {
            InitializeComponent();

            ApplicationsDataGrid.ItemsSource = _applications;

            InitializeDatabase();
            LoadApplications();
        }

        private void InitializeDatabase()
        {
            using var db = new ApplicationDbContext();
            db.Database.EnsureCreated();
        }

        private void LoadApplications()
        {
            _applications.Clear();

            using var db = new ApplicationDbContext();

            var applicationsFromDatabase = db.JobApplications
                .OrderByDescending(application => application.ApplicationDate)
                .ToList();

            foreach (var application in applicationsFromDatabase)
            {
                _applications.Add(application);
            }
        }

        private void AddApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            string companyName = CompanyTextBox.Text.Trim();
            string positionTitle = PositionTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(positionTitle))
            {
                MessageBox.Show("Bitte Unternehmen und Position angeben.", "Fehlende Informationen", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedStatus = "Beworben";

            if (StatusComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content is string status)
            {
                selectedStatus = status;
            }

            var application = new JobApplication
            {
                CompanyName = companyName,
                PositionTitle = positionTitle,
                ContactPerson = ContactPersonTextBox.Text.Trim(),
                ContactEmail = ContactEmailTextBox.Text.Trim(),
                ApplicationDate = DateTime.Today,
                Status = selectedStatus,
                Notes = NotesTextBox.Text.Trim()
            };

            using var db = new ApplicationDbContext();
            db.JobApplications.Add(application);
            db.SaveChanges();

            _applications.Add(application);

            ClearForm();
        }

        private void DeleteApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationsDataGrid.SelectedItem is not JobApplication selectedApplication)
            {
                MessageBox.Show(
                    "Bitte zuerst eine Bewerbung in der Tabelle auswählen.",
                    "Keine Bewerbung ausgewählt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var result = MessageBox.Show(
                $"Möchtest du die Bewerbung bei '{selectedApplication.CompanyName}' wirklich löschen?",
                "Bewerbung löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            using var db = new ApplicationDbContext();

            var applicationFromDatabase = db.JobApplications
                .FirstOrDefault(application => application.Id == selectedApplication.Id);

            if (applicationFromDatabase is null)
            {
                MessageBox.Show(
                    "Die Bewerbung wurde in der Datenbank nicht gefunden.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            db.JobApplications.Remove(applicationFromDatabase);
            db.SaveChanges();

            _applications.Remove(selectedApplication);
        }

        private void ClearForm()
        {
            CompanyTextBox.Clear();
            PositionTextBox.Clear();
            ContactPersonTextBox.Clear();
            ContactEmailTextBox.Clear();
            NotesTextBox.Clear();
            StatusComboBox.SelectedIndex = 0;
        }
    }
}