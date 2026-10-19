using System;
using System.Collections.Generic;

namespace NEXUS
{
    // CLASE PERSONAJE
    abstract class Personaje
    {
        public string Nombre { get; protected set; }
        public int Vida { get; protected set; }

        public Personaje(string nombre, int vida)
        {
            Nombre = nombre;
            Vida = vida;
        }

        public virtual void MostrarInformacion()
        {
            Console.WriteLine("Nombre: " + Nombre);
            Console.WriteLine("Vida: " + Vida);
        }

        public virtual void RecibirDanio(int cantidad)
        {
            Vida -= cantidad;

            if (Vida < 0)
                Vida = 0;
        }

        public abstract void Actuar(Cadete cadete);
    }


    // CLASE CADETE
    class Cadete : Personaje
    {
        public int Energia { get; private set; }
        public int Experiencia { get; private set; }
        public Inventario Inventario { get; private set; }

        public Cadete(string nombre)
            : base(nombre, 100)
        {
            Energia = 60;
            Experiencia = 0;
            Inventario = new Inventario();
        }

        public void GastarEnergia(int cantidad)
        {
            Energia -= cantidad;

            if (Energia < 0)
                Energia = 0;
        }

        public void RecuperarEnergia(int cantidad)
        {
            Energia += cantidad;

            if (Energia > 100)
                Energia = 100;
        }

        public void GanarExperiencia(int cantidad)
        {
            Experiencia += cantidad;
        }

        public void AgregarEscudo(int cantidad)
        {
            Inventario.Agregar(
                new Objeto("Escudo activo", "Escudo", cantidad)
            );
        }

        public override void MostrarInformacion()
        {
            Console.WriteLine("\n===== INFORMACION DEL CADETE =====");
            Console.WriteLine("Nombre: " + Nombre);
            Console.WriteLine("Vida: " + Vida + "%");
            Console.WriteLine("Energia: " + Energia + "%");
            Console.WriteLine("Experiencia: " + Experiencia + " XP");
        }

        public override void Actuar(Cadete cadete)
        {
            Console.WriteLine("El cadete explora la realidad.");
        }
    }


    // CLASE ROBOT
    class Robot : Personaje
    {
        public Robot(string nombre)
            : base(nombre, 50)
        {
        }

        public override void MostrarInformacion()
        {
            Console.WriteLine("\n===== ROBOT =====");
            Console.WriteLine("Nombre: " + Nombre);
            Console.WriteLine("Vida: " + Vida);
            Console.WriteLine("Tipo: Robot de seguridad");
        }

        public override void Actuar(Cadete cadete)
        {
            Console.WriteLine("\n🤖 ROBOT: Sistema defensivo activado.");
            Console.WriteLine("El robot realiza un ataque defensivo.");

            cadete.RecibirDanio(10);
            cadete.GastarEnergia(5);
        }
    }


    // CLASE ENEMIGO
    class Enemigo : Personaje
    {
        public Enemigo(string nombre)
            : base(nombre, 70)
        {
        }

        public override void MostrarInformacion()
        {
            Console.WriteLine("\n===== ENEMIGO =====");
            Console.WriteLine("Nombre: " + Nombre);
            Console.WriteLine("Vida: " + Vida);
            Console.WriteLine("Tipo: Entidad hostil");
        }

        public override void Actuar(Cadete cadete)
        {
            Console.WriteLine("\n👾 ENEMIGO: La entidad inicia un combate.");
            cadete.RecibirDanio(15);
        }
    }


    // CLASE OBJETO
    class Objeto
    {
        public string Nombre { get; private set; }
        public string Tipo { get; private set; }
        public int Poder { get; private set; }

        public Objeto(string nombre, string tipo, int poder)
        {
            Nombre = nombre;
            Tipo = tipo;
            Poder = poder;
        }

        public void Mostrar()
        {
            Console.WriteLine(
                Nombre + " | Tipo: " + Tipo + " | Poder: " + Poder
            );
        }
    }


