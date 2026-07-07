using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JobApplicationTracker.Data;
using JobApplicationTracker.Models;
using Microsoft.Win32;
using System.IO;
using System.Text;
using JobApplicationTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace JobApplicationTracker
{
    public partial class MainWindow : Window
    {
private readonly ObservableCollection<JobApplication> _applications = new();
private readonly JobApplicationService _jobApplicationService = new();
private readonly AttachmentService _attachmentService = new();

private JobApplication? _selectedApplication;

private string _currentAttachmentPath = string.Empty;
private string _selectedAttachmentSourcePath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            ApplicationsDataGrid.ItemsSource = _applications;
            ApplicationDatePicker.SelectedDate = DateTime.Today;

            InitializeDatabase();
            ApplyFilters();
        }

        private void InitializeDatabase()
        {
            using var db = new ApplicationDbContext();

            try
            {
                db.Database.EnsureCreated();

                try
                {
                    db.Database.ExecuteSqlRaw(
                        "ALTER TABLE JobApplications ADD COLUMN AttachmentPath TEXT NOT NULL DEFAULT ''");
                }
                catch
                {
                    // Spalte existiert bereits.
                }

                db.JobApplications.Any();
            }
            catch
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                try
                {
                    db.Database.ExecuteSqlRaw(
                        "ALTER TABLE JobApplications ADD COLUMN AttachmentPath TEXT NOT NULL DEFAULT ''");
                }
                catch
                {
                    // Falls die Spalte bereits durch EnsureCreated angelegt wurde.
                }
            }
        }

        private void ApplyFilters()
        {
            if (SearchTextBox is null || StatusFilterComboBox is null)
            {
                return;
            }

            string searchText = SearchTextBox.Text.Trim();
            string selectedStatus = GetSelectedFilterStatus();

            var filteredApplications = _jobApplicationService.GetApplications(searchText, selectedStatus);

            _applications.Clear();

            foreach (var application in filteredApplications)
            {
                _applications.Add(application);
            }

            UpdateDashboard();
        }

        private string GetSelectedFilterStatus()
        {
            if (StatusFilterComboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Content is string status)
            {
                return status;
            }

            return "Alle";
        }

        private void AddApplicationButton_Click(object sender, RoutedEventArgs e)
        {
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

            if (StatusComboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Content is string status)
            {
                selectedStatus = status;
            }

            string attachmentPath = GetAttachmentPathForSaving(companyName, positionTitle);

            var application = new JobApplication
            {
                CompanyName = companyName,
                PositionTitle = positionTitle,
                ContactPerson = ContactPersonTextBox.Text.Trim(),
                ContactEmail = ContactEmailTextBox.Text.Trim(),
                ApplicationDate = ApplicationDatePicker.SelectedDate ?? DateTime.Today,
                Status = selectedStatus,
                AttachmentPath = attachmentPath,
                Notes = NotesTextBox.Text.Trim()
            };

            _jobApplicationService.AddApplication(application);

            ApplyFilters();
            ResetFormSelection();
        }

        private void DeleteApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedApplication();
        }

        private void DeleteSelectedApplication()
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

            try
            {
                _jobApplicationService.DeleteApplication(selectedApplication.Id);
                _attachmentService.DeleteAttachmentIfExists(selectedApplication.AttachmentPath);

                ApplyFilters();
                ResetFormSelection();
            }
            catch (InvalidOperationException exception)
            {
                MessageBox.Show(
                    exception.Message,
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            ResetFormSelection();
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

            if (StatusComboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Content is string status)
            {
                selectedStatus = status;
            }

            string attachmentPath = GetAttachmentPathForSaving(companyName, positionTitle);

            var updatedApplication = new JobApplication
            {
                Id = _selectedApplication.Id,
                CompanyName = companyName,
                PositionTitle = positionTitle,
                ContactPerson = ContactPersonTextBox.Text.Trim(),
                ContactEmail = ContactEmailTextBox.Text.Trim(),
                ApplicationDate = ApplicationDatePicker.SelectedDate ?? DateTime.Today,
                Status = selectedStatus,
                Notes = NotesTextBox.Text.Trim(),
                AttachmentPath = attachmentPath
            };

            try
            {
                _jobApplicationService.UpdateApplication(updatedApplication);

                ApplyFilters();
                ResetFormSelection();
            }
            catch (InvalidOperationException exception)
            {
                MessageBox.Show(
                    exception.Message,
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
            _currentAttachmentPath = selectedApplication.AttachmentPath;
            _selectedAttachmentSourcePath = string.Empty;
            AttachmentFileTextBlock.Text = _attachmentService.GetAttachmentFileName(_currentAttachmentPath);

            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Content is string status && status == selectedApplication.Status)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClearFormButton_Click(object sender, RoutedEventArgs e)
        {
            ResetFormSelection();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetFormSelection();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                if (Keyboard.FocusedElement is TextBox)
                {
                    return;
                }

                if (ApplicationsDataGrid.SelectedItem is JobApplication)
                {
                    DeleteSelectedApplication();
                    e.Handled = true;
                }
            }
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            if (_applications.Count == 0)
            {
                MessageBox.Show(
                    "Es sind keine Bewerbungen zum Exportieren vorhanden.",
                    "Export nicht möglich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Title = "Bewerbungen als CSV exportieren",
                Filter = "CSV-Datei (*.csv)|*.csv",
                FileName = $"Bewerbungen_{DateTime.Now:yyyy-MM-dd}.csv"
            };

            bool? result = saveFileDialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            var csvBuilder = new StringBuilder();

            csvBuilder.AppendLine("Unternehmen;Position;Kontaktperson;E-Mail;Bewerbungsdatum;Status;Notizen");

            foreach (var application in _applications)
            {
                csvBuilder.AppendLine(
                    $"{EscapeCsvValue(application.CompanyName)};" +
                    $"{EscapeCsvValue(application.PositionTitle)};" +
                    $"{EscapeCsvValue(application.ContactPerson)};" +
                    $"{EscapeCsvValue(application.ContactEmail)};" +
                    $"{application.ApplicationDate:dd.MM.yyyy};" +
                    $"{EscapeCsvValue(application.Status)};" +
                    $"{EscapeCsvValue(application.Notes)}");
            }

            File.WriteAllText(saveFileDialog.FileName, csvBuilder.ToString(), Encoding.UTF8);

            MessageBox.Show(
                "Die Bewerbungen wurden erfolgreich exportiert.",
                "Export abgeschlossen",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string escapedValue = value.Replace("\"", "\"\"");

            if (escapedValue.Contains(';') ||
                escapedValue.Contains('"') ||
                escapedValue.Contains('\n') ||
                escapedValue.Contains('\r'))
            {
                escapedValue = $"\"{escapedValue}\"";
            }

            return escapedValue;
        }

        private void UpdateDashboard()
        {
            var allApplications = _jobApplicationService.GetAllApplications();

            int totalCount = allApplications.Count;
            int appliedCount = allApplications.Count(application => application.Status == "Beworben");
            int confirmedCount = allApplications.Count(application => application.Status == "Erhalt bestätigt");
            int rejectedCount = allApplications.Count(application => application.Status == "Abgelehnt");
            int acceptedCount = allApplications.Count(application => application.Status == "Zusage");

            TotalCountTextBlock.Text = $"Gesamt: {totalCount}";
            AppliedCountTextBlock.Text = $"Beworben: {appliedCount}";
            ConfirmedCountTextBlock.Text = $"Erhalt bestätigt: {confirmedCount}";
            RejectedCountTextBlock.Text = $"Abgelehnt: {rejectedCount}";
            AcceptedCountTextBlock.Text = $"Zusage: {acceptedCount}";
        }

        private void SelectPdfButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "PDF-Stellenausschreibung auswählen",
                Filter = "PDF-Dateien (*.pdf)|*.pdf"
            };

            bool? result = openFileDialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            _selectedAttachmentSourcePath = openFileDialog.FileName;
            AttachmentFileTextBlock.Text = Path.GetFileName(_selectedAttachmentSourcePath);
        }

        private void OpenPdfButton_Click(object sender, RoutedEventArgs e)
        {
            string attachmentPath = !string.IsNullOrWhiteSpace(_selectedAttachmentSourcePath)
                ? _selectedAttachmentSourcePath
                : _currentAttachmentPath;

            try
            {
                _attachmentService.OpenAttachment(attachmentPath);
            }
            catch
            {
                MessageBox.Show(
                    "Es wurde keine gültige PDF-Datei gefunden.",
                    "PDF nicht gefunden",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private string GetAttachmentPathForSaving(string companyName, string positionTitle)
        {
            if (string.IsNullOrWhiteSpace(_selectedAttachmentSourcePath))
            {
                return _currentAttachmentPath;
            }

            return _attachmentService.CopyPdfToAttachmentFolder(
                _selectedAttachmentSourcePath,
                companyName,
                positionTitle);
        }

        private void RemovePdfButton_Click(object sender, RoutedEventArgs e)
        {
            _currentAttachmentPath = string.Empty;
            _selectedAttachmentSourcePath = string.Empty;
            AttachmentFileTextBlock.Text = "Kein Anhang ausgewählt";
        }

        private void ResetFormSelection()
        {
            ClearForm();
            _selectedApplication = null;
            ApplicationsDataGrid.SelectedItem = null;
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

            _currentAttachmentPath = string.Empty;
            _selectedAttachmentSourcePath = string.Empty;
            AttachmentFileTextBlock.Text = "Kein Anhang ausgewählt";
        }
    }
}