# NEXUS — Exploración de Eridani-9

NEXUS es un proyecto académico desarrollado en C# que presenta una experiencia interactiva de exploración espacial. Su versión principal utiliza Windows Forms y está ambientada en el año 2297, durante una expedición al planeta Eridani-9.

El jugador forma parte de una misión espacial en la que debe explorar instalaciones, investigar una anomalía, tomar decisiones y administrar los recursos de la nave.

## Objetivo del proyecto

Aplicar los conocimientos de programación en C# mediante el desarrollo de un videojuego interactivo que integre interfaces gráficas, lógica de programación, persistencia de información y una base de datos relacional.

## Funcionalidades implementadas

### 1. Interfaz y navegación
- Menú principal.
- Navegación entre las pantallas del juego.
- Interfaz de misión y exploración.
- Enciclopedia del universo NEXUS.
- Historial de misiones.

### 2. Exploración espacial
- Movimiento del personaje mediante teclado.
- Seis habitaciones conectadas.
- Puertas, objetos y elementos interactivos.
- Inventario y objetos necesarios para avanzar.
- Mapa de exploración y objetivos.

### 3. Sistema narrativo
- Historia ambientada en el año 2297.
- Asistente de inteligencia artificial ficticio AURA.
- Diálogos y documentos narrativos.
- Decisiones que afectan el desarrollo de la misión.
- Diferentes desenlaces.

### 4. Eventos y desafíos
- Eventos dinámicos durante la exploración.
- Desafíos interactivos.
- Secuencias que deben resolverse.
- Alertas del sistema AURA.
- Cambios en los recursos y condiciones del entorno.
- Progresión condicionada por objetivos y objetos obtenidos.

### 5. Persistencia de datos
- Registro de expediciones.
- Almacenamiento del progreso de exploración.
- Registro de decisiones y resultados.
- Persistencia mediante SQL Server.
- Conservación del estado de la partida para continuar la exploración.

## Tecnologías utilizadas

- C#.
- .NET 10.
- Windows Forms.
- Microsoft SQL Server.
- SQL Server Management Studio (SSMS).
- Git y GitHub.
- Visual Studio.

## Base de datos — NexusDB

El proyecto integra Microsoft SQL Server para almacenar información relacionada con las expediciones y el progreso del juego.

Las tablas principales son:

- `Cadetes`
- `Expediciones`
- `EstadoNave`
- `DecisionesExpedicion`
- `BitacoraMisiones`

Los scripts SQL se encuentran en la carpeta `base de datos`.

## Requisitos

- Sistema operativo Windows.
- Visual Studio con la carga de trabajo Desarrollo de escritorio de .NET.
- SDK de .NET 10.
- Microsoft SQL Server.
- SQL Server Management Studio (SSMS).

## Instalación y ejecución

1. Descargar o clonar el repositorio.
2. Abrir la solución principal de NEXUS en Visual Studio.
3. Configurar SQL Server y ejecutar los scripts de creación y actualización de la base de datos.
4. Verificar la cadena de conexión en `NEXUS_WindowsForms/Services/DatabaseService.cs`.
5. Restaurar las dependencias del proyecto.
6. Seleccionar el proyecto Windows Forms como proyecto de inicio.
7. Ejecutar mediante F5.

## Estructura general del repositorio

- `NEXUS_WindowsForms/`: aplicación principal Windows Forms.
- `NEXUS_WindowsForms/Content/`: contenido narrativo y desafíos.
- `NEXUS_WindowsForms/Controls/`: controles de exploración.
- `NEXUS_WindowsForms/Models/`: modelos de datos.
- `NEXUS_WindowsForms/Rendering/`: elementos gráficos.
- `NEXUS_WindowsForms/Services/`: lógica de servicios y persistencia.
- `base de datos/`: scripts SQL.
- `documentos/`: documentación técnica.
- `evidencias/`: capturas y pruebas del proyecto.

El repositorio también conserva un prototipo anterior desarrollado en consola, separado de la aplicación gráfica principal.

## Resultado de la misión principal

La misión principal permite investigar la señal de Eridani-9 y tomar decisiones sobre el tratamiento de la anomalía.

En la partida de prueba documentada se consiguió contener la anomalía, recuperar datos y registrar el resultado de la expedición en SQL Server.

## Documentación complementaria

En la carpeta de documentación se incluyen materiales técnicos, de diseño y de base de datos. En la carpeta de evidencias se conservan capturas y resultados de las pruebas realizadas.

## Autoría

Proyecto académico NEXUS — Exploración de Eridani-9.

Desarrollado como práctica de programación con C#, Windows Forms y SQL Server.
