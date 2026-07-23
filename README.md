# ImmersingLinker

一款适用于班级教学一体机的班级信息整合平台，提供班级管理、课程信息获取、自动化工作流等功能。

## 项目架构

```
ImmersingLinker/
├── ImmersingLinker.Core/          # 核心类库 (领域模型、接口、服务)
├── ImmersingLinker.SDK/           # 开发者 SDK (HTTP 客户端)
├── ImmersingLinker.Server/        # ASP.NET Web API 后端
├── ImmersingLinker.CLI/           # F# 终端命令行工具
├── ImmersingLinker.Test/          # xUnit 单元测试
```

### 依赖关系

```
Core (最底层，无项目依赖)
├──→ SDK (引用 Core)
│       └──→ CLI (F#, 引用 SDK)
├──→ Server (引用 Core)
└──→ Test (引用 Core + Server)
```

## 功能模块

### 1. 班级管理
- 班级 CRUD 操作
- 学生信息管理
- 扩展属性系统（支持自定义属性注入）
- 分组规则与小组管理

### 2. 课程信息
- 当前/下一节课信息
- 课程表查询
- 时间状态监控
- 与 ClassIsland 集成

### 3. 自动化引擎
- **触发器 (Trigger)**：事件驱动或轮询触发
- **规则 (Rule)**：支持 AllSatisfied/AnySatisfied 模式，支持嵌套和取反
- **动作 (Action)**：支持正向执行和反向回滚
- **自动化计划**：组合 Trigger + RuleSet + Actions 的完整工作流

### 4. ClassIsland 集成
- 通过 IPC 与 ClassIsland 教学软件通信
- 获取课程信息、时间状态、配置文件等

## 快速开始

### 环境要求
- .NET 10.0 SDK
- ClassIsland（可选，用于课程信息功能）

### 启动服务器

```bash
# 运行 Web API 服务器
dotnet run --project ImmersingLinker.Server

# 默认监听端口: 5157
```

### 使用 CLI 工具

```bash
# 测试服务器连接
dotnet run --project ImmersingLinker.CLI -- test-connection

# 查看当前课程
dotnet run --project ImmersingLinker.CLI -- lesson current

# 列出所有班级
dotnet run --project ImmersingLinker.CLI -- class list

# 创建班级
dotnet run --project ImmersingLinker.CLI -- class create --name "测试班级"

# JSON 输出格式
dotnet run --project ImmersingLinker.CLI -- class list --json
```

## API 文档

启动服务器后，访问以下地址查看 API 文档：
- Scalar API 文档: `http://localhost:5157/scalar/v1`
- OpenAPI JSON: `http://localhost:5157/openapi/v1.json`

### 主要 API 端点

#### 班级管理
| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/class` | 获取所有班级 |
| GET | `/class/infos` | 获取所有班级信息 |
| GET | `/class/{classGuid}` | 获取指定班级 |
| POST | `/class` | 创建班级 |
| PUT | `/class/{classGuid}` | 更新班级 |
| DELETE | `/class/{classGuid}` | 删除班级 |

#### 学生管理
| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/class/{classGuid}/student` | 获取班级所有学生 |
| POST | `/class/{classGuid}/student` | 添加学生 |
| PUT | `/class/{classGuid}/student/{studentId}` | 更新学生 |
| DELETE | `/class/{classGuid}/student/{studentId}` | 删除学生 |

