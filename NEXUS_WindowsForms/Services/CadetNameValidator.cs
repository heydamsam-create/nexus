namespace NEXUS_WindowsForms.Services
{
    /// <summary>
    /// Normaliza y valida el nombre de un cadete antes de guardarlo.
    /// </summary>
    public static class CadetNameValidator
    {
        public static string Normalize(string? nombreCadete)
        {
            string nombreLimpio = string.IsNullOrWhiteSpace(nombreCadete)
                ? "Cadete Vance"
                : nombreCadete.Trim();

            if (nombreLimpio.Length > 60)
            {
                throw new ArgumentException(
                    "El nombre del cadete no puede superar 60 caracteres.",
                    nameof(nombreCadete));
            }

            return nombreLimpio;
        }
    }
}
