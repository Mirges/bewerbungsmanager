using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using JobApplicationTracker.Data;
using JobApplicationTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows.Input;

namespace JobApplicationTracker
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<JobApplication> _applications = new();

        private JobApplication? _selectedApplication;

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
                ApplicationDate = ApplicationDatePicker.SelectedDate ?? DateTime.Today,
                Status = selectedStatus,
                Notes = NotesTextBox.Text.Trim()
            };

            using var db = new ApplicationDbContext();
            db.JobApplications.Add(application);
            db.SaveChanges();

            _applications.Add(application);

            ClearForm();
            _selectedApplication = null;
            ApplicationsDataGrid.SelectedItem = null;
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

        private void ApplicationsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ApplicationsDataGrid.SelectedItem is not JobApplication selectedApplication)
            {
                return;
            }

            _selectedApplication = selectedApplication;

            CompanyTextBox.Text = selectedApplication.CompanyName;
            PositionTextBox.Text = selectedApplication.PositionTitle;
            ContactPersonTextBox.Text = selectedApplication.ContactPerson;
            ContactEmailTextBox.Text = selectedApplication.ContactEmail;
            ApplicationDatePicker.SelectedDate = selectedApplication.ApplicationDate;
            NotesTextBox.Text = selectedApplication.Notes;

            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Content is string status && status == selectedApplication.Status)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void UpdateApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedApplication is null)
            {
                MessageBox.Show(
                    "Bitte zuerst eine Bewerbung in der Tabelle auswählen.",
                    "Keine Bewerbung ausgewählt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            string companyName = CompanyTextBox.Text.Trim();
            string positionTitle = PositionTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(positionTitle))
            {
                MessageBox.Show(
                    "Bitte Unternehmen und Position angeben.",
                    "Fehlende Informationen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string selectedStatus = "Beworben";

            if (StatusComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content is string status)
            {
                selectedStatus = status;
            }

            using var db = new ApplicationDbContext();

            var applicationFromDatabase = db.JobApplications
                .FirstOrDefault(application => application.Id == _selectedApplication.Id);

            if (applicationFromDatabase is null)
            {
                MessageBox.Show(
                    "Die Bewerbung wurde in der Datenbank nicht gefunden.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            applicationFromDatabase.CompanyName = companyName;
            applicationFromDatabase.PositionTitle = positionTitle;
            applicationFromDatabase.ContactPerson = ContactPersonTextBox.Text.Trim();
            applicationFromDatabase.ContactEmail = ContactEmailTextBox.Text.Trim();
            applicationFromDatabase.ApplicationDate = ApplicationDatePicker.SelectedDate ?? DateTime.Today;
            applicationFromDatabase.Status = selectedStatus;
            applicationFromDatabase.Notes = NotesTextBox.Text.Trim();

            db.SaveChanges();

            _selectedApplication.CompanyName = applicationFromDatabase.CompanyName;
            _selectedApplication.PositionTitle = applicationFromDatabase.PositionTitle;
            _selectedApplication.ContactPerson = applicationFromDatabase.ContactPerson;
            _selectedApplication.ContactEmail = applicationFromDatabase.ContactEmail;
            _selectedApplication.ApplicationDate = applicationFromDatabase.ApplicationDate;
            _selectedApplication.Status = applicationFromDatabase.Status;
            _selectedApplication.Notes = applicationFromDatabase.Notes;

            ApplicationsDataGrid.Items.Refresh();

            ClearForm();
            _selectedApplication = null;
            ApplicationsDataGrid.SelectedItem = null;
        }


        private void ResetFormSelection()
        {
            ClearForm();
            _selectedApplication = null;
            ApplicationsDataGrid.SelectedItem = null;
        }

        private void ClearFormButton_Click(object sender, RoutedEventArgs e)
        {
            ResetFormSelection();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetFormSelection();
            }
        }

        private void ClearForm()
        {
            CompanyTextBox.Clear();
            PositionTextBox.Clear();
            ContactPersonTextBox.Clear();
            ContactEmailTextBox.Clear();
            ApplicationDatePicker.SelectedDate = DateTime.Today;
            NotesTextBox.Clear();
            StatusComboBox.SelectedIndex = 0;
        }
    }
}