    // CLASE INVENTARIO
    class Inventario
    {
        private List<Objeto> objetos;
        ObtenerNombresDeEnergia()
        {
            return objetos 
                .Where(objeto => objeto.nombre)
                .OrderBy(nombre => nombre)
                .Tolist

        public Inventario()
        {
            objetos = new List<Objeto>();
        }

        // AGREGAR
        public void Agregar(Objeto objeto)
        {
            objetos.Add(objeto);

            Console.WriteLine(
                "\n Objeto agregado al inventario: " + objeto.Nombre
            );
        }

        // CONSULTAR
        public void Mostrar()
        {
            Console.WriteLine("\n INVENTARIO ");

            if (objetos.Count == 0)
            {
                Console.WriteLine("El inventario esta vacio.");
                return;
            }

            for (int i = 0; i < objetos.Count; i++)
            {
                Console.Write((i + 1) + ". ");
                objetos[i].Mostrar();
            }
        }

        // UTILIZAR
        public bool Usar(int numero, Cadete cadete)
        {
            if (numero < 1 || numero > objetos.Count)
            {
                Console.WriteLine(
                    " No existe ese objeto en el inventario."
                );

                return false;
            }

            Objeto objeto = objetos[numero - 1];

            if (objeto.Tipo == "Energia")
            {
                cadete.RecuperarEnergia(objeto.Poder);

                Console.WriteLine(
                    "\n🔋 Célula utilizada."
                );

                Console.WriteLine(
                    "Energia recuperada: +" + objeto.Poder
                );
            }
            else if (objeto.Tipo == "Escudo")
            {
                Console.WriteLine(
                    "\n🛡️ Escudo activado."
                );

                Console.WriteLine(
                    "Proteccion: +" + objeto.Poder
                );
            }
            else if (objeto.Tipo == "Ataque")
            {
                Console.WriteLine(
                    "\n🔫 Modulo de ataque preparado."
                );
            }
            else if (objeto.Tipo == "Analizador")
            {
                Console.WriteLine(
                    "\n🔬 Analizador activado."
                );
            }
            else
            {
                Console.WriteLine(
                    "Este objeto no puede utilizarse."
                );

                return false;
            }

            // ELIMINAR DESPUES DE USAR
            objetos.RemoveAt(numero - 1);

            Console.WriteLine(
                "Objeto eliminado del inventario."
            );

            return true;
        }

        public bool EstaVacio()
        {
            return objetos.Count == 0;
        }
    }


    // CLASE ZONA
    class Zona
    {
        public string Nombre { get; private set; }
        public string Descripcion { get; private set; }

        public Zona(string nombre, string descripcion)
        {
            Nombre = nombre;
            Descripcion = descripcion;
        }

        public void Mostrar()
        {
            Console.WriteLine("\nZona: " + Nombre);
            Console.WriteLine(Descripcion);
        }
    }


    // CLASE MISION
    class Mision
    {
        public string Nombre { get; private set; }

        public bool LlegoAnomalia { get; private set; }
        public bool NucleoEncontrado { get; private set; }
        public bool Sobrevivio { get; private set; }
        public bool Regreso { get; private set; }

        public Mision()
        {
            Nombre = "La anomalia perdida";

            LlegoAnomalia = false;
            NucleoEncontrado = false;
            Sobrevivio = false;
            Regreso = false;
        }

        public void LlegarAnomalia()
        {
            LlegoAnomalia = true;
        }

        public void EncontrarNucleo()
        {
            NucleoEncontrado = true;
        }

        public void Sobrevivir()
        {
            Sobrevivio = true;
        }

        public void Regresar()
        {
            Regreso = true;
        }

        public bool EstaCompletada()
        {
            return LlegoAnomalia &&
                   NucleoEncontrado &&
                   Sobrevivio &&
                   Regreso;
        }

        public void Mostrar()
        {
            Console.WriteLine("\n===== MISION =====");
            Console.WriteLine("Mision: " + Nombre);

            Console.WriteLine(
                "[ " + (LlegoAnomalia ? "X" : " ") +
                " ] Llegar a la zona de anomalia"
            );

            Console.WriteLine(
                "[ " + (NucleoEncontrado ? "X" : " ") +
                " ] Encontrar el nucleo energetico"
            );

            Console.WriteLine(
                "[ " + (Sobrevivio ? "X" : " ") +
                " ] Sobrevivir al encuentro"
            );

            Console.WriteLine(
                "[ " + (Regreso ? "X" : " ") +
                " ] Regresar al punto de extraccion"
            );
        }
    }


    // CLASE SISTEMA NEXUS
    class SistemaNexus
    {
        private Cadete cadete;
        private Mision mision;

        private Zona bosque;
        private Zona ruinas;
        private Zona anomalia;
        private Zona extraccion;

