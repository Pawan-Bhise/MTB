using System.ComponentModel.DataAnnotations;

namespace PMEHCRM.Models
{
    public class CRMFormat
    {
        [Key]
        public int Id { get; set; }

        // CRM Page Front Fields

        public string? Name { get; set; }  // Pop-up

        [Required(ErrorMessage = "Calling number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(13, ErrorMessage = "Phone number cannot exceed 13 characters.")]
        public string? CallingNumber { get; set; }  // Pop-up

        public string? TypeOfCaller { get; set; }  // Pop-up: Agent, Member

        public string? CustomerSegment { get; set; }  // Pop-up: Agent, Primer, Elite, Member, Normal, Ruby/VIP

        public string? TypeOfCall { get; set; }  // Dropdown: Query, Request, Complaint

        public string? Category { get; set; }  // Dropdown

        public string? SubCategory { get; set; }  // Dropdown (related to Category)

        public string? SubSubCategory { get; set; }  // Dropdown (related to SubCategory)

        // Customer Information (To Open SR)

        public string? CustomerName { get; set; }  // Input

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(13, ErrorMessage = "Phone number cannot exceed 13 characters.")]
        public string? PhoneNo { get; set; }  // Input

        [EmailAddress]
        public string? Email { get; set; }  // Input

        public string? AgentName { get; set; }  // Auto-filled with logged-in agent

        public string? RequestDetails { get; set; }  // Open text area for requests/complaints

        public string? Remarks { get; set; }  // Open text area for requests/complaints

        public string? Channel { get; set; } // Dropdown: Call Center, Mail, Viber, Facebook Messenger, Website, Phone
        public string? Comment { get; set; } // Comment-box for User
        // Metadata
        public DateTime? CreatedDateTime { get; set; }  // Auto Date and time
        public DateTime DateTimeSubmitted { get; set; }  // Auto-filled during submission

    }
}
