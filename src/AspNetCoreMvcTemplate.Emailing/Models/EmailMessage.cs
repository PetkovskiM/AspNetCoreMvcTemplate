using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreMvcTemplate.Emailing.Models
{
    // Can be extended with additional properties like From, Cc, Bcc, Attachments, multiple recipients, template keys as needed...
    public class EmailMessage
    {
        public string To { get; init; } = string.Empty;

        public string Subject { get; init; } = string.Empty;

        public string HtmlBody { get; init; } = string.Empty;

        public string? TextBody { get; init; }
    }
}