        public SistemaNexus()
        {
            mision = new Mision();

            bosque = new Zona(
                "Bosque de Cristal",
                "Una zona llena de vegetacion y señales energeticas."
            );

            ruinas = new Zona(
                "Ruinas de IRIS",
                "Antiguas estructuras con señales de un robot."
            );

            anomalia = new Zona(
                "Zona de Anomalia",
                "Una enorme alteracion energetica."
            );

            extraccion = new Zona(
                "Punto de Extraccion",
                "Zona segura para regresar a la base."
            );
        }


        
        // INICIAR
        public void Iniciar()
        {
            Console.Title = "NEXUS v1.0 - Sistema de Inmersion";

            MostrarInicio();

            CrearCadete();

            Menu();
        }


        // INICIO
        private void MostrarInicio()
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(" ");
            Console.WriteLine("NEXUS");
            Console.WriteLine(" ");
            Console.WriteLine(" ");

            Console.ResetColor();

            Console.WriteLine(
                "\nMISIÓN: LA ANOMALIA PERDIDA"
            );

            Console.WriteLine(
                "\nPresiona ENTER para comenzar..."
            );

            Console.ReadLine();
        }


        // CREAR CADETE
        private void CrearCadete()
        {
            Console.Clear();

            Console.WriteLine(
                "IDENTIFICACION DEL CADETE"
            );

            Console.Write("\nNombre del cadete: ");

            string nombre = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                nombre = "Cadete Explorer";
            }

            cadete = new Cadete(nombre);

            Console.WriteLine(
                "\nIdentificacion completada."
            );

            Console.WriteLine(
                "Bienvenido, Cadete " + cadete.Nombre
            );

            // OBJETOS INICIALES
            cadete.Inventario.Agregar(
                new Objeto(
                    "Celula de energia",
                    "Energia",
                    25
                )
            );

            cadete.Inventario.Agregar(
                new Objeto(
                    "Escudo portatil",
                    "Escudo",
                    20
                )
            );

            cadete.Inventario.Agregar(
                new Objeto(
                    "Analizador",
                    "Analizador",
                    1
                )
            );

            Console.WriteLine(
                "\nPresiona ENTER para continuar..."
            );

            Console.ReadLine();
        }


        // MENU
        private void Menu()
        {
            string opcion;

            do
            {
                Console.Clear();

                Console.ForegroundColor =
                    ConsoleColor.Cyan;

                Console.WriteLine(
                    " "
                );

                Console.WriteLine(
                    "       NEXUS // SISTEMA"
                );

                Console.WriteLine(
                    " "
                );

                Console.ResetColor();

                Console.WriteLine(
                    "\nCadete: " + cadete.Nombre
                );

                Console.WriteLine(
                    "Vida: " + cadete.Vida + "%"
                );

                Console.WriteLine(
                    "Energia: " + cadete.Energia + "%"
                );

                Console.WriteLine(
                    "XP: " + cadete.Experiencia
                );

                Console.WriteLine("\n1. Explorar");
                Console.WriteLine("2. Inventario");
                Console.WriteLine("3. Usar objeto");
                Console.WriteLine("4. Ver mision");
                Console.WriteLine("5. Ver estado");
                Console.WriteLine("0. Salir");

                Console.Write(
                    "\nOpcion: "
                );

                opcion = Console.ReadLine();

                switch (opcion)
                {
                    case "1":
                        Explorar();
                        break;

                    case "2":
                        cadete.Inventario.Mostrar();
                        Pausa();
                        break;

                    case "3":
                        UsarObjeto();
                        break;

                    case "4":
                        mision.Mostrar();
                        Pausa();
                        break;

                    case "5":
                        cadete.MostrarInformacion();
                        Pausa();
                        break;

                    case "0":
                        Console.WriteLine(
                            "\nNEXUS cerrado."
                        );
                        break;

                    default:
                        Console.WriteLine(
                            "\n Opcion invalida."
                        );

                        Console.WriteLine(
                            "El sistema continuara normalmente."
                        );

                        Pausa();
                        break;
                }

            } while (
                opcion != "0" &&
                cadete.Vida > 0 &&
                !mision.EstaCompletada()
            );


            if (mision.EstaCompletada())
            {
                MisionCompletada();
            }

            if (cadete.Vida <= 0)
            {
                Console.Clear();

                Console.ForegroundColor =
                    ConsoleColor.Red;

                Console.WriteLine(
                    "================================"
                );

                Console.WriteLine(
                    "       MISION FALLIDA"
                );

                Console.WriteLine(
                    "================================"
                );

                Console.WriteLine(
                    "\nEl cadete no logro regresar con vida."
                );

                Console.ResetColor();
            }
        }


