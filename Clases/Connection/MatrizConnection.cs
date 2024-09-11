using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Interfaz.Connection
{
    public class MatrizConnection
    {
        private string CONNECTION_STRING_NAME = "Data Source=NITRO-AN515\\SQLEXPRESS;Initial Catalog=MatrizAutomatas;Integrated Security=True";
        public SqlConnection conexion = null;
        private static MatrizConnection _instance = null;
        private string TABLA_MATRIZ = "[dbo].[matriz$]";

        /// <summary>
        /// Constructor de la clase MatrizConnection que establece la conexión a la base de datos.
        /// </summary>
        public MatrizConnection()
        {
            try
            {
                // Establecer la conexión utilizando la cadena de conexión proporcionada.
                conexion = new SqlConnection(CONNECTION_STRING_NAME);
            }
            catch (Exception ex)
            {
                // En caso de error al establecer la conexión, lanzar una excepción.
                throw new Exception("[Error] No se logró establecer la conexión a la base de datos. Adicional: " + ex.Message);
            }
        }

        /// <summary>
        /// Método para establecer una conexión con la base de datos.
        /// </summary>
        private void conectar()
        {
            if (conexion.State == System.Data.ConnectionState.Open)
                conexion.Close();

            conexion.Open();
        }

        /// <summary>
        /// Método para cerrar la conexión a la base de datos.
        /// </summary>
        private void desconectar()
        {
            conexion.Close();
        }

        /// <summary>
        /// Propiedad estática que devuelve una instancia única de MatrizConnection.
        /// </summary>
        public static MatrizConnection GetInstance
        {
            get
            {
                if (_instance == null) _instance = new MatrizConnection();
                return _instance;
            }
        }

        /// <summary>
        /// Realiza una consulta tipo SELECT que regresa un único valor.
        /// </summary>
        /// <param name="columna">Nombre de la columna a seleccionar (SELECT)</param>
        /// <param name="estado">Valor del estado a buscar (WHERE)</param>
        /// <returns>El valor único obtenido de la consulta.</returns>
        public string obtenerResultado(string columna, int estado)
        {
            conectar();

            string resultado = null;
            SqlCommand command = new SqlCommand(
                "SELECT m.[" + columna + "] FROM " + TABLA_MATRIZ + " m WHERE estado = @estado",
                conexion
            );

            command.Parameters.AddWithValue("@estado", estado);

            try
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        resultado = (string)reader[columna];
                    }
                }
            }
            catch { }

            desconectar();
            return resultado;
        }

        /// <summary>
        /// Realiza una búsqueda del token (Columna CAT) para determinado estado.
        /// </summary>
        /// <param name="estado">El estado que se está buscando.</param>
        /// <returns>El token encontrado.</returns>
        public string obtenerToken(int estado)
        {
            conectar();

            string resultado = null;
            SqlCommand command = new SqlCommand(
                "SELECT m.CAT FROM " + TABLA_MATRIZ + " m WHERE estado = @estado",
                conexion
            );

            command.Parameters.AddWithValue("@estado", estado);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    resultado = (string)reader["CAT"];
                }
            }

            desconectar();
            return resultado;
        }

        /// <summary>
        /// Obtiene la descripción para determinado error, en base al token único del error.
        /// </summary>
        /// <param name="token">Token asignado al error que se busca.</param>
        /// <returns>La descripción del error.</returns>
        public string obtenerErrorPorToken(string token)
        {
            conectar();

            string resultado = null;
            SqlCommand command = new SqlCommand(
                "SELECT m2.FDC FROM " + TABLA_MATRIZ + " m INNER JOIN " + TABLA_MATRIZ + " m2 ON m2.Estado = m.Estado + 1 WHERE m.cat = @token",
                conexion
            );

            command.Parameters.AddWithValue("@token", token);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    resultado = (string)reader["FDC"];
                }
            }

            desconectar();
            return resultado;
        }

        /// <summary>
        /// Obtiene el token del error para determinada descripción, en base al token único del error.
        /// </summary>
        /// <param name="descripcion">La descripción del error obtenida.</param>
        /// <returns>El token del error.</returns>
        public string obtenerErrorPorDescripcion(string descripcion)
        {
            conectar();

            string resultado = null;
            SqlCommand command = new SqlCommand(
                "SELECT m2.CAT FROM " + TABLA_MATRIZ + " m INNER JOIN " + TABLA_MATRIZ + " m2 ON m2.Estado = m.Estado - 1 WHERE m.FDC = @descripcion",
                conexion
            );

            command.Parameters.AddWithValue("@descripcion", descripcion);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    resultado = (string)reader["CAT"];
                }
            }

            desconectar();
            return resultado;
        }
    }
}