#### 课程信息
| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/lesson/current/subject` | 当前课程科目 |
| GET | `/lesson/current/next-class-subject` | 下一节课科目 |
| GET | `/lesson/current/state` | 当前时间状态 |
| GET | `/lesson/current/time-layout-item` | 当前时间布局项 |
| GET | `/lesson/current/class-plan` | 当前课程表 |
| GET | `/lesson/current/selected-index` | 当前选中索引 |
| GET | `/lesson/current/is-class-plan-enabled` | 课程表是否启用 |
| GET | `/lesson/current/is-class-plan-loaded` | 课程表是否已加载 |
| GET | `/lesson/current/is-lesson-confirmed` | 课程是否已确认 |
| GET | `/lesson/next/class-time-layout-item` | 下一节课时间布局项 |
| GET | `/lesson/next/breaking-time-layout-item` | 下一休息时间布局项 |
| GET | `/lesson/previous/class-subject` | 上一节课科目 |
| GET | `/lesson/previous/class-time-layout-item` | 上一节课时间布局项 |
| GET | `/lesson/previous/breaking-time-layout-item` | 上一休息时间布局项 |
| GET | `/lesson/timer/on-class-left` | 当前课程剩余时间 |
| GET | `/lesson/timer/on-breaking-left` | 当前休息剩余时间 |
| GET | `/lesson/timer/elapsed-since-previous-class` | 自上节课以来经过时间 |
| GET | `/lesson/timer/elapsed-since-previous-breaking` | 自上休息以来经过时间 |
| GET | `/lesson/timer/elapsed-since-previous-any` | 自上事件以来经过时间 |
| GET | `/lesson/profile/current-profile-path` | 当前配置文件路径 |
| GET | `/lesson/profile/is-trusted` | 当前配置文件是否受信任 |
| GET | `/lesson/profile` | 获取配置文件 |

#### 自动化管理
| 方法 | 路由 | 说明 |
|------|------|------|
| GET | `/automation` | 获取所有计划信息 |
| GET | `/automation/{planGuid}` | 获取指定计划 |
| POST | `/automation` | 创建计划 |
| POST | `/automation/{planGuid}/trigger` | 手动触发 |
| POST | `/automation/invoke/{tag}` | 通过 tag 触发 UrlTrigger |
| PUT | `/automation/{planGuid}` | 更新计划 |
| DELETE | `/automation/{planGuid}` | 删除计划 |

## SDK 使用

### 安装

```bash
dotnet add package ImmersingLinker.SDK
```

### 示例代码

```csharp
using ImmersingLinker.SDK;

// 创建客户端
var classClient = new ClassServiceClient(port: 5157);
var lessonClient = new LessonServiceClient(port: 5157);

// 测试连接
var connected = await classClient.TestConnectionAsync();

// 获取所有班级
var classes = await classClient.GetAllClassesAsync();

// 获取当前课程
var currentLesson = await lessonClient.GetCurrentLessonAsync();
```

## 开发指南

### 项目结构规范

- **命名空间**：按领域划分 (Controllers / Services / Extensions)
- **文件命名**：PascalCase，与类名一致
- **私有字段**：下划线前缀 `_fieldName`

### 编码规范

- 使用 C# 12.0+ 特性（nullable、pattern matching 等）
- 异步方法统一使用 async/await
- 字符串插值优先 $""
- 异常捕获精准捕获特定 Exception

### 测试

```bash
# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test ImmersingLinker.Test
```

## 自动化引擎扩展

### 添加新的触发器

```csharp
[Trigger("ilinker.MyTrigger", "我的触发器")]
public class MyTrigger : Trigger
{
    // 实现触发逻辑
}
```

### 添加新的规则

```csharp
[Rule("ilinker.MyRule", "我的规则")]
public class MyRule : Rule
{
    public override bool IsSatisfied()
    {
        // 实现规则判断逻辑
    }
}
```

### 添加新的动作

```csharp
[Action("ilinker.MyAction", "我的动作")]
public class MyAction : Action
{
    protected override Task OnInvokeAsync()
    {
        // 实现正向执行逻辑
        return Task.CompletedTask;
    }

    protected override Task OnRevertAsync()
    {
        // 实现回滚逻辑
        return Task.CompletedTask;
    }
}
```

## 数据存储

- **班级数据**：`Data/Classes/{guid}.json`
- **自动化计划**：`Data/Automation/{guid}.json`
- **存储方式**：JSON 文件持久化

## 依赖项

| 包名 | 版本 | 说明 |
|------|------|------|
| ClassIsland.Shared.IPC | 2.1.0.1 | ClassIsland IPC 通信 |
| Microsoft.AspNetCore.OpenApi | 10.0.6 | OpenAPI 支持 |
| Scalar.AspNetCore | 2.16.3 | API 文档界面 |
| xUnit | 2.9.3 | 单元测试框架 |
| Moq | 4.20.72 | Mock 框架 |

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建人员 - 特性分支 (`git checkout -b feature/progcc/AmazingFeature`)
3. 提交更改 (`git commit -m 'feat: Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request
