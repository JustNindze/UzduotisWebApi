using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
namespace UzduotisWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserDataController : ControllerBase
    {
        private readonly ILogger<UserDataController> _logger;

        public UserDataController(ILogger<UserDataController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("create")]
        public Dictionary<string, object> Create(CreateRequest createRequest)
        {
            if (createRequest.ExpirationPeriod > UserData.MaxExpirationPeriod)
            {
                return GetMessage(Status.ExpirationPeriodError, 
                                  null, 
                                  string.Format("ExpirationPeriod value for '{0}' is greater than MaxExpirationPeriod value", createRequest.Key));
            }

            UserData.ModifyData(DataMethod.Create, createRequest.Key, createRequest.Value, createRequest.ExpirationPeriod);
            return GetMessage(Status.Ok, null, string.Format("Created '{0}' successfully", createRequest.Key));
        }

        [HttpPost]
        [Route("append")]
        public Dictionary<string, object> Append(AppendRequest appendRequest)
        {
            UserData.ModifyData(DataMethod.Append, appendRequest.Key, appendRequest.Value, null);
            return GetMessage(Status.Ok, null, string.Format("Added to '{0}' successfully", appendRequest.Key));
        }

        [HttpPost]
        [Route("delete")]
        public Dictionary<string, object> Delete(DeleteRequest deleteRequest)
        {
            if (UserData.ModifyData(DataMethod.Delete, deleteRequest.Key, null, null) == Status.KeyNotFound)
            {
                return GetMessage(Status.KeyNotFound, null, string.Format("Key '{0}' not found", deleteRequest.Key));
            }
            else
            {
                return GetMessage(Status.Ok, null, string.Format("Deleted '{0}' successfully", deleteRequest.Key));
            }
        }

        [HttpGet]
        [Route("get/{key}")]
        public Dictionary<string, object> Get(string key)
        {
            int period;
            try
            {
                period = Convert.ToInt32(UserData.Keys[key][0]);
            }
            catch (KeyNotFoundException)
            {
                return GetMessage(Status.KeyNotFound, null, string.Format("Key '{0}' not found", key));
            }
            UserData.Keys[key][1] = DateTime.Now.AddDays(period);
            return GetMessage(Status.OkGet, UserData.Data[key]);
        }

        //This method converts all messages and data into Dictionary which then is converted into json format
        [NonAction]
        public Dictionary<string, object> GetMessage(Status status, List<object> data, string message = null)
        {
            var messageJson = new Dictionary<string, object>();

            switch (status)
            {
                case Status.OkGet:
                    messageJson.Add("data", data);
                    break;
                case Status.Ok:
                case Status.Error:
                case Status.KeyNotFound:
                case Status.ExpirationPeriodError:
                    messageJson.Add(status == Status.Ok ? "success" : "error", new Dictionary<string, object> { { "message", message } });
                    break;
            }

            return messageJson;
        }
    }
}