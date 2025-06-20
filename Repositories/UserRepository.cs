using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Threading;
using HomeCharts.Models;

namespace HomeCharts.Repositories
{
    public class UserRepository : RepositoryBase, IUserRepository
    {
        public bool AuthenticateUser(NetworkCredential credential)
        {
            bool validUser;
            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "select *from [User] where userName=@userName and [password]=@password";
                command.Parameters.Add("@username", SqlDbType.NChar).Value = credential.UserName;
                command.Parameters.Add("@password", SqlDbType.NChar).Value = credential.Password;
                validUser = command.ExecuteScalar() == null ? false : true;
            }

            return validUser;
        }

        public void Add(UserModel model)
        {
            throw new System.NotImplementedException();
        }

        public void Edit(UserModel model)
        {
            throw new System.NotImplementedException();
        }

        public void Remove(int id)
        {
            throw new System.NotImplementedException();
        }

        public UserModel GetById(int id)
        {
            throw new System.NotImplementedException();
        }

        public UserModel GetByUserName(string userName)
        {
            UserModel user = null;
            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "select *from [User] where userName=@userName";
                command.Parameters.Add("@username", SqlDbType.NChar).Value = userName;

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new UserModel
                        {
                            Id = reader[0].ToString(),
                            UserName = reader[1].ToString(),
                            Password = string.Empty,
                            Name = reader[3].ToString(),
                            LastName = reader[4].ToString(),
                            Email = reader[5].ToString()
                        };
                    }
                }
            }

            return user;
        }

        public IEnumerable<UserModel> GetByAll()
        {
            throw new System.NotImplementedException();
        }
    }
}