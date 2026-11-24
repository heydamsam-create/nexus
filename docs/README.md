# Documentación de NEXUS

La aplicación principal documentada aquí es `NEXUS_WindowsForms`, C# con Windows Forms y .NET 10, abierta desde `NEXUS.slnx`.

## Guías actuales

- [README principal](../README.md): requisitos, SQL Server, configuración de conexión, compilación y ejecución.
- [Arquitectura actual](arquitectura-actual.md): formularios, navegación, exploración y persistencia implementada.
- [Análisis técnico](analisis-tecnico.md): paquetes, requisitos, capacidades y límites actuales.
- [Diseño del juego](diseno-del-juego.md): controles, mapa, narrativa y sistemas jugables.
- [Modelo de datos](modelo-de-datos-propuesto.md): esquema SQL, partidas guardadas y resultados.

## Documentos de propuesta y material académico

- [Arquitectura propuesta](arquitectura-propuesta.md) conserva una propuesta previa y no describe la estructura actual del proyecto.
- Los manuales, diagramas, presentaciones y capturas de esta carpeta son material académico/histórico complementario. Para instalar y ejecutar la versión actual, sigue el README principal.

Los scripts SQL de la aplicación se encuentran en `../database/`. Antes de ejecutar el script de creación, consulta la advertencia del README: recrea las tablas y puede borrar datos existentes.
