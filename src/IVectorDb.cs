﻿using AIOChatbot.VectorDbs;

namespace AIOChatbot;

public interface IVectorDb
{
    Task InitAsync();
    Task<(int SuccessCount, int ErrorCount)> ProcessAsync(IEnumerable<SearchModelDto> models, CancellationToken cancellationToken, string? collectionName = null);
    Task DeleteAsync();
    Task<IEnumerable<IndexedDocument>> SearchAsync(string[] searchTexts, CancellationToken cancellationToken);
}
