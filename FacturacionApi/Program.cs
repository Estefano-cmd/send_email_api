using Microsoft.AspNetCore.Http.HttpResults;

using Microsoft.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Text.Json;



var builder = WebApplication.CreateBuilder(args);

/* builder.Services.AddSingleton<SqlConnection>(provider =>
{
    var connectionStringBuilder = new SqlConnectionStringBuilder
    {
        DataSource = "",
        UserID = "",
        Password = "",
        InitialCatalog = "",
        TrustServerCertificate = true
    };
    return new SqlConnection(connectionStringBuilder.ConnectionString);
}); */

var app = builder.Build();

/* app.MapGet("/all", async context =>
{
    var connection = context.RequestServices.GetRequiredService<SqlConnection>();
    try
    {
        await connection.OpenAsync();
        String sql = "SELECT * FROM afcausas";
        List<Causas> results = new List<Causas>();
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {

                    int idcausa = reader.GetInt32(reader.GetOrdinal("idcausa"));
                    string descripcion = reader.GetString(reader.GetOrdinal("descripcion"));
                    string fechaUltMod = reader.IsDBNull(reader.GetOrdinal("fechaUltMod")) ? null : reader.GetString(reader.GetOrdinal("fechaUltMod"));
                    short estado = reader.GetInt16(reader.GetOrdinal("estado"));
                    string usuario = reader.GetString(reader.GetOrdinal("usuario"));

                    Causas causa = new Causas(idcausa, descripcion, fechaUltMod, estado, usuario);
                    results.Add(causa);
                }
            }
        }
        await context.Response.WriteAsJsonAsync(results);
    }
    catch (SqlException e)
    {
        await context.Response.WriteAsync($"Error al conectar a la base de datos: {e.Message}");
    }
}); */

app.MapPost("/email", async context =>
{
    try
    {
        using (var reader = new StreamReader(context.Request.Body))
        {
            var requestBody = await reader.ReadToEndAsync();
            var emailData = JsonSerializer.Deserialize<EmailData>(requestBody);

            var senderEmail = emailData.SenderEmail;
            var receiverEmail = emailData.ReceiverEmail;
            var password = emailData.Password;
            var copyEmail = emailData.CopyEmail;
            var copyHiddenEmail = emailData.CopyHiddenEmail;

            var smtpClient = new SmtpClient("smtp.office365.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true,
            };

            var message = new MailMessage
            {
                From = new MailAddress(senderEmail),
                Subject = emailData.subject,
                Body = emailData.body,
            };
            message.To.Add(receiverEmail);

            if (!string.IsNullOrEmpty(copyEmail))
            {
                message.CC.Add(copyEmail);
            }

            if(!string.IsNullOrEmpty(copyHiddenEmail)){
                message.Bcc.Add(copyHiddenEmail);
            }

            var filePath = emailData.filePath;
            if (File.Exists(filePath))
            {
                var attachment = new Attachment(filePath);
                message.Attachments.Add(attachment);
            }

            await smtpClient.SendMailAsync(message);

            await context.Response.WriteAsync("Correo electrónico enviado correctamente.");
        }
    }
    catch (Exception ex)
    {
        await context.Response.WriteAsync($"Error al enviar el correo electrónico: {ex.Message}");
    }
});

app.Run();

public record Causas(int idcausa, string descripcion, string fechaUltMod, short estado, string usuario);
public class EmailData
{
    public string SenderEmail { get; set; }
    public string ReceiverEmail { get; set; }
    public string CopyEmail { get; set; }
    public string CopyHiddenEmail { get; set; }
    public string Password { get; set; }
    public string filePath { get; set; }
    public string subject {  get; set; }
    public string body { get; set; }
}
