using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Interfaz.Clases.Facade
{
    class EnsambladorFacade
    {
        public void ensambladorGo(List<string> instruccionesTripletasEnsamblador)
        {
            //var codigoConvertidoEnC = convertirVariablesACodigoC(instruccionesTripletasEnsamblador);

            //Paso 1. Generar un archivo con extension .C en la carpeta donde nos encontramos actualmente
            var rutaActual = Directory.GetCurrentDirectory();
            //var rutaArchivoC = rutaActual + "\\codigoC.c";
            //File.WriteAllText(rutaArchivoC, codigoConvertidoEnC);
            Traducir("C:\\Users\\juanm\\source\\repos\\Interfaz\\bin\\Debug\\codigoEntrada.txt", "C:\\Users\\juanm\\source\\repos\\Interfaz\\bin\\Debug\\codigoC.c");


            //Paso 2. Generar un archivo con el codigo ensamblador utilizando la sintaxis MASM
            var rutaArchivoEnsamblador = rutaActual + "\\codigoEnsamblador.asm";
            var comando = "gcc -S codigoC.c -o output.asm -masm=intel";

            var proceso = new Process();
            proceso.StartInfo.FileName = "cmd.exe";
            proceso.StartInfo.Arguments = "/C " + comando;
            proceso.StartInfo.WorkingDirectory = rutaActual;
            proceso.StartInfo.UseShellExecute = false;
            proceso.StartInfo.RedirectStandardOutput = true;
            proceso.StartInfo.RedirectStandardError = true;
            proceso.StartInfo.CreateNoWindow = true;
            proceso.Start();
            proceso.WaitForExit();
            //proceso.Close();


            //Paso 3. Leer el archivo de codigo ensamblador y colocarlo en la forma CodigoEnsamblador
            rutaArchivoEnsamblador = rutaArchivoEnsamblador.Replace("codigoEnsamblador", "output");
            var codigoEnsamblador = File.ReadAllText(rutaArchivoEnsamblador);
            var codigoEnsambladorForma = new CodigoEnsamblador(codigoEnsamblador);
            codigoEnsambladorForma.Show();
        }

        private string convertirVariablesACodigoC(List<string> codigo)
        {

            string lineas = "#include <stdio.h>\nint main() {\n";
            foreach (string linea in codigo)
            {
                System.Diagnostics.Debug.WriteLine(linea);

                if (linea.Contains("="))
                {
                    if (linea.Contains("declarar"))
                        lineas += $"\nint {linea.Replace("declarar", "")};";
                    else
                        lineas += $"\n{linea};";
                }

                if (linea.Contains("leer"))
                {
                    var segundaPalabra = linea.Split(' ')[1];
                    lineas += $"\nscanf(\"%i\", &{segundaPalabra});";
                }

                if (linea.Contains("mientras"))
                {
                    var segundaPalabra = linea.Split(' ')[1];
                    lineas += $"\nwhile(\"%i\", &{segundaPalabra});";
                }

                if (linea.Contains("imprimir"))
                {
                    var segundaPalabra = linea.Split(' ')[1];
                    if (segundaPalabra.Contains('\"'))
                    {
                        lineas += $"\nprintf({linea.Replace("imprimir ", "")});";
                    }
                    else
                    {
                        lineas += $"\nprintf(\"%i@\", {segundaPalabra});";
                    }
                }

            }
            lineas += $"\nprintf(\"@Compilacion finalizada..\"); \nreturn 0; }}";
            return lineas;
        }

        public static void Traducir(string rutaArchivoEntrada, string rutaArchivoSalida)
        {
            try
            {
                string contenidoEntrada = File.ReadAllText(rutaArchivoEntrada);

                // Remover comentarios entre ##
                contenidoEntrada = Regex.Replace(contenidoEntrada, "##.*?##", "", RegexOptions.Singleline);

                // Reemplazar declaraciones de variables
                contenidoEntrada = Regex.Replace(contenidoEntrada, @"declarar variable (\w+) = (.+);", "int $1 = $2;");

                // Reemplazar palabras clave del pseudocódigo por sus equivalentes en C
                contenidoEntrada = contenidoEntrada.Replace("mientras", "while");
                contenidoEntrada = contenidoEntrada.Replace("siNo", "else");
                contenidoEntrada = contenidoEntrada.Replace("si", "if");
                contenidoEntrada = contenidoEntrada.Replace("leer", "scanf");
                contenidoEntrada = contenidoEntrada.Replace("segun", "switch");
                contenidoEntrada = contenidoEntrada.Replace("caso", "case");
                contenidoEntrada = contenidoEntrada.Replace("romper", "break");
                contenidoEntrada = contenidoEntrada.Replace("cierto", "1");
                contenidoEntrada = contenidoEntrada.Replace("falso", "0");

                // Agregar paréntesis en las condiciones para while y if
                contenidoEntrada = Regex.Replace(contenidoEntrada, @"while\s+(\w+\s*[<>=!]+\s*\d+)", "while ($1)");
                contenidoEntrada = Regex.Replace(contenidoEntrada, @"if\s+(\w+\s*[<>=!]+\s*\d+)", "if ($1)");

                // Reemplazar las declaraciones printf
                contenidoEntrada = Regex.Replace(contenidoEntrada, @"imprimir\s+\""(.*?)\"";", "printf(\"$1\");");
                contenidoEntrada = Regex.Replace(contenidoEntrada, @"imprimir\s+(\w+);", "printf(\"%d\", $1);");

                // Ajustar el formato de las líneas con *
                contenidoEntrada = Regex.Replace(contenidoEntrada, @"(\d+ \* .*?)(?<!;)$", "$1;");

                // Agregar ; al final de las líneas que lo requieran en C
                contenidoEntrada = Regex.Replace(contenidoEntrada, @"(\d+ \* .*?)(?<!;)\s*$", "$1;");
                contenidoEntrada = contenidoEntrada.Replace("{", "{\n");
                contenidoEntrada = contenidoEntrada.Replace("}", "\n}");

                // Agregar i++ al final de la cláusula while
                contenidoEntrada = Regex.Replace(contenidoEntrada, @"while\s+\((.*?)\)\s*\{", "while ($1) {\n    i++;");

                // Englobar condiciones después de if en paréntesis
                contenidoEntrada = Regex.Replace(contenidoEntrada, @"if\s+(.*?)(?=\{)", "if ($1)");

                // Agregar cabecera de programa C y formatear salida
                string contenidoSalida = "#include <stdio.h>\n\nint main() {\n" + contenidoEntrada + "\n    return 0;\n}";

                // Remover cualquier ; después de una llave }
                contenidoSalida = Regex.Replace(contenidoSalida, @"}\s*;", "}\n");

                // Guardar el contenido traducido en un archivo .c
                File.WriteAllText(rutaArchivoSalida, contenidoSalida);
                Console.WriteLine("Pseudocódigo traducido y guardado en " + rutaArchivoSalida);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al traducir el pseudocódigo: " + ex.Message);
            }
        }

    }
}
