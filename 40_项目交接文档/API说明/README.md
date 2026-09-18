# API 与环境配置说明

当前 MVP 核心为离线游戏，尚未接入 Neon、广告、登录或云存档。本目录只记录未来接口契约，不保存任何真实凭据。

## Neon PostgreSQL 规划

本地配置文件：`90_本地私密配置_不提交/.env`。

可用变量：

```dotenv
DATABASE_URL=your_database_url
NEON_DATABASE_URL=your_neon_database_url
```

仓库根目录 `.env.example` 只提供上述占位符。未来服务适配层按部署环境选择一个变量，缺失时应给出不含连接串的明确错误；不得回退到硬编码地址，不得把连接串打印到日志。

数据库连接、Schema、迁移、权限模型和云端 API 都不属于当前阶段。首次启用前必须单独评审最小权限、密钥轮换、迁移回滚、开发/测试/生产隔离及隐私要求。
