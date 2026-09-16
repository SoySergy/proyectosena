using System.Collections.Concurrent;
using proyectosena.Interfaces.Services;

namespace proyectosena.Tests.Infrastructure
{
    public enum EmailKind
    {
        EmailConfirmation,
        PasswordReset,
        ManagerInvitation,
        AlreadyRegistered
    }

    public sealed record SentEmail(EmailKind Kind, string To, string? Code);

    /// <summary>
    /// Buzón falso: en vez de mandar el correo, lo apunta. Las pruebas leen de
    /// aquí el código que la persona habría recibido.
    /// </summary>
    public sealed class FakeEmailService : IEmailService
    {
        private readonly ConcurrentQueue<SentEmail> _sent = new();

        public Task SendPasswordResetCodeAsync(string toEmail, string code)
            => Record(EmailKind.PasswordReset, toEmail, code);

        // Siempre entrega: lo que se prueba aquí es el recorrido, no el SMTP.
        public async Task<bool> SendManagerInvitationAsync(string toEmail, string name, string code, int expiryMinutes)
        {
            await Record(EmailKind.ManagerInvitation, toEmail, code);
            return true;
        }

        public Task SendEmailVerificationCodeAsync(string toEmail, string name, string code, int expiryMinutes)
            => Record(EmailKind.EmailConfirmation, toEmail, code);

        public Task SendAlreadyRegisteredNoticeAsync(string toEmail, string name)
            => Record(EmailKind.AlreadyRegistered, toEmail, null);

        /// <summary>Todo lo que se le mandó a una dirección, en orden.</summary>
        public IReadOnlyList<SentEmail> SentTo(string email)
            => _sent.Where(sent => string.Equals(sent.To, email, StringComparison.OrdinalIgnoreCase)).ToList();

        /// <summary>El código del último correo de ese tipo que recibió la dirección.</summary>
        public string LastCode(string email, EmailKind kind)
            => SentTo(email).Last(sent => sent.Kind == kind).Code!;

        private Task Record(EmailKind kind, string to, string? code)
        {
            _sent.Enqueue(new SentEmail(kind, to, code));
            return Task.CompletedTask;
        }
    }
}
