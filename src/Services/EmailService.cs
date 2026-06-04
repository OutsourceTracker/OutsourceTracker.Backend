using SendGrid;
using SendGrid.Helpers.Mail;

namespace OutsourceTracker.Services;

public class EmailService
{
    private readonly ISendGridClient _client;
    private readonly IConfiguration _config;

    public EmailService(ISendGridClient client, IConfiguration config)
    {
        _config = config.GetRequiredSection("SendGrid");
        _client = client;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(_config["FromEmail"], _config["FromName"]),
            Subject = subject,
            HtmlContent = htmlContent
        };
        msg.AddTo(new EmailAddress(toEmail));

        var response = await _client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new Exception($"Email sending failed: {response.StatusCode} - {body}");
        }
    }

    public async Task SendTemplateEmailAsync(string toEmail, string templateId, Dictionary<string, string> substitutions)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(_config["FromEmail"], _config["FromName"]),
            TemplateId = templateId,
            Personalizations = new List<Personalization>
            {
                new Personalization
                {
                    Tos = [new EmailAddress(toEmail)],
                    TemplateData = substitutions
                }
            }
        };
        var response = await _client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new Exception($"Email sending failed: {response.StatusCode} - {body}");
        }
    }

    public async Task SendTemplateEmailAsync(IEnumerable<string> toEmails, string templateId, object templateData)
    {
        if (toEmails == null || !toEmails.Any())
            return;

        var msg = new SendGridMessage()
        {
            From = new EmailAddress(_config["FromEmail"], _config["FromName"]),
            TemplateId = templateId,
            Personalizations = new List<Personalization>
            {
                new Personalization
                {
                    Tos = toEmails.Select(e => new EmailAddress(e)).ToList(),
                    TemplateData = templateData
                }
            }
        };
        var response = await _client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new Exception($"Email sending failed: {response.StatusCode} - {body}");
        }
    }
}
