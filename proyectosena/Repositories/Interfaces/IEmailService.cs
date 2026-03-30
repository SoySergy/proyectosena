namespace proyectosena.Repositories.Interfaces
{
    public interface IEmailService
    {
        public Task SendPasswordResetCodeAsync(string toEmail, string code);
    }
}
