namespace ConsoleApp
{
    using System;
    using DPUruNet;
    using System.Text.Json;
    class Program
    {



        internal static void Main(string[] args)
        {
            //NOTA: Poner Try -Catch en caso de error en la validacion de resolucion
            //NOTA: Repetir la captura en caso de error o no capturar nada
            //NOTA: Pasar el array a una lista para mayor flexibilidad
            //NOTA: Agregar el indice de resultado de las huellas comparadas
            //NOTA: Devolver por consola el resultado de la comparacion
            //Terminar programa si pasan mas de 10 segundos sin capturar huella

            Fmd huella = null;
            Fmd huella2 = null;

            if (args.Length > 0 && args[0] == "0")
            //if (true)
            {
                //Console.Write("Apagando lector...");
                ApagarLector();
                return;
            }
            else if (args.Length > 0 && args[0] == "1")
            {
                //Console.Write("Creando Huella...");

                CrearHuella();
            }
            else if (args.Length > 0 && args[0] == "2")
            {
                //Console.Write("Capturando Huella...");

                //Capturar Huella sencilla
                CapturarHuella();
            }
            else if (args.Length > 2 && args[0] == "3")
            {


                string textoHuella1 = args[1];
                string textoHuella2 = args[2];

                //Comparar dos huellas
                huella = Fmd.DeserializeXml(textoHuella1);
                huella2 = Fmd.DeserializeXml(textoHuella2);

                //return;

                if (huella == null || huella2 == null)
                {
                    Console.Write("Error. No se pudo crear alguna de las huellas.");
                    return;
                }

                CompareResult resultado = Comparison.Compare(huella, 0, huella2, 0);

                Console.Write("Correcto. Resultado: " + resultado.Score);

            }
            else if (args.Length > 2 && args[0] == "4")
            {
                string textoHuellaCapturada = args[1];
                string rutaArchivoJson = args[2];

                try
                {
                    // 1. Deserializar la huella del dedo puesto en el lector
                    var fmdCapturada = Fmd.DeserializeXml(textoHuellaCapturada);

                    // 2. Leer archivo y deserializar lista
                    if (!File.Exists(rutaArchivoJson))
                    {
                        Console.Write("Error: El archivo no existe en la ruta proporcionada.");
                        return;
                    }

                    string jsonContenido = File.ReadAllText(rutaArchivoJson);

                    // Configuración para que ignore mayúsculas/minúsculas en el JSON
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var listaClientes = JsonSerializer.Deserialize<List<ClienteData>>(jsonContenido, opciones);

                    string cedulaEncontrada = "NOT_FOUND";

                    if (listaClientes != null)
                    {
                        // 3. Comparación masiva en memoria
                        foreach (var cliente in listaClientes)
                        {
                            try
                            {
                                var fmdBase = Fmd.DeserializeXml(cliente.HuellaXml);
                                CompareResult resultado = Comparison.Compare(fmdCapturada, 0, fmdBase, 0);

                                // Umbral estándar 2147
                                if (resultado.Score < 2147)
                                {
                                    cedulaEncontrada = cliente.Cedula;
                                    break;
                                }
                            }
                            catch { continue; }
                        }
                    }

                    Console.Write(cedulaEncontrada);
                }
                catch (Exception ex)
                {
                    Console.Write("Error: " + ex.Message);
                }
            }

            //Console.ReadKey();
            return;
        }

        //Apagar lector

        static void ApagarLector()
        {
            ReaderCollection lectores;
            Reader lector;

            //Lectores de huellas
            lectores = ReaderCollection.GetReaders();

            if (lectores.Count < 1)
            {
                Console.Write("Correcto. No hay lectores conectados.");
                return;
            }

            lector = lectores[0];

            //Iniciar Lector
            lector.Open(Constants.CapturePriority.DP_PRIORITY_EXCLUSIVE);

            if (lector.Capabilities.Resolutions.Length < 1)
            {
                Console.Write("Error. El lector no tiene resoluciones disponibles.");
                return;
            }


            try
            {
                lector.Dispose();
                Console.Write("Correcto. Lector apagado correctamente.");
            }
            catch (Exception ex)
            {
                Console.Write("Error al apagar el lector: " + ex.Message);
            }

            lectores.Dispose();
        }

