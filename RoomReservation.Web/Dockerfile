FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RoomReservation.Domain/ RoomReservation.Domain/
COPY RoomReservation.Infrastructure/ RoomReservation.Infrastructure/
COPY RoomReservation.Application/ RoomReservation.Application/
COPY RoomReservation.Web/ RoomReservation.Web/
RUN dotnet restore RoomReservation.Web/RoomReservation.Web.csproj
RUN dotnet publish RoomReservation.Web/RoomReservation.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RoomReservation.Web.dll", "--urls", "http://0.0.0.0:8080"]