        // EXPLORAR
        private void Explorar()
        {
            Console.Clear();

            Console.WriteLine(
                "========== EXPLORACION =========="
            );

            Console.WriteLine("\n1. " + bosque.Nombre);
            Console.WriteLine("2. " + ruinas.Nombre);
            Console.WriteLine("3. " + anomalia.Nombre);
            Console.WriteLine("4. " + extraccion.Nombre);
            Console.WriteLine("0. Regresar");

            Console.Write(
                "\nSelecciona una zona: "
            );

            string opcion = Console.ReadLine();

            switch (opcion)
            {
                case "1":
                    ExplorarBosque();
                    break;

                case "2":
                    ExplorarRuinas();
                    break;

                case "3":
                    ExplorarAnomalia();
                    break;

                case "4":
                    ExplorarExtraccion();
                    break;

                case "0":
                    break;

                default:
                    Console.WriteLine(
                        "\n Zona invalida."
                    );

                    Pausa();
                    break;
            }
        }


        // BOSQUE
        private void ExplorarBosque()
        {
            Console.Clear();

            bosque.Mostrar();

            Console.WriteLine(
                "\n OBJETO ENCONTRADO"
            );

            Console.WriteLine(
                "Encontraste una celula de energia."
            );

            cadete.Inventario.Agregar(
                new Objeto(
                    "Celula de energia",
                    "Energia",
                    25
                )
            );

            cadete.GastarEnergia(5);

            Console.WriteLine(
                "\n Evento: perdida de energia -5"
            );

            Console.WriteLine(
                "\nTambien encontraste informacion."
            );

            cadete.GanarExperiencia(10);

            Console.WriteLine(
                "+10 XP"
            );

            Pausa();
        }


        // RUINAS
        private void ExplorarRuinas()
        {
            Console.Clear();

            ruinas.Mostrar();

            Console.WriteLine(
                "\n ENTIDAD DETECTADA"
            );

            Robot robot = new Robot("ROBOT R-07");

            Encuentro(robot);

            Pausa();
        }


        // ANOMALIA
        private void ExplorarAnomalia()
        {
            Console.Clear();

            anomalia.Mostrar();

            Console.ForegroundColor =
                ConsoleColor.Red;

            Console.WriteLine(
                "\n⚡ ANOMALIA DETECTADA"
            );

            Console.ResetColor();

            mision.LlegarAnomalia();

            Console.WriteLine(
                "\nLa energia de la zona es inestable."
            );

            cadete.GastarEnergia(10);

            Console.WriteLine(
                "Perdiste 10 puntos de energia."
            );

            Console.WriteLine(
                "\n Una entidad aparece frente a ti."
            );

            Enemigo enemigo =
                new Enemigo("ENTIDAD X");

            Encuentro(enemigo);

            if (cadete.Vida > 0)
            {
                Console.WriteLine(
                    "\n Encontraste el NUCLEO ENERGETICO."
                );

                cadete.Inventario.Agregar(
                    new Objeto(
                        "Nucleo energetico",
                        "Ataque",
                        1
                    )
                );

                mision.EncontrarNucleo();

                Console.WriteLine(
                    "+30 XP"
                );

                cadete.GanarExperiencia(30);

                mision.Sobrevivir();
            }

            Pausa();
        }


        // EXTRACCION
        private void ExplorarExtraccion()
        {
            Console.Clear();

            extraccion.Mostrar();

            if (
                mision.LlegoAnomalia &&
                mision.NucleoEncontrado &&
                mision.Sobrevivio &&
                cadete.Vida > 0
            )
            {
                Console.WriteLine(
                    "\n🚀 Punto de extraccion alcanzado."
                );

                mision.Regresar();

                Console.WriteLine(
                    "El cadete regreso con vida."
                );
            }
            else
            {
                Console.WriteLine(
                    "\n Extraccion denegada."
                );

                Console.WriteLine(
                    "Todavia no completaste todos los objetivos."
                );
            }

            Pausa();
        }


