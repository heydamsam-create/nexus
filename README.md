# NEXUS — Exploración de Eridani-9

NEXUS es un videojuego académico de exploración espacial desarrollado en C# con Windows Forms y .NET 10. El proyecto principal es `NEXUS_WindowsForms` y se abre desde `NEXUS.slnx`. El jugador explora una estación abandonada en Eridani-9, investiga una anomalía y toma decisiones que influyen en el desenlace de la expedición.

## Funcionalidades actuales

- Menú principal y navegación entre misión, enciclopedia e historial.
- Exploración 2D desde arriba de seis zonas conectadas, con movimiento por teclado, cámara y minimapa.
- Puertas con requisitos, objetos coleccionables e inventario.
- Interacciones con terminales, documentos, diálogos y decisiones narrativas.
- Eventos y desafíos de secuencia, con efectos sobre oxígeno, integridad del casco y datos.
- Enemigos que detectan y persiguen al jugador, ataques, disparo con mouse o botón y daño a la integridad del casco.
- Guardado y carga de expediciones en curso en SQL Server, además de consulta de resultados completados.
- Cambio entre ventana y pantalla completa con F11; Esc vuelve al modo ventana.

## Tecnologías y dependencias

- C# y .NET 10 (`net10.0-windows`).
- Windows Forms.
- Paquete NuGet directo: `Microsoft.Data.SqlClient` 7.1.2. Las dependencias transitivas se restauran automáticamente desde NuGet.
- SQL Server para guardar y cargar partidas, y consultar los resultados completados.

No se utiliza un motor de videojuegos externo. Los gráficos del escenario se dibujan con Windows Forms/GDI+ y los recursos del proyecto.

## Requisitos para otra computadora

- Windows compatible con Windows Forms.
- Visual Studio con soporte para .NET 10 y la carga de trabajo **Desarrollo de escritorio de .NET**, o el SDK de .NET 10 para compilar por línea de comandos.
- SQL Server (por ejemplo, SQL Server Express o Developer) accesible desde la computadora. La persistencia y el historial requieren una base de datos configurada.
- SQL Server Management Studio (SSMS) es opcional; puede utilizarse para ejecutar los scripts y revisar la base de datos.
- Acceso a NuGet durante la primera restauración del paquete, salvo que este ya esté en caché.

Sin SQL Server accesible, el inicio del juego puede mostrar un error de lectura del historial y las funciones de guardar, cargar y consultar registros no estarán disponibles correctamente.

## Obtener, abrir y ejecutar

Desde PowerShell:

```powershell
git clone <URL-del-repositorio>
cd <carpeta-del-repositorio>
dotnet restore NEXUS.slnx
dotnet build NEXUS.slnx
dotnet run --project NEXUS_WindowsForms/NEXUS_WindowsForms.csproj
```

En Visual Studio:

1. Clona o descarga el repositorio.
2. Abre `NEXUS.slnx` desde la carpeta raíz.
3. Si Visual Studio lo solicita, restaura los paquetes NuGet.
4. Confirma que `NEXUS_WindowsForms` sea el proyecto de inicio.
5. Ejecuta con **F5**.

`NEXUS.slnx` contiene solamente el proyecto Windows Forms actual. El prototipo de consola `nexus1.2mrj.csproj` se conserva aparte y no forma parte de esa solución. Las soluciones antiguas también se conservan, pero sus referencias no corresponden a las rutas actuales; utiliza `NEXUS.slnx`.

## Configurar SQL Server

El proyecto utiliza `Microsoft.Data.SqlClient` y una cadena de conexión configurada en `NEXUS_WindowsForms/Services/DatabaseService.cs`. Primero lee la variable de entorno `NEXUS_SQL_CONNECTION_STRING`. Si no existe, usa `localhost\SQLEXPRESS` como instancia local predeterminada:

```text
Server=localhost\SQLEXPRESS;Database=NexusDB;Trusted_Connection=True;TrustServerCertificate=True;
```

Ese servidor predeterminado es específico de la computadora de desarrollo y puede no existir en otro equipo. Define la variable antes de iniciar NEXUS, ajustando servidor, instancia y autenticación. Ejemplo temporal para la sesión actual de PowerShell con autenticación integrada de Windows:

```powershell
$env:NEXUS_SQL_CONNECTION_STRING = 'Server=localhost\SQLEXPRESS;Database=NexusDB;Trusted_Connection=True;TrustServerCertificate=True;'
```

La cuenta de Windows que ejecuta NEXUS debe tener permisos sobre `NexusDB`. Si se utiliza autenticación SQL, configura la cadena correspondiente fuera del repositorio y no guardes credenciales en el código ni en Git.

Los scripts están en `database/`:

1. `database/01_CreateDatabase.sql` crea la base y el esquema inicial.
2. `database/02_AddExplorationSave.sql` agrega la columna del estado serializado de exploración, si falta.

**Advertencia:** el script `01_CreateDatabase.sql` elimina y vuelve a crear las tablas si ya existen. Ejecútalo únicamente para preparar una base nueva o vacía; no lo vuelvas a ejecutar sobre una base con datos que quieras conservar. El código también crea la columna `EstadoExploracionJson` cuando detecta que falta, pero la cuenta SQL necesita permiso para alterar el esquema.

## Guardado y carga

- El botón **GUARDAR PARTIDA** guarda la etapa, decisiones, recursos de la nave y el estado detallado de exploración en SQL Server. La actividad del juego programa un guardado automático después de 1,2 segundos sin nuevos cambios; al volver desde la misión también intenta guardar el progreso.
- El botón **CARGAR PARTIDA** muestra las expediciones que siguen en estado `EnCurso` y restaura la etapa, oxígeno, integridad del casco, datos, decisiones y estado del mapa.
- El estado del mapa se serializa como JSON en `dbo.EstadoNave.EstadoExploracionJson`. Incluye posición, objetos, zonas descubiertas, puertas, objetivos, eventos, desafíos, diálogos y estado de los enemigos.
- Al completar la misión, se escribe un resultado en `dbo.BitacoraMisiones`; el formulario **REGISTROS** consulta ese historial de SQL Server.
- El contexto de la aplicación también conserva una copia local JSON de los resultados en `%LOCALAPPDATA%\NEXUS\mission-history.json`. La pantalla de registros actual consulta SQL Server, no ese archivo JSON.
- Si falla el guardado SQL al terminar una misión, el evento de finalización aún intenta escribir la copia JSON local; como **REGISTROS** consulta SQL, ese resultado no aparecerá allí hasta que se haya registrado correctamente en la base.

## Estructura principal

```text
NEXUS.slnx
NEXUS_WindowsForms/
  Content/       Narrativa y catálogo de desafíos/eventos
  Controls/      Vista y lógica de exploración 2D
  Models/        Estado de exploración y resultados
  Properties/    Recursos integrados
  Rendering/     Sprite del explorador
  Resources/     Imágenes de interfaz
  Services/      Misión y persistencia SQL/JSON
database/        Creación y actualización del esquema SQL
docs/            Documentación técnica y de diseño
evidencias/      Capturas y material del proyecto
```

El repositorio mantiene además el prototipo de consola y soluciones antiguas. Estos no son necesarios para abrir la aplicación principal.

## Controles de exploración

- `W`, `A`, `S`, `D` o flechas: moverse.
- `E`: interactuar con objetos y terminales.
- `I`: abrir/cerrar inventario; dentro del inventario, flechas o `W`/`S` seleccionan y `E`/Enter usa el elemento seleccionado.
- `J`: abrir/cerrar el registro de objetivos.
- `1`–`4`: responder a desafíos; durante diálogos, `1`/`2` seleccionan opciones.
- Clic izquierdo o botón **DISPARAR**: disparar.
- `F11`: alternar pantalla completa; `Esc`: volver al modo ventana.

## Documentación

- [Arquitectura actual](docs/arquitectura-actual.md)
- [Diseño del juego](docs/diseno-del-juego.md)
- [Modelo de datos actual](docs/modelo-de-datos-propuesto.md)
- [Análisis técnico](docs/analisis-tecnico.md)
- [Índice de documentos](docs/README.md)

`docs/arquitectura-propuesta.md` conserva una propuesta de arquitectura y no describe la organización implementada.

## Autoría

Proyecto académico NEXUS — Exploración de Eridani-9, desarrollado como práctica de programación con C#, Windows Forms y SQL Server.
