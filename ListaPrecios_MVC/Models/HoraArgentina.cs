namespace ListaPrecios_MVC.Models
{
    public static class HoraArgentina
    {
        private static readonly TimeZoneInfo ZonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");

        // Fecha y hora actual en Argentina
        public static DateTime Ahora()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaArgentina);
        }

        // Fecha de hoy en Argentina, sin hora
        public static DateTime Hoy()
        {
            return Ahora().Date;
        }
    }
}