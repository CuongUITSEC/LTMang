using System.Data.SqlClient;
using LoginDemo.Model;

namespace LoginDemo.Service
{
    public class UserService
    {
        private readonly string connectionString = "Server=localhost;Database=LoginDemoDb;Trusted_Connection=True;";

        public User GetUserByUsername(string username)
        {
            User user = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Username, Password FROM Users WHERE Username = @Username";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new User
                        {
                            Username = reader.GetString(0),
                            Password = reader.GetString(1)
                        };
                    }
                }
            }

            return user;
        }
    }
}
