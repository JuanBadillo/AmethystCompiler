using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Interfaz.Clases.Facade
{
    class SintaxisFacade
    {
        private List<Error> errores;
        private bool generaError = false;
        public SintaxisFacade()
        {
            errores = new List<Error>();
        }

        // Método principal que realiza el análisis sintáctico
        public RichTextBox sintaxisGo(RichTextBox txtSintaxis, RichTextBox txtLexico)
        {

            // Procesamiento previo de los datos léxicos para eliminar hashtags
            string[] lines = RemoverHashtags(txtLexico);
            for (int i = 0; i < lines.Length; i++)
            {               
                lines[i] = lines[i].TrimEnd();
                Console.WriteLine("[DEBUG - SintaxisFacade.cs] Hashtags eliminados ");
            }

            // Escribir las líneas procesadas en un archivo temporal
            File.WriteAllLines(@"..\..\Externos\lexicoTokens.tmpalscript", lines);

            // Obtener la ruta del analizador sintáctico desde la configuración
            string go = @System.Configuration.ConfigurationManager.AppSettings["sintax"];

            // Eliminar el archivo de resultados del análisis sintáctico si existe
            if (File.Exists(go + "sintaxisResult.tmpalscript"))
            {
                Console.WriteLine("[DEBUG] Existe sintaxisResult ");
                File.Delete(go + "sintaxisResult.tmpalscript");
            }

            // Iniciar un proceso para ejecutar el analizador sintáctico en segundo plano
            var sintaxis = new Process
            {
                StartInfo = {
                    FileName = "node",
                    WorkingDirectory = go,
                    Arguments = "AnalizadorSintactico.js",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            sintaxis.Start();

            // Leer continuamente el archivo de resultados hasta que esté disponible
            string[] resultadoAnalisisSintaxis = null;
            do
            {
                try
                {
                    resultadoAnalisisSintaxis = File.ReadAllLines(@"..\..\Externos\sintaxisResult.tmpalscript");
                    txtSintaxis.Lines = resultadoAnalisisSintaxis;
                }
                catch (Exception)
                {
                    // Manejar excepciones mientras se busca el archivo de resultados
                    //Console.WriteLine("[DEBUG] Buscando archivo...");
                }
            } while (resultadoAnalisisSintaxis == null);



            // Devolver el RichTextBox con los resultados del análisis sintáctico
            return txtSintaxis;
        }

        // Método para remover hashtags de las líneas de texto léxico
        private string[] RemoverHashtags(RichTextBox txtLexico)
        {
            var nuevasLineas = new List<string>();
            foreach (string linea in txtLexico.Lines)
            {
                // Reemplazar hashtags con "IDEN" utilizando una expresión regular
                var temp = Regex.Replace(linea, @"\bIDEN#[0-9]+", "IDEN");
                nuevasLineas.Add(temp);
            }

            // Devolver las líneas procesadas como un array de strings
            return nuevasLineas.ToArray();
        }
    }
}
