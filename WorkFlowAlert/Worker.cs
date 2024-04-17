using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using MimeKit;
using MimeKit.Text;
using WorkFlowAlert.Model;

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

    // protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    // {
    //     var connectionString = _configuration.GetConnectionString("DefaultConnection");
    //     
    //     while (!stoppingToken.IsCancellationRequested)
    //     {
    //         using (SqlConnection connection = new SqlConnection(connectionString))
    //         {
    //             await connection.OpenAsync(stoppingToken);
    //             using (SqlCommand command = new SqlCommand("SELECT * FROM POPORI", connection))
    //             {
    //                 using (SqlDataReader reader = await command.ExecuteReaderAsync(stoppingToken))
    //                 {
    //                     while (await reader.ReadAsync(stoppingToken))
    //                     {
    //                         var rowData = new Dictionary<string, object>();
    //                         for (int i = 0; i < reader.FieldCount; i++)
    //                         {
    //                             rowData.Add(reader.GetName(i), reader.GetValue(i));
    //                         }
    //                         // var json = JsonSerializer.Serialize(rowData);
    //                         
    //                         int isComplete = Convert.ToInt32(rowData["ISCOMPLETE"]);
    //                         string audtUser = Convert.ToString(rowData["AUDTUSER"]);
    //                         
    //                         if (rowData["AUDTTIME"] is decimal audtTimeDecimal)
    //                         {
    //                             string audtTimeString = audtTimeDecimal.ToString("00000000");
    //                             if (DateTime.TryParseExact(audtTimeString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime audtTime))
    //                             {
    //                                 
    //                             }
    //                             else
    //                             {
    //                                 _logger.LogError("AUDTTIME is not a valid DateTime");
    //                             }
    //                         }
    //                         else
    //                         {
    //                             _logger.LogError("AUDTTIME is not a Decimal");
    //                         }
    //                         
    //                         
    //                         if (isComplete == 0)
    //                         {
    //                                 _logger.LogInformation("Send email to {audtUser}. for new order", audtUser);
    //                         }
    //                         else if (isComplete == 1)
    //                         {
    //                             const string emailSubject = "Purchase Order Completed";
    //                             const string emailBody = "Your purchase order has been completed";
    //                             const string to = "philipkelly407@gmail.com";
    //                             await SendEmail(to, emailSubject, emailBody);
    //                             _logger.LogInformation("Send email to {audtUser}. for completed order", audtUser);
    //                         }
    //                     }
    //                 }
    //             }
    //         }
    //         
    //         _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
    //         await Task.Delay(1000, stoppingToken);
    //     }
    // }
    
    
    
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var url = _configuration["ApiSettings:apiUrl"];
        var key = _configuration["ApiSettings:key"];
        var host = _configuration["ApiSettings:host"];
        
        var httpClient = new HttpClient();
        // Set the Authorization header
        
        
        // for rapid api
        httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", "77939a538fmsh26c344b7fb24915p1e1831jsnf2271ddd6296");
        httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", "exchange-rate-api1.p.rapidapi.com");
        // httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "767a6d5a-bf4b-4043-95d6-0bb6ce4a40ba");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Send a GET request to the API
            // var response = await httpClient.GetAsync("https://exchange-rates-api.oanda.com/v2/rates/spot.json?base=USD&quote=GHS&date_time=2023-10-04", stoppingToken);
            var response = await httpClient.GetAsync("https://exchange-rate-api1.p.rapidapi.com/latest?base=USD", stoppingToken);

            if (response.IsSuccessStatusCode)
            {
               
                // Deserialize the response
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ApiResponse>(content);
                
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync(stoppingToken);
                    
                    // I need current date
                    DateTime date = DateTime.Now;
                    
                    // convert the date to decimal
                    decimal audtDate = Convert.ToDecimal(date.ToString("yyyyMMdd"));
                    // get the current time and convert it to decimal
                    decimal audtTime = Convert.ToDecimal(date.ToString("HHmmss"));
                    
                    string dateTimeString = data.quotes[0].date_time;
                    DateTime dateTime = DateTime.Parse(dateTimeString);
                    string dateString = dateTime.ToString("yyyyMMdd");
                    decimal dateDecimal = Convert.ToDecimal(dateString);
                    
                    decimal rateDate = dateDecimal;
                    decimal rate = Convert.ToDecimal(data.quotes[0].ask);
                    
                    // _logger.LogInformation("Rate date is  {date}", rateDate);
                    
                    string columns = "HOMECUR, RATETYPE, SOURCECUR, RATEDATE, AUDTDATE, AUDTTIME, AUDTUSER, AUDTORG, RATE, SPREAD,DATEMATCH,RATEOPER";
                    string values = $"'GHC','SP','USD','{rateDate}', '{audtDate}', '{audtTime}', 'ADMIN', 'TARDAT', '{rate}', '0.0000000','3','1'";
                    using (SqlCommand command = new SqlCommand($"INSERT INTO [TARKWAUSD].[dbo].[CSCRD] ({columns}) VALUES ({values})", connection))
                    {
                        await command.ExecuteNonQueryAsync(stoppingToken);
                    }
                }
            }

            // _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            // await Task.Delay(600000, stoppingToken);
        }
    }
}