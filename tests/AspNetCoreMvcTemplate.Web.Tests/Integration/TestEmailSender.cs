using System.Collections.Concurrent;
using AspNetCoreMvcTemplate.Emailing.Abstractions;
using AspNetCoreMvcTemplate.Emailing.Models;

namespace AspNetCoreMvcTemplate.Web.Tests.Integration;

// In-memory IEmailSender koj ja zachuvuva sekoja poraka vo lista. Vo testovi sakame
// da znaeme sto bi se ispratilo (sodrzhina, naslov, link), bez vsushnost da
// pratame email. Trivijalna implementacija - ConcurrentBag e thread-safe za
// slucaj na paralelni testovi.
public class TestEmailSender : IEmailSender
{
    public ConcurrentBag<EmailMessage> SentMessages { get; } = new();

    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        SentMessages.Add(message);
        return Task.FromResult(new EmailSendResult { Succeeded = true });
    }

    public void Clear()
    {
        SentMessages.Clear();
    }
}
