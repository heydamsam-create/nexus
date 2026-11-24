# Diseño jugable actual de NEXUS

Este documento describe la aplicación Windows Forms que implementa `NEXUS_WindowsForms`. El prototipo de consola que permanece en la raíz es un proyecto independiente y no forma parte de la misión gráfica.

## Premisa e historia

En el universo narrativo de NEXUS, ambientado en 2297, la expedición llega a Eridani-9 siguiendo una señal emitida desde el cráter de Eos. El jugador explora la instalación abandonada, consulta registros de la tripulación y decide cómo responder a la anomalía. AURA, la IA de la nave, presenta alertas y diálogos. El código incluye narrativa para la misión y una enciclopedia; algunos antecedentes se presentan como marco narrativo y no como una funcionalidad independiente del juego.

## Recorrido y zonas

La misión combina una fase inicial de exploración en tiempo real con etapas de aproximación, contacto, exploración de superficie y decisión ante la anomalía. Para completar la expedición, el jugador debe recorrer el mapa, recoger elementos, completar objetivos y desafíos, y elegir un desenlace final.

El mapa actual tiene seis zonas conectadas:

1. Centro de control.
2. Sala de comunicaciones.
3. Laboratorio biológico.
4. Almacén técnico.
5. Enfermería.
6. Cámara de contención alienígena.

Hay puertas con requisitos de acceso, objetos coleccionables, terminales, documentos, elementos secretos, mobiliario con colisiones, minimapa y objetivos visibles. Las puertas, habitaciones descubiertas y objetos se registran en el estado de exploración.

## Controles

| Entrada | Acción |
|---|---|
| `W`, `A`, `S`, `D` o flechas | Moverse por el escenario. |
| `E` | Interactuar con objetos, puertas y terminales; avanzar/confirmar en diálogos. |
| `I` | Abrir o cerrar el inventario. Dentro, usar flechas o `W`/`S` para seleccionar y `E`/Enter para usar un objeto. |
| `J` | Abrir o cerrar el registro de objetivos. |
| `1`–`4` | Introducir botones de los desafíos; `1`/`2` seleccionan respuestas en diálogos. |
| Clic izquierdo o botón `DISPARAR` | Disparar al enemigo apuntado o hacia la orientación del personaje. |
| `F11` / `Esc` | Activar pantalla completa / volver a ventana. |

## Sistemas de exploración

- **Recursos:** oxígeno, integridad del casco y datos recuperados. Determinadas interacciones, desafíos y eventos los modifican.
- **Inventario:** recoge y utiliza objetos cuando la acción correspondiente está implementada; algunos objetos cumplen requisitos de puertas o desafíos.
- **Narrativa:** los diálogos presentan personajes y a AURA, ofrecen elecciones y registran decisiones para influir en etapas posteriores.
- **Eventos y desafíos:** fallos de soporte vital, comunicaciones, energía, seguridad y anomalía pueden activar secuencias; completarlas puede reparar sistemas o cambiar recursos.
- **Enemigos y combate:** los enemigos detectan y persiguen al jugador, pueden atacarlo y reciben daño de los disparos. La integridad del casco llega a cero para activar la derrota.
- **Objetivos:** el registro de misión muestra objetivos pendientes y completados; completar exploración permite avanzar a las etapas narrativas siguientes.

## Decisiones y desenlaces

Las decisiones principales de `MissionGame` modifican recursos y banderas de ruta. La exploración también guarda decisiones narrativas locales, que se aplican a la lógica de misión al restaurar la partida. En la etapa final se puede contener el núcleo, extraer datos y retirarse, o neutralizar la fuente. Los resultados implementados incluyen **ANOMALÍA CONTENIDA**, **CONTENCIÓN PARCIAL**, **RECOLECCIÓN EXITOSA**, **REPLIEGUE CON DATOS LIMITADOS** y **AMENAZA NEUTRALIZADA**. La etiqueta exacta depende de la opción final y, en ciertos desenlaces, de los recursos disponibles.

## Guardado

Las partidas en curso se guardan en SQL Server; su estado de mapa se serializa como JSON en `dbo.EstadoNave.EstadoExploracionJson`. Se guardan la posición, recursos, etapa, decisiones, inventario, habitaciones, puertas, objetivos, eventos, desafíos, diálogos y estado de enemigos. El botón **GUARDAR PARTIDA** guarda manualmente; la actividad programa un guardado automático cuando pasan 1,2 segundos sin nuevos cambios. **CARGAR PARTIDA** restaura una expedición en curso.

Los resultados terminados se escriben en `dbo.BitacoraMisiones`; `FormRegistros` consulta SQL Server. El contexto también escribe una copia JSON local en `%LOCALAPPDATA%\NEXUS\mission-history.json`, pero esa copia no es la fuente de datos del formulario de registros. Si falla SQL al finalizar, la copia JSON puede existir sin aparecer en `FormRegistros`.

## Límites

- No hay un motor externo: el mapa y los sprites se dibujan en el proyecto WinForms con GDI+.
- El guardado persistente depende de que SQL Server esté instalado, accesible y configurado con permisos adecuados.
- El prototipo de consola conserva una aventura distinta y no comparte el motor de la aplicación gráfica.
- La misión y el contenido que aparece en pantalla son los que están implementados en `MissionGame`, `ExplorationView`, `NexusNarrative` y `WorldChallengeCatalog`; las propuestas de `docs/arquitectura-propuesta.md` no describen funciones disponibles.
