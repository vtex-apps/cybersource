namespace service.Controllers
{
    using Cybersource.Models;
    using Cybersource.Services;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System;
    using Vtex.Api.Context;

    public class EventsController : Controller
    {
        private readonly IIOServiceContext _context;
        private readonly IVtexApiService _vtexApiService;

        public EventsController(IIOServiceContext context, IVtexApiService vtexApiService)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._vtexApiService = vtexApiService ?? throw new ArgumentNullException(nameof(vtexApiService));
        }

        public void AllStates(string account, string workspace)
        {
            string bodyAsText = new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync().Result;
            AllStatesNotification allStatesNotification = JsonConvert.DeserializeObject<AllStatesNotification>(bodyAsText);
            _context.Vtex.Logger.Debug("Order Broadcast", null, $"Notification {bodyAsText}");
            bool success = _vtexApiService.ProcessNotification(allStatesNotification).Result;
            if (!success)
            {
                _context.Vtex.Logger.Warn("Order Broadcast", null, $"Failed to Process Notification {bodyAsText}");
                throw new Exception("Failed to Process Notification");
            }
        }
    }
}
