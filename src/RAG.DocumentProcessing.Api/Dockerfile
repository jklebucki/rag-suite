FROM mcr.microsoft.com/dotnet/sdk:10.0.100-noble AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/RAG.DocumentProcessing.Abstractions/RAG.DocumentProcessing.Abstractions.csproj src/RAG.DocumentProcessing.Abstractions/
COPY src/RAG.DocumentProcessing.Core/RAG.DocumentProcessing.Core.csproj src/RAG.DocumentProcessing.Core/
COPY src/RAG.DocumentProcessing.Providers.Docling/RAG.DocumentProcessing.Providers.Docling.csproj src/RAG.DocumentProcessing.Providers.Docling/
COPY src/RAG.DocumentProcessing.Api/RAG.DocumentProcessing.Api.csproj src/RAG.DocumentProcessing.Api/
RUN dotnet restore src/RAG.DocumentProcessing.Api/RAG.DocumentProcessing.Api.csproj

COPY src/RAG.DocumentProcessing.Abstractions/ src/RAG.DocumentProcessing.Abstractions/
COPY src/RAG.DocumentProcessing.Core/ src/RAG.DocumentProcessing.Core/
COPY src/RAG.DocumentProcessing.Providers.Docling/ src/RAG.DocumentProcessing.Providers.Docling/
COPY src/RAG.DocumentProcessing.Api/ src/RAG.DocumentProcessing.Api/
RUN dotnet publish src/RAG.DocumentProcessing.Api/RAG.DocumentProcessing.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.9-noble AS final
WORKDIR /app
RUN mkdir -p /app/App_Data && chown -R $APP_UID /app
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RAG.DocumentProcessing.Api.dll"]
