# Diseño del juego NEXUS: misión de Eridani-9

Este documento separa la misión jugable que existe en Windows Forms del contexto narrativo y de los escenarios del prototipo de consola. Las etapas y efectos de decisión descritos a continuación corresponden a `MissionGame`.

## Universo y premisa

La expedición está situada en 2297. La nave de exploración NEXUS llega a Eridani-9 después de detectar una señal repetida desde una estructura bajo el cráter de Eos. El cadete debe investigar la señal, recuperar datos y mantener a la tripulación a salvo.

La enciclopedia incluye fichas sobre la nave, la anomalía, los lugares, la cronología y los personajes. Marca explícitamente algunas partes como marco narrativo propuesto. No hay una cronología detallada del universo ni biografías de tripulantes con nombres establecidos en el código actual.

## Flujo jugable implementado

La misión tiene un briefing inicial y cuatro etapas. Cada etapa permite escoger una o más acciones. La puntuación final se calcula como la suma de oxígeno, integridad del casco y datos recuperados después de limitar cada recurso al rango 0–100.

| Etapa | Decisiones | Efecto en recursos y ruta |
|---|---|---|
| Briefing | Iniciar exploración | Avanza a aproximación; no cambia recursos. |
| Aproximación | Escanear desde órbita | +15 datos y establece la ruta de escaneo orbital. |
| Aproximación | Descender directamente | −10 oxígeno, −10 casco y +8 datos; establece descenso directo. |
| Contacto | Responder a la señal | +12 datos y la señal queda marcada como confiable para el texto posterior. |
| Contacto | Seguir el origen | −12 oxígeno, −5 casco y +15 datos. |
| Exploración | Enviar una sonda | +15 datos; determina la variante narrativa de la siguiente etapa. |
| Exploración | Desplegar equipo | −20 oxígeno, −5 casco y +24 datos. |
| Anomalía | Contener el núcleo | −5 oxígeno, −10 casco y +12 datos. |
| Anomalía | Extraer datos y retirarse | −5 oxígeno y +8 datos. |
| Anomalía | Neutralizar la fuente | −8 casco y +3 datos. |

Los indicadores de oxígeno, casco y datos se limitan individualmente entre 0 y 100. Las decisiones previas quedan registradas en una lista de títulos de opción.

## Desenlaces

El resultado depende de la elección final y, en determinados casos, de los valores finales:

- **ANOMALÍA CONTENIDA:** elegir contener el núcleo y terminar con casco ≥75, oxígeno ≥60 y datos ≥50.
- **CONTENCIÓN PARCIAL:** elegir contenerlo sin cumplir todos esos umbrales.
- **RECOLECCIÓN EXITOSA:** elegir extracción y terminar con datos ≥55.
- **REPLIEGUE CON DATOS LIMITADOS:** elegir extracción con menos de 55 datos.
- **AMENAZA NEUTRALIZADA:** elegir neutralizar la fuente.

El resultado incluye resumen, nombre de misión, fecha local, puntuación, recursos finales y decisiones; se emite al contexto de aplicación para su guardado. La opción de repetir reinicia la misión y los recursos en memoria.

## Contenido de prototipo anterior

El `Program.cs` de consola conserva una aventura diferente con `Cadete`, vida, energía, experiencia e inventario, además de `Robot`, `Enemigo`, `Zona` y la misión “La anomalía perdida”. Entre sus zonas aparecen Bosque de Cristal, Ruinas de IRIS, Zona de Anomalía y Punto de Extracción. Esas mecánicas no forman parte del flujo actual de `FormMision`; se documentan como referencia del prototipo legado, no como funciones de la aplicación gráfica.

## Presentación

`FormMision` presenta narrativa, estado, recursos, progreso y botones para las decisiones. La interfaz se construye desde `FormMision.cs`; este documento no afirma cambios en los recursos ni en el diseño de `Form1`. Los textos de la enciclopedia se organizan en `NexusEncyclopedia` y varias entradas señalan qué elementos siguen siendo propuesta narrativa.

## Límites narrativos y mecánicos

- La misión actual es una secuencia de etapas fija, no un mapa libre ni un sistema de misiones múltiples.
- No incluye combate, inventario, niveles, tripulantes con nombre ni gestión de partidas guardadas.
- El historial almacena solo resultados finalizados; no permite retomar decisiones a mitad de misión.
- No se ha definido el origen institucional de NEXUS ni el origen o propósito de la anomalía.
- Los desenlaces son etiquetas narrativas calculadas por reglas del código; no son escenas cinemáticas separadas.
