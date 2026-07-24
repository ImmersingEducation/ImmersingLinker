"""数据传输类型定义。

本模块包含 ImmerseLinker 所有模型类、多态注册表以及离线工厂方法。
多态类通过 ``PolymorphicRegistry`` 实现 ``@classmethod from_dict``，
支持基类引用反序列化为具体子类实例。
"""

from __future__ import annotations

from dataclasses import dataclass, field, fields, asdict
from typing import Any
from uuid import UUID, uuid4

from .enums import Gender, RuleSetSatisfyMode


# ============================================================================
# Polymorphic type registry & deserialization helpers
# ============================================================================

_trigger_registry: dict[str, type[Trigger]] = {}
_action_registry: dict[str, type[Action]] = {}
_rule_base_registry: dict[str, type[RuleBase]] = {}


def _register_trigger(cls: type[Trigger]) -> type[Trigger]:
    _trigger_registry[cls.__name__] = cls
    return cls


def _register_action(cls: type[Action]) -> type[Action]:
    _action_registry[cls.__name__] = cls
    return cls


def _register_rule_base(cls: type[RuleBase]) -> type[RuleBase]:
    _rule_base_registry[cls.__name__] = cls
    return cls


def _deserialize_trigger(data: dict[str, Any]) -> Trigger | None:
    if data is None:
        return None
    type_name: str = data.get("$type", "")
    cls = _trigger_registry.get(type_name)
    if cls is None:
        return UnknownTrigger.from_dict(data)
    return cls.from_dict(data)


def _deserialize_action(data: dict[str, Any]) -> Action | None:
    if data is None:
        return None
    type_name: str = data.get("$type", "")
    cls = _action_registry.get(type_name)
    if cls is None:
        return UnknownAction.from_dict(data)
    return cls.from_dict(data)


def _deserialize_rule_base(data: dict[str, Any]) -> RuleBase | None:
    if data is None:
        return None
    type_name: str = data.get("$type", "")
    cls = _rule_base_registry.get(type_name)
    if cls is None:
        if "SatisfyMode" in data or "Rules" in data:
            return RuleSet.from_dict(data)
        return UnknownRuleBase.from_dict(data)
    return cls.from_dict(data)


# ============================================================================
# Class domain models
# ============================================================================


@dataclass
class Application:
    """表示一个已注册的应用程序。"""

    UniqueId: str
    """应用唯一标识符（AppId）。"""

    Name: str
    """应用显示名称。"""


@dataclass
class ClassInfo:
    """表示一个班级的简要信息（GUID + 名称）。"""

    Guid: UUID
    """班级唯一标识符。"""

    Name: str
    """班级名称。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> ClassInfo:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data.get("Name", ""),
        )


@dataclass
class ClassExtraProperty:
    """班级扩展属性，由外部应用注册。"""

    Application: Application
    """拥有此属性的应用。"""

    Name: str
    """属性名称。"""

    Value: Any = None
    """属性值。"""

    def to_dict(self) -> dict[str, Any]:
        d: dict[str, Any] = {}
        d["Application"] = {
            "UniqueId": self.Application.UniqueId,
            "Name": self.Application.Name,
        }
        d["Name"] = self.Name
        d["Value"] = self.Value
        return d

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> ClassExtraProperty:
        return cls(
            Application=Application(**data["Application"]),
            Name=data["Name"],
            Value=data.get("Value"),
        )


@dataclass
class StudentExtraProperty:
    """学生扩展属性，由外部应用注册。"""

    Application: Application
    """拥有此属性的应用。"""

    Name: str
    """属性名称。"""

    Value: Any = None
    """属性值。"""

    def to_dict(self) -> dict[str, Any]:
        d: dict[str, Any] = {}
        d["Application"] = {
            "UniqueId": self.Application.UniqueId,
            "Name": self.Application.Name,
        }
        d["Name"] = self.Name
        d["Value"] = self.Value
        return d

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> StudentExtraProperty:
        return cls(
            Application=Application(**data["Application"]),
            Name=data["Name"],
            Value=data.get("Value"),
        )


@dataclass
class Student:
    """表示一个学生。"""

    Guid: UUID
    """学生唯一标识符。"""

    Name: str
    """学生姓名。"""

    StudentIdInClass: int
    """学生在班级内的学号。"""

    Gender: Gender
    """学生性别。"""

    ExtraProperties: list[StudentExtraProperty] = field(default_factory=list)
    """学生扩展属性列表。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Student:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data["Name"],
            StudentIdInClass=data["StudentIdInClass"],
            Gender=Gender(data["Gender"]) if isinstance(data["Gender"], str) else data["Gender"],
            ExtraProperties=[
                StudentExtraProperty.from_dict(p) for p in data.get("ExtraProperties", [])
            ],
        )


