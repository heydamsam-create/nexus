# NEXUS — Exploración de Eridani-9

NEXUS es un proyecto académico de exploración espacial con dos implementaciones C# en el repositorio: la aplicación gráfica Windows Forms, que es el proyecto principal de esta documentación, y un prototipo anterior de consola. La aplicación gráfica presenta una misión narrativa ambientada en 2297, una enciclopedia del universo y un historial local de misiones finalizadas.

## Objetivo

Practicar programación con C# y desarrollar una experiencia interactiva de exploración espacial. La versión Windows Forms permite navegar entre pantallas, tomar decisiones durante una misión, observar cambios en los recursos y consultar resultados guardados.

## Funcionalidades implementadas

- Navegación entre el menú principal, la misión, la enciclopedia y el historial.
- Una misión interactiva en Eridani-9, con decisiones que modifican oxígeno, integridad del casco y datos recuperados.
- Varios desenlaces según la decisión final y los valores de recursos.
- Guardado automático de los resultados finalizados en JSON dentro del perfil local del usuario.
- Consulta del historial con fecha, desenlace, puntuación, recursos y decisiones.
- Enciclopedia navegable con datos establecidos y contenido narrativo marcado como propuesta.

El prototipo de consola en `Program.cs` y `nexus1.2mrj.csproj` permanece en el repositorio. No está incluido en la solución principal `NEXUS.slnx` y sus zonas/personajes no son mecánicas de la misión WinForms actual.

## Tecnologías

- C#.
- .NET 10 (`net10.0-windows` para Windows Forms).
- Windows Forms.
- `System.Text.Json` para serializar el historial local.
- Visual Studio y Git/GitHub como herramientas del proyecto.

No se configura SQL Server ni Entity Framework Core en el proyecto Windows Forms.

## Requisitos

- Windows, para ejecutar la aplicación Windows Forms.
- Visual Studio con la carga de trabajo **Desarrollo de escritorio de .NET**, o el SDK de .NET 10 instalado.
- Git, si se va a clonar el repositorio.

## Abrir y ejecutar desde GitHub en Visual Studio

1. En GitHub, usa **Code > Download ZIP** o clona el repositorio con Git.
2. Extrae el ZIP si corresponde.
3. Abre `NEXUS.slnx` desde Visual Studio. Esta solución contiene `NEXUS_WindowsForms/NEXUS_WindowsForms.csproj`.
4. Si tu instalación de Visual Studio no abre el formato `.slnx`, abre directamente `NEXUS_WindowsForms/NEXUS_WindowsForms.csproj`.
5. Espera a que Visual Studio restaure las dependencias del proyecto.
6. Selecciona `NEXUS_WindowsForms` como proyecto de inicio y pulsa **F5** (depuración) o **Ctrl+F5** (sin depuración).

También puedes compilar desde una terminal en la carpeta raíz del repositorio:

```powershell
dotnet build NEXUS.slnx
```

La compilación no ejecuta la aplicación; la ventana gráfica requiere Windows.

## Estructura del repositorio

```text
NEXUS.slnx                         Solución principal: solo el proyecto Windows Forms
NEXUS_WindowsForms/
  Program.cs                       Punto de entrada
  NexusApplicationContext.cs       Navegación y ciclo de vida de formularios
  Form1.cs                         Menú principal y eventos de navegación
  FormMision.cs                    Interfaz y presentación de la misión
  FormInformacion.cs               Interfaz de la enciclopedia
  FormRegistros.cs                 Interfaz del historial y sus detalles
  Services/
    MissionGame.cs                 Etapas, decisiones, recursos y desenlaces
    MissionHistoryStore.cs         Lectura y escritura del historial JSON
  Models/MissionResult.cs          Datos de un resultado finalizado
  Content/NexusEncyclopedia.cs     Secciones y textos de la enciclopedia
  NexusTheme.cs                    Colores y elementos de estilo compartidos
  Resources/                       Imágenes utilizadas por el proyecto
  Properties/Resources.resx        Recursos de imagen integrados
nexus1.2mrj.csproj                 Proyecto de consola anterior, fuera de NEXUS.slnx
Program.cs                         Código del prototipo de consola
docs/                              Documentación técnica y de diseño
evidencias/                        Material de diseño de referencia
```

Los archivos `Designer.cs` y `.resx` pertenecen al diseño y los recursos de los formularios; la lógica de pantallas generada dinámicamente está principalmente en sus archivos `.cs`.

## Historial de misiones

El archivo se crea al finalizar y guardar una misión en:

```text
%LOCALAPPDATA%\NEXUS\mission-history.json
```

Consulta [docs/modelo-de-datos-propuesto.md](docs/modelo-de-datos-propuesto.md) para el formato almacenado y [docs/arquitectura-actual.md](docs/arquitectura-actual.md) para la relación entre clases.

## Documentación

- [Arquitectura actual](docs/arquitectura-actual.md)
- [Diseño del juego](docs/diseno-del-juego.md)
- [Modelo de datos e historial](docs/modelo-de-datos-propuesto.md)
- [Análisis técnico y limitaciones](docs/analisis-tecnico.md)
- [Propuesta de arquitectura](docs/arquitectura-propuesta.md) — material de planificación, no descripción de componentes ya implementados.
