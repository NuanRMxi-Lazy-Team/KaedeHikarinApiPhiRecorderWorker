# KaedeHikarinApiPhiRecorderWorker

本项目服务于 KaedeHikarinCialloTeamApi 的与 Phigros 同人谱面渲染相关的功能，主要用于将谱面数据渲染为视频。  
本项目与鸽游及《Phigros》官方不存在授权、合作或运营关系。  
本项目遵循 GPL v3 开源协议，任何人均可在遵循 GPL v3 协议的前提下使用、修改和分发本项目的源代码。  
如有侵权，请联系[我](mailto:nrlt@nuanr-mxi.com)或在 issue 中提出，我将尽快处理。

## 定位与许可证隔离

Worker **只负责渲染**，对 API 内部一无所知；它只消费 API 通过 RabbitMQ 下发的渲染任务。

- Worker 为 **GPLv3**，独立仓库（主 API 仓库以 git 子模块引入，两级嵌套到原生库）。
- **零程序集引用**：API 不引用 Worker，Worker 不引用 API 与 Shared；双方仅在各自仓库内维护 一份逐字对齐的消息契约 DTO（
  `Messaging/Contracts`），通过 RabbitMQ JSON 消息解耦。
- 消息契约版本写入消息头 `contract-version`（当前 `1`）。

## 消息拓扑（全部 durable）

| 名称                 | 类型          | 方向         | 说明                                                                            |
|----------------------|---------------|--------------|---------------------------------------------------------------------------------|
| `phi.render.tasks`   | 队列          | API → Worker | 渲染任务；prefetch=1 公平分发，多 Worker 实例天然水平扩展；DLX `phi.render.dlx` |
| `phi.render.events`  | 队列          | Worker → API | 进度与终态事件；API 按 JobId 幂等落库                                           |
| `phi.render.control` | fanout 交换机 | API → Worker | 取消指令扇出给所有实例；各实例绑独占队列并缓存已取消 JobId（TTL=队列等待超时）  |

消息全部持久化（`Persistent=true`），任务完成后才 ack；Worker 崩溃时消息自动 requeue 由其他实例接管。

## 任务链路

1. 消费任务 → 检查队列等待超时与已取消缓存
2. 用消息中的 **谱面预签名 URL** 下载谱面包到临时目录
3. 通过原生库 `PhiRenderer` 渲染，进度事件节流发布
4. 产物上传到 Worker **自有 S3 桶**，生成预签名 URL 随 Done 事件返回

## 超时与取消（可自由配置，见 `Rendering` 配置节）

- `QueueWaitTimeout`（默认 30 分钟）：任务在队列中等待超过该时长 → 出队时发 `Failed(QueueWaitTimeout)` 并 ack
- `JobExecutionTimeout`（默认 30 分钟）：任务在 Worker 内运行超过该时长 → 取消原生任务并发 `Failed(ExecutionTimeout)`
- 取消：API 发布 control 消息 → 在跑任务立即取消，排队任务出队时识别

## 构建与部署

- **一步构建**：`dotnet build` 自动触发 native 侧 `cargo build --locked`（工具链由
  `KaedeHikarinApiPhiRecorderWorkerLib/rust-toolchain.toml` 固定，rustup 自动安装），并拷贝 `phi_recorder.dll` /
  `libphi_recorder.so`、`phi-renderer-host.exe` / `phi-renderer-host` 与 assets 到输出目录；`dotnet publish`
  同样包含（Windows/Linux 产物自动按平台选择）。
- 裸机部署：`dotnet publish` 产物部署到目标机（.NET 10 运行时 + ffmpeg），可多实例分布式部署共享同一 RabbitMQ 队列。
- 配置参考 `appsettings.example.json`（`RabbitMq`、`Rendering`、`S3`、`PhiRecorder` 各节）。

### Docker 部署（仅 Worker 本体）

镜像内已附带 **ffmpeg**、Xvfb 与 Mesa 软渲染/X11/ALSA 运行库，开箱即用：

```bash
# 在 Worker 仓库根目录构建（会自动构建 native 并打入镜像）
docker build -t kaede-hikarin-api-worker:latest .

# 运行：通过环境变量注入 RabbitMQ / S3 / 超时等配置
docker run -d --name phi-worker \
  -e RabbitMq__HostName=rabbit.example.com \
  -e RabbitMq__UserName=phi \
  -e RabbitMq__Password=*** \
  -e S3__AccessKey=*** -e S3__SecretKey=*** \
  -e S3__ServiceUrl=https://<ACCOUNT_ID>.r2.cloudflarestorage.com \
  -e S3__BucketName=phi-render-output \
  -e Rendering__QueueWaitTimeout=00:30:00 \
  -e Rendering__JobExecutionTimeout=00:30:00 \
  kaede-hikarin-api-worker:latest
```

- 默认以 `xvfb-run` 提供虚拟显示并使用 Mesa 软件渲染；带 GPU 的宿主机可覆盖 `ENTRYPOINT` 并挂载宿主显示以获得硬件加速。
- 多实例直接多 `docker run`，共享同一 RabbitMQ 队列即可水平扩展。
- 所有配置项均可用 `Section__Key` 形式的环境变量覆盖（如 `PhiRecorder__FfmpegPath=ffmpeg`）。

## 测试

```bash
dotnet test KaedeHikarinCialloTeam.PhiRecorder.Worker.Tests
```

原生库自身的 Rust 测试见 `KaedeHikarinApiPhiRecorderWorkerLib/AGENTS.md`。