@dataclass
class Group:
    """表示一个分组，包含一组学生 GUID。"""

    Guid: UUID
    """分组唯一标识符。"""

    Name: str
    """分组名称。"""

    Contains: set[UUID] = field(default_factory=set)
    """该分组包含的学生 GUID 集合。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Group:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data["Name"],
            Contains={
                UUID(g) if isinstance(g, str) else g for g in data.get("Contains", [])
            },
        )


@dataclass
class GroupingRule:
    """表示一个分组规则。"""

    Guid: UUID
    """分组规则唯一标识符。"""

    Name: str
    """分组规则名称。"""

    Groups: list[Group] = field(default_factory=list)
    """该规则下的分组列表。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> GroupingRule:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data["Name"],
            Groups=[Group.from_dict(g) for g in data.get("Groups", [])],
        )


@dataclass
class GroupingRuleResponse:
    """分组规则查询响应，包含未分配学生列表。"""

    Guid: UUID
    """分组规则唯一标识符。"""

    Name: str
    """分组规则名称。"""

    Groups: list[Group] = field(default_factory=list)
    """该规则下的分组列表。"""

    UnassignedStudentGuids: list[UUID] = field(default_factory=list)
    """未分配到任何分组的学生 GUID 列表。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> GroupingRuleResponse:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data["Name"],
            Groups=[Group.from_dict(g) for g in data.get("Groups", [])],
            UnassignedStudentGuids=[
                UUID(u) if isinstance(u, str) else u
                for u in data.get("UnassignedStudentGuids", [])
            ],
        )


@dataclass
class Class:
    """表示一个完整的班级模型，包含学生、分组规则和扩展属性。"""

    Guid: UUID
    """班级唯一标识符。"""

    Name: str
    """班级名称。"""

    Students: list[Student] = field(default_factory=list)
    """班级内的学生列表。"""

    GroupingRules: list[GroupingRule] = field(default_factory=list)
    """班级的分组规则列表。"""

    ExtraProperties: list[ClassExtraProperty] = field(default_factory=list)
    """班级扩展属性列表。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Class:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data["Name"],
            Students=[Student.from_dict(s) for s in data.get("Students", [])],
            GroupingRules=[
                GroupingRule.from_dict(r) for r in data.get("GroupingRules", [])
            ],
            ExtraProperties=[
                ClassExtraProperty.from_dict(p)
                for p in data.get("ExtraProperties", [])
            ],
        )


# ============================================================================
# Class request DTOs
# ============================================================================


@dataclass
class CreateClassRequest:
    """创建班级请求。"""

    Name: str
    """班级名称。"""


@dataclass
class UpdateClassRequest:
    """更新班级请求。"""

    Name: str
    """班级名称。"""


@dataclass
class CreateStudentRequest:
    """创建学生请求。"""

    Name: str
    """学生姓名。"""

    StudentIdInClass: int
    """学生在班级内的学号。"""

    Gender: Gender
    """学生性别。"""


@dataclass
class UpdateStudentRequest:
    """更新学生请求。"""

    Name: str
    """学生姓名。"""

    Gender: Gender
    """学生性别。"""

    GroupInClass: str
    """学生所在分组名称。"""


@dataclass
class CreateExtraPropertyRequest:
    """创建扩展属性请求。"""

    AppId: str
    """应用 AppId。"""

    Name: str
    """属性名称。"""

    Value: Any = None
    """属性值。"""


@dataclass
class UpdateExtraPropertyRequest:
    """更新扩展属性请求。"""

    Value: Any = None
    """属性值。"""


@dataclass
class CreateGroupingRuleRequest:
    """创建分组规则请求。"""

    Name: str
    """分组规则名称。"""


@dataclass
class UpdateGroupingRuleRequest:
    """更新分组规则请求。"""

    Name: str
    """分组规则名称。"""


@dataclass
class CreateGroupRequest:
    """创建分组请求。"""

    Name: str
    """分组名称。"""


@dataclass
class UpdateGroupRequest:
    """更新分组请求。"""

    Name: str
    """分组名称。"""


# ============================================================================
# Automation abstractions (server-side base types, also appear in API responses)
# ============================================================================


@dataclass
class Trigger:
    """自动化触发器基类。"""

    TriggerFired: Any = None
    """触发器触发时的事件/时间点。"""

    def to_dict(self) -> dict[str, Any]:
        return {"$type": type(self).__name__}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Trigger:
        return cls()


@dataclass
class Action:
    """自动化动作基类。"""

    Revertable: bool = False
    """该动作是否支持撤回。"""

    def to_dict(self) -> dict[str, Any]:
        return {"$type": type(self).__name__, "Revertable": self.Revertable}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Action:
        return cls(Revertable=data.get("Revertable", False))


