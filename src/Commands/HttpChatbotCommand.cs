﻿using AIOChatbot.Configurations;
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
                    await ProcessRequestAsync(config, request, response, inferenceWorkflow, cancellationToken);
                }
                else
                {
                    CreateResponse(response, new ChatbotHttpErrorResponse { Message = $"http method is invalid, needs to be: {AcceptedHttpMethod}" });
                }
            }
            catch (JsonException jEx)
            {
                logger.LogError(jEx, "http request body is not valid");
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
        IConfig config,
        HttpListenerRequest request,
        HttpListenerResponse response,
        IInferenceWorkflow inferenceWorkflow,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var req = JsonSerializer.Deserialize<ChatbotHttpRequest>(json);
        if (req is not null && req.Query is not null)
        {
            if (config.DisableInferenceInputs && req.StepsInputs?.Count > 0)
            {
                CreateResponse(response, new ChatbotHttpErrorResponse
                {
                    Message = "StepsInputs is not allowed"
                }, 403);
                return;
            }

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

            var res = await inferenceWorkflow.ExecuteAsync(req.Query, chatHistory, req.StepsInputs, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (res.ErrorMessage is not null)
            {
                CreateResponse(response, new ChatbotHttpErrorResponse
                {
                    Message = res.ErrorMessage,
                    InferenceDurationInMilliseconds = res.DurationInMilliseconds
                }, res.IsInternalError == true ? 500 : 400);
            }
            else
            {
                if (res.Text is null)
                {
                    CreateResponse(response, new ChatbotHttpErrorResponse
                    {
                        Message = "no response from bot",
                        InferenceDurationInMilliseconds = res.DurationInMilliseconds
                    });
                    return;
                }

                var documents = new List<ChatbotDocumentHttpResponse>();
                if (res.Documents is not null)
                {
                    for (int i = 0; i < res.Documents.Length; i++)
                    {
                        string id = $"[doc{i}]";
                        if (res.Text.Contains(id))
                        {
                            var text = res.Documents[i].Text;
                            if (text is null)
                            {
                                continue;
                            }

                            documents.Add(new ChatbotDocumentHttpResponse
                            {
                                Id = id,
                                Content = text,
                                Title = res.Documents[i].Title,
                                Source = res.Documents[i].Source
                            });
                        }
                    }
                }

                CreateResponse(response, new ChatbotHttpResponse
                {
                    Bot = res.Text,
                    Intents = res.Intents,
                    InferenceDurationInMilliseconds = res.DurationInMilliseconds,
                    TotalCompletionTokens = res.TotalCompletionTokens,
                    TotalPromptTokens = res.TotalPromptTokens,
                    TotalEmbeddingTokens = res.TotalEmbeddingTokens,
                    Documents = documents,
                    StepOutputs = req.EnableDiagnosticLog == true ?
                        res.Steps?.ToDictionary(step => step.Name ?? throw new Exception("step name is null"), step => step.Outputs) : null
                });
            }
        }
        else
        {
            CreateResponse(response, new ChatbotHttpErrorResponse { Message = "http request body is not valid" });
        }
    }

    private static void CreateResponse(HttpListenerResponse response, ChatbotHttpErrorResponse chatbotHttpErrorResponse, int status = 400)
    {
        CreateResponse(response, status, JsonSerializer.Serialize(chatbotHttpErrorResponse));
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
        response.ContentType = "application/json";
        var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
    }
}
