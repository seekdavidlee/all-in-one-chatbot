# Introduction

AIO (All-in-one) Chatbot is a .NET based solution that contains everything required for a Retrieval Augmented Generation (RAG) based Chatbot solution. The goal is for anyone to take this solution as-in and refactor or remove components. You will notice that it is monolithic codebase and this is intentional to allow for the Chatbot to run everything inside of a Container.

AIO Chatbot supports both the Azure OpenAI interface and also the ability to load language models locally. As an example, you may have a scenario where you are embedding with a local model, but your are using one of Azure OpenAI's hosted LLM like GPT 3.5.

## Design

The entry point of this solution is a Console based application that accepts arguments but most of the configurations come from environment variables. I should note that the arguments can also be environment variables. The following are the available commands.

1. chatbot - runs a chat experience in console
1. httpchatbot - starts a http server to process inference requests
1. ingest - ingest from configured data sources, run chunking and produce search models, embedding and finally create a search index, this is all done in a single replica
1. ingest-queue-processing - from queue, pull chunked search models, embedding and finally create a search index, this allows you to distribute load across replicas
1. delete-search - deletes a search index
1. import-ground-truths - import ground truths from Excel docs with a mapping file into Azure Blob storage
1. import-metrics - import metrics used during evaluation into Azure blob storage
1. local-evals - run evaluations locally, this allows you to load inference locally or you can also point to a hosted inference
1. remote-evals - run evaluations remotely
1. ingest-queue-evals - run a single evaluation unit-of-work, polls message from an Azure Storage Queue which is taking a ground truth and running it with a metric X number of times
1. ingest-queue-inference - run a inference, polls message from an Azure Storage Queue and running inference
1. summarize - generates a summary blob against all the evaluation results
1. show-metric-eval - shows the summary in console

Each of the command requires a specific set of configurations. More documentation will be coming shortly for this.

### Why .NET for this Solution?

Python is the language of choice for AI use cases because there are already tons of existing frameworks such as LangChain, Promptflow etc as well as libaries for chunking etc. This said, I believe this does not preclude other languages due to lack of framework or libraries. The goal at the end of the day is to statisfy your specific use cases for a RAG based Chatbot solution and with other languages such as .NET, this means you may have to have extra efforts to write custom code that may already be in a Python based framework or library.

That said, we should also note that one can takes parts of the solution and keep the rest in the language of your choice. For example, one can write the data source chunking as Python but leave the operational aspect of the solution for embedding and ingesting into search index in .NET.

Lastly, at least for me personally, given my background in .NET, to be able to see this solution working E2E in .NET, is such a great learning experience!

## High level Components

The solution contains the following interfaces for each of the components:

### Ingestion

Ingestion takes care of pulling data from various data sources, performing chunking, then vectorizing the Content/ embedding and finally persisting into a Vector database which can be Azure AI Search or ChromaDb. At the end of this process, your search index is created.

* IIngestionDataSource - generate search models from data source, chunking is done here as well because this is where the actual document characteristics determines your chunking strategy
* IEmbedding - your embedding interface like Azure OpenAI's hosted text-embedding-3-large.
* IVectorDb - vector db
* IngestionProcessor - this is a orchestration layer for IIngestionDataSource, IEmbedding and IVectorDb, it is possible to run parts of this work with replicas to help improve processing times, take a look at the `ingest-queue-processing` command

### Inference

This is the core part of a Chatbot which you can send a question and maybe a chat history for context and get response back. Internally it will run through a step called determine intent to get one or more search queries, get search documents back, and finally the LLM can summarize and produce a response with citations from the search documents returned.

* IInferenceWorkflow - runs inference locally or persist to a queue and let another replica process the request

### Evaluation

For a Retrieval Augmented Generation (RAG) based Chatbot solution, we need to ensure the response from the solution matches with our expectations given the responses are not guaranteed to be deterministic. This means doing experiments on your Ingestion and Inference components to figure out what inference prompts we need to refine, maybe playing around with temperature settings etc. This is why we have ground truths with questions and expected responses and have the experiment to validates with our specific metrics. A score and a reason for the score will help us through this proess.

* IGroundTruthReader - produces ground truth from the specific data source, currently only Excel is supported
* EvaluationRunner - this is a orchestration layer for IGroundTruthReader, IInferenceWorkflow and EvaluationMetricWorkflow which takes a metric configuration, pass it into an LLM and determines a reason and a score for the metric

## Deployment

A powershell script `RunContainerApps.ps1` has been created to allow you to deploy to the Cloud. More documentation will be coming shortly.

## References

Check out the following:

* [microsoft-phi-2](https://huggingface.co/easynet/microsoft-phi-2-GGUF/tree/main)
* [ChromaDBSharp](https://github.com/ksanman/ChromaDBSharp)
