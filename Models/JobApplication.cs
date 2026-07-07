using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace JobApplicationTracker.Models
{
    public class JobApplication
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string PositionTitle { get; set; } = string.Empty;

        public string ContactPerson { get; set; } = string.Empty;

        public string ContactEmail { get; set; } = string.Empty;

        public DateTime ApplicationDate { get; set; } = DateTime.Today;

        public string Status { get; set; } = "Beworben";

        public string Notes { get; set; } = string.Empty;

        public string AttachmentPath { get; set; } = string.Empty;

        [NotMapped]
        public string AttachmentDisplay
        {
            get
            {
                return string.IsNullOrWhiteSpace(AttachmentPath)
                    ? "-"
                    : Path.GetFileName(AttachmentPath);
            }
        }
    }
}