# Análisis técnico de NEXUS

## Proyecto principal y herramientas

- `NEXUS_WindowsForms/NEXUS_WindowsForms.csproj`: aplicación C# Windows Forms con `OutputType=WinExe`, `TargetFramework=net10.0-windows`, nullable e implicit usings.
- `NEXUS.slnx`: solución activa, con referencia al proyecto Windows Forms.
- `nexus1.2mrj.csproj` y `Program.cs` en la raíz: prototipo de consola legado, independiente de la solución principal.
- Las soluciones antiguas se conservan, pero sus rutas de proyecto apuntan a ubicaciones que no existen en el repositorio actual. No deben usarse para abrir la aplicación principal.

La aplicación gráfica se inicia desde `NEXUS_WindowsForms/Program.cs` con `Application.Run(new NexusApplicationContext())`.

## Dependencias

El proyecto declara un paquete NuGet directo:

| Paquete | Versión | Uso |
|---|---:|---|
| `Microsoft.Data.SqlClient` | 7.1.2 | Conexión ADO.NET con SQL Server. |

NuGet restaura las dependencias transitivas durante `dotnet restore`. No se utiliza Entity Framework Core ni un motor externo de videojuegos. La exploración 2D y sus gráficos se implementan en el proyecto mediante Windows Forms y GDI+.

## Funcionalidades verificadas en el código

- Cuatro pantallas conectadas: menú, misión/exploración, enciclopedia e historial.
- Mapa 2D con seis zonas, puertas, colisiones, cámara y minimapa.
- Movimiento mediante WASD o flechas; interacción con `E`; inventario con `I`; registro de objetivos con `J`.
- Coleccionables, documentos, terminales, diálogos ramificados, decisiones, objetivos, eventos y desafíos de secuencia.
- Enemigos que detectan y persiguen al jugador, reciben daño y atacan; disparo con clic izquierdo o botón en pantalla.
- Recursos de oxígeno, integridad del casco y datos recuperados, con efectos de algunas acciones y desafíos.
- Resultado de misión con desenlace, resumen, puntuación, recursos finales y decisiones.
- Ventana maximizada y modo pantalla completa con `F11`; `Esc` devuelve al modo ventana.

## SQL Server y configuración

`DatabaseService` crea conexiones con `Microsoft.Data.SqlClient`. Lee primero la variable de entorno `NEXUS_SQL_CONNECTION_STRING`; si no está definida, usa:

```text
Server=localhost\SQLEXPRESS;Database=NexusDB;Trusted_Connection=True;TrustServerCertificate=True;
```

Esta instancia predeterminada pertenece al equipo de desarrollo. En otra computadora hay que definir la variable con la instancia, base y autenticación disponibles allí. La cuenta de Windows o el usuario SQL debe tener permisos sobre la base; además, la primera inicialización del esquema requiere permisos para crear/alterar objetos.

El esquema se crea mediante `database/01_CreateDatabase.sql`; `database/02_AddExplorationSave.sql` agrega `EstadoExploracionJson`. El script inicial elimina y recrea las tablas existentes: solo debe usarse al preparar una base nueva o vacía. El servicio puede agregar la columna de exploración si no existe.

## Persistencia

Las partidas en curso se guardan en SQL Server en `Expediciones`, `EstadoNave` y `DecisionesExpedicion`. `EstadoNave` guarda etapa/recursos y una columna JSON (`EstadoExploracionJson`) para el estado detallado del mundo, incluida posición, inventario, habitaciones descubiertas, puertas, objetivos, desafíos, eventos, diálogos y enemigos. Se puede guardar con el botón de la misión; la actividad programa un guardado automático tras 1,2 segundos sin nuevos cambios. Las expediciones en curso se pueden seleccionar y cargar.

Al terminar una misión, `DatabaseService` inserta el resultado en `BitacoraMisiones` y marca la expedición vinculada como completada. La vista `FormRegistros` consulta esa tabla y carga decisiones vinculadas desde `DecisionesExpedicion`. El evento `MissionCompleted` también escribe una copia JSON local mediante `MissionHistoryStore` en `%LOCALAPPDATA%\NEXUS\mission-history.json`; ese archivo no es la fuente que consulta `FormRegistros`.

## Requisitos para clonar y ejecutar

- Windows con soporte para Windows Forms.
- .NET 10 SDK para compilar desde CLI, o Visual Studio con soporte .NET 10 y la carga **Desarrollo de escritorio de .NET**.
- SQL Server accesible para guardar/cargar partidas y ver el historial; SSMS es opcional para ejecutar scripts.
- Acceso a NuGet durante la primera restauración del paquete.

Desde la raíz del repositorio:

```powershell
git clone <URL-del-repositorio>
cd <carpeta-del-repositorio>
dotnet restore NEXUS.slnx
dotnet build NEXUS.slnx
dotnet run --project NEXUS_WindowsForms/NEXUS_WindowsForms.csproj
```

En Visual Studio, abre `NEXUS.slnx`, restaura paquetes si se solicita, confirma `NEXUS_WindowsForms` como proyecto de inicio y ejecuta con F5. Para configurar SQL desde PowerShell antes de iniciar la aplicación:

```powershell
$env:NEXUS_SQL_CONNECTION_STRING = 'Server=localhost\SQLEXPRESS;Database=NexusDB;Trusted_Connection=True;TrustServerCertificate=True;'
```

No guardes contraseñas ni cadenas privadas dentro del repositorio.

## Límites conocidos

- La aplicación depende de Windows Forms y está dirigida a `net10.0-windows`.
- No se verificó desde esta documentación que SQL Server esté instalado o accesible en otra computadora; hay que probar la conexión allí.
- El historial visible usa SQL Server; la copia JSON local no está integrada como fuente de recuperación para `FormRegistros`. Si falla el guardado SQL de un resultado, puede quedar la copia JSON sin que la pantalla de registros lo muestre.
- El prototipo de consola no forma parte de `NEXUS.slnx`; las soluciones antiguas contienen referencias rotas.
- No hay un proyecto de pruebas automatizadas incluido en la solución principal.
