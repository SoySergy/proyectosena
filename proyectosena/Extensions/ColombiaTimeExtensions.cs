namespace proyectosena.Extensions
{
    // Quien pide "las solicitudes del 30 de septiembre" piensa en el día de
    // Colombia, pero RequestDate, ChangeDate y RegistrationDate se guardan en
    // UTC. Sin convertir, lo creado la noche del 30 en Bogotá caería en el 1.
    //
    // Colombia está en UTC−5 todo el año: no tiene horario de verano desde
    // 1993. Un desfase fijo evita depender de la base de zonas horarias del
    // sistema, que no es la misma en Windows, en la imagen Docker y en CI.
    public static class ColombiaTimeExtensions
    {
        private static readonly TimeSpan ColombiaOffset = TimeSpan.FromHours(-5);

        // Instante UTC en que empieza ese día en Colombia: las 00:00 en Bogotá
        // son las 05:00 UTC.
        public static DateTime StartOfColombianDayUtc(this DateOnly day)
            => DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue) - ColombiaOffset, DateTimeKind.Utc);
    }
}
