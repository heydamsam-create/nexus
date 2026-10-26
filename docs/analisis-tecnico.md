# Análisis técnico: evolución de NEXUS

## 1. Contexto y evidencia

El repositorio público `heydamsam-create/nexus` contiene `Program.cs`, `nexus1.2mrj.csproj`, `README.md`, `.gitignore` y `docs/`. La implementación consultada usa C# y consola. En `Program.cs` se observan las clases `Personaje`, `Cadete`, `Robot`, `Enemigo`, `Objeto`, `Inventario`, `Zona`, `Mision` y `SistemaNexus`. La misión “La anomalía perdida” contiene cuatro condiciones de progreso. Esto permite que la propuesta gráfica mantenga el modelo conceptual de H3 en lugar de inventar otro juego.

## 2. Alternativas evaluadas

| Alternativa | Ventajas | Desventajas | Decisión |
|---|---|---|---|
| Mantener consola y mejorar menús | Cambio pequeño; útil para validar reglas | No cumple el objetivo de interfaz gráfica | Mantener solo como referencia de H3 |
| Windows Forms con lógica en eventos | Fácil de comenzar; todo en un proyecto | Acoplamiento, difícil de probar y mantener | No recomendada como arquitectura final |
| Windows Forms + dominio/servicios/repositorios | Separación clara; pruebas y evolución más sencillas | Requiere refactor gradual y más archivos | Recomendada |
| Guardado solo en archivos JSON | Fácil para prototipo local; no requiere servidor SQL | No cumple persistencia SQL Server solicitada y tiene límites para consultas compartidas | Solo alternativa temporal de prueba |
| SQL Server con consultas parametrizadas | Datos relacionales, restricciones, transacciones y consultas | Requiere instalación/configuración y manejo de errores | Recomendada para persistencia |
| ORM como Entity Framework Core | Menos SQL repetitivo y mapeo de entidades | Curva de aprendizaje y configuración adicional | Evaluar tras dominar el esquema; ADO.NET es opción didáctica simple |

## 3. Decisiones recomendadas

1. **Primero asegurar una versión de referencia:** compilar la versión H3 y registrar errores reales antes de reorganizarla.
2. **Separar clases:** mover cada clase a un archivo `.cs` sin cambiar las reglas; comprobar que sigue compilando.
3. **Separar dominio y presentación:** quitar gradualmente `Console.WriteLine` de las clases de dominio y devolver resultados que pueda mostrar la UI.
4. **Crear Windows Forms:** implementar menú, exploración, misiones e inventario siguiendo `diseno-del-juego.md`.
5. **Agregar servicios:** `JuegoService` y `PartidaService` coordinan casos de uso, sin convertir los formularios en la lógica del juego.
6. **Agregar SQL Server:** definir tablas, repositorios y guardado/carga transaccional después de que las reglas estén estables.
7. **Probar casos principales:** energía mínima/máxima, daño, objeto inválido, consumo de objetos y las cuatro condiciones de misión.

## 4. Viabilidad y riesgos

- **Viabilidad:** la lógica de H3 ya tiene entidades identificables, por lo que es viable conservarla y adaptar su interacción.
- **Riesgo de refactor:** al estar concentrado el código en `Program.cs`, mover métodos puede introducir errores. Hacer cambios pequeños y compilar tras cada paso.
- **Riesgo de persistencia:** si el guardado de una partida se hace en varias operaciones sin transacción, puede quedar parcialmente guardada.
- **Riesgo de interfaz:** si se implementan reglas dentro de botones, habrá duplicación y será más difícil probarlas.
- **Riesgo de credenciales:** nunca publicar contraseñas o cadenas de conexión con secretos en GitHub.
- **Riesgo de alcance:** no priorizar animaciones antes de tener una partida que se pueda jugar, guardar y cargar.

## 5. Plan de implementación sugerido

### Etapa 1 — Estabilización
- Recompilar la versión actual.
- Registrar errores y comprobar que las funciones de H3 operan.
- Crear una etiqueta o rama de respaldo antes de refactorizar.

### Etapa 2 — Ordenamiento del código
- Separar las clases en archivos.
- Corregir dependencias y validaciones.
- Reducir llamadas directas a consola en las clases de dominio.

### Etapa 3 — Interfaz
- Crear los cuatro formularios prioritarios.
- Conectar eventos a servicios de aplicación.
- Verificar navegación y retroalimentación.

### Etapa 4 — Persistencia
- Crear esquema SQL versionado.
- Implementar repositorios y transacciones.
- Probar guardar, cerrar, volver a abrir y cargar una partida.

### Etapa 5 — Calidad y entrega
- Ejecutar pruebas manuales y de dominio.
- Actualizar README y documentación.
- Crear commits pequeños y descriptivos.
- Adjuntar evidencias reales en `docs/evidencias/`.

## 6. Fuentes técnicas consultables

Las siguientes fuentes son documentación oficial o primaria. Deben consultarse para confirmar detalles concretos de la versión de .NET instalada en el equipo.

- Microsoft Learn — Windows Forms: https://learn.microsoft.com/dotnet/desktop/winforms/
- Microsoft Learn — ADO.NET: https://learn.microsoft.com/dotnet/framework/data/adonet/
- Microsoft Learn — SQL Server y documentación técnica: https://learn.microsoft.com/sql/
- Microsoft Learn — Entity Framework Core: https://learn.microsoft.com/ef/core/
- GitHub Docs — Ignorar archivos: https://docs.github.com/get-started/git-basics/ignoring-files
- GitHub Docs — Buenas prácticas de commits: https://docs.github.com/pull-requests/committing-changes-to-your-project/creating-and-editing-commits/about-commits

## 7. Verificación de información obtenida con IA

La IA se utilizó como apoyo para organizar alternativas, proponer un diseño y elaborar un modelo inicial de datos. Sus sugerencias no se consideran evidencia por sí solas.

Verificaciones necesarias antes de la implementación:
- Confirmar en el código local las clases, propiedades y métodos mencionados.
- Compilar en Visual Studio y usar los errores del compilador como evidencia de estado.
- Consultar documentación oficial de Microsoft para compatibilidad de Windows Forms, .NET y SQL Server.
- Probar en ejecución las reglas de energía, inventario y misión.
- Revisar el diagrama y el esquema con el código real antes de implementarlos.
- Corregir este documento si una prueba contradice una afirmación.

## 8. Criterios de aceptación

- Los documentos describen clases y zonas de H3, no un juego ajeno.
- Los cuatro wireframes tienen elementos y navegación explicados.
- La arquitectura distingue UI, aplicación, dominio y persistencia.
- El modelo de datos permite guardar y reconstruir una partida.
- El README indica qué está implementado y qué es una propuesta.
- El repositorio no contiene binarios temporales ni secretos.
- Los commits permiten identificar cada grupo de cambios.
