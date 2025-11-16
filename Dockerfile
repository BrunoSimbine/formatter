# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app


# copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore 

# copy and publish app and libraries
COPY . ./
RUN dotnet publish -c Release -o out


# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
EXPOSE 5001
WORKDIR /app
COPY --from=build /app/out .
# Uncomment to enable non-root user
# USER $APP_UID
ENTRYPOINT ["./formatter"]