using Diplo.Translator.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Diplo.Translator.Services
{
    public interface IHttpJsonService
    {
        Task<ResponseWrapper<R>> Post<D, R>(string endpoint, D data, Dictionary<string, string> queryParams = null, Dictionary<string, string> headers = null)
            where D : class
            where R : class;
    }
}