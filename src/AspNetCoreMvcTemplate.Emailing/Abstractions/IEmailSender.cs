using AspNetCoreMvcTemplate.Emailing.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreMvcTemplate.Emailing.Abstractions
{
    public interface IEmailSender
    {
        Task<EmailSendResult> SendAsync(EmailMessage message,CancellationToken cancellationToken = default);
    }
}
