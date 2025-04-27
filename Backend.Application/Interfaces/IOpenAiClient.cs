using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Application.Interfaces
{
    public interface IOpenAiClient<TRequest, TResponse>
    {
        /// <summary>
        /// Végrehajt egy OpenAI‐kérést a TRequest‐ben leírt input alapján,
        /// és deszerializálja a választ TResponse típusra.
        /// </summary>
        Task<TResponse> ExecuteAsync(TRequest request);
    }

}
