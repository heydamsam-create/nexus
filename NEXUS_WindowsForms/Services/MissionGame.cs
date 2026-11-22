using NEXUS_WindowsForms.Models;

namespace NEXUS_WindowsForms.Services
{
    internal sealed class MissionOption
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }

        public MissionOption(string id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }
    }

    internal sealed class MissionGame
    {
        private enum MissionStage
        {
            Briefing,
            Exploration,
            Approach,
            Signal,
            Surface,
            Core,
            Completed
        }

        private MissionStage _stage = MissionStage.Briefing;
        private bool _scannedFromOrbit;
        private bool _descendedEarly;
        private bool _trustedSignal;
        private bool _sentProbe;
        private readonly List<string> _decisions = new();
        private MissionResult? _result;

        public int Oxygen { get; private set; } = 100;
        public int HullIntegrity { get; private set; } = 100;
        public int DataRecovered { get; private set; }
        public int TotalSteps => 5;

        public int ProgressStep => _stage switch
        {
            MissionStage.Briefing => 0,
            MissionStage.Exploration => 1,
            MissionStage.Approach => 2,
            MissionStage.Signal => 3,
            MissionStage.Surface => 4,
            MissionStage.Core => 5,
            MissionStage.Completed => 5,
            _ => 0
        };

        public string PhaseName => _stage switch
        {
            MissionStage.Briefing => "BRIEFING DE LA TRIPULACIÓN",
            MissionStage.Exploration => "ETAPA 1  //  EXPLORACIÓN DE NEXUS",
            MissionStage.Approach => "ETAPA 2  //  APROXIMACIÓN",
            MissionStage.Signal => "ETAPA 3  //  CONTACTO",
            MissionStage.Surface => "ETAPA 4  //  EXPLORACIÓN DE EOS",
            MissionStage.Core => "ETAPA 5  //  ANOMALÍA",
            MissionStage.Completed => _result?.Outcome ?? "MISIÓN FINALIZADA",
            _ => "OPERACIÓN UMBRAL DE EOS"
        };

        public string StatusText => _stage == MissionStage.Completed
            ? "MISIÓN COMPLETADA"
            : _stage == MissionStage.Briefing
                ? "NAVE EN ÓRBITA DE ESPERA"
                : "TRIPULACIÓN EN OPERACIÓN";

        public string RouteName => _descendedEarly
            ? "DESCENSO DIRECTO"
            : _scannedFromOrbit
                ? "ESCANEO ORBITAL"
                : "RUTA PENDIENTE";

        public string Narrative => _stage switch
        {
            MissionStage.Briefing =>
                "AÑO 2297. La nave de exploración NEXUS ha llegado a Eridani-9 tras recibir una señal repetida desde una estructura bajo la superficie.\r\n\r\n" +
                "Tu objetivo es investigar el origen de la señal, recuperar información útil y mantener a la tripulación a salvo. Cada decisión afectará los recursos y el desenlace de la operación.",

            MissionStage.Approach =>
                "Eridani-9 ocupa ahora las pantallas del puente. Una anomalía energética pulsa bajo el cráter de Eos y altera los instrumentos de navegación.\r\n\r\n" +
                "Debes elegir cómo aproximarte. Un escaneo puede revelar más datos antes del descenso; entrar directamente ahorra tiempo, pero expone el casco a la interferencia.",

            MissionStage.Signal => _scannedFromOrbit
                ? "El escaneo orbital revela un patrón matemático dentro de la señal. Parece una respuesta, no una transmisión automática. La fuente está activa bajo el cráter.\r\n\r\n" +
                  "Puedes contestar con el protocolo de investigación de NEXUS o seguir la señal antes de que vuelva a cambiar."
                : "La interferencia aumenta mientras NEXUS se aproxima. Entre el ruido, los sensores detectan pulsos que podrían ser una señal de auxilio.\r\n\r\n" +
                  "Puedes intentar descifrarla desde la nave o seguir su origen con los sistemas de navegación.",

            MissionStage.Surface => _trustedSignal
                ? "La respuesta enviada desde NEXUS estabiliza la señal por unos segundos. Aparece un mapa parcial de una instalación enterrada.\r\n\r\n" +
                  "Una sonda puede explorar primero el terreno; un equipo de superficie podría recuperar más información, con mayor gasto de oxígeno y riesgo para la nave."
                : "La señal conduce a una instalación parcialmente cubierta por polvo mineral. Sus lecturas cambian cada vez que la nave se acerca.\r\n\r\n" +
                  "Una sonda reduce el riesgo para la tripulación; una expedición en superficie puede recuperar datos más completos.",

            MissionStage.Core => _sentProbe
                ? "La sonda alcanza una cámara bajo el cráter y transmite la imagen de un núcleo luminoso. El objeto responde a los pulsos de NEXUS y modifica el campo energético.\r\n\r\n" +
                  "La decisión final determinará qué logra llevarse la tripulación de Eridani-9."
                : "El equipo localiza el núcleo de la anomalía dentro de la instalación. Su energía fluctúa y el casco de NEXUS registra daños por la interferencia.\r\n\r\n" +
                  "Debes decidir si contener el núcleo, extraer los datos y retirarte, o neutralizar la fuente.",

            MissionStage.Completed => _result is null
                ? "La misión ha finalizado."
                : $"{_result.Summary}\r\n\r\nRESULTADO: {_result.Outcome}\r\nPUNTUACIÓN DE MISIÓN: {_result.Score}",

            _ => string.Empty
        };

        public IReadOnlyList<MissionOption> GetOptions()
        {
            return _stage switch
            {
                MissionStage.Briefing => new List<MissionOption>
                {
                    new("START", "INICIAR EXPLORACIÓN", "Entra en la nave y prepara la expedición.")
                },
                MissionStage.Exploration => Array.Empty<MissionOption>(),
                MissionStage.Approach => new List<MissionOption>
                {
                    new("SCAN_ORBIT", "ESCANEAR DESDE ÓRBITA", "Recupera datos antes de acercarte al cráter."),
                    new("DESCEND", "DESCENDER DIRECTAMENTE", "Acércate rápido y asume el riesgo de la interferencia.")
                },
                MissionStage.Signal => new List<MissionOption>
                {
                    new("DECODE", "RESPONDER A LA SEÑAL", "Usa el protocolo de investigación de NEXUS."),
                    new("FOLLOW_SIGNAL", "SEGUIR EL ORIGEN", "Avanza hacia la fuente antes de que desaparezca.")
                },
                MissionStage.Surface => new List<MissionOption>
                {
                    new("SEND_PROBE", "ENVIAR UNA SONDA", "Explora la instalación sin exponer al equipo."),
                    new("FIELD_TEAM", "DESPLEGAR EQUIPO", "Recupera más datos con un coste para la tripulación.")
                },
                MissionStage.Core => new List<MissionOption>
                {
                    new("CONTAIN", "CONTENER EL NÚCLEO", "Intenta estabilizar la anomalía."),
                    new("EXTRACT", "EXTRAER DATOS Y RETIRARSE", "Prioriza la información y la evacuación."),
                    new("DESTROY", "NEUTRALIZAR LA FUENTE", "Destruye el núcleo para detener su señal.")
                },
                MissionStage.Completed => new List<MissionOption>
                {
                    new("RESTART", "REPETIR MISIÓN", "Inicia una nueva expedición con recursos restaurados.")
                },
                _ => Array.Empty<MissionOption>()
            };
        }

        public MissionResult? SelectOption(string optionId)
        {
            if (_stage == MissionStage.Completed)
            {
                return null;
            }

            MissionOption? selectedOption = GetOptions().FirstOrDefault(option => option.Id == optionId);
            if (selectedOption is null)
            {
                return null;
            }

            _decisions.Add(selectedOption.Title);

            switch (_stage)
            {
                case MissionStage.Briefing:
                    _stage = MissionStage.Exploration;
                    break;

                case MissionStage.Approach:
                    if (optionId == "SCAN_ORBIT")
                    {
                        _scannedFromOrbit = true;
                        DataRecovered += 15;
                    }
                    else
                    {
                        _descendedEarly = true;
                        Oxygen -= 10;
                        HullIntegrity -= 10;
                        DataRecovered += 8;
                    }
                    _stage = MissionStage.Signal;
                    break;

                case MissionStage.Signal:
                    if (optionId == "DECODE")
                    {
                        _trustedSignal = true;
                        DataRecovered += 12;
                    }
                    else
                    {
                        Oxygen -= 12;
                        HullIntegrity -= 5;
                        DataRecovered += 15;
                    }
                    _stage = MissionStage.Surface;
                    break;

                case MissionStage.Surface:
                    if (optionId == "SEND_PROBE")
                    {
                        _sentProbe = true;
                        DataRecovered += 15;
                    }
                    else
                    {
                        Oxygen -= 20;
                        HullIntegrity -= 5;
                        DataRecovered += 24;
                    }
                    _stage = MissionStage.Core;
                    break;

                case MissionStage.Core:
                    return CompleteMission(optionId);
            }

            ClampResources();
            return null;
        }

        public void Reset()
        {
            _stage = MissionStage.Briefing;
            _scannedFromOrbit = false;
            _descendedEarly = false;
            _trustedSignal = false;
            _sentProbe = false;
            _decisions.Clear();
            _result = null;
            Oxygen = 100;
            HullIntegrity = 100;
            DataRecovered = 0;
        }
        public string CurrentStageKey => _stage.ToString();
        public bool ScannedFromOrbit => _scannedFromOrbit;
        public bool DescendedEarly => _descendedEarly;
        public bool TrustedSignal => _trustedSignal;
        public bool SentProbe => _sentProbe;
        public IReadOnlyList<string> Decisions => _decisions.AsReadOnly();

        public void RecordExplorationAction(string decision, int oxygenChange, int hullChange, int dataChange)
        {
            if (_stage != MissionStage.Exploration)
            {
                return;
            }

            _decisions.Add(decision);
            Oxygen += oxygenChange;
            HullIntegrity += hullChange;
            DataRecovered += dataChange;
            ClampResources();
        }

        public void CompleteExploration()
        {
            if (_stage == MissionStage.Exploration)
            {
                _stage = MissionStage.Approach;
            }
        }

        public void RestoreFromSave(
            string stageKey,
            int oxygen,
            int hullIntegrity,
            int dataRecovered,
            bool scannedFromOrbit,
            bool descendedEarly,
            bool trustedSignal,
            bool sentProbe,
            IEnumerable<string> decisions)
        {
            _stage = Enum.TryParse(stageKey, out MissionStage parsedStage)
                ? parsedStage
                : MissionStage.Briefing;
            Oxygen = Math.Clamp(oxygen, 0, 100);
            HullIntegrity = Math.Clamp(hullIntegrity, 0, 100);
            DataRecovered = Math.Clamp(dataRecovered, 0, 100);
            _scannedFromOrbit = scannedFromOrbit;
            _descendedEarly = descendedEarly;
            _trustedSignal = trustedSignal;
            _sentProbe = sentProbe;
            _decisions.Clear();
            _decisions.AddRange(decisions);
            _result = null;
        }
        private MissionResult CompleteMission(string choiceId)
        {
            if (choiceId == "CONTAIN")
            {
                Oxygen -= 5;
                HullIntegrity -= 10;
                DataRecovered += 12;
            }
            else if (choiceId == "EXTRACT")
            {
                Oxygen -= 5;
                DataRecovered += 8;
            }
            else if (choiceId == "DESTROY")
            {
                HullIntegrity -= 8;
                DataRecovered += 3;
            }

            ClampResources();
            string outcome;
            string summary;

            if (choiceId == "CONTAIN" && HullIntegrity >= 75 && Oxygen >= 60 && DataRecovered >= 50)
            {
                outcome = "ANOMALÍA CONTENIDA";
                summary = "La tripulación estabilizó el núcleo y recuperó datos suficientes para que NEXUS pueda analizar la anomalía durante el viaje de regreso.";
            }
            else if (choiceId == "CONTAIN")
            {
                outcome = "CONTENCIÓN PARCIAL";
                summary = "El núcleo quedó parcialmente estabilizado. NEXUS inicia el retorno con información útil, aunque la anomalía no pudo aislarse por completo.";
            }
            else if (choiceId == "EXTRACT" && DataRecovered >= 55)
            {
                outcome = "RECOLECCIÓN EXITOSA";
                summary = "NEXUS se retira de Eridani-9 con un conjunto amplio de datos y la tripulación a salvo.";
            }
            else if (choiceId == "EXTRACT")
            {
                outcome = "REPLIEGUE CON DATOS LIMITADOS";
                summary = "La tripulación se retiró a tiempo. Los datos recuperados son limitados, pero permitirán continuar el análisis desde la nave.";
            }
            else
            {
                outcome = "AMENAZA NEUTRALIZADA";
                summary = "La fuente fue destruida y la señal cesó. NEXUS abandona el cráter con los registros que pudo conservar.";
            }

            _stage = MissionStage.Completed;
            _result = new MissionResult
            {
                MissionName = "Operación Umbral de Eos",
                CompletedAt = DateTime.Now,
                Outcome = outcome,
                Summary = summary,
                Score = Oxygen + HullIntegrity + DataRecovered,
                OxygenRemaining = Oxygen,
                HullIntegrity = HullIntegrity,
                DataRecovered = DataRecovered,
                Decisions = new List<string>(_decisions)
            };

            return _result;
        }

        private void ClampResources()
        {
            Oxygen = Math.Clamp(Oxygen, 0, 100);
            HullIntegrity = Math.Clamp(HullIntegrity, 0, 100);
            DataRecovered = Math.Clamp(DataRecovered, 0, 100);
        }
    }
}
