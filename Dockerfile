# syntax=docker/dockerfile:1

# 构建阶段：.NET 10 SDK + Rust 工具链（版本由 KaedeHikarinApiPhiRecorderWorkerLib/rust-toolchain.toml 固定）
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        build-essential \
        ca-certificates \
        curl \
        libasound2-dev \
        libgtk-3-dev \
        pkg-config \
    && rm -rf /var/lib/apt/lists/*
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --default-toolchain 1.98.1 --profile minimal
ENV PATH="/root/.cargo/bin:${PATH}"

WORKDIR /src
# 原生库先行构建，形成独立缓存层；随后 .NET 发布复用该产物
COPY KaedeHikarinApiPhiRecorderWorkerLib/ KaedeHikarinApiPhiRecorderWorkerLib/
RUN cargo build --locked --release --workspace --manifest-path KaedeHikarinApiPhiRecorderWorkerLib/Cargo.toml
COPY . .
RUN dotnet publish KaedeHikarinCialloTeam.PhiRecorder.Worker.csproj -c Release -o /app/publish -p:SkipPhiNativeBuild=true

# 运行阶段：.NET 10 运行时 + ffmpeg + EGL/Mesa/ALSA 依赖
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ffmpeg \
        intel-media-va-driver \
        libasound2t64 \
        libegl1 \
        libgl1 \
        libgl1-mesa-dri \
        libglx-mesa0 \
        libva2 \
        libvpl2 \
        libx11-6 \
        libxcursor1 \
        libxinerama1 \
        libxkbcommon0 \
        libxrandr2 \
        libxi6 \
        procps \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s \
    CMD pgrep -x dotnet > /dev/null || exit 1

# 入口脚本使用 EGL pbuffer 离屏渲染，不启动 Xvfb。GPU 由容器运行时透传。
ENTRYPOINT ["docker-entrypoint.sh"]
