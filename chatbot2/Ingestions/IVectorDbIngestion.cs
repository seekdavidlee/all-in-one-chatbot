﻿namespace chatbot2.Ingestions;

public interface IVectorDbIngestion
{
    Task<List<SearchModel>> LoadDataAsync(CancellationToken cancellationToken);
}
