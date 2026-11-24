using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using NEXUS_WindowsForms.Models;

namespace NEXUS_WindowsForms.Services
{
    internal sealed class SavedExpeditionInfo
    {
        public int IdExpedicion { get; set; }
        public string NombreCadete { get; set; } = string.Empty;
        public string NombreRanura { get; set; } = string.Empty;
        public string EtapaActual { get; set; } = string.Empty;
        public string? EstadoExploracionJson { get; set; }
        public int Oxigeno { get; set; }
        public int IntegridadCasco { get; set; }
        public int DatosRecuperados { get; set; }
        public bool EscaneoOrbital { get; set; }
        public bool DescensoDirecto { get; set; }
        public bool SenalConfiable { get; set; }
        public bool SondaEnviada { get; set; }
        public DateTime FechaUltimoGuardado { get; set; }
        public List<string> Decisiones { get; set; } = new();
    }

    internal sealed class DatabaseService
    {
        // Cadena de conexión hacia tu servidor local SQL Server
        private readonly string _connectionString =
            Environment.GetEnvironmentVariable("NEXUS_SQL_CONNECTION_STRING")
            ?? @"Server=localhost\SQLEXPRESS;Database=NexusDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        private static void EnsureExplorationStateColumn(SqlConnection connection)
        {
            const string migrationSql = @"
                IF COL_LENGTH('dbo.EstadoNave', 'EstadoExploracionJson') IS NULL
                BEGIN
                    ALTER TABLE dbo.EstadoNave
                        ADD EstadoExploracionJson NVARCHAR(MAX) NULL;
                END;";

            using SqlCommand command = new(migrationSql, connection);
            command.ExecuteNonQuery();
        }

