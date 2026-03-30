using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using proyectosena.Interfaces;
using proyectosena.Repositories.Interfaces;

namespace proyectosena.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendPasswordResetCodeAsync(string toEmail, string code)
        {
            var settings = _config.GetSection("EmailSettings");

            // Construye el mensaje
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                settings["SenderName"],
                settings["SenderEmail"]
            ));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Código de recuperación – RecyRoute";

            // Cuerpo del correo en HTML
            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family:sans-serif;max-width:480px;margin:auto'>
                        <h2 style='color:#2E7D32'>RecyRoute</h2>
                        <p>Recibimos una solicitud para restablecer tu contraseña.</p>
                        <p>Tu código de verificación es:</p>
                        <div style='font-size:2.5rem;font-weight:bold;letter-spacing:10px;
                                    color:#2E7D32;text-align:center;padding:1rem 0'>
                            {code}
                        </div>
                        <p style='color:#666;font-size:0.875rem'>
                            Este código expira en <strong>15 minutos</strong>.<br>
                            Si no solicitaste esto, ignora este correo.
                        </p>
                    </div>"
            };

            // Envía el correo usando Gmail SMTP
            using var client = new SmtpClient();
            await client.ConnectAsync(
                settings["Host"],
                int.Parse(settings["Port"]!),
                SecureSocketOptions.StartTls
            );
            await client.AuthenticateAsync(settings["SenderEmail"], settings["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}