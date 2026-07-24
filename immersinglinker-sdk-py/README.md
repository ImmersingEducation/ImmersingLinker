# ImmersingLinker Python SDK

适用于 [ClassIsland](https://github.com/ClassIsland/ClassIsland) 生态的 Python 异步客户端库，提供班级信息管理、课程查询和自动化计划操作的完整 API 封装。

## 安装

```bash
pip install immersinglinker-sdk-py
```

开发模式：

```bash
pip install -e ".[dev]"
```

## 快速开始

```python
import asyncio
from immersinglinker import AppServiceClient, ClassServiceClient, LessonServiceClient

async def main():
    # 连接测试
    async with AppServiceClient(port="51000") as app:
        ok = await app.test_connection()
        print(f"连接状态: {ok}")

    # 查询班级信息
    async with ClassServiceClient(port="51000") as client:
        classes = await client.get_all_classes()
        for cls in classes:
            print(f"班级: {cls.Name} ({cls.Guid})")

    # 查询当前课程
    async with LessonServiceClient(port="51000") as client:
        subject = await client.get_current_subject()
        state = await client.get_current_state()
        print(f"当前状态: {state.name}")

asyncio.run(main())
```

## API 概览

### 客户端类

| 客户端 | 用途 | 方法数 |
|--------|------|--------|
| `AppServiceClient` | 连接测试 | 2 |
| `ClassServiceClient` | 班级/学生/扩展属性/分组规则 CRUD | 31 |
| `LessonServiceClient` | 课程查询/时间管理/档案 | 19 |
| `AutomationServiceClient` | 自动化计划 CRUD + 离线工厂 | 13 |

所有客户端均支持 `async with` 上下文管理器模式。

### 数据模型

- **班级**: `Class`, `ClassInfo`, `Student`, `Group`, `GroupingRule`, `GroupingRuleResponse`
- **扩展属性**: `ClassExtraProperty`, `StudentExtraProperty`, `Application`
- **自动化**: `AutomationPlan`, `AutomationPlanInfo`, `UrlTrigger`, `RuleSet`, `Action`
- **课程**: `Subject`, `TimeLayoutItem`, `ClassPlan`, `Profile`
- **枚举**: `Gender`, `RuleSetSatisfyMode`, `TimeState`
- **请求 DTO**: `CreateClassRequest`, `UpdateClassRequest`, `CreateStudentRequest` 等

### 离线工厂

无需网络连接即可创建模型实例，用于测试或预配置：

```python
from immersinglinker import ClassServiceClient, AutomationServiceClient, Gender

# 创建班级和学生
cls = ClassServiceClient.create_class_offline("高一(1)班")
student = ClassServiceClient.create_student_offline("张三", 1, Gender.Male)

# 创建自动化计划
trigger = AutomationServiceClient.create_trigger_dto_offline("UrlTrigger", {"Tag": "http://..."})
action = AutomationServiceClient.create_action_dto_offline("SomeAction")
plan = AutomationServiceClient.create_plan_request_offline(
    name="我的计划", revertable=True, trigger=trigger, rule_set=None, actions=[action]
)
```

## 异常处理

```python
from immersinglinker import ImmersingLinkerError

try:
    async with ClassServiceClient(port="51000") as client:
        cls = await client.get_class_by_guid("invalid-guid")
except ImmersingLinkerError as e:
    print(f"请求失败: {e}")
```

## 开发

```bash
# 安装开发依赖
pip install -e ".[dev]"

# 运行测试
pytest

# 运行带覆盖率的测试
pytest --cov=immersinglinker
```

## 许可证

MIT License
