using AIOChatbot.Configurations;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIOChatbot.Ingestions;

public class LocalDirectoryIngestion : IIngestionDataSource
{
    private readonly IConfig config;
    private readonly ILogger<LocalDirectoryIngestion> logger;

    public LocalDirectoryIngestion(IConfig config, ILogger<LocalDirectoryIngestion> logger)
    {
        this.config = config;
        this.logger = logger;
    }

    public async Task<List<SearchModelDto>> LoadDataAsync(CancellationToken cancellationToken)
    {
        var dataSourcePathsStr = (Environment.GetEnvironmentVariable("DataSourcePaths") ?? throw new Exception("Missing DataSourcePaths"));
        bool isBlob = dataSourcePathsStr.StartsWith(Util.BlobPrefix);
        string[] dataSourcePaths = (isBlob ? dataSourcePathsStr[Util.BlobPrefix.Length..] : dataSourcePathsStr).Split(',');
        var htmlReader = new HtmlReader(this.config, this.logger);

        List<SearchModelDto> results = [];

        foreach (var dataSourcePath in dataSourcePaths)
        {
            logger.LogInformation("processing data source: {dataSourcePath}...", dataSourcePath);
            var (Pages, Logs) = isBlob ? await htmlReader.ReadBlobsAsync(dataSourcePath, cancellationToken) : await htmlReader.ReadFilesAsync(dataSourcePath, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return results;
            }

            foreach (var page in Pages)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return results;
                }
                if (page.Sections.Count == 0)
                {
                    logger.LogWarning("page has no sections: {pagePath}", page.Context.PagePath);
                    continue;
                }

                logger.LogDebug("processing page: {pagePath}...", page.Context.PagePath);

                foreach (var section in page.Sections)
                {
                    foreach (var txtChunk in section.TextChunks)
                    {
                        results.Add(new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Content = txtChunk.Text,
                            MetaData = JsonSerializer.Serialize(txtChunk.MetaDatas),
                            Filepath = page.Context.PagePath,
                            ContentToVectorized = txtChunk.Text,
                        });
                    }
                }
            }

            foreach (var log in Logs)
            {
                logger.LogDebug("log: {logText}, source: {logSource}", log.Text, log.Source);
            }
        }

        return results;
    }
}
