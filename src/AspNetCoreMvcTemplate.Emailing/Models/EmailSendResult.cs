using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreMvcTemplate.Emailing.Models
{
    public class EmailSendResult
    {
        public bool Succeeded { get; init; }

        public string? ErrorMessage { get; init; }

        public string? ProviderMessageId { get; init; }
    }
}