        // ENCUENTROS
        private void Encuentro(Personaje personaje)
        {
            Console.WriteLine(
                "\n===== SISTEMA DE ENCUENTRO ====="
            );

            Console.WriteLine(
                "Entidad: " + personaje.Nombre
            );

            Console.WriteLine(
                "\n1. Atacar"
            );

            Console.WriteLine(
                "2. Escapar"
            );

            Console.WriteLine(
                "3. Utilizar objeto"
            );

            Console.WriteLine(
                "4. Analizar"
            );

            Console.Write(
                "\nAccion: "
            );

            string opcion = Console.ReadLine();

            if (opcion == "1")
            {
                Atacar(personaje);
            }
            else if (opcion == "2")
            {
                Escapar(personaje);
            }
            else if (opcion == "3")
            {
                UsarObjeto();
            }
            else if (opcion == "4")
            {
                AnalizarEntidad(personaje);
            }
            else
            {
                Console.WriteLine(
                    "\n Accion invalida."
                );

                Console.WriteLine(
                    "La entidad permanece activa."
                );
            }
        }


        // ATACAR
        private void Atacar(Personaje personaje)
        {
            if (personaje == null)
            {
                Console.WriteLine(
                    " No existe una entidad para atacar."
                );

                return;
            }

            Console.WriteLine(
                "\n Ataque ejecutado."
            );

            personaje.RecibirDanio(50);

            Console.WriteLine(
                "Daño causado: 50"
            );

            if (personaje.Vida <= 0)
            {
                Console.WriteLine(
                    "\n ENTIDAD DERROTADA."
                );

                cadete.GanarExperiencia(25);

                Console.WriteLine(
                    "+25 XP"
                );
            }
            else
            {
                Console.WriteLine(
                    "\nLa entidad sigue con vida."
                );

                personaje.Actuar(cadete);
            }
        }


        // ESCAPAR
        private void Escapar(Personaje personaje)
        {
            Console.WriteLine(
                "\n El cadete intenta escapar."
            );

            cadete.GastarEnergia(10);

            Console.WriteLine(
                "Escapaste del encuentro."
            );

            Console.WriteLine(
                "Energia -10"
            );
        }


        // ANALIZAR
        private void AnalizarEntidad(Personaje personaje)
        {
            if (personaje == null)
            {
                Console.WriteLine(
                    " No existe ninguna entidad."
                );

                return;
            }

            Console.WriteLine(
                "\n ANALISIS DE ENTIDAD"
            );

            personaje.MostrarInformacion();

            Console.WriteLine(
                "\nNEXUS recomienda actuar con precaucion."
            );
        }


        // USAR OBJETO
        private void UsarObjeto()
        {
            Console.Clear();

            cadete.Inventario.Mostrar();

            if (cadete.Inventario.EstaVacio())
            {
                Console.WriteLine(
                    "\n No tienes objetos."
                );

                Pausa();

                return;
            }

            Console.Write(
                "\nNumero del objeto: "
            );

            string entrada = Console.ReadLine();

            int numero;

            if (!int.TryParse(
                entrada,
                out numero
            ))
            {
                Console.WriteLine(
                    "\n Debes introducir un numero."
                );

                Pausa();

                return;
            }

            if (numero <= 0)
            {
                Console.WriteLine(
                    "\n Numero invalido."
                );

                Pausa();

                return;
            }

            cadete.Inventario.Usar(
                numero,
                cadete
            );

            Pausa();
        }


        // MISION COMPLETADA
        
        private void MisionCompletada()
        {
            Console.Clear();

            Console.ForegroundColor =
                ConsoleColor.Green;

            Console.WriteLine(
                " "
            );

            Console.WriteLine(
                "         MISION COMPLETADA"
            );

            Console.WriteLine(
                " "
            );

            Console.ResetColor();

            Console.WriteLine(
                "\nLa anomalia fue investigada."
            );

            Console.WriteLine(
                "El nucleo energetico fue recuperado."
            );

            Console.WriteLine(
                "El cadete sobrevivio al encuentro."
            );

            Console.WriteLine(
                "El cadete regreso al punto de extraccion."
            );

            Console.WriteLine(
                "\n RECOMPENSAS"
            );

            Console.WriteLine(
                "+100 XP"
            );

            cadete.GanarExperiencia(100);

            Console.WriteLine(
                "+1 Modulo desconocido"
            );

            cadete.Inventario.Agregar(
                new Objeto(
                    "Modulo desconocido",
                    "Analizador",
                    1
                )
            );

            Console.WriteLine(
                "\n FIN DE LA MISION "
            );

            Pausa();
        }


        // PAUSA
        private void Pausa()
        {
            Console.WriteLine(
                "\nPresiona ENTER para continuar..."
            );

            Console.ReadLine();
        }
    }


    // PROGRAM
    
    class Program
    {
        static void Main()
        {
            SistemaNexus nexus =
                new SistemaNexus();

            nexus.Iniciar();
        }
    }
}
