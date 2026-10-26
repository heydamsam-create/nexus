# Arquitectura actual de NEXUS (H3)

## 1. Alcance de la revisión

La revisión se realizó sobre el repositorio público `heydamsam-create/nexus`, rama `main`, en particular `Program.cs` y `nexus1.2mrj.csproj`. En GitHub, `Program.cs` aparece como un archivo grande con aproximadamente 1.183 líneas. La documentación pública del repositorio describe NEXUS v1.0 como una aventura en C# ambientada en 2297.

> Esta descripción refleja lo que puede verificarse en los archivos públicos consultados. Antes de implementar cambios, conviene compilar el proyecto localmente en Visual Studio y comprobar que no haya modificaciones sin subir.

## 2. Clases observadas en `Program.cs`

| Clase | Responsabilidad actual observada |
|---|---|
| `Personaje` | Clase abstracta con `Nombre`, `Vida`, `MostrarInformacion()`, `RecibirDanio(int)` y método abstracto `Actuar(Cadete)`. |
| `Cadete` | Hereda de `Personaje`; mantiene `Energia`, `Experiencia` e `Inventario`; permite gastar/recuperar energía y ganar experiencia. |
| `Robot` | Hereda de `Personaje`, inicializa vida y redefine información/comportamiento. |
| `Enemigo` | Hereda de `Personaje`; su acción observada causa daño al cadete. |
| `Objeto` | Contiene `Nombre`, `Tipo` y `Poder`. |
| `Inventario` | Administra una lista de objetos; permite agregar, mostrar, usar y comprobar si está vacío. |
| `Zona` | Contiene nombre y descripción y puede mostrar la información de la zona. |
| `Mision` | Representa “La anomalía perdida” con cuatro condiciones booleanas: llegar a la anomalía, encontrar núcleo, sobrevivir y regresar. |
| `SistemaNexus` | Coordina la creación de zonas, el cadete, la misión, el inicio y el menú de consola. |

## 3. Funcionalidades existentes identificadas

- Experiencia interactiva basada en consola mediante `Console.WriteLine`, `Console.ReadLine` y menús.
- Cadete con vida inicial 100, energía inicial 60 y experiencia inicial 0.
- Energía limitada entre 0 y 100 en los métodos revisados.
- Daño aplicado a personajes y limitado para que la vida no sea negativa.
- Inventario de objetos con tipo y poder; el uso depende del tipo de objeto.
- Zonas base: Bosque de Cristal, Ruinas de IRIS, Zona de Anomalía y Punto de Extracción.
- Misión “La anomalía perdida” con cuatro condiciones necesarias para completar la misión.
- Uso de herencia y polimorfismo a través de `Personaje`, `Cadete`, `Robot` y `Enemigo`.

## 4. Limitaciones arquitectónicas

1. **Interfaz acoplada a la lógica:** clases de dominio imprimen directamente en consola. Al pasar a Windows Forms, esto mezcla presentación y reglas y dificulta probarlas.
2. **Concentración en un archivo:** muchas clases y responsabilidades aparecen en `Program.cs`; es más difícil localizar cambios y mantener el código.
3. **Sin persistencia confirmada:** en los archivos consultados no se identificó una capa de repositorio SQL Server para guardar/cargar partidas.
4. **Estado en memoria:** objetos de cadete, misión y zonas se crean durante la ejecución; el progreso podría perderse al cerrar la aplicación.
5. **Modelo de misión específico:** los objetivos son propiedades booleanas fijas; es simple para una misión, pero menos flexible al incorporar muchas misiones.
6. **Validación y pruebas:** las reglas de negocio deben separarse y probarse sin depender de `Console`.
7. **LINQ / compilación:** en la vista pública consultada, la sección de `Inventario` alrededor de `ObtenerNombresDeEnergia()` muestra una expresión LINQ aparentemente incompleta o mal formada. Debe verificarse en el archivo local y corregirse solo después de revisar la versión real, porque una página web puede representar mal el formato o el repositorio puede haber cambiado.

## 5. Relación entre H3 y la evolución

La propuesta no elimina las clases del H3. `Cadete`, `Personaje`, `Objeto`, `Inventario`, `Zona`, `Mision` y `SistemaNexus` constituyen el punto de partida. El primer refactor será extraerlas a archivos y organizar responsabilidades; después se reemplazará la interacción de consola por formularios que llamen a servicios de juego.

## 6. Próxima verificación local

- Abrir `nexus1.2mrj.csproj` en Visual Studio.
- Ejecutar **Compilar > Recompilar solución** y guardar el resultado.
- Revisar advertencias y errores antes de reorganizar clases.
- Registrar una captura del resultado en `docs/evidencias/`.
- No declarar que SQL Server o Windows Forms están implementados hasta que existan código y pruebas que lo demuestren.
