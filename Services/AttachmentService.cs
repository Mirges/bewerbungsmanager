using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace JobApplicationTracker.Services
{
    public class AttachmentService
    {
        public string CopyPdfToAttachmentFolder(string sourceFilePath, string companyName, string positionTitle)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return string.Empty;
            }

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string attachmentFolder = Path.Combine(appDataPath, "Bewerbungsmanager", "Attachments");

            Directory.CreateDirectory(attachmentFolder);

            string safeCompanyName = MakeSafeFileName(companyName);
            string safePositionTitle = MakeSafeFileName(positionTitle);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string targetFileName = $"{safeCompanyName}_{safePositionTitle}_{timestamp}.pdf";
            string targetFilePath = Path.Combine(attachmentFolder, targetFileName);

            File.Copy(sourceFilePath, targetFilePath, overwrite: false);

            return targetFilePath;
        }

        public void OpenAttachment(string attachmentPath)
        {
            if (string.IsNullOrWhiteSpace(attachmentPath) || !File.Exists(attachmentPath))
            {
                throw new FileNotFoundException("Die PDF-Datei wurde nicht gefunden.");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = attachmentPath,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }

        public void DeleteAttachmentIfExists(string attachmentPath)
        {
            if (string.IsNullOrWhiteSpace(attachmentPath))
            {
                return;
            }

            if (File.Exists(attachmentPath))
            {
                File.Delete(attachmentPath);
            }
        }

        public string GetAttachmentFileName(string attachmentPath)
        {
            if (string.IsNullOrWhiteSpace(attachmentPath))
            {
                return "Kein Anhang ausgewählt";
            }

            return Path.GetFileName(attachmentPath);
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unbekannt";
            }

            string invalidCharacters = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidPattern = $"[{invalidCharacters}]";

            string safeValue = Regex.Replace(value, invalidPattern, "_");
            safeValue = safeValue.Replace(" ", "_");

            return safeValue;
        }
    }
}