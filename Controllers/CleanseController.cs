using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        readonly Regex validator = new Regex("^[0-9]{4}$");
 
        readonly ILogger<DataCleanerController> _logger;

        public DataCleanerController(ILogger<DataCleanerController> logger)
        {
            _logger = logger;
        }

        [HttpPost("cleanse")]
        public async Task<IActionResult> Post([FromBody] ExpandoObject patient)
        {       
            try
            {   
                object v  = null;
                if(((IDictionary<String, object>)patient).TryGetValue("id", out v) && validator.IsMatch(v.ToString()) )
                {
                         
                    var id = ((dynamic) patient).id.ToString();

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

                            patch.ApplyTo(patient);
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
                else
                {
                    // Bad or missing Id
                    return StatusCode(StatusCodes.Status400BadRequest);
                }
            }
            catch(Exception ex)
            {
                _logger.LogCritical($"cleanse: Internal Server Error {ex.Message}.  ");
                 return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return ( new JsonResult(patient));
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
