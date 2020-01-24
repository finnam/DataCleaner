using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace DataCleaner.Utils
{
    public interface IRestApi
    {
        Task<string> GetEndPointAsync(string ep, string qp);
    }
}
