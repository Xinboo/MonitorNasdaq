# MonitorNasdaq

纳斯达克100指数每日简报推送工具。每个美股交易日后，北京时间早上 9:00 自动推送日报到微信。

## 推送内容

```
纳指日报

2026-07-13
当日收盘：29,264.10
前日收盘：29,825.11
当日涨跌：-1.88%
52周最高：30,660.60
距52周高点：-4.55%
```

## 环境要求

- .NET 8 SDK
- [Server酱](https://sct.ftqq.com/) 账号（用于微信推送）

## 快速开始

1. 克隆项目

```bash
git clone https://github.com/Xinboo/MonitorNasdaq.git
cd MonitorNasdaq/MonitorNasdaq
```

2. 配置

将 `appsettings.Example.json` 复制为 `appsettings.json`，填入你的 Server酱 Key：

```bash
cp appsettings.Example.json appsettings.json
```

```json
{
  "Monitor": {
    "Symbol": "^NDX",
    "ServerChanKey": "你的Server酱Key",
    "ReportHourBeijing": 9
  }
}
```

3. 运行

```bash
dotnet run
```

程序会常驻运行，每天北京时间 9:00 自动推送。

## 立即测试

```bash
dotnet run -- --now
```

添加 `--now` 参数会立即推送一次日报，方便验证配置是否正确。

## 配置说明

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `Symbol` | Yahoo Finance 股票代码 | `^NDX` |
| `ServerChanKey` | Server酱推送 Key | - |
| `ReportHourBeijing` | 每日推送时间（北京时间，整点） | `9` |

## Docker 部署

```bash
docker pull ghcr.io/xinboo/monitornasdaq:latest

docker run -d \
  -e Monitor__ServerChanKey=你的Server酱Key \
  --name monitornasdaq \
  --restart unless-stopped \
  ghcr.io/xinboo/monitornasdaq:latest
```

也可以用 `--now` 参数立即测试：

```bash
docker run --rm \
  -e Monitor__ServerChanKey=你的Server酱Key \
  ghcr.io/xinboo/monitornasdaq:latest \
  --now
```

## 推送时间表

| 北京时间 | 昨天是 | 是否交易日次日 | 行为 |
|---|---|---|---|
| 周一 9:00 | 映射为周五 | 是 | 发送（与周六重复） |
| 周二 9:00 | 周一 | 是 | 发送 |
| 周三 9:00 | 周二 | 是 | 发送 |
| 周四 9:00 | 周三 | 是 | 发送 |
| 周五 9:00 | 周四 | 是 | 发送 |
| 周六 9:00 | 周五 | 是 | 发送 |
| 周日 9:00 | 周六 | 否 | 跳过 |

## 数据来源

行情数据来自 Yahoo Finance，无需 API Key。

## License

MIT
