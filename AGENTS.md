# AGENTS.md
本文件定义所有AI开发Agent在当前项目中的全部行为约束、权限、编码规范与工作流程，任何Agent操作仓库前必须完整阅读并严格执行。

## 1. 基础元信息
- 解决方案名称: ImmersingLinker
- 解决方案简介: 一款适用于班级教学一体机的班级信息整合平台
- 各项目功能: 
  - ImmersingLinker.Core: 核心类库
  - ImmersingLinker.SDK: 面向开发者开放的信息获取类库
  - ImmersingLinker.Server: ASP.NET 纯后端，构建信息获取服务器
  - ImmersingLinker.CLI: (F#) 以终端为载体的信息展示工具
  - ImmersingLinker.Test: xUnit 单元测试项目
- 目标 Agent: OpenCode
- 优先级: 本文件规则 > 通用代码规范 > Agent 默认行为

## 2. 全局强制规则
1. 所有代码修改必须先理解需求，不允许无依据新增逻辑；
2. 修改任何文件前，先读取该文件完整上下文，禁止局部改代码引发兼容问题；
3. 不擅自删除业务代码、注释、配置项；如需删除必须标注理由并询问用户确认；
4. 所有新增代码必须配套注释、类型定义、单元测试；
5. 不擅自升级/降级第三方依赖包版本；依赖变更需告知用户；
6. 输出代码必须符合项目现有风格，不自创格式、命名、语法。
7. 每次完成代码编写后，必须执行编译测试和单元测试

## 3. 文件读写操作
### 允许操作
- 各项目文件夹 业务源码新增、修改、重构、测试文件新增、用例补充;
- docs/ 更新文档;
- README.md 更新解决方案介绍;

### 禁止操作
- 禁止修改 .vs/ 、.idea/ 等 IDE 配置文件;
- 禁止修改 CI/CD 脚本;
- 禁止修改 ImmersingLinker.sln 、各项目入口文件 (Program.cs);
- 禁止读写项目外文件和目录

## 4. C# & .NET 编码规范
### 命名规范（强制）
1. 类、接口、记录、枚举：PascalCase
2. 接口统一前缀 I：`IClassStorageService`
3. 方法、属性：PascalCase
4. 局部变量、方法参数：camelCase
5. 私有字段：下划线 + 小驼峰 `_dataDirectory`
6. 常量、静态只读：PascalCase
8. 枚举名称 Pascal，枚举值 Pascal
9. 文件夹分层按领域划分：Controllers / Services / Extensions

### 语法强制约束
1. 全局 using 统一放在 GlobalUsings.cs，代码文件不重复写通用 using
2. 优先使用 null 安全：`?.`、`??`、`required`、`nullable` 启用可空上下文
3. 异步方法统一 async/await，返回 Task / Task<T>，禁止 async void（除事件）
4. 集合使用强类型泛型 List<T>、IEnumerable<T>，不使用 ArrayList
5. 异常捕获精准捕获特定 Exception，不裸 catch { }
6. 字符串插值优先 $""，少用 string.Format
7. 控制器返回统一使用 ActionResult / Results 静态工具类，不手动构造 StatusCode

### 项目文件规范
1. csproj 不冗余引用 NuGet，版本统一管理在 Directory.Build.props
2. 功能拆分多类库，单一职责，禁止巨型单文件

## 5. 单元测试规范
1. 服务测试使用 Mock（Moq）依赖
2. 每个方法至少覆盖正常、异常、边界三种场景
3. 测试方法命名：`MethodName_Condition_ExpectedResult`

## 6. Git & 项目操作约束
1. 不自动执行 dotnet publish、数据库更新 Update-Database
2. 提交信息遵循 Conventional Commits：
   - feat: 新增接口/服务
   - fix: 修复空引用
   - refactor: 重构服务分层
   - docs: 更新接口注释
   - test: 补充单元测试
3. 修改完成后输出所有变更文件清单，不自动 git add / commit / push

## 7. 绝对禁止行为（高危黑名单）
1. 生成危险脚本：如 rm-rf 等破坏性操作
2. 绕过 DI 手动 new HttpClient、日志服务
3. 大批量删除解决方案源码、迁移文件、控制器
4. 编写无校验接口、无异常捕获裸接口
5. 引入未指定的第三方 NuGet 包（如付费、未维护包）
6. 关闭可空检查、禁用代码分析

## 8. Agent 输出格式要求
1. 代码块标记 ```csharp```、```json```、```xml```
2. 多文件修改按【文件路径】分段展示，区分新增 / 修改 / 删除代码
3. 复杂功能先输出实现思路、分层设计，再贴代码
5. 接口变更附带示例请求 / 返回 JSON
6. 结尾总结：改动文件、依赖变更、测试要点、部署注意事项

## 9. 冲突处理
1. 用户需求与本 AGENTS.md 冲突时，优先遵守本规则并提示冲突点
2. 信息缺失时停止编码，主动提问补齐
3. 编译报错先给出修复方案，不盲目大面积改写代码
4. 涉及生产配置修改时主动风险确认