        public bool ProbarConexion(out string mensaje)
        {
            try
            {
                using SqlConnection conn = GetConnection();
                conn.Open();
                mensaje = "Conexión exitosa con NexusDB.";
                return true;
            }
            catch (Exception ex)
            {
                mensaje = $"Error de conexión SQL: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Obtiene o crea el perfil de un cadete por su nombre.
        /// </summary>
        public int ObtenerOCrearCadete(string nombreCadete)
        {
            string nombreLimpio = string.IsNullOrWhiteSpace(nombreCadete) ? "Cadete Vance" : nombreCadete.Trim();

            using SqlConnection conn = GetConnection();
            conn.Open();

            const string selectSql = "SELECT IdCadete FROM dbo.Cadetes WHERE NombreCadete = @Nombre;";
            using (SqlCommand selectCmd = new(selectSql, conn))
            {
                selectCmd.Parameters.AddWithValue("@Nombre", nombreLimpio);
                object? existingId = selectCmd.ExecuteScalar();
                if (existingId != null && existingId != DBNull.Value)
                {
                    return Convert.ToInt32(existingId);
                }
            }

            const string insertSql = @"
                INSERT INTO dbo.Cadetes (NombreCadete, Rango)
                OUTPUT INSERTED.IdCadete
                VALUES (@Nombre, 'Cadete de Exploración');";

            using SqlCommand insertCmd = new(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@Nombre", nombreLimpio);
            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        /// <summary>
        /// Guarda o actualiza una expedición en curso (partidas guardadas) usando una transacción SQL.
        /// </summary>
        public int GuardarExpedicionEnCurso(
            int? idExpedicionExistente,
            string nombreCadete,
            string nombreRanura,
            string etapaActual,
            int oxigeno,
            int integridadCasco,
            int datosRecuperados,
            bool escaneoOrbital,
            bool descensoDirecto,
            bool senalConfiable,
            bool sondaEnviada,
            IReadOnlyList<string> decisiones,
            string? estadoExploracionJson = null)
        {
            int idCadete = ObtenerOCrearCadete(nombreCadete);

            using SqlConnection conn = GetConnection();
            conn.Open();
            EnsureExplorationStateColumn(conn);
            using SqlTransaction tx = conn.BeginTransaction();

            try
            {
                int idExpedicion;

                if (idExpedicionExistente.HasValue && idExpedicionExistente.Value > 0)
                {
                    idExpedicion = idExpedicionExistente.Value;
                    const string updateExpSql = @"
                        UPDATE dbo.Expediciones
                        SET NombreRanura = @Ranura,
                            EtapaActual = @Etapa,
                            EstadoExpedicion = 'EnCurso',
                            FechaUltimoGuardado = SYSDATETIME()
                        WHERE IdExpedicion = @IdExpedicion;";

                    using SqlCommand cmdExp = new(updateExpSql, conn, tx);
                    cmdExp.Parameters.AddWithValue("@Ranura", nombreRanura);
                    cmdExp.Parameters.AddWithValue("@Etapa", etapaActual);
                    cmdExp.Parameters.AddWithValue("@IdExpedicion", idExpedicion);
                    cmdExp.ExecuteNonQuery();

                    const string updateEstadoSql = @"
                        UPDATE dbo.EstadoNave
                        SET Oxigeno = @Oxigeno,
                            IntegridadCasco = @Casco,
                            DatosRecuperados = @Datos,
                            EscaneoOrbital = @Escaneo,
                            DescensoDirecto = @Descenso,
                            SenalConfiable = @Senal,
                            SondaEnviada = @Sonda,
                            EstadoExploracionJson = @EstadoExploracionJson
                        WHERE IdExpedicion = @IdExpedicion;";

                    using SqlCommand cmdEstado = new(updateEstadoSql, conn, tx);
                    cmdEstado.Parameters.AddWithValue("@Oxigeno", oxigeno);
                    cmdEstado.Parameters.AddWithValue("@Casco", integridadCasco);
                    cmdEstado.Parameters.AddWithValue("@Datos", datosRecuperados);
                    cmdEstado.Parameters.AddWithValue("@Escaneo", escaneoOrbital);
                    cmdEstado.Parameters.AddWithValue("@Descenso", descensoDirecto);
                    cmdEstado.Parameters.AddWithValue("@Senal", senalConfiable);
                    cmdEstado.Parameters.AddWithValue("@Sonda", sondaEnviada);
                    cmdEstado.Parameters.AddWithValue("@EstadoExploracionJson", (object?)estadoExploracionJson ?? DBNull.Value);
                    cmdEstado.Parameters.AddWithValue("@IdExpedicion", idExpedicion);
                    cmdEstado.ExecuteNonQuery();
                }
                else
                {
                    const string insertExpSql = @"
                        INSERT INTO dbo.Expediciones (IdCadete, NombreRanura, EtapaActual, EstadoExpedicion)
                        OUTPUT INSERTED.IdExpedicion
                        VALUES (@IdCadete, @Ranura, @Etapa, 'EnCurso');";

                    using SqlCommand cmdExp = new(insertExpSql, conn, tx);
                    cmdExp.Parameters.AddWithValue("@IdCadete", idCadete);
                    cmdExp.Parameters.AddWithValue("@Ranura", nombreRanura);
                    cmdExp.Parameters.AddWithValue("@Etapa", etapaActual);
                    idExpedicion = Convert.ToInt32(cmdExp.ExecuteScalar());

                    const string insertEstadoSql = @"
                        INSERT INTO dbo.EstadoNave
                        (IdExpedicion, Oxigeno, IntegridadCasco, DatosRecuperados, EscaneoOrbital, DescensoDirecto, SenalConfiable, SondaEnviada, EstadoExploracionJson)
                        VALUES
                        (@IdExpedicion, @Oxigeno, @Casco, @Datos, @Escaneo, @Descenso, @Senal, @Sonda, @EstadoExploracionJson);";

                    using SqlCommand cmdEstado = new(insertEstadoSql, conn, tx);
                    cmdEstado.Parameters.AddWithValue("@IdExpedicion", idExpedicion);
                    cmdEstado.Parameters.AddWithValue("@Oxigeno", oxigeno);
                    cmdEstado.Parameters.AddWithValue("@Casco", integridadCasco);
                    cmdEstado.Parameters.AddWithValue("@Datos", datosRecuperados);
                    cmdEstado.Parameters.AddWithValue("@Escaneo", escaneoOrbital);
                    cmdEstado.Parameters.AddWithValue("@Descenso", descensoDirecto);
                    cmdEstado.Parameters.AddWithValue("@Senal", senalConfiable);
                    cmdEstado.Parameters.AddWithValue("@Sonda", sondaEnviada);
                    cmdEstado.Parameters.AddWithValue("@EstadoExploracionJson", (object?)estadoExploracionJson ?? DBNull.Value);
                    cmdEstado.ExecuteNonQuery();
                }

                // Sincronizar decisiones tomadas
                const string deleteDecSql = "DELETE FROM dbo.DecisionesExpedicion WHERE IdExpedicion = @IdExpedicion;";
                using (SqlCommand delCmd = new(deleteDecSql, conn, tx))
                {
                    delCmd.Parameters.AddWithValue("@IdExpedicion", idExpedicion);
                    delCmd.ExecuteNonQuery();
                }

                const string insertDecSql = @"
                    INSERT INTO dbo.DecisionesExpedicion (IdExpedicion, NumeroPaso, Etapa, OpcionElegida)
                    VALUES (@IdExpedicion, @Paso, @Etapa, @Opcion);";

                for (int i = 0; i < decisiones.Count; i++)
                {
                    using SqlCommand decCmd = new(insertDecSql, conn, tx);
                    decCmd.Parameters.AddWithValue("@IdExpedicion", idExpedicion);
                    decCmd.Parameters.AddWithValue("@Paso", i + 1);
                    decCmd.Parameters.AddWithValue("@Etapa", etapaActual);
                    decCmd.Parameters.AddWithValue("@Opcion", decisiones[i]);
                    decCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return idExpedicion;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Lista las partidas guardadas en curso para poder cargarlas al reiniciar el juego.
        /// </summary>
        public List<SavedExpeditionInfo> ObtenerExpedicionesEnCurso()
        {
            var lista = new List<SavedExpeditionInfo>();
            using SqlConnection conn = GetConnection();
            conn.Open();
            EnsureExplorationStateColumn(conn);

            const string sql = @"
                SELECT e.IdExpedicion, c.NombreCadete, e.NombreRanura, e.EtapaActual, e.FechaUltimoGuardado,
                       n.Oxigeno, n.IntegridadCasco, n.DatosRecuperados,
                       n.EscaneoOrbital, n.DescensoDirecto, n.SenalConfiable, n.SondaEnviada,
                       n.EstadoExploracionJson
                FROM dbo.Expediciones e
                INNER JOIN dbo.Cadetes c ON e.IdCadete = c.IdCadete
                INNER JOIN dbo.EstadoNave n ON e.IdExpedicion = n.IdExpedicion
                WHERE e.EstadoExpedicion = 'EnCurso'
                ORDER BY e.FechaUltimoGuardado DESC;";

            using (SqlCommand cmd = new(sql, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    lista.Add(new SavedExpeditionInfo
                    {
                        IdExpedicion = reader.GetInt32(0),
                        NombreCadete = reader.GetString(1),
                        NombreRanura = reader.GetString(2),
                        EtapaActual = reader.GetString(3),
                        FechaUltimoGuardado = reader.GetDateTime(4),
                        Oxigeno = reader.GetInt32(5),
                        IntegridadCasco = reader.GetInt32(6),
                        DatosRecuperados = reader.GetInt32(7),
                        EscaneoOrbital = reader.GetBoolean(8),
                        DescensoDirecto = reader.GetBoolean(9),
                        SenalConfiable = reader.GetBoolean(10),
                        SondaEnviada = reader.GetBoolean(11),
                        EstadoExploracionJson = reader.IsDBNull(12) ? null : reader.GetString(12)
                    });
                }
            }

            foreach (SavedExpeditionInfo exp in lista)
            {
                const string decSql = @"
                    SELECT OpcionElegida
                    FROM dbo.DecisionesExpedicion
                    WHERE IdExpedicion = @IdExpedicion
                    ORDER BY NumeroPaso ASC;";

                using SqlCommand decCmd = new(decSql, conn);
                decCmd.Parameters.AddWithValue("@IdExpedicion", exp.IdExpedicion);
                using SqlDataReader decReader = decCmd.ExecuteReader();
                while (decReader.Read())
                {
                    exp.Decisiones.Add(decReader.GetString(0));
                }
            }

            return lista;
        }

        /// <summary>
        /// Guarda una misión finalizada en BitacoraMisiones (reemplaza al JSON).
        /// </summary>
        public void GuardarResultadoMision(MissionResult result, int? idExpedicion = null, string nombreCadete = "Cadete Vance")
        {
            int idCadete = ObtenerOCrearCadete(nombreCadete);

            using SqlConnection conn = GetConnection();
            conn.Open();
            using SqlTransaction tx = conn.BeginTransaction();

            try
            {
                const string insertBitacoraSql = @"
                    INSERT INTO dbo.BitacoraMisiones
                    (IdExpedicion, NombreMision, Desenlace, Resumen, PuntuacionTotal, OxigenoFinal, CascoFinal, DatosFinales, FechaFinalizacion)
                    VALUES
                    (@IdExpedicion, @NombreMision, @Desenlace, @Resumen, @Puntuacion, @Oxigeno, @Casco, @Datos, @Fecha);";

                using (SqlCommand cmd = new(insertBitacoraSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@IdExpedicion", (object?)idExpedicion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NombreMision", result.MissionName);
                    cmd.Parameters.AddWithValue("@Desenlace", result.Outcome);
                    cmd.Parameters.AddWithValue("@Resumen", result.Summary);
                    cmd.Parameters.AddWithValue("@Puntuacion", result.Score);
                    cmd.Parameters.AddWithValue("@Oxigeno", result.OxygenRemaining);
                    cmd.Parameters.AddWithValue("@Casco", result.HullIntegrity);
                    cmd.Parameters.AddWithValue("@Datos", result.DataRecovered);
                    cmd.Parameters.AddWithValue("@Fecha", result.CompletedAt);
                    cmd.ExecuteNonQuery();
                }

                if (idExpedicion.HasValue && idExpedicion.Value > 0)
                {
                    const string closeExpSql = @"
                        UPDATE dbo.Expediciones
                        SET EstadoExpedicion = 'Completada',
                            EtapaActual = 'Completed',
                            FechaUltimoGuardado = SYSDATETIME()
                        WHERE IdExpedicion = @IdExpedicion;";

                    using SqlCommand closeCmd = new(closeExpSql, conn, tx);
                    closeCmd.Parameters.AddWithValue("@IdExpedicion", idExpedicion.Value);
                    closeCmd.ExecuteNonQuery();
                }

                const string updateCadeteSql = @"
                    UPDATE dbo.Cadetes
                    SET ExpedicionesTotales = ExpedicionesTotales + 1,
                        MejorPuntuacion = CASE WHEN @Score > MejorPuntuacion THEN @Score ELSE MejorPuntuacion END
                    WHERE IdCadete = @IdCadete;";

                using (SqlCommand cadCmd = new(updateCadeteSql, conn, tx))
                {
                    cadCmd.Parameters.AddWithValue("@Score", result.Score);
                    cadCmd.Parameters.AddWithValue("@IdCadete", idCadete);
                    cadCmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Carga el historial de misiones completadas desde SQL Server.
        /// </summary>
        public List<MissionResult> ObtenerHistorialMisiones()
        {
            var resultados = new List<MissionResult>();
            using SqlConnection conn = GetConnection();
            conn.Open();

            const string sql = @"
                SELECT b.IdBitacora, b.NombreMision, b.FechaFinalizacion, b.Desenlace,
                       b.Resumen, b.PuntuacionTotal, b.OxigenoFinal, b.CascoFinal, b.DatosFinales,
                       b.IdExpedicion
                FROM dbo.BitacoraMisiones b
                ORDER BY b.FechaFinalizacion DESC;";

            var mapaExpediciones = new Dictionary<MissionResult, int?>();

            using (SqlCommand cmd = new(sql, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var item = new MissionResult
                    {
                        MissionName = reader.GetString(1),
                        CompletedAt = reader.GetDateTime(2),
                        Outcome = reader.GetString(3),
                        Summary = reader.GetString(4),
                        Score = reader.GetInt32(5),
                        OxygenRemaining = reader.GetInt32(6),
                        HullIntegrity = reader.GetInt32(7),
                        DataRecovered = reader.GetInt32(8)
                    };
                    int? idExp = reader.IsDBNull(9) ? null : reader.GetInt32(9);
                    resultados.Add(item);
                    mapaExpediciones[item] = idExp;
                }
            }

            foreach (var par in mapaExpediciones)
            {
                if (!par.Value.HasValue) continue;

                const string decSql = @"
                    SELECT OpcionElegida
                    FROM dbo.DecisionesExpedicion
                    WHERE IdExpedicion = @IdExpedicion
                    ORDER BY NumeroPaso ASC;";

                using SqlCommand decCmd = new(decSql, conn);
                decCmd.Parameters.AddWithValue("@IdExpedicion", par.Value.Value);
                using SqlDataReader decReader = decCmd.ExecuteReader();
                while (decReader.Read())
                {
                    par.Key.Decisions.Add(decReader.GetString(0));
                }
            }

            return resultados;
        }
    }
}
