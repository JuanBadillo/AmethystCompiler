using Interfaz.Clases;
using Interfaz.Clases.Facade;
using Interfaz.Clases.IO;
using Interfaz.Connection;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Interfaz
{
    public partial class FormPrincipal : Form {
        LexicoFacade lexicoFacade = null;
        SintaxisFacade sintaxisFacade = null;
        SemanticaFacade semanticaFacade = null;
        TripletasFacade tripletasFacade = null;
        EnsambladorFacade ensambladorFacade = null;


        List<string> instruccionesTripletasEnsamblador;

        SaveFileDialog saveFileDialog = new SaveFileDialog();
        Dictionary<string, Identificador> identificadores;
        bool codigoConErrores = false;

        //Extension de archivo fuente
        readonly OpenFileDialog openFileDialog = new OpenFileDialog() {
            AddExtension = true,
            Filter = "Amethyst (*.amth)|*.amth",
            DefaultExt = "amth"
            //Filter = "ALScript (*.alscript)|*.alscript",
            //DefaultExt = "alscript"
        };

        public FormPrincipal() {
            InitializeComponent();
            inicializarRTXBX();
            sintaxisFacade = new SintaxisFacade();
            semanticaFacade = new SemanticaFacade();
            tripletasFacade = new TripletasFacade();
            ensambladorFacade = new EnsambladorFacade();
            identificadores = new Dictionary<string, Identificador>();
        }

        public string pathret()
        {
            return txtCodificacion.Text;
        }

        public FormPrincipal(string file) : this() {
            if (file != string.Empty && Path.GetExtension(file).ToLower().Equals(".alscript")) {
                var texto = OutputArchivo.Cargar(file);
                txtCodificacion.Text = texto;
            }
        }

        /// <summary>
        /// Acciones Click
        /// </summary>
        #region Acciones Click
        private void btnLimpiar_Click(object sender, EventArgs e) {
            txtCodificacion.Text = "";
            limpiarTodo();
            
            txtCodificacion.ReadOnly = false;
            dgvIdentificadores.Rows.Clear();
            dgvErrores.Rows.Clear();            
        }

        /* Compilar Lexico completo */
        private void btnCompilar_Click(object sender, EventArgs e) {
            if (string.IsNullOrEmpty(txtCodificacion.Text) || string.IsNullOrWhiteSpace(txtCodificacion.Text)) {
                MessageBox.Show("Favor de generar un codigo para poderlo compilar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            limpiarTodo();
            lexicoFacade = new LexicoFacade();
            
            bool tieneErrores = compilarLexico();
            codigoConErrores = tieneErrores;
            if (tieneErrores) MessageBox.Show("Programa compilado con algunos errores, favor de verificar tabla de errores.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                MessageBox.Show("Programa compilado correctamente.", "¡Éxito!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtCodificacion.Select(0, 999);
                Color customColor = Color.FromArgb(80, 36, 108);
                txtCodificacion.SelectionBackColor = customColor;
            }
            btnGuardarArchivoToken.Enabled = !tieneErrores;
            btnSintaxis.Enabled = true;

            //Crea archivo para guardar el codigo
            string filePath = "C:\\Users\\juanm\\source\\repos\\Interfaz\\bin\\Debug\\codigoEntrada.txt";
            // Obtener el texto del TextBox y guardar el codigo
            string textToSave = txtCodificacion.Text;
            try
            {
                // Escribir el texto en el archivo
                File.WriteAllText(filePath, textToSave);
            }
            catch (Exception ex)
            {
                // Informar de errores ocurridos
                MessageBox.Show("An error occurred while saving the file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        #region Matriz
        /* Conexion a matriz para lexico linea por linea */
        private string CONNECTION_STRING_NAME = "Data Source=NITRO-AN515\\SQLEXPRESS;Initial Catalog=MatrizAutomatas;Integrated Security=True";
        public SqlConnection conexion = null;
        private static MatrizConnection _instance = null;
        private string TABLA_MATRIZ = "[dbo].[matriz$]";
        #endregion
        #region Diccionario
        // Diccionario de identificadores
        Dictionary<string, int> idenDictionary = new Dictionary<string, int>();
        int idenCounter = 1;
        #endregion

        /* Compilar Lexico linea por linea*/
        private void btnLexicoDebug_Click(object sender, EventArgs e)
        {
            string seleccion = "ca";
            string estado = "1";
            string palabraEvaluada = "";
            string lineaTokens = "";
            string tokenFile = "";            

            // Obtener todas las líneas del RichTextBox
            string[] lines = txtCodificacion.Lines;
            int lineNumber = 1; // Line number counter

            // Procesar cada línea del codigo fuente
            foreach (string line in lines)
            {
                txtLineaEvalua.Text = line;
                txtTokens.Text = "";
                lineaTokens = "";               
                txtNumLinea1.Text = lineNumber.ToString();
                txtNumLinea2.Text = lineNumber.ToString();

                // Agregar delimitadores especiales a la línea actual
                string inputCode = line + "♠♦";
                inputCode = inputCode.Replace(" ", "♠♦");

                //Regex
                string pattern = @"(\S)([();\{\}])|([();\{\}])(\S)|(\()(\S+)(\))|(\{)(\S+)(\})";
                inputCode = Regex.Replace(inputCode, pattern, "$1♠♦$2$3♠♦$4♠♦$5").Trim();
                inputCode = Regex.Replace(inputCode, @"(?:♠♦)+", "♠♦");
                inputCode = inputCode.Replace("♠♠♦♦", "");
                inputCode = Regex.Replace(inputCode, @"(^♠♦)+", "", RegexOptions.Multiline);

                // Procesar cada carácter de la línea actual
                foreach (char letter in inputCode)
                {
                    string charEvaluado = letter.ToString();
                    seleccion = "C" + letter.ToString();

                    // Condiciones para letras minúsculas
                    if (letter >= 'i' && letter <= 'z')
                    {
                        seleccion = "c" + letter.ToString();
                    }
                    // Condiciones para letras mayúsculas
                    else if (letter >= 'A' && letter <= 'Z')
                    {
                        seleccion = "C" + letter.ToString() + letter.ToString();
                    }
                    // Condiciones para otros caracteres especiales
                    switch (letter)
                    {
                        case '.':
                            seleccion = "Cpunto";
                            break;
                        case '!':
                            seleccion = "Cnot";
                            break;
                        case '}':
                            seleccion = "C}";
                            break;
                        case '{':
                            seleccion = "C{";
                            break;
                        case ')':
                            seleccion = "C)";
                            break;
                        case '(':
                            seleccion = "C(";
                            break;
                        case '♠':
                            seleccion = "FDC";
                            charEvaluado = "";
                            break;
                        case '♦':
                            seleccion = "CAT";
                            charEvaluado = "";
                            break;
                    }

                    //SQL Matriz
                    try
                    {
                        // Abrir la conexión
                        using (SqlConnection conexion = new SqlConnection(CONNECTION_STRING_NAME))
                        {
                            conexion.Open();

                            // Query SQL
                            string query = "SELECT [" + seleccion + "] FROM [dbo].[matriz$] WHERE Estado = @estado";
                            using (SqlCommand comando = new SqlCommand(query, conexion))
                            {
                                //MessageBox.Show(inputCode); 
                                comando.Parameters.AddWithValue("@estado", estado);

                                var resultado = comando.ExecuteScalar();

                                if (resultado != null)
                                {
                                    txtEstado.Text = estado;
                                    txtEstadoSig.Text = resultado.ToString();
                                    palabraEvaluada = palabraEvaluada + charEvaluado;
                                    txtPalabra.Text = palabraEvaluada;
                                    estado = resultado.ToString();

                                    if (seleccion == "CAT" && estado[0] != '2')
                                    {
                                        estado = "1";
                                        
                                        //Detectar Identificadores
                                        if (resultado.ToString() == "IDEN")
                                        {
                                            if (!idenDictionary.ContainsKey(palabraEvaluada))
                                            {
                                                // Nuevo IDEN, agregar al diccionario e incrementar el contador
                                                idenDictionary[palabraEvaluada] = idenCounter;
                                                idenCounter++;
                                                this.dgvIdentificadores.Rows.Add(resultado.ToString() + "#" + idenDictionary[palabraEvaluada], palabraEvaluada, "N/A", "");
                                            }

                                            // Agregar el IDEN con su numero correspondiente
                                            lineaTokens += resultado.ToString() + "#" + idenDictionary[palabraEvaluada] + " ";

                                            
                                        }
                                        else
                                        {
                                            // Tokens No-IDEN
                                            lineaTokens += resultado.ToString() + " ";
                                        }
                                        palabraEvaluada = "";
                                        txtTokens.Text = lineaTokens;
                                    }

                                }
                                else
                                {
                                    MessageBox.Show("No se encontraron resultados.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }

                    MessageBox.Show("Evaluando");
                }

                if (string.IsNullOrEmpty(tokenFile))
                {
                    tokenFile = lineaTokens; // No new line on the first iteration
                }
                else
                {
                    tokenFile = tokenFile + "\n" + lineaTokens; // Add a new line before subsequent tokens
                }

                txtLexico.Text = tokenFile;
                lineNumber++;
            }


                   
        }




        /* Compilar Sintactico*/
        private void btnSintaxis_Click(object sender, EventArgs e) {
            bool tieneErrores = compilarSintaxis();
            codigoConErrores = tieneErrores;
            if (tieneErrores)
            {
                MessageBox.Show("Existen errores de sintaxis. Favor de revisar el codigo.", "Errores de sintaxis", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("¡Programa correcto!", "Operacion exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCodificacion.Select(0, 999); 
                Color customColor = Color.FromArgb(80, 36, 108);
                txtCodificacion.SelectionBackColor = customColor;
            }
        }

        /* Compilar Semantico*/
        private void btnSemantica_Click(object sender, EventArgs e) {
            bool tieneErrores = compilarSemantica();
            codigoConErrores = tieneErrores;
            if(tieneErrores) MessageBox.Show("Existen errores de semantica. Favor de revisar el codigo.", "Errores de semantica", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else MessageBox.Show("¡Programa correcto!", "Operacion exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /* Compilar Codigo Intermedio*/
        private void btnTripletas_Click(object sender, EventArgs e) {
            generarCodigoIntermedio();
            MessageBox.Show("¡Programa correcto!", "Operacion exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /* Compilar Ensamblador*/
        private void btnEnsamblador_Click(object sender, EventArgs e) {
            ejecutarEnsamblador();
            MessageBox.Show("¡Programa correcto!", "Operacion exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);            
        }

        /* Compilar todo*/
        private void btnRunAll_Click(object sender, EventArgs e) {
            btnCompilar.PerformClick();
            btnSintaxis.PerformClick();
            btnSemantica.PerformClick();
            btnTripletas.PerformClick();
            btnEnsamblador.PerformClick();
        }

        /* Guardar archivo de tokens*/
        private void btnGuardarArchivoToken_Click(object sender, EventArgs e) {
            saveFileDialog = new SaveFileDialog {
                Title = "Guardar archivo de tokens",
                Filter = "Amethyst Token (*.amtt)|*.amtt",
                DefaultExt = "amtt",
                AddExtension = true,
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                OutputArchivo.Guardar(txtLexico.Text, saveFileDialog.FileName);
                MessageBox.Show("¡Archivo de tokens guardado correctamente!", "Guardado", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        /* Guardar archivo de codigo fuente*/
        private void btnGuardarCodigo_Click(object sender, EventArgs e) {
            saveFileDialog = new SaveFileDialog {
                Filter = "Amethyst (*.amth)|*.amth",
                Title = "Guardar codigo",
                DefaultExt = "amth",
                AddExtension = true,
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                OutputArchivo.Guardar(txtCodificacion.Text, saveFileDialog.FileName);
                MessageBox.Show("¡Codigo guardado correctamente!", "Guardado", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
            }
        }

        /* Abrir archivo de codigo fuente*/
        private void btnCargarCodigo_Click(object sender, EventArgs e) {
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                var codigo = OutputArchivo.Cargar(openFileDialog.FileName);
                btnLimpiar.PerformClick();
                txtCodificacion.Text = codigo;
            }
            AddLineNumbersLineaCodigo();
        }

        /* Ver ASM*/
        private void button1_Click(object sender, EventArgs e)
        {
            if (codigoConErrores) return;
            CodigoEnsamblador codigoEnsamblador = new CodigoEnsamblador();
            codigoEnsamblador.Show();
        }

        #endregion

        /// <summary>
        /// Metodos
        /// </summary>
        #region Metodos
        private bool compilarLexico() {
            bool tieneErrores = false;
            Compilado resultado = lexicoFacade.compilarCodigo(txtCodificacion.Text.Replace("\n", " \n") + " ");

            txtLexico.Text = resultado.CodigoCompilado;

            if (resultado.Errores.Count > 0) {
                tieneErrores = true;
                cargarErrores(resultado.Errores);
                pintar(resultado.PalabrasARemarcarError);
            }

            if (resultado.Identificadores.Count > 0) {
                identificadores = new Dictionary<string, Identificador>();
                foreach(Identificador i in resultado.Identificadores) identificadores.Add("IDEN#" + i.Secuencial, i);

                cargarIdentificadores();                
            }

            return tieneErrores;
        }

        private bool compilarSintaxis()
        {
            if (codigoConErrores) return true;

            // Realizar el análisis sintáctico y obtener el resultado como RichTextBox
            RichTextBox resultado = sintaxisFacade.sintaxisGo(txtSintaxis, txtLexico);

            // Convertir el texto del RichTextBox a una lista de strings
            List<string> lineas = new List<string>(resultado.Lines);

            // Variable para verificar si hay errores
            bool tieneErrores = false;

            // Iterar sobre las líneas de texto
            for (int i = 0; i < lineas.Count; i++)
            {
                // Verificar si la línea actual contiene la cadena "ERR"
                if (lineas[i].Contains("ERR"))
                {
                    string palabraErronea = lineas[i - 1];
                    if (lineas[i - 1].Contains("ASIG")) { palabraErronea = "="; }
                    if (lineas[i - 1].Contains("PRI6"))   { palabraErronea = "declarar";}
                    if (lineas[i - 1].Contains("PRI17")) {  palabraErronea = "mientras"; }
                    if (lineas[i - 1].Contains("PRI4")) {  palabraErronea = "leer"; }
                    if (lineas[i - 1].Contains("PRI5")) { palabraErronea = "imprimir"; }

                    int numeroBloque = ObtenerNumeroBloque(txtSintaxis.Text, lineas[i - 1]);

                    var indices = ObtenerIndicesLinea(txtCodificacion, numeroBloque-1);
                    int indiceInicio = indices.Item1;
                    int longitudLinea = indices.Item2;

                    dgvErrores.Rows.Add("ERR", numeroBloque, "Error de sintaxis", palabraErronea);

                    // Mostrar un mensaje de error indicando la línea afectada
                    MessageBox.Show($"Error de sintaxis en la línea {i}: {lineas[i - 1]}", "Error de sintaxis", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    txtCodificacion.Select(indiceInicio, longitudLinea);
                    txtCodificacion.SelectionBackColor = Color.Red;

                    // Establecer la bandera de tieneErrores a true
                    tieneErrores = true;
                }
            }

            // Devolver true si hay errores, de lo contrario, false
            return tieneErrores;
        }

        private int ObtenerNumeroBloque(string texto, string cadenaBuscada)
        {
            // Dividir el texto en bloques usando líneas en blanco como separadores
            string[] bloques = texto.Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None);

            // Iterar sobre los bloques para buscar la cadena en cada uno
            for (int i = 0; i < bloques.Length; i++)
            {
                if (bloques[i].Contains(cadenaBuscada) && bloques[i].Contains("ERR"))
                {
                    // La cadena se encuentra en este bloque, por lo que retornamos el número de bloque
                    return i + 1; // Sumamos 1 porque los índices comienzan en 0 pero queremos mostrar números de bloque basados en 1
                }
            }

            // Si la cadena no se encuentra en ningún bloque, retornamos -1
            return -1;
        }

        private (int, int) ObtenerIndicesLinea(RichTextBox richTextBox, int numeroLinea)
        {
            int indiceInicio = -1;
            int longitudLinea = -1;

            // Verificar que el número de línea sea válido
            if (numeroLinea >= 0 && numeroLinea < richTextBox.Lines.Length)
            {
                // Obtener el índice de inicio y la longitud de la línea
                indiceInicio = richTextBox.GetFirstCharIndexFromLine(numeroLinea);
                longitudLinea = richTextBox.Lines[numeroLinea].Length;
            }

            return (indiceInicio, longitudLinea);
        }

        private bool compilarSemantica() {
            if(codigoConErrores) return true;

            ///PASO 1: Verificar apertura/cierre de llaves
            ///....
            if(semanticaFacade.determinarErrorLlaves(txtLexico)) {
                txtSemantica.Text = "ERR --> Es necesario revisar la apertura/cierre de llaves";
                return true;
            }

            ///PASO 2: Verificar declaraciones repetidas
            ///....
            string declaraciones = semanticaFacade.determinarErrorDeclaracionesRepetidas(txtLexico);
            if(declaraciones != null) {
                string identificador = "";
                foreach(KeyValuePair<string, Identificador> id in identificadores) {
                    Identificador i = id.Value; if(declaraciones.Equals(i.Secuencial.ToString())) identificador = i.Nombre;
                }
                txtSemantica.Text = $"ERR --> Variable [{identificador}] ya fue declarada anteriormente.\n";
                return true;
            }

            ///PASO 3: Actualizar tipos de datos
            ///....
            identificadores = semanticaFacade.actualizarTiposDeDatos(txtLexico, txtSintaxis, identificadores);
            cargarIdentificadores();

            ///PASO 4: Verificar tipos de datos
            ///....
            txtSemantica = semanticaFacade.determinarErrorTipoDatos(txtLexico, txtSemantica, identificadores);
            if(txtSemantica.Text.Contains("ERR")) {
                if(!txtSemantica.Text.Contains("declarada")) txtSemantica.Text += "ERR --> Verificar tipos de datos..";
                return true;
            } else {
                txtSemantica.Text += "\n\n<---------------------------------------------->\n";
            }

            ///PASO 5: Verificar gramatica de semantica
            ///....
            txtSemantica = semanticaFacade.semanticaGo(txtSemantica, txtSintaxis);
            
            return txtSemantica.Text.Contains("ERR");
        }

        private void generarCodigoIntermedio() {
            if(codigoConErrores) return;
            instruccionesTripletasEnsamblador = tripletasFacade.buscarValoresInstrucciones(tripletasFacade.buscarInstruccionesAEvaluar(txtLexico), txtCodificacion);
            richTextBox1.Text = tripletasFacade.tripletasGo(instruccionesTripletasEnsamblador);
        }

        private void ejecutarEnsamblador() {
            if(codigoConErrores) return;
            ensambladorFacade.ensambladorGo(instruccionesTripletasEnsamblador);
        }

        private void pintar(List<string> palabras) {
            palabras.ForEach(palabra => {
                try {
                    txtCodificacion.Select(txtCodificacion.Text.IndexOf(palabra), palabra.Length);
                    txtCodificacion.SelectionBackColor = Color.Red;
                } catch (Exception) { }
            });
        }

        private void cargarErrores(List<Error> errores) {
            dgvErrores.Rows.Clear();

            foreach (Error e in errores) {
                dgvErrores.Rows.Add(e.Token, e.Linea, lexicoFacade.obtenerDescripcionError(e), e.Palabra);
            }
        }

        private void cargarIdentificadores() {
            dgvIdentificadores.Rows.Clear();

            foreach(KeyValuePair<string, Identificador> id in identificadores) {
                Identificador i = id.Value;
                dgvIdentificadores.Rows.Add(i.getToken(), i.Nombre, lexicoFacade.determinarTipoDato(i.TipoDato), i.Valor);
            }
        }

        private void limpiarTodo() {
            txtLexico.Text = "";
            txtSintaxis.Text = "";
            txtSemantica.Text = "";
            richTextBox1.Text = "";
            dgvErrores.Rows.Clear();
            dgvIdentificadores.Rows.Clear();
        }

        #region RichTextBox
        private void inicializarRTXBX() {
            txtNumeracionCodificacion.Font = txtCodificacion.Font;
            txtCodificacion.Select();
            AddLineNumbersLineaCodigo();
            txtLexico.Font = txtLexico.Font;
            txtLexico.Select();
            AddLineNumbersCompilacion();
            txtCodificacion.Select();
            txtCodificacion.SelectionStart = txtCodificacion.TextLength;
        }

        private void AddLineNumbersLineaCodigo() {
            // create & set Point pt to (0,0)    
            Point pt = new Point(0, 0);
            Point pt2 = new Point(0, 0);
            // get First Index & First Line from the richTextBoxs  
            int First_Index = txtCodificacion.GetCharIndexFromPosition(pt);
            int First_Line = txtCodificacion.GetLineFromCharIndex(First_Index);

            txtCodificacion.GetCharIndexFromPosition(pt);
            txtCodificacion.GetLineFromCharIndex(First_Index);
            // set X & Y coordinates of Point pt to ClientRectangle Width & Height respectively    
            pt.X = ClientRectangle.Width;
            pt.Y = ClientRectangle.Height;

            pt2.X = ClientRectangle.Width;
            pt2.Y = ClientRectangle.Height;
            // get Last Index & Last Line from the richTextBoxs    
            int Last_Index = txtCodificacion.GetCharIndexFromPosition(pt);
            int Last_Line = txtCodificacion.GetLineFromCharIndex(Last_Index);

            int Last_Index2 = txtLexico.GetCharIndexFromPosition(pt2);
            int Last_Line2 = txtLexico.GetLineFromCharIndex(Last_Index2);
            // set Center alignment to LineNumberTextBox    
            txtNumeracionCodificacion.SelectionAlignment = HorizontalAlignment.Center;

            txtNumeracionCompilacion.SelectionAlignment = HorizontalAlignment.Center;
            // set LineNumberTextBox text to null & width to getWidth() function value    
            txtNumeracionCodificacion.Text = "";
            txtNumeracionCodificacion.Width = getWidthLineaCodigo();

            txtNumeracionCompilacion.Text = "";
            txtNumeracionCompilacion.Width = getWidthLineaCodigo();
            // now add each line number to LineNumberTextBox upto last line    
            for (int i = First_Line; i <= Last_Line + 1; i++) {
                txtNumeracionCodificacion.Text += i + 1 + "\n";
            }

            for (int i = First_Line; i <= Last_Line + 1; i++) {
                txtNumeracionCompilacion.Text += i + 1 + "\n";
            }
        }

        private void AddLineNumbersCompilacion() {
            // create & set Point pt to (0,0)    
            Point pt = new Point(0, 0);
            // get First Index & First Line from the richTextBoxs  
            int First_Index = txtLexico.GetCharIndexFromPosition(pt);
            int First_Line = txtLexico.GetLineFromCharIndex(First_Index);
            // set X & Y coordinates of Point pt to ClientRectangle Width & Height respectively    
            pt.X = ClientRectangle.Width;
            pt.Y = ClientRectangle.Height;
            // get Last Index & Last Line from the richTextBoxs    
            int Last_Index = txtLexico.GetCharIndexFromPosition(pt);
            int Last_Line = txtLexico.GetLineFromCharIndex(Last_Index);
            // set Center alignment to LineNumberTextBox    
            txtNumeracionCompilacion.SelectionAlignment = HorizontalAlignment.Center;
            // set LineNumberTextBox text to null & width to getWidth() function value    
            txtNumeracionCompilacion.Text = "";
            txtNumeracionCompilacion.Width = getWidthCompilacion();
            // now add each line number to LineNumberTextBox upto last line    
            for (int i = First_Line; i <= Last_Line + 1; i++) {
                txtNumeracionCompilacion.Text += i + 1 + "\n";
            }
        }

        private int getWidthLineaCodigo() {
            int w = 25;
            // get total lines of richTextBoxLineaCodigo 
            int line = txtNumeracionCodificacion.Lines.Length;

            if (line <= 99) {
                w = 20 + (int)txtNumeracionCodificacion.Font.Size;
            } else if (line <= 999) {
                w = 30 + (int)txtNumeracionCodificacion.Font.Size;
            } else {
                w = 50 + (int)txtNumeracionCodificacion.Font.Size;
            }

            return w;
        }

        private int getWidthCompilacion() {
            int w = 25;
            // get total lines of richTextBoxCompilacion
            int line = txtNumeracionCompilacion.Lines.Length;

            if (line <= 99) {
                w = 20 + (int)txtNumeracionCompilacion.Font.Size;
            } else if (line <= 999) {
                w = 30 + (int)txtNumeracionCompilacion.Font.Size;
            } else {
                w = 50 + (int)txtNumeracionCompilacion.Font.Size;
            }

            return w;
        }
        #endregion
        #endregion

        /// <summary>
        /// Eventos
        /// </summary>
        #region Eventos
        private void Form1_Resize(object sender, EventArgs e) {
            AddLineNumbersLineaCodigo();
            AddLineNumbersCompilacion();
            //MessageBox.Show("Tamaño" + this.Width + this.Height);
        }

        private void txtCodificacion_SelectionChanged(object sender, EventArgs e) {
            Point pt = txtCodificacion.GetPositionFromCharIndex(txtCodificacion.SelectionStart);
            if (pt.X == 1) {
                AddLineNumbersLineaCodigo();
            }
        }

        private void txtCodificacion_VScroll(object sender, EventArgs e) {
            txtNumeracionCodificacion.Text = "";
            AddLineNumbersLineaCodigo();
            txtNumeracionCodificacion.Invalidate();
        }

        private void txtCodificacion_TextChanged(object sender, EventArgs e) {
            string strCodigo = txtCodificacion.Text;
            int ic = 2;
            txtNumeracionCodificacion.Text = "1";
            foreach (var c in strCodigo)
            {
                if ((int)c == 10)
                {
                    txtNumeracionCodificacion.Text += "\n" + ic;
                    ic++;
                    txtNumeracionCodificacion.SelectionStart = txtNumeracionCodificacion.TextLength;
                    txtNumeracionCodificacion.ScrollToCaret();
                }
            }
            txtCodificacion.ReadOnly = false;
            

            btnSintaxis.Enabled = !btnSintaxis.Enabled;

            List<string> definedKeywords = new List<string> { "imprimir", "declarar", "leer", "mientras" };
            //ApplyKeywordsColor(definedKeywords, Color.Plum);

            //ApplyVarColor("variable", Color.SteelBlue);

            //ApplyNumberColor(Color.DarkSeaGreen);

            //ApplyCustomColor(Color.SkyBlue);

            //ApplyEncapsulatedColor("##", "##", Color.OliveDrab);
            //ApplyEncapsulatedColor("\"", "\"", Color.DarkSalmon);

            //ApplySymbolColor(Color.White);

            
        }

        private void ApplyVarColor(string keyword, Color color)
        {
            int originalSelectionStart = txtCodificacion.SelectionStart;
            int originalSelectionLength = txtCodificacion.SelectionLength;

            int index = 0;
            while (index < txtCodificacion.TextLength)
            {
                index = txtCodificacion.Find(keyword, index, RichTextBoxFinds.WholeWord);
                if (index == -1) break;

                // Guardar la posición y longitud de la selección actual
                int selectionStart = txtCodificacion.SelectionStart;
                int selectionLength = txtCodificacion.SelectionLength;

                // Cambiar el color de la palabra
                txtCodificacion.Select(index, keyword.Length);
                txtCodificacion.SelectionColor = color;

                // Restaurar la selección original
                txtCodificacion.Select(selectionStart, selectionLength);

                index += keyword.Length;
            }

            // Restaurar la posición y longitud de la selección original
            txtCodificacion.Select(originalSelectionStart, originalSelectionLength);
        }

        private void ApplyKeywordsColor(List<string> keywords, Color color)
        {
            int originalSelectionStart = txtCodificacion.SelectionStart;
            int originalSelectionLength = txtCodificacion.SelectionLength;

            foreach (string keyword in keywords)
            {
                int index = 0;
                while (index < txtCodificacion.TextLength)
                {
                    index = txtCodificacion.Find(keyword, index, RichTextBoxFinds.WholeWord);
                    if (index == -1) break;

                    // Guardar la posición y longitud de la selección actual
                    int selectionStart = txtCodificacion.SelectionStart;
                    int selectionLength = txtCodificacion.SelectionLength;

                    // Cambiar el color de la palabra clave
                    txtCodificacion.Select(index, keyword.Length);
                    txtCodificacion.SelectionColor = color;

                    // Restaurar la selección original
                    txtCodificacion.Select(selectionStart, selectionLength);

                    index += keyword.Length;
                }
            }

            // Restaurar la posición y longitud de la selección original
            txtCodificacion.Select(originalSelectionStart, originalSelectionLength);
        }

        private void ApplyEncapsulatedColor(string startDelimiter, string endDelimiter, Color color)
        {
            int originalSelectionStart = txtCodificacion.SelectionStart;
            int originalSelectionLength = txtCodificacion.SelectionLength;

            int index = 0;
            while (index < txtCodificacion.TextLength)
            {
                index = txtCodificacion.Find(startDelimiter, index, RichTextBoxFinds.None);
                if (index == -1) break;

                int endIndex = txtCodificacion.Find(endDelimiter, index + startDelimiter.Length, RichTextBoxFinds.None);
                if (endIndex == -1) break;

                // Guardar la posición y longitud de la selección actual
                int selectionStart = txtCodificacion.SelectionStart;
                int selectionLength = txtCodificacion.SelectionLength;

                // Cambiar el color del texto encapsulado, incluyendo los delimitadores
                txtCodificacion.Select(index, endIndex - index + endDelimiter.Length);
                txtCodificacion.SelectionColor = color;

                // Restaurar la selección original
                txtCodificacion.Select(selectionStart, selectionLength);

                index = endIndex + endDelimiter.Length;
            }

            // Restaurar la posición y longitud de la selección original
            txtCodificacion.Select(originalSelectionStart, originalSelectionLength);
        }

        private void ApplyNumberColor(Color color)
        {
            int originalSelectionStart = txtCodificacion.SelectionStart;
            int originalSelectionLength = txtCodificacion.SelectionLength;

            // Definir un patrón de expresión regular para encontrar números
            string pattern = @"\b\d+\b";
            Regex regex = new Regex(pattern);

            // Buscar coincidencias de números en el texto
            MatchCollection matches = regex.Matches(txtCodificacion.Text);

            // Iterar sobre las coincidencias y aplicar el color a cada número
            foreach (Match match in matches)
            {
                int startIndex = match.Index;
                int length = match.Length;

                // Cambiar el color del número
                txtCodificacion.Select(startIndex, length);
                txtCodificacion.SelectionColor = color;
            }

            // Restaurar la posición y longitud de la selección original
            txtCodificacion.Select(originalSelectionStart, originalSelectionLength);
        }

        private void ApplySymbolColor(Color color)
        {
            int originalSelectionStart = txtCodificacion.SelectionStart;
            int originalSelectionLength = txtCodificacion.SelectionLength;

            // Lista de símbolos a buscar
            string[] symbols = { "=", "-", "*", "/", "+", "(", ")", "{", "}", ":", ";", "<", ">","&","|","!" };

            // Iterar sobre cada símbolo y aplicar el color
            foreach (string symbol in symbols)
            {
                int index = 0;
                while (index < txtCodificacion.TextLength)
                {
                    index = txtCodificacion.Find(symbol, index, RichTextBoxFinds.None);
                    if (index == -1) break;

                    // Cambiar el color del símbolo
                    txtCodificacion.Select(index, symbol.Length);
                    txtCodificacion.SelectionColor = color;

                    index += symbol.Length;
                }
            }

            // Restaurar la posición y longitud de la selección original
            txtCodificacion.Select(originalSelectionStart, originalSelectionLength);
        }

        private void ApplyCustomColor(Color color)
        {
            int originalSelectionStart = txtCodificacion.SelectionStart;
            int originalSelectionLength = txtCodificacion.SelectionLength;

            // Palabras clave específicas a excluir
            List<string> excludedKeywords = new List<string> { "imprimir", "declarar", "leer", "mientras", "variable" };

            // Patrón de expresión regular para encontrar palabras no definidas
            string pattern = @"\b(?!(" + string.Join("|", excludedKeywords.Select(Regex.Escape)) + @")\b)(?![0-9])\w+\b";

            // Excluir palabras entre ##
            pattern += @"|##(?:.(?!##))*##";

            // Excluir palabras entre ""
            pattern += @"|""(?:.(?!""))*""";

            Regex regex = new Regex(pattern);

            // Buscar coincidencias de palabras no definidas en el texto
            MatchCollection matches = regex.Matches(txtCodificacion.Text);

            // Iterar sobre las coincidencias y aplicar el color a cada palabra no definida
            foreach (Match match in matches)
            {
                int startIndex = match.Index;
                int length = match.Length;

                // Cambiar el color de la palabra no definida
                txtCodificacion.Select(startIndex, length);
                txtCodificacion.SelectionColor = color;
            }

            // Restaurar la posición y longitud de la selección original
            txtCodificacion.Select(originalSelectionStart, originalSelectionLength);
        }


        private void txtCodificacion_FontChanged(object sender, EventArgs e) {
            txtNumeracionCodificacion.Font = txtCodificacion.Font;
            txtCodificacion.Select();
            AddLineNumbersLineaCodigo();
        }

        private void txtLineaCodigo_FontChanged(object sender, EventArgs e) {
            txtNumeracionCodificacion.Font = txtCodificacion.Font;
            txtCodificacion.Select();
            AddLineNumbersLineaCodigo();
        }

        private void txtLineaCodigo_MouseDown(object sender, MouseEventArgs e) {
            txtCodificacion.Select();
            txtNumeracionCodificacion.DeselectAll();
        }

        //--------------------------------------------------------------------------//
        private void txtCompilacion_SelectionChanged(object sender, EventArgs e) {
            Point pt = txtLexico.GetPositionFromCharIndex(txtLexico.SelectionStart);
            if (pt.X == 1) {
                AddLineNumbersCompilacion();
            }
        }

        private void txtCompilacion_VScroll(object sender, EventArgs e) {
            txtNumeracionCompilacion.Text = "";
            AddLineNumbersCompilacion();
            txtNumeracionCompilacion.Invalidate();
        }

        private void txtCompilacion_TextChanged(object sender, EventArgs e) {
            if (txtLexico.Text == "") {
                AddLineNumbersCompilacion();
            }
        }

        private void txtCompilacion_FontChanged(object sender, EventArgs e) {
            txtNumeracionCompilacion.Font = txtLexico.Font;
            txtLexico.Select();
            AddLineNumbersCompilacion();
        }

        private void txtNumeracionCompilacion_FontChanged(object sender, EventArgs e) {
            txtNumeracionCompilacion.Font = txtLexico.Font;
            txtLexico.Select();
            AddLineNumbersCompilacion();
        }

        private void txtNumeracionCompilacion_MouseDown(object sender, MouseEventArgs e) {
            txtLexico.Select();
            txtNumeracionCompilacion.DeselectAll();
        }
        #endregion

        /* -~-~- */
        #region -~-~-
        private void FormPrincipal_Load(object sender, EventArgs e)
        {

        }
        private void dgvErrores_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void dgvIdentificadores_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void txtNumeracionCompilacion_TextChanged(object sender, EventArgs e)
        {

        }
        private void txtNumeracionCodificacion_TextChanged(object sender, EventArgs e)
        {

        }
        private void button2_Click(object sender, EventArgs e)
        {
            generarCodigoIntermedio();
            MessageBox.Show("¡Programa correcto!", "Operacion exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }
        private void button2_Click_1(object sender, EventArgs e)
        {
            List<string> definedKeywords = new List<string> { "imprimir", "declarar", "leer", "mientras", "si", "siNo" };
            ApplyKeywordsColor(definedKeywords, Color.Plum);

            ApplyVarColor("variable", Color.SteelBlue);

            ApplyNumberColor(Color.DarkSeaGreen);

            ApplyCustomColor(Color.SkyBlue);

            ApplyEncapsulatedColor("##", "##", Color.OliveDrab);
            ApplyEncapsulatedColor("\"", "\"", Color.DarkSalmon);

            ApplySymbolColor(Color.White);
        }
        private void txtSintaxis_TextChanged(object sender, EventArgs e)
        {

        }
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
        private void txtSemantica_TextChanged(object sender, EventArgs e)
        {

        }
        #endregion
    }
}