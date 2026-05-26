FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CourseVideo.API.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg fonts-dejavu-core libfontconfig1 python3 python3-pip python3-venv \
    && rm -rf /var/lib/apt/lists/*

RUN python3 -m venv /opt/venv \
    && /opt/venv/bin/pip install --no-cache-dir edge-tts

ENV PATH="/opt/venv/bin:$PATH"

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CourseVideo.API.dll"]