@dataclass
class RuleBase:
    """自动化规则基类。"""

    Guid: UUID = field(default_factory=uuid4)
    """规则唯一标识符。"""

    Not: bool = False
    """是否对规则结果取反。"""

    def to_dict(self) -> dict[str, Any]:
        return {"$type": type(self).__name__, "Guid": str(self.Guid), "Not": self.Not}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> RuleBase:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data.get("Guid"), str) else data.get("Guid", uuid4()),
            Not=data.get("Not", False),
        )


# ============================================================================
# Concrete automation types
# ============================================================================


@_register_trigger
@dataclass
class UrlTrigger(Trigger):
    """URL 触发器，通过轮询 URL 返回的 JSON 值来触发。"""

    Tag: str = ""
    """触发器标签，对应轮询 URL。"""

    def to_dict(self) -> dict[str, Any]:
        return {"$type": "UrlTrigger", "Tag": self.Tag}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> UrlTrigger:
        return cls(Tag=data.get("Tag", ""))


@dataclass
class UnknownTrigger(Trigger):
    """未知触发器类型，保留原始 JSON 数据以供向前兼容。"""

    _raw: dict[str, Any] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return {"$type": self._raw.get("$type", "UnknownTrigger"), **self._raw}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> UnknownTrigger:
        return cls(_raw=data)


@dataclass
class UnknownAction(Action):
    """未知动作类型，保留原始 JSON 数据以供向前兼容。"""

    _raw: dict[str, Any] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return {"$type": self._raw.get("$type", "UnknownAction"), **self._raw}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> UnknownAction:
        return cls(_raw=data, Revertable=data.get("Revertable", False))


@_register_rule_base
@dataclass
class RuleSet(RuleBase):
    """规则集，包含多个子规则及其满足模式。"""

    SatisfyMode: RuleSetSatisfyMode = RuleSetSatisfyMode.AllSatisfied
    """满足模式：AllSatisfied（全部满足）或 AnySatisfied（任一满足）。"""

    Rules: list[RuleBase] = field(default_factory=list)
    """子规则列表。"""

    def to_dict(self) -> dict[str, Any]:
        return {
            "$type": "RuleSet",
            "Guid": str(self.Guid),
            "Not": self.Not,
            "SatisfyMode": self.SatisfyMode.value,
            "Rules": [r.to_dict() for r in self.Rules],
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> RuleSet:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data.get("Guid"), str) else data.get("Guid", uuid4()),
            Not=data.get("Not", False),
            SatisfyMode=RuleSetSatisfyMode(data["SatisfyMode"]) if isinstance(data.get("SatisfyMode"), str) else data.get("SatisfyMode", RuleSetSatisfyMode.AllSatisfied),
            Rules=[r for d in data.get("Rules", []) if (r := _deserialize_rule_base(d)) is not None],
        )


@dataclass
class UnknownRuleBase(RuleBase):
    """未知规则类型，保留原始 JSON 数据以供向前兼容。"""

    _raw: dict[str, Any] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return {
            "$type": self._raw.get("$type", "UnknownRuleBase"),
            "Guid": str(self.Guid),
            "Not": self.Not,
            **{k: v for k, v in self._raw.items() if k not in ("$type", "Guid", "Not")},
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> UnknownRuleBase:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data.get("Guid"), str) else data.get("Guid", uuid4()),
            Not=data.get("Not", False),
            _raw=data,
        )


# ============================================================================
# Automation domain models
# ============================================================================


