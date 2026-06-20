# ChatExtra

FFXIV 国服 ExtraChat —— 跨大区、端到端加密、不限成员数量的额外聊天频道。

## 特性

- **跨大区通讯**：支持陆行鸟、莫古力、猫小胖、豆豆豆柴四大区任意世界
- **端到端加密**：服务端无法解密消息内容和频道名称
- **不限成员数**：没有传统部队/通讯贝的成员上限
- **身份系统**：管理员 / 队长 / 组员 / 待确认

## 安全与隐私

消息和频道名称在成员之间加密。服务端仅知道哪些角色在哪些频道中（运营必需），
但无法读取消息内容或频道名称。

创建频道时，客户端生成一个随机共享密钥。邀请他人时，通过 Diffie-Hellman 密钥交换
在两人之间安全传输共享密钥，服务端无法获知。

## 加密原理

1. 创建频道 → 客户端生成随机共享密钥，本地保存
2. 邀请用户 → 服务端协调双方 DH 密钥交换，邀请方用交换后的密钥加密共享密钥发送给被邀请方
3. 被邀请方解密后保存共享密钥
4. 消息发送 → 用共享密钥加密，服务端仅透传密文

## 自建服务端

### 依赖

- Rust (edition 2021)
- SQLite

### 部署

```bash
git clone https://github.com/QianChangUwU/ChatExtra.git
cd ChatExtra/server

# 初始化数据库（sqlx 编译时需要）
cat migrations/*.sql | sqlite3 database.sqlite

# 编译
cargo build --release

# 启动（首次启动后删除 database.sqlite，让服务端自动重建）
rm -f database.sqlite
nohup ./target/release/extra-chat-server config.toml &
```

### 配置

`config.toml` 示例：

```toml
[server]
address = "0.0.0.0:8080"

[limits]
max_channels_per_user = 50

[registration]
verify_on_lodestone = false
```

- `address`：监听地址（默认 `127.0.0.1:1996`，CN 服建议改为 `0.0.0.0:8080`）
- `verify_on_lodestone`：国服跳过 Lodestone 验证（设为 `false`）

### 防火墙

开放 TCP 端口（如 `8080`），确保客户端可以访问 `ws://<IP>:8080`。

## 客户端

### 安装

通过 Dalamud CN 插件管理器安装，或手动将 `ExtraChat.dll` 放入 `%APPDATA%\XIVLauncherCN\pluginDevs\ExtraChat\`。

### 命令

| 命令 | 说明 |
|------|------|
| `/extrachat` | 打开设置界面 |
| `/extrachat server <url>` | 设置服务端地址（如 `ws://1.2.3.4:8080`） |
| `/ecl1` ~ `/eclN` | 在对应频道中发言 |

### 首次使用

1. 打开 ExtraChat 设置（`/extrachat`）
2. 设置服务端地址：`/extrachat server ws://<你的服务器IP>:8080`
3. 注册账号 → 创建频道 → 邀请好友

## 开发

### 项目结构

```
ChatExtra/
├── server/          # Rust 服务端
│   ├── src/
│   │   ├── handlers/     # 各协议处理器
│   │   ├── types/        # 协议类型定义
│   │   └── main.rs       # 入口
│   ├── migrations/       # SQLite 迁移
│   └── Cargo.toml
├── client/
│   └── ExtraChat/        # Dalamud 插件（C#）
│       ├── Ui/           # ImGui 界面
│       ├── Protocol/     # 协议类型
│       ├── Util/         # 工具类
│       └── ExtraChat.csproj
├── VERSION               # 版本号（唯一来源）
└── .github/workflows/    # CI 发布流程
```

### 版本管理

版本号统一写在 **`VERSION`** 文件中，格式为 `A.B.C.DEE`：

- `A.B.C.D` = 上游 ExtraChat 版本
- `EE` = CN 定制补丁号（00~99）

两端编译时自动读取此文件，只需修改一处即可发布新版。

## 鸣谢

- [ExtraChat](https://git.sharlayan.cloud/anna/ExtraChat) — 上游项目
- [Dalamud](https://github.com/goatcorp/Dalamud) — FFXIV 插件框架
