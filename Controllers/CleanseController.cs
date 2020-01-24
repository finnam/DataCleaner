using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace DataCleaner.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class DataCleanerController: ControllerBase
    {
        const string BASE_URL = "https://preferenceprovider.herokuapp.com/";
        const ushort RETRIES = 3;

        const long DELAY_TICKS = 10000;
        private readonly ILogger<DataCleanerController> _logger;

        public DataCleanerController(ILogger<DataCleanerController> logger)
        {
            _logger = logger;
        }

        [HttpPost("cleanse")]
        public async Task<IActionResult> Post([FromBody] ExpandoObject p)
        {       
            try
            {  
                var id = ((dynamic) p).id.ToString();
                _logger.LogInformation($"cleanse: called for patient {id}.  ");

                var innerApi = new Utils.RestApi(BASE_URL);
                Func<Task<string>> post = async () => await innerApi.GetEndPointAsync("preferences", id);

                var prefs = await Retry(post, new TimeSpan(DELAY_TICKS), RETRIES); 
                
                if ( !String.IsNullOrEmpty(prefs))
                {
                    var jDoc = JsonDocument.Parse(prefs);
                    var pref = jDoc.RootElement.GetProperty("patientPreference");
                    if( pref.GetString().Equals("OBFUSCATE_ID"))
                    {
                       _logger.LogInformation($"cleanse: patient {id} obfuscated.  ");
                        var newId = jDoc.RootElement.GetProperty("newId");
                        JsonPatchDocument patch = new JsonPatchDocument();
                        patch.Replace("/id", newId.GetString());

                        patch.ApplyTo(p);
                    }
                    else
                    {
                       _logger.LogInformation($"cleanse: patient {id} clear text.  ");
                    }
                }
                else
                {
                    _logger.LogError($"cleanse: Gateway Timeout after {RETRIES} attempts.  ");
                    return StatusCode(StatusCodes.Status504GatewayTimeout);
                }
            }
            catch(Exception ex)
            {
                _logger.LogCritical($"cleanse: Internal Server Error {ex.Message}.  ");
                 StatusCode(StatusCodes.Status500InternalServerError);
            }

            return ( new JsonResult(p));
        }  

        public async Task<T> Retry<T>(Func<Task<T>> action, TimeSpan retryInterval, int retryCount) 
        {
            try
            {
                return await action();
            }
            catch when (retryCount != 0)
            {
                await Task.Delay(retryInterval);
                return await Retry(action, retryInterval, --retryCount);
            }
            catch 
            {
                return default(T);
            }
        }
    }
}
