
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using System.Net;

namespace MyDurableFunctionApp
{
    public class MyActivityFunction
    {
        private readonly ILogger<MyActivityFunction> _logger;

        public MyActivityFunction(ILogger<MyActivityFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(MyActivityFunction))]
        public async Task<string> Run(
            [ActivityTrigger] string input)
        {
            _logger.LogInformation("Processing activity with input: {input}", input);
            
            // Simulate work
            
            return await Task.FromResult(input.ToUpper());
        }
    }

    public class MyOrchestrationFunction
    {
        [Function(nameof(MyOrchestrationFunction))]
        public async Task<string> Run(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger<MyOrchestrationFunction>();
            string? input = context.GetInput<string>();
            input ??= string.Empty;
            
            logger.LogInformation("Orchestration started with input: {input}", input);
            
            string result = await context.CallActivityAsync<string>(
                nameof(MyActivityFunction),
                input);
            
            logger.LogInformation("Orchestration completed with result: {result}", result);
            return result;
        }
    }

    public class DurableFunctionStarter
    {
        [Function(nameof(DurableFunctionStarter))]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(DurableFunctionStarter));
            
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(MyOrchestrationFunction),
                input: "Hello, Durable World!");
            
            logger.LogInformation("Started orchestration with ID = {instanceId}", instanceId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { instanceId });
            return response;
        }
    }
}