@dataclass
class AutomationPlanInfo:
    """自动化计划简要信息（GUID + 名称）。"""

    Guid: UUID
    """计划唯一标识符。"""

    Name: str = ""
    """计划名称。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> AutomationPlanInfo:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data.get("Name", ""),
        )


@dataclass
class AutomationPlan:
    """完整的自动化计划，包含触发器、规则集和动作列表。"""

    Guid: UUID
    """计划唯一标识符。"""

    Name: str
    """计划名称。"""

    Revertable: bool
    """计划是否支持撤回。"""

    Trigger: Trigger
    """触发器。"""

    RuleSet: RuleSet | None
    """规则集，可为 None（无条件触发）。"""

    Actions: list[Action]
    """触发后执行的动作列表。"""

    def to_dict(self) -> dict[str, Any]:
        return {
            "Guid": str(self.Guid),
            "Name": self.Name,
            "Revertable": self.Revertable,
            "Trigger": self.Trigger.to_dict(),
            "RuleSet": self.RuleSet.to_dict() if self.RuleSet else None,
            "Actions": [a.to_dict() for a in self.Actions],
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> AutomationPlan:
        trigger = _deserialize_trigger(data["Trigger"])
        if trigger is None:
            trigger = Trigger()
        rule_set_raw = data.get("RuleSet")
        rule_set = _deserialize_rule_base(rule_set_raw) if rule_set_raw else None
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data["Name"],
            Revertable=data["Revertable"],
            Trigger=trigger,
            RuleSet=rule_set,  # type: ignore[arg-type]
            Actions=[a for d in data.get("Actions", []) if (a := _deserialize_action(d)) is not None],
        )


# ============================================================================
# Automation request DTOs
# ============================================================================


@dataclass
class TriggerDto:
    """触发器请求 DTO。"""

    TriggerKey: str
    """触发器类型标识符。"""

    Properties: dict[str, Any] | None = None
    """触发器附加属性。"""


@dataclass
class ActionDto:
    """动作请求 DTO。"""

    ActionKey: str
    """动作类型标识符。"""

    Properties: dict[str, Any] | None = None
    """动作附加属性。"""


@dataclass
class RuleNodeDto:
    """规则节点请求 DTO，支持单条规则或嵌套规则集。"""

    RuleKey: str | None = None
    """规则类型标识符，与 RuleSet 二选一。"""

    Properties: dict[str, Any] | None = None
    """规则附加属性。"""

    Not: bool = False
    """是否对规则结果取反。"""

    RuleSet: RuleSetDto | None = None
    """嵌套规则集，与 RuleKey 二选一。"""


@dataclass
class RuleSetDto:
    """规则集请求 DTO。"""

    SatisfyMode: RuleSetSatisfyMode
    """满足模式。"""

    Not: bool
    """是否对规则集结果取反。"""

    Rules: list[RuleNodeDto]
    """子规则节点列表。"""


@dataclass
class CreateAutomationPlanRequest:
    """创建自动化计划请求。"""

    Name: str
    """计划名称。"""

    Revertable: bool
    """是否支持撤回。"""

    Trigger: TriggerDto
    """触发器。"""

    RuleSet: RuleSetDto | None
    """规则集。"""

    Actions: list[ActionDto]
    """动作列表。"""


@dataclass
class UpdateAutomationPlanRequest:
    """更新自动化计划请求。"""

    Name: str
    """计划名称。"""

    Revertable: bool
    """是否支持撤回。"""

    Trigger: TriggerDto
    """触发器。"""

    RuleSet: RuleSetDto | None
    """规则集。"""

    Actions: list[ActionDto]
    """动作列表。"""


# ============================================================================
# Lesson / ClassIsland stub types (from external ClassIsland.Shared.IPC NuGet)
# ============================================================================


@dataclass
class Subject:
    """表示一节课的信息（科目、教师、教室、时间）。"""

    Name: str = ""
    """科目名称。"""

    Teacher: str = ""
    """授课教师。"""

    Room: str = ""
    """教室。"""

    StartTime: str = ""
    """开始时间（HH:MM:SS 格式）。"""

    EndTime: str = ""
    """结束时间（HH:MM:SS 格式）。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Subject:
        return cls(
            Name=data.get("Name", ""),
            Teacher=data.get("Teacher", ""),
            Room=data.get("Room", ""),
            StartTime=data.get("StartTime", ""),
            EndTime=data.get("EndTime", ""),
        )


@dataclass
class TimeLayoutItem:
    """时间布局项，表示一天中的一个时间段。"""

    StartTime: str = ""
    """开始时间（HH:MM:SS 格式）。"""

    EndTime: str = ""
    """结束时间（HH:MM:SS 格式）。"""

    TimeType: int = 0
    """时间段类型（0=上课，1=课间，2=午休等）。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> TimeLayoutItem:
        return cls(
            StartTime=data.get("StartTime", ""),
            EndTime=data.get("EndTime", ""),
            TimeType=data.get("TimeType", 0),
        )


@dataclass
class ClassPlan:
    """课程表，包含当天的课程列表和有效时间布局。"""

    Classes: list[Subject] = field(default_factory=list)
    """课程列表。"""

    ValidTimeLayoutItems: list[TimeLayoutItem] = field(default_factory=list)
    """有效时间布局项列表。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> ClassPlan:
        return cls(
            Classes=[Subject.from_dict(s) for s in data.get("Classes", [])],
            ValidTimeLayoutItems=[
                TimeLayoutItem.from_dict(t) for t in data.get("ValidTimeLayoutItems", [])
            ],
        )


@dataclass
class Profile:
    """用户档案，包含已注册的科目映射。"""

    Subjects: dict[str, Subject] = field(default_factory=dict)
    """科目名称到 Subject 的映射。"""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Profile:
        return cls(
            Subjects={
                k: Subject.from_dict(v) for k, v in data.get("Subjects", {}).items()
            },
        )
