using System.Diagnostics;

namespace SuperShop.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId =>
            !string.IsNullOrEmpty(RequestId);

        public string? ErrorMessage { get; set; }

        public string? ErrorCode { get; set; }

        public string? ReturnUrl { get; set; }
    }
}