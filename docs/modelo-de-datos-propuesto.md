# Modelo de resultado e historial JSON

## Estado

El proyecto Windows Forms ya implementa el guardado local de resultados finalizados mediante JSON. El esquema SQL descrito en versiones anteriores de este documento era una propuesta para partidas completas; no está conectado y no forma parte del modelo actual. Esta página documenta lo que el código implementa hoy.

## `MissionResult`

`NEXUS_WindowsForms/Models/MissionResult.cs` declara las propiedades públicas que `System.Text.Json` serializa:

| Propiedad | Tipo | Uso |
|---|---|---|
| `Id` | `Guid` | Identificador único del resultado; se inicializa con `Guid.NewGuid()`. El almacén lo usa para evitar duplicados. |
| `MissionName` | `string` | Nombre de la misión finalizada. |
| `CompletedAt` | `DateTime` | Fecha y hora local de finalización (`DateTime.Now`). |
| `Outcome` | `string` | Etiqueta del desenlace. |
| `Summary` | `string` | Texto que resume el desenlace. |
| `Score` | `int` | Suma de oxígeno, integridad del casco y datos recuperados al finalizar. |
| `OxygenRemaining` | `int` | Oxígeno final, limitado entre 0 y 100. |
| `HullIntegrity` | `int` | Integridad final del casco, limitada entre 0 y 100. |
| `DataRecovered` | `int` | Datos finales recuperados, limitados entre 0 y 100. |
| `Decisions` | `List<string>` | Títulos de las opciones elegidas durante la misión. |

Ejemplo representativo de un elemento JSON (los valores dependen de la partida):

```json
{
  "Id": "71c8d267-4f44-4a28-8be1-3374d315da40",
  "MissionName": "Operación Umbral de Eos",
  "CompletedAt": "2026-11-18T12:00:00",
  "Outcome": "RECOLECCIÓN EXITOSA",
  "Summary": "Resumen del desenlace.",
  "Score": 264,
  "OxygenRemaining": 83,
  "HullIntegrity": 95,
  "DataRecovered": 86,
  "Decisions": ["INICIAR EXPLORACIÓN", "ESCANEAR DESDE ÓRBITA"]
}
```

El identificador del ejemplo es ilustrativo; en ejecución se genera un GUID nuevo. El archivo contiene una lista JSON de resultados, no un objeto individual.

## Flujo de guardado y consulta

1. `MissionGame` crea un resultado cuando se toma una decisión final.
2. `FormMision` asigna `LastResult` y emite el evento `MissionCompleted`.
3. `NexusApplicationContext` entrega el resultado a `MissionHistoryStore.TrySave`.
4. El almacén lee la lista existente, comprueba si ya existe el mismo `Id`, añade el resultado, crea el directorio si hace falta y escribe JSON indentado en un archivo temporal antes de reemplazar el archivo de destino.
5. Si la lectura o escritura falla, el almacén devuelve un mensaje de error y el contexto lo presenta en un cuadro de diálogo.
6. Al abrir el formulario de registros, `FormRegistros` vuelve a cargar el archivo y muestra fecha, desenlace, puntuación y detalles del resultado.

Los errores al cargar se muestran en el formulario. Si el archivo todavía no existe o está vacío, la aplicación muestra el estado de historial vacío. La lectura descarta entradas nulas y, en memoria, elimina IDs repetidos; asigna un ID a resultados antiguos que tengan `Guid.Empty` y ordena por fecha descendente. La escritura se evita para resultados cuyo `Id` ya está guardado.

## Ubicación

El almacén usa `Environment.SpecialFolder.LocalApplicationData` y construye la ruta:

```text
%LOCALAPPDATA%\NEXUS\mission-history.json
```

La ruta efectiva depende del perfil de Windows con el que se ejecuta la aplicación. Los datos no se sincronizan entre equipos ni se guardan dentro del repositorio.

## Alcance y limitaciones

- Se guardan resultados terminados, no el estado de una misión en curso.
- No hay perfiles de jugador, identificador de partida, inventario ni progreso reanudable en este JSON.
- El identificador deduplica una misma instancia de resultado; repetir la misión crea otro resultado con otro `Id`.
- El bloqueo implementado coordina operaciones sobre la instancia del almacén utilizada por la aplicación; no se documenta como bloqueo entre procesos.
- SQL Server, ADO.NET y Entity Framework Core no están implementados ni configurados en los proyectos actuales.

## Modelo SQL anterior

El antiguo esquema relacional de Jugador, Partida, EstadoCadete, ProgresoMision, Inventario y zonas puede servir como idea futura para guardar partidas completas. No debe interpretarse como una base creada, un requisito de ejecución o una característica disponible en NEXUS hoy.
