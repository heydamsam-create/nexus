# Análisis técnico de NEXUS

## Alcance y proyectos

El repositorio contiene dos proyectos C#:

- `NEXUS_WindowsForms/NEXUS_WindowsForms.csproj`: aplicación principal de escritorio con `OutputType=WinExe`, `TargetFramework=net10.0-windows`, Windows Forms habilitado, nullable e implicit usings.
- `nexus1.2mrj.csproj`: prototipo de consola que apunta a `net10.0` y compila el `Program.cs` de la raíz, excluyendo fuentes bajo `NEXUS_WindowsForms`.

`NEXUS.slnx` referencia el proyecto Windows Forms. Existen también `nexus_v2.slnx` y `nexus_v2.slnx..slnx`; no son las soluciones indicadas como entrada principal en este documento. La aplicación gráfica se inicia en `NEXUS_WindowsForms/Program.cs` mediante `Application.Run(new NexusApplicationContext())`.

## Lenguaje y herramientas

- C# y .NET 10.
- Windows Forms para la interfaz gráfica.
- `System.Text.Json` para el historial local.
- Visual Studio con Desarrollo de escritorio de .NET, o SDK de .NET 10 para compilar.
- Git/GitHub para control de versiones y distribución del código.

No aparecen referencias de paquetes NuGet, conexión SQL, Entity Framework Core ni servicios de red en el archivo de proyecto Windows Forms revisado.

## Funcionalidades verificadas en código

- Cuatro formularios: menú (`Form1`), misión (`FormMision`), enciclopedia (`FormInformacion`) e historial (`FormRegistros`).
- Una instancia de cada formulario gestionada durante la sesión por `NexusApplicationContext`; eventos para navegar, volver y salir.
- Misión narrativa de varias etapas implementada en `MissionGame`, con opciones, cambios limitados de oxígeno/casco/datos y desenlaces según decisión y umbrales.
- Resultado con ID GUID, nombre, fecha, desenlace, resumen, puntuación, recursos finales y decisiones.
- Guardado automático JSON de resultados finalizados en `%LOCALAPPDATA%\NEXUS\mission-history.json`, detección de duplicados por ID y manejo de errores de lectura/escritura.
- Historial consultable con selección de un registro y visualización de detalles; estado explícito cuando está vacío o no puede leerse.
- Enciclopedia navegable alimentada por `NexusEncyclopedia`; algunos textos identifican su contenido como marco narrativo propuesto.
- Recursos gráficos bajo `NEXUS_WindowsForms/Resources/` y recursos integrados desde `Properties/Resources.resx`.

El código de consola implementa un prototipo previo con clases de personaje, inventario, objetos, zonas y una misión diferente. No es el motor utilizado por `FormMision`.

## Organización

La lógica de navegación está en `NexusApplicationContext.cs`; la mecánica de misión se separa en `Services/MissionGame.cs`; persistencia en `Services/MissionHistoryStore.cs`; el resultado está en `Models/MissionResult.cs`; los textos de enciclopedia están en `Content/NexusEncyclopedia.cs`. Los formularios secundarios crean buena parte de sus controles desde sus archivos `.cs`. Los `.Designer.cs` y `.resx` existentes forman parte del proyecto y no se describen aquí como modificados.

## Limitaciones actuales

- Windows Forms requiere Windows para ejecutarse; el destino es `net10.0-windows`.
- No se guardan partidas en curso y no hay función de continuar/reanudar una misión.
- El JSON se almacena en el perfil local; no hay sincronización, cuentas ni persistencia compartida.
- La misión es una secuencia fija de etapas, no un mapa libre, combate, inventario o sistema ampliado de misiones.
- El prototipo de consola y la aplicación gráfica tienen modelos separados; no comparten una capa de dominio integrada.
- No se encontró un proyecto de pruebas automatizadas entre los archivos de proyecto del repositorio.
- Las rutas y compatibilidad de las soluciones antiguas no se documentan como comprobadas; la solución principal examinada es `NEXUS.slnx`.

## Compilación y ejecución

Desde la carpeta raíz:

```powershell
dotnet build NEXUS.slnx
```

Para ejecutar la interfaz, abrir `NEXUS.slnx` o el `.csproj` Windows Forms en Visual Studio, seleccionar `NEXUS_WindowsForms` como proyecto de inicio y ejecutar en Windows. La presencia de archivos compilados en `bin/` u `obj/` no sustituye una compilación reciente ni confirma que la aplicación se haya probado visualmente.

## Elementos fuera de alcance

El diseño SQL descrito en documentos previos es una propuesta y no una tecnología conectada. La expansión de la cronología, los antecedentes institucionales de NEXUS y las biografías de la tripulación son temas narrativos abiertos. La aplicación no implementa las funciones del prototipo original como parte de su misión Windows Forms.
