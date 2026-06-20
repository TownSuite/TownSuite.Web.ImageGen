#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# =============================================================================
# Native image codecs (aom / libheif / libde265 / x265) decode UNTRUSTED bytes
# from the image proxy. They are a memory-safety attack surface, so keep them
# patched:
#   * aom / libheif are pinned to explicit tags below — bump them when upstream
#     ships security releases. Track advisories:
#       - libheif:  https://github.com/strukturag/libheif/releases
#       - aom:      https://aomedia.googlesource.com/aom / CVE feeds (e.g. CVE-2025-8879)
#   * libde265 / x265 come from Alpine (apk) and are patched by rebuilding on an
#     updated base image — rebuild this image regularly so apk pulls fixes.
# Last reviewed / bumped: 2026-06-19.
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS libheif-build
# add nasm and pkgconfig for assembler and pkg-config detection
RUN apk add --no-cache build-base git cmake yasm nasm pkgconfig perl libde265 libde265-dev x265 x265-dev
WORKDIR /tmp
RUN git clone --branch v3.14.1 --depth 1 --single-branch https://aomedia.googlesource.com/aom
WORKDIR /tmp/aom
# set POSIX / large-file defines so fseeko/ftello are visible on musl
RUN mkdir -p build && cd build && \
    cmake -DCMAKE_BUILD_TYPE=Release \
          -DCMAKE_C_FLAGS="-D_XOPEN_SOURCE=700 -D_FILE_OFFSET_BITS=64" \
          -DCMAKE_CXX_FLAGS="-D_XOPEN_SOURCE=700 -D_FILE_OFFSET_BITS=64" \
          .. && make -j$(nproc) && make install
WORKDIR /app
RUN git clone --branch v1.23.0 --depth 1 --single-branch https://github.com/strukturag/libheif.git
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

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["TownSuite.Web.ImageGen/TownSuite.Web.ImageGen.csproj", "TownSuite.Web.ImageGen/"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["Directory.Build.props", "Directory.Build.props"]
RUN dotnet restore "TownSuite.Web.ImageGen/TownSuite.Web.ImageGen.csproj"
# Supply-chain gate: fail the build if any direct or transitive NuGet package has a
# known security advisory. NuGetAudit in Directory.Build.props also enforces this during
# restore; this step surfaces the finding explicitly in CI logs.
# Note: Alpine /bin/sh has no `pipefail`, so a `| tee` pipeline would mask a failing
# `dotnet list`. Redirect to a file and check its exit status explicitly instead, so the
# build fails both when the command errors and when a vulnerable package is reported.
RUN dotnet list "TownSuite.Web.ImageGen/TownSuite.Web.ImageGen.csproj" package --vulnerable --include-transitive > /tmp/vuln.txt 2>&1; \
    status=$?; \
    cat /tmp/vuln.txt; \
    if [ "$status" -ne 0 ]; then \
      echo "ERROR: 'dotnet list package --vulnerable' failed (exit $status)." >&2; exit "$status"; \
    fi; \
    if grep -q "has the following vulnerable packages" /tmp/vuln.txt; then \
      echo "ERROR: vulnerable NuGet packages detected (see list above)." >&2; exit 1; \
    fi
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
RUN mkdir -p /app/cache && \
    chown -R $APP_UID /app/cache
USER $APP_UID
ENTRYPOINT ["dotnet", "TownSuite.Web.ImageGen.dll"]