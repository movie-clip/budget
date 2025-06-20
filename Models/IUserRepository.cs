using System.Collections.Generic;
using System.Net;

namespace HomeCharts.Models
{
    public interface IUserRepository
    {
        bool AuthenticateUser(NetworkCredential credential);
        void Add(UserModel model);
        void Edit(UserModel model);
        void Remove(int id);
        UserModel GetById(int id);
        UserModel GetByUserName(string userName);
        IEnumerable<UserModel> GetByAll();
    }
}