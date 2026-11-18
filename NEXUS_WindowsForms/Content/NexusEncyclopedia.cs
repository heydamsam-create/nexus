namespace NEXUS_WindowsForms.Content
{
    internal sealed class EncyclopediaSection
    {
        public string ShortTitle { get; }
        public string Category { get; }
        public string Title { get; }
        public string Lead { get; }
        public string Content { get; }
        public string Note { get; }
        public bool IsEstablished { get; }

        public EncyclopediaSection(
            string shortTitle,
            string category,
            string title,
            string lead,
            string content,
            string note,
            bool isEstablished)
        {
            ShortTitle = shortTitle;
            Category = category;
            Title = title;
            Lead = lead;
            Content = content;
            Note = note;
            IsEstablished = isEstablished;
        }
    }

    internal static class NexusEncyclopedia
    {
        public static IReadOnlyList<EncyclopediaSection> Sections { get; } = new List<EncyclopediaSection>
        {
            new(
                "ORIGEN DE NEXUS",
                "IDENTIDAD",
                "El proyecto NEXUS",
                "Una nave de exploración, una señal bajo la superficie y una misión que busca comprender antes de intervenir.",
                "NEXUS es la nave de exploración que da nombre a la expedición actual y al universo narrativo del proyecto. Su viaje por Eridani-9 comienza después de que los instrumentos detectan una señal repetida desde una estructura enterrada bajo el cráter de Eos.\r\n\r\n" +
                "En esta propuesta narrativa, NEXUS representa una iniciativa de investigación dedicada a estudiar mundos desconocidos y fenómenos que todavía no tienen explicación. La prioridad de la tripulación es registrar evidencia, proteger al cadete y decidir con cuidado cómo responder a la anomalía.\r\n\r\n" +
                "Los archivos actuales no fijan una fecha de construcción, una organización fundadora ni la expansión oficial del nombre NEXUS. Esos elementos quedan abiertos para futuras historias.",
                "AMPLIACIÓN NARRATIVA · La misión y la nave existen en el proyecto; su origen institucional aún no está definido.",
                false),

            new(
                "AÑO 2297",
                "CRONOLOGÍA",
                "Año 2297",
                "La expedición de NEXUS ocurre en un futuro donde el espacio profundo forma parte de la exploración humana.",
                "El año 2297 es el marco temporal establecido para la aventura. El protagonista es un cadete enviado a investigar una realidad desconocida y una anomalía que podría afectar su estabilidad.\r\n\r\n" +
                "La operación de Eridani-9 comienza cuando NEXUS llega al sector y localiza una señal bajo la superficie. El objetivo inmediato es estudiar la fuente, recuperar información y mantener a la tripulación a salvo.\r\n\r\n" +
                "La documentación del proyecto no especifica acontecimientos históricos con fechas exactas entre el presente y 2297. Por eso, esta enciclopedia conserva el año de la misión sin presentar una cronología inventada como dato confirmado.",
                "DATO DEL UNIVERSO · 2297 y la misión de un cadete están descritos en la documentación del proyecto.",
                true),

            new(
                "EXPLORACIÓN ESPACIAL",
                "HISTORIA",
                "La era de exploración",
                "El registro conocido muestra la transición desde la observación remota hasta las expediciones con tripulación.",
                "Marco narrativo propuesto: la exploración espacial comenzó con observatorios y sondas, que permitieron estudiar mundos lejanos sin enviar equipos de superficie. Más adelante, las naves de investigación llevaron instrumentos y tripulaciones a regiones donde una lectura a distancia no bastaba.\r\n\r\n" +
                "En la misión actual, esa evolución se refleja en el uso de sensores orbitales, sistemas de navegación, protocolos de investigación y una sonda de superficie. El cadete interviene cuando la información remota deja preguntas que requieren una decisión humana.\r\n\r\n" +
                "No se han definido guerras, tratados, colonias ni hitos históricos fechados para este universo. La historia queda abierta para ampliarse sin contradecir el marco de 2297.",
                "MARCO NARRATIVO · La secuencia general de exploración es nueva; no añade funcionalidades al juego.",
                false),

            new(
                "NAVE Y TECNOLOGÍA",
                "EQUIPAMIENTO",
                "La nave NEXUS",
                "Los instrumentos que aparecen en la misión ayudan a leer señales, elegir rutas y evaluar los recursos de la tripulación.",
                "La misión presenta a NEXUS como una nave de exploración equipada con sensores para realizar escaneos orbitales, sistemas de navegación para aproximarse al cráter de Eos y un protocolo de investigación para responder a señales. La tripulación también puede desplegar una sonda antes de enviar un equipo a la superficie.\r\n\r\n" +
                "Durante la expedición se vigilan tres indicadores: oxígeno, integridad del casco y datos recuperados. Las decisiones del jugador afectan esos valores y el desenlace.\r\n\r\n" +
                "El proyecto no describe el motor, el armamento, el escudo ni las comunicaciones de NEXUS. Esta ficha se limita a los instrumentos y recursos mencionados en la misión interactiva.",
                "DATOS DE LA MISIÓN · Sensores, navegación, protocolo de investigación, sonda y recursos aparecen en el flujo actual.",
                true),

            new(
                "MUNDOS Y LUGARES",
                "CARTOGRAFÍA",
                "Mundos y emplazamientos",
                "Eridani-9 es el destino de la misión actual. Otros nombres proceden de la documentación del prototipo original.",
                "ERIDANI-9\r\nMundo objetivo de la expedición NEXUS. Los sensores registran una anomalía bajo la superficie y una instalación enterrada en el cráter de Eos.\r\n\r\n" +
                "CRÁTER DE EOS\r\nZona de aproximación y exploración. Desde allí se localiza la señal y se decide entre el escaneo, el uso de una sonda o el despliegue de un equipo.\r\n\r\n" +
                "BOSQUE DE CRISTAL · RUINAS DE IRIS · ZONA DE ANOMALÍA · PUNTO DE EXTRACCIÓN\r\nEstos lugares figuran en los documentos del prototipo de consola. Su relación geográfica con Eridani-9 todavía no está establecida. No hay otros planetas nombrados en los archivos revisados.",
                "CARTOGRAFÍA · Se distingue el destino de la misión actual de las zonas citadas en el prototipo anterior.",
                true),

            new(
                "ANOMALÍA ESPACIAL",
                "FENÓMENOS",
                "La señal de Eos",
                "Una fuente energética bajo tierra emite pulsos y altera los instrumentos de NEXUS.",
                "La anomalía se detecta bajo el cráter de Eos, en una estructura enterrada. La señal se repite y cambia mientras NEXUS se aproxima. Un escaneo orbital puede revelar un patrón matemático; responder a los pulsos puede estabilizarlos brevemente y mostrar un mapa parcial.\r\n\r\n" +
                "En la cámara inferior hay un núcleo luminoso que reacciona a los pulsos de la nave y modifica el campo energético. La tripulación todavía no conoce su origen, su propósito ni si contiene una forma de vida.\r\n\r\n" +
                "La misión permite intentar contener el núcleo, extraer datos y retirarse, o neutralizar la fuente. Ninguna opción confirma por sí sola qué creó la anomalía.",
                "REGISTRO DE CAMPO · Descripción basada en los acontecimientos y opciones de la misión actual.",
                true),

            new(
                "PERSONAJES Y MISIÓN",
                "PERSONAL",
                "La tripulación de NEXUS",
                "El cadete es el protagonista jugable. Los registros todavía no asignan nombres propios al equipo.",
                "EL CADETE\r\nProtagonista de la aventura: explora el entorno, toma decisiones y participa en la investigación. La identidad personal y los antecedentes del cadete no están definidos en los archivos actuales.\r\n\r\n" +
                "LA TRIPULACIÓN\r\nEl equipo de NEXUS mantiene la nave y analiza las lecturas durante la expedición. La misión registra sus recursos, pero todavía no presenta integrantes con nombre o biografía propios.\r\n\r\n" +
                "ROBOT Y ENEMIGO\r\nRobot de seguridad y enemigo son tipos de personaje que aparecen en el prototipo de consola. No participan en la misión WinForms actual de Eridani-9.\r\n\r\n" +
                "ANTECEDENTE DE LA MISIÓN\r\nNEXUS recibió una señal repetida desde una estructura subterránea. El cadete debe investigar el origen, asegurar datos y elegir cómo resolver el encuentro con el núcleo.",
                "REGISTRO DE PERSONAJES · Se separan los roles de la misión actual de los tipos del prototipo de consola.",
                true)
        };
    }
}
