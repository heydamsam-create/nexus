# Modelo de datos actual: expediciones y resultados

> El nombre histórico del archivo contiene “propuesto”, pero la aplicación actual sí implementa persistencia SQL Server. Esta página describe el código y los scripts presentes; no es un diseño futuro.

## SQL Server

`DatabaseService` usa ADO.NET con `Microsoft.Data.SqlClient`. El esquema definido en `database/01_CreateDatabase.sql` contiene:

| Tabla | Datos que almacena |
|---|---|
| `Cadetes` | Nombre, rango, expediciones completadas y mejor puntuación. |
| `Expediciones` | Ranura, sector, etapa, estado (`EnCurso`/`Completada`/`Abandonada`) y fechas. |
| `EstadoNave` | Oxígeno, integridad del casco, datos, banderas de ruta y `EstadoExploracionJson`. |
| `DecisionesExpedicion` | Decisiones de una expedición, ordenadas por número de paso. |
| `BitacoraMisiones` | Resultado final: misión, desenlace, resumen, puntuación, recursos y fecha. Puede vincularse a una expedición. |

`database/02_AddExplorationSave.sql` agrega la columna nullable `EstadoExploracionJson NVARCHAR(MAX)` si no existe. `DatabaseService` también comprueba esa columna y la agrega durante operaciones de guardado/carga si falta.

## `MissionResult`

`NEXUS_WindowsForms/Models/MissionResult.cs` representa el resultado en memoria:

| Propiedad | Tipo | Uso actual |
|---|---|---|
| `Id` | `Guid` | Identifica la copia JSON local; no es la clave primaria de `BitacoraMisiones`. |
| `MissionName` | `string` | Nombre de la misión. |
| `CompletedAt` | `DateTime` | Fecha y hora de finalización. |
| `Outcome` | `string` | Desenlace calculado por `MissionGame`. |
| `Summary` | `string` | Descripción textual del desenlace. |
| `Score` | `int` | Puntuación total calculada al finalizar. |
| `OxygenRemaining` | `int` | Oxígeno final. |
| `HullIntegrity` | `int` | Integridad final del casco. |
| `DataRecovered` | `int` | Datos finales recuperados. |
| `Decisions` | `List<string>` | Opciones elegidas durante la misión. |

En SQL, `BitacoraMisiones.IdBitacora` es una clave `INT IDENTITY`; el GUID de `MissionResult` no se copia a esa columna. Las decisiones se guardan en `DecisionesExpedicion` y se consultan asociadas al ID de expedición cuando existe.

## Estado serializado de exploración

`ExplorationSaveState` se serializa con `System.Text.Json` y se almacena en `EstadoNave.EstadoExploracionJson`. Sus campos incluyen:

- Posición del jugador (`PlayerX`, `PlayerY`).
- Objetos recogidos/usados, documentos leídos y secretos encontrados.
- Habitaciones exploradas, puertas desbloqueadas y objetivos completados.
- Diálogos vistos/en curso y decisiones narrativas.
- Eventos disparados/activos/completados, relojes de eventos y desafíos completados/en curso.
- Posición, salud, alerta y estado de eliminación de enemigos.

Los recursos principales (oxígeno, integridad y datos), la etapa de la expedición y algunas banderas narrativas se guardan en columnas SQL aparte. Al cargar, `FormMision` restaura los campos de SQL y deserializa el estado JSON. Si el JSON de exploración no se puede leer, el formulario avisa e inicia esa parte del mapa con un estado nuevo.

## Flujo de guardado y carga de partida

1. El jugador puede guardar con **GUARDAR PARTIDA**; la actividad programa un guardado automático cuando transcurren 1,2 segundos sin nuevos cambios.
2. `FormMision` llama `DatabaseService.GuardarExpedicionEnCurso` con la etapa, recursos, decisiones y JSON del mapa.
3. `DatabaseService` crea o actualiza `Expediciones` y `EstadoNave`, sincroniza `DecisionesExpedicion` y confirma la operación con una transacción.
4. **CARGAR PARTIDA** lista expediciones `EnCurso` de SQL Server.
5. La selección restaura recursos, decisiones, etapa y estado de exploración.

## Historial de misiones completadas

Al terminar, `DatabaseService.GuardarResultadoMision` inserta el resultado en `BitacoraMisiones` y, si hay ID de expedición, marca esa expedición como completada. `FormRegistros` obtiene el historial mediante `DatabaseService.ObtenerHistorialMisiones`; por ello, necesita conexión SQL para mostrar registros.

El evento `MissionCompleted` también entrega el `MissionResult` a `MissionHistoryStore`, que escribe una copia local JSON en:

```text
%LOCALAPPDATA%\NEXUS\mission-history.json
```

El formulario `FormRegistros` no usa actualmente ese archivo como fuente de historial. SQL Server es la fuente que muestra la interfaz de registros. Si falla la escritura SQL al finalizar, puede quedar la copia JSON local sin que el resultado aparezca en la pantalla de registros.

## Configuración y preparación de base

La conexión se obtiene primero de la variable de entorno `NEXUS_SQL_CONNECTION_STRING`. Si no está definida, el código usa `localhost\SQLEXPRESS` con la base `NexusDB` y autenticación integrada de Windows. Si el servidor o la instancia tienen otro nombre, configura la variable con el destino correcto y concede permisos a la cuenta que ejecutará la aplicación.

Para inicializar una base nueva, se ejecutan los scripts en orden: `database/01_CreateDatabase.sql` y después `database/02_AddExplorationSave.sql`. **El primer script elimina y recrea tablas si ya existen; no debe ejecutarse sobre una base con datos que se quieran conservar.** La cuenta de SQL necesita permisos de creación y alteración durante la instalación/migración, y permisos de lectura/escritura para el uso normal.

## Límites y compatibilidad

- Las partidas se guardan en SQL Server; no se guardan dentro del repositorio.
- La copia local JSON de resultados y el historial SQL son dos persistencias distintas; el formulario de registros consulta únicamente SQL.
- Partidas antiguas sin `EstadoExploracionJson` reciben la columna nullable, y al cargarse pueden inicializar el estado de mapa con valores predeterminados.
- La migración añade una columna, no recrea las tablas ni borra los registros existentes.
