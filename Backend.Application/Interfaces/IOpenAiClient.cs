using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Application.Interfaces
{
    public interface IOpenAiClient<TRequest, TResponse>
    {
        Task<TResponse> ExecuteAsync(TRequest request);
    }

}
