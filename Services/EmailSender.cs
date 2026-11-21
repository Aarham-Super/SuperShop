using System.Collections.Concurrent;
namespace SuperShop.Services
{
    public sealed class PasswordResetRequestLimiter
    {
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _requests = new();
        public bool TryAcquire(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) key = "unknown";
            var queue = _requests.GetOrAdd(key, _ => new Queue<DateTime>());
            lock (queue)
            {
                var now = DateTime.UtcNow;
                while (queue.Count > 0 && now - queue.Peek() > TimeSpan.FromHours(1)) queue.Dequeue();
                if (queue.Count >= 5) return false;
                queue.Enqueue(now);
                return true;
            }
        }
    }

    public sealed class EmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;
        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetCodeAsync(string recipient, string code, CancellationToken cancellationToken = default)
        {
            var host = _configuration["Email:Host"];
            var from = _configuration["Email:FromAddress"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("Email delivery is not configured.");

            var port = int.TryParse(_configuration["Email:Port"], out var parsedPort) ? parsedPort : 587;
            using var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(from, _configuration["Email:FromName"] ?? "SuperShop"),
                Subject = "Your SuperShop password reset code",
                Body = $"Your SuperShop password reset code is {code}. It expires in 10 minutes and can only be used once. If you did not request this, you can ignore this email.\n\nDo not share this code with anyone.",
                IsBodyHtml = false
            };
            message.To.Add(recipient);
            using var client = new System.Net.Mail.SmtpClient(host, port)
            {
                EnableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) || ssl,
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                Timeout = 15000
            };
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new System.Net.NetworkCredential(username, password);
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Password reset email sent.");
        }
    }
}
