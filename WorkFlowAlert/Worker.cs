using System.Globalization;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using MimeKit;
using MimeKit.Text;

namespace WorkFlowAlert;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(stoppingToken);
                using (SqlCommand command = new SqlCommand("SELECT * FROM POPORI", connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync(stoppingToken))
                    {
                        while (await reader.ReadAsync(stoppingToken))
                        {
                            var rowData = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                rowData.Add(reader.GetName(i), reader.GetValue(i));
                            }
                            
                            int isComplete = Convert.ToInt32(rowData["ISCOMPLETE"]);
                            string audtUser = Convert.ToString(rowData["AUDTUSER"]);
                            var sendTo = _configuration["EmailConfiguration:SendTo"];
                            
                            if (rowData["AUDTTIME"] is decimal audtTimeDecimal)
                            {
                                string audtTimeString = audtTimeDecimal.ToString("00000000");
                                if (DateTime.TryParseExact(audtTimeString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime audtTime))
                                {
                                    
                                }
                                else
                                {
                                    _logger.LogError("AUDTTIME is not a valid DateTime");
                                }
                            }
                            else
                            {
                                _logger.LogError("AUDTTIME is not a Decimal");
                            }
                            
                            
                            if (isComplete == 0)
                            {
                                    _logger.LogInformation("Send email to {audtUser}. for new order", audtUser);
                            }
                            else if (isComplete == 1)
                            {
                                const string emailSubject = "Purchase Order Completed";
                                const string emailBody = "Your purchase order has been completed";
                                
                                await SendEmail(sendTo, emailSubject, emailBody);
                                _logger.LogInformation("Send email to {audtUser}. for completed order", audtUser);
                            }
                        }
                    }
                }
            }
            
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
    
    
    public async Task SendEmail(string to, string subject, string body)
    {
        var from = _configuration["EmailConfiguration:From"];
        var smtpServer = _configuration["EmailConfiguration:SmtpServer"];
        var port = _configuration["EmailConfiguration:Port"];
        var smtpUsername = _configuration["EmailConfiguration:SmtpUsername"];
        var smtpPassword = _configuration["EmailConfiguration:SmtpPassword"]; 
        
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(from));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Plain) { Text = body };

        try
        {
            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(smtpServer, int.Parse(port), SecureSocketOptions.StartTls);
            await smtpClient.AuthenticateAsync(smtpUsername, smtpPassword);
            await smtpClient.SendAsync(email);
            await smtpClient.DisconnectAsync(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
}