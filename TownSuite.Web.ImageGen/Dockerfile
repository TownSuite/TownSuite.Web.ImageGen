#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS libheif-build
RUN apk add --no-cache build-base git cmake yasm perl libde265 libde265-dev x265 x265-dev
WORKDIR /tmp
RUN git clone --depth 1 https://aomedia.googlesource.com/aom
WORKDIR /tmp/aom
RUN mkdir -p build && cd build && cmake .. && make -j$(nproc) && make install
WORKDIR /app
RUN git clone --depth 1 https://github.com/strukturag/libheif.git
WORKDIR /app/libheif
RUN mkdir build
WORKDIR /app/libheif/build
RUN cmake \
    -DCMAKE_BUILD_TYPE=Release \
    -DWITH_EXAMPLES=OFF \
    -DWITH_LIBDE265=ON \
    -DWITH_X265=ON \
    ..
RUN make -j$(nproc)
RUN make install

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["TownSuite.Web.ImageGen/TownSuite.Web.ImageGen.csproj", "TownSuite.Web.ImageGen/"]
RUN dotnet restore "TownSuite.Web.ImageGen/TownSuite.Web.ImageGen.csproj"
COPY . .
WORKDIR "/src/TownSuite.Web.ImageGen"
RUN dotnet build "TownSuite.Web.ImageGen.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TownSuite.Web.ImageGen.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=libheif-build /usr/lib/libde265.so* /usr/lib/
COPY --from=libheif-build /usr/lib/libx265.so* /usr/lib/
COPY --from=libheif-build /usr/local/lib/libheif.so* /usr/local/lib
COPY --from=libheif-build /usr/lib/libstdc++.so.6 /usr/lib/libstdc++.so.6
COPY --from=libheif-build /usr/lib/libgcc_s.so.1 /usr/lib/libgcc_s.so.1
COPY --from=libheif-build /usr/lib/libnuma.so.1 /usr/lib/libnuma.so.1
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/wwwroot/cache && \
    chown -R $APP_UID /app/wwwroot/cache
USER $APP_UID
ENTRYPOINT ["dotnet", "TownSuite.Web.ImageGen.dll"]