using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Interfaces.Repositories;

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

            await SendAsync(message);
        }

        // Invites a newly created manager to set their own password.
        // The account already exists; the code is what proves they own the mailbox.
        public async Task SendManagerInvitationAsync(string toEmail, string name, string code, int expiryMinutes)
        {
            var settings = _config.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                settings["SenderName"],
                settings["SenderEmail"]
            ));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Bienvenido a RecyRoute – Activa tu cuenta de gestor";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family:sans-serif;max-width:480px;margin:auto'>
                        <h2 style='color:#2E7D32'>RecyRoute</h2>
                        <p>Hola <strong>{name}</strong>,</p>
                        <p>Se creó una cuenta de gestor para ti. Para activarla necesitas
                           establecer tu propia contraseña con este código:</p>
                        <div style='font-size:2.5rem;font-weight:bold;letter-spacing:10px;
                                    color:#2E7D32;text-align:center;padding:1rem 0'>
                            {code}
                        </div>
                        <p style='color:#666;font-size:0.875rem'>
                            Este código expira en <strong>{expiryMinutes} minutos</strong>.<br>
                            Si expira, usa la opción <em>¿Olvidaste tu contraseña?</em>
                            en la pantalla de inicio de sesión para pedir uno nuevo.
                        </p>
                    </div>"
            };

            await SendAsync(message);
        }

        // Shared SMTP delivery for every message this service builds
        private async Task SendAsync(MimeMessage message)
        {
            var settings = _config.GetSection("EmailSettings");

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