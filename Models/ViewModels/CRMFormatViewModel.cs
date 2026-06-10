namespace PMEHCRM.Models.ViewModels
{
    public class CRMFormatViewModel
    {
        public string? TypeOfCall { get; set; }
        public string? CallingNumber { get; set; }
        public DateTime? DateSubmitted { get; set; }
        public List<CRMFormat> Records { get; set; }
    }
}
