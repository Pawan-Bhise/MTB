using System.ComponentModel.DataAnnotations;

namespace PMEHCRM.Models
{
    public class TicketManagement
    {
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
        public string? SrOrTicket { get; set; }
        public string? CustomerId { get; set; }  // Identifier for customer (not a foreign key)
        public string? CustomerName { get; set; }  // Input

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(13, ErrorMessage = "Phone number cannot exceed 13 characters.")]
        public string? PhoneNo { get; set; }  // Input

        [EmailAddress]
        public string? Email { get; set; }  // Input

        public string? Address { get; set; }  // Customer Address
        public string? City { get; set; }  // Customer City
        public string? State { get; set; }  // Customer State
        public string? Country { get; set; }  // Customer Country
        public string? ZipCode { get; set; }  // Customer Zip Code
        public string? AgentName { get; set; }  // Auto-filled with logged-in agent
        public string? RequestDetails { get; set; }  // Open text area for requests/complaints
        public string? Remarks { get; set; }  // Open text area for requests/complaints

        // MTB Add-ons
        public string? TicketNumber { get; set; }  // Auto-generated
        public string? LevelOfConflict { get; set; }  // Dropdown: High, Medium, Low
        public string? TicketStatus { get; set; }  // Dropdown: Open, Reopen, Pending, Waiting for Customer, Escalate, Resolve, Close
        public DateTime? CreatedDateTime { get; set; }  // Auto Date and time
        public DateTime? EscalatedDateTime { get; set; }  // Auto Date and time
        public DateTime? ResolvedDateTime { get; set; }  // Auto Date and time
        public DateTime? ClosedDateTime { get; set; }  // Auto Date and time
        public string? ServiceLevel { get; set; }  // SLA Information
        public string? Assignee { get; set; }  // Assigned agent, alert message to assign
        public string? ReAssignee { get; set; }  // Re-assigned agent, alert message to assign
        public string? Channel { get; set; }  // Dropdown: Call Center, Mail, Viber, Facebook Messenger, Website, Phone
        public string? Resolver { get; set; }  // Dropdown: Name of resolver
        public string? SLAStatus { get; set; }  // Auto: Exceeded SLA, Within SLA
        public string? Comment { get; set; } // Comment-box for User
        public string? ModifiedBy { get; set; }

        // Metadata
        public DateTime DateTimeSubmitted { get; set; }  // Auto-filled during submission
        public DateTime? DateTimeModified { get; set; }
        public virtual ICollection<Attachment> Attachments { get; set; }
    }
}
