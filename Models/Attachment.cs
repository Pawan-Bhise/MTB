using System.ComponentModel.DataAnnotations;

namespace PMEHCRM.Models
{
    public class Attachment
    {
        [Key]
        public int Id { get; set; } // Primary Key
        public string FileName { get; set; } // Name of the uploaded file
        public string FilePath { get; set; } // Path where the file is stored
        public string FileType { get; set; } // Type (e.g., image/png, video/mp4)
        public int TicketId { get; set; } // Foreign Key to the Ticket
    }
}
