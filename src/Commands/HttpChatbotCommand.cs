using AIOChatbot.Configurations;
using AIOChatbot.Inferences;
using AIOChatbot.Llms;
using AIOChatbot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AIOChatbot.Commands;

public class HttpChatbotCommand : ICommandAction
{
    private readonly IConfig config;
    private readonly IEnumerable<IInferenceWorkflow> inferenceWorkflows;
    private readonly ILogger<HttpChatbotCommand> logger;

    public HttpChatbotCommand(IConfig config, IEnumerable<IInferenceWorkflow> inferenceWorkflows, ILogger<HttpChatbotCommand> logger)
    {
        this.config = config;
        this.inferenceWorkflows = inferenceWorkflows;
        this.logger = logger;
    }
    public string Name => "httpchatbot";
    const string AcceptedHttpMethod = "POST";
    public bool LongRunning => true;

    public async Task ExecuteAsync(IConfiguration argsConfiguration, CancellationToken cancellationToken)
    {
        var inferenceWorkflow = inferenceWorkflows.GetInferenceWorkflow(config);

        var listener = new HttpListener();
        listener.Prefixes.Add(config.ChatbotHttpEndpoint);
        listener.Start();

        logger.LogInformation("Listening on: {chatbotHttpEndpoint}", config.ChatbotHttpEndpoint);

        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;

            try
            {
                context = await listener.GetContextAsync().WaitAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            var request = context.Request;
            var response = context.Response;

            try
            {
                if (request.HttpMethod == AcceptedHttpMethod)
                {
                    await ProcessRequestAsync(request, response, inferenceWorkflow, cancellationToken);
                }
                else
                {
                    CreateResponse(response, new ChatbotHttpErrorResponse { Message = $"http method is invalid, needs to be: {AcceptedHttpMethod}" });
                }
            }
            catch (JsonException)
            {
                CreateResponse(response, new ChatbotHttpErrorResponse { Message = "http request body is not valid" });
            }
            finally
            {
                response.Close();
            }
        }

        logger.LogInformation("Stopping: {chatbotHttpEndpoint}", config.ChatbotHttpEndpoint);
        listener.Stop();
    }

    private static async Task ProcessRequestAsync(
        HttpListenerRequest request,
        HttpListenerResponse response,
        IInferenceWorkflow inferenceWorkflow,
        CancellationToken cancellationToken)
    {
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            var json = await reader.ReadToEndAsync(cancellationToken);
            var req = JsonSerializer.Deserialize<ChatbotHttpRequest>(json);
            if (req is not null && req.Query is not null)
            {
                ChatHistory? chatHistory;
                if (req.ChatHistory is not null)
                {
                    chatHistory = new ChatHistory
                    {
                        Chats = req.ChatHistory
                    };
                }
                else
                {
                    chatHistory = null;
                }

                var res = await inferenceWorkflow.ExecuteAsync(req.Query, chatHistory, null, cancellationToken);

                if (res.ErrorMessage is not null)
                {
                    CreateResponse(response, new ChatbotHttpErrorResponse
                    {
                        Message = res.ErrorMessage,
                        InferenceDurationInMilliseconds = res.DurationInMilliseconds
                    });
                }
                else
                {
                    CreateResponse(response, new ChatbotHttpResponse
                    {
                        Bot = res.Text,
                        InferenceDurationInMilliseconds = res.DurationInMilliseconds
                    });
                }
            }
            else
            {
                CreateResponse(response, new ChatbotHttpErrorResponse { Message = "http request body is not valid" });
            }
        }
    }

    private static void CreateResponse(HttpListenerResponse response, ChatbotHttpErrorResponse chatbotHttpErrorResponse)
    {
        CreateResponse(response, 400, JsonSerializer.Serialize(chatbotHttpErrorResponse));
    }

    private static void CreateResponse(HttpListenerResponse response, ChatbotHttpResponse chatbotHttpResponse)
    {
        CreateResponse(response, 200, JsonSerializer.Serialize(chatbotHttpResponse));
    }

    private static void CreateResponse(HttpListenerResponse response, int status, string message)
    {
        response.StatusCode = status;
        var buffer = System.Text.Encoding.UTF8.GetBytes(message);
        response.ContentLength64 = buffer.Length;
        var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
    }
}