        static Fmd CrearHuella()
        {
            ReaderCollection lectores;
            Reader lector;
            CaptureResult resultadoDeCaptura = null;
            Fid[] capturasFid = { null, null, null, null };
            Fmd[] capturasFmd = { null, null, null, null };
            Fmd fmdFinal = null;
            string mensaje = "";
            bool esExito = true;

            //Lectores de huellas
            lectores = ReaderCollection.GetReaders();

            //Verifica si hay lectores conectados
            if (lectores.Count < 1)
            {
                Console.Write("Error. No hay lectores conectados.");
                return null;
            }

            //Muestra los lectores conectados

            //foreach (Reader l in lectores)
            //{
            //    Console.Write("Lector: " + l.Description.SerialNumber);
            //}

            //Selecciona el primer lector

            lector = lectores[0];

            //Console.Write("Lector: " + lector.Description.SerialNumber);

            //Iniciar Lector
            lector.Open(Constants.CapturePriority.DP_PRIORITY_EXCLUSIVE);

            try
            {
                if (lector.Capabilities.Resolutions.Length < 1)
                {
                    Console.Write("Error. El lector no tiene resoluciones disponibles.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.Write("Error al abrir el lector: " + ex.Message);
                return null;
            }

            //Captura de huella

            try
            {
                for (int i = 0; i < 4; i++)
                {
                    //Console.Write("Coloca el dedo en el lector para capturar la huella " + (i + 1) + ": ");
                    resultadoDeCaptura = lector.Capture(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, 5000, lector.Capabilities.Resolutions[0]);
                    esExito = (resultadoDeCaptura.ResultCode == Constants.ResultCode.DP_SUCCESS) && esExito;
                    if (esExito)
                    {
                        capturasFid[i] = resultadoDeCaptura.Data;
                        capturasFmd[i] = FeatureExtraction.CreateFmdFromFid(resultadoDeCaptura.Data, Constants.Formats.Fmd.ANSI).Data;
                        //Console.Write("Captura " + (i + 1) + " exitosa: " + resultadoDeCaptura.Score);
                    }
                    else
                    {
                        continue;
                        //Console.Write("Captura " + (i + 1) + " fallida: " + resultadoDeCaptura.ResultCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("Error durante la captura: " + ex.Message);
                return null;
            }

            //Crear FMD final
            if (!esExito)
            {
                lector.Dispose();
                Console.Write("Error. No se pudo crear el FMD final por errores en la captura.");
                return null;
            }

            try
            {
                fmdFinal = Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ANSI, capturasFmd).Data;
            }
            catch (Exception ex)
            {
                Console.Write("Error durante la creación del FMD final: " + ex.Message);
                return null;
            }


            string fmdJson = Fmd.SerializeXml(fmdFinal);

            //Console.Write("Fmd Final: " + fmdJson);

            lector.Dispose();

            lectores.Dispose();

            Console.Write(fmdJson);

            return fmdFinal;
        }

        static Fmd CapturarHuella()
        {
            ReaderCollection lectores;
            Reader lector;
            CaptureResult resultadoDeCaptura = null;
            Fid capturaFid = null;
            Fmd fmdFinal = null;
            bool esExito = false;

            //Lectores de huellas
            lectores = ReaderCollection.GetReaders();

            //Verifica si hay lectores conectados
            if (lectores.Count < 1)
            {
                Console.Write("Error. No hay lectores conectados.");
                return null;
            }

            //Muestra los lectores conectados

            //foreach (Reader l in lectores)
            //{
            //    Console.Write("Lector: " + l.Description.SerialNumber);
            //}

            //Selecciona el primer lector

            lector = lectores[0];

            //Console.Write("Lector: " + lector.Description.SerialNumber);

            //Iniciar Lector
            lector.Open(Constants.CapturePriority.DP_PRIORITY_EXCLUSIVE);

            try
            {
                if (lector.Capabilities.Resolutions.Length < 1)
                {
                    Console.Write("Error. El lector no tiene resoluciones disponibles.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.Write("Error al abrir el lector: " + ex.Message);
                return null;
            }

            //Captura de huella

            try
            {
                //Console.Write("Coloca el dedo en el lector para capturar la huella");
                resultadoDeCaptura = lector.Capture(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, 5000, lector.Capabilities.Resolutions[0]);
                esExito = (resultadoDeCaptura.ResultCode == Constants.ResultCode.DP_SUCCESS);
                if (esExito)
                {
                    capturaFid = resultadoDeCaptura.Data;
                    fmdFinal = FeatureExtraction.CreateFmdFromFid(resultadoDeCaptura.Data, Constants.Formats.Fmd.ANSI).Data;
                    //Console.Write("Captura exitosa: " + resultadoDeCaptura.Score);
                }
                else
                {
                    Console.Write("Error. Captura fallida: " + resultadoDeCaptura.ResultCode);
                }

            }
            catch (Exception ex)
            {
                Console.Write("Error durante la captura: " + ex.Message);
                esExito = false;
            }

            //Crear FMD final
            if (!esExito)
            {
                lector.Dispose();
                lectores.Dispose();
                //Console.Write("No se pudo crear el FMD final por errores en la captura.");
                return null;
            }

            string fmdJson = Fmd.SerializeXml(fmdFinal);

            //Console.Write(fmdJson);

            lector.Dispose();
            lectores.Dispose();

            Console.Write(fmdJson);

            return fmdFinal;
        }
    }

    // Clase de apoyo para System.Text.Json
    public class ClienteData
    {
        public required string Cedula { get; set; }
        public required string HuellaXml { get; set; }
    }
}