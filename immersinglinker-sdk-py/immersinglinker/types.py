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
    UniqueId: str
    Name: str


@dataclass
class ClassInfo:
    Guid: UUID
    Name: str

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> ClassInfo:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data.get("Name", ""),
        )


@dataclass
class ClassExtraProperty:
    Application: Application
    Name: str
    Value: Any = None

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
    Application: Application
    Name: str
    Value: Any = None

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
    Guid: UUID
    Name: str
    StudentIdInClass: int
    Gender: Gender
    ExtraProperties: list[StudentExtraProperty] = field(default_factory=list)

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
    Guid: UUID
    Name: str
    Contains: set[UUID] = field(default_factory=set)

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
    Guid: UUID
    Name: str
    Groups: list[Group] = field(default_factory=list)

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> GroupingRule:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data["Name"],
            Groups=[Group.from_dict(g) for g in data.get("Groups", [])],
        )


@dataclass
class GroupingRuleResponse:
    Guid: UUID
    Name: str
    Groups: list[Group] = field(default_factory=list)
    UnassignedStudentGuids: list[UUID] = field(default_factory=list)

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
    Guid: UUID
    Name: str
    Students: list[Student] = field(default_factory=list)
    GroupingRules: list[GroupingRule] = field(default_factory=list)
    ExtraProperties: list[ClassExtraProperty] = field(default_factory=list)

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
    Name: str


@dataclass
class UpdateClassRequest:
    Name: str


@dataclass
class CreateStudentRequest:
    Name: str
    StudentIdInClass: int
    Gender: Gender


@dataclass
class UpdateStudentRequest:
    Name: str
    Gender: Gender
    GroupInClass: str


@dataclass
class CreateExtraPropertyRequest:
    AppId: str
    Name: str
    Value: Any = None


@dataclass
class UpdateExtraPropertyRequest:
    Value: Any = None


@dataclass
class CreateGroupingRuleRequest:
    Name: str


@dataclass
class UpdateGroupingRuleRequest:
    Name: str


@dataclass
class CreateGroupRequest:
    Name: str


@dataclass
class UpdateGroupRequest:
    Name: str


# ============================================================================
# Automation abstractions (server-side base types, also appear in API responses)
# ============================================================================


@dataclass
class Trigger:
    TriggerFired: Any = None

    def to_dict(self) -> dict[str, Any]:
        return {"$type": type(self).__name__}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Trigger:
        return cls()


@dataclass
class Action:
    Revertable: bool = False

    def to_dict(self) -> dict[str, Any]:
        return {"$type": type(self).__name__, "Revertable": self.Revertable}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Action:
        return cls(Revertable=data.get("Revertable", False))


@dataclass
class RuleBase:
    Guid: UUID = field(default_factory=uuid4)
    Not: bool = False

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
    Tag: str = ""

    def to_dict(self) -> dict[str, Any]:
        return {"$type": "UrlTrigger", "Tag": self.Tag}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> UrlTrigger:
        return cls(Tag=data.get("Tag", ""))


@dataclass
class UnknownTrigger(Trigger):
    _raw: dict[str, Any] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return {"$type": self._raw.get("$type", "UnknownTrigger"), **self._raw}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> UnknownTrigger:
        return cls(_raw=data)


@dataclass
class UnknownAction(Action):
    _raw: dict[str, Any] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return {"$type": self._raw.get("$type", "UnknownAction"), **self._raw}

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> UnknownAction:
        return cls(_raw=data, Revertable=data.get("Revertable", False))


@_register_rule_base
@dataclass
class RuleSet(RuleBase):
    SatisfyMode: RuleSetSatisfyMode = RuleSetSatisfyMode.AllSatisfied
    Rules: list[RuleBase] = field(default_factory=list)

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
    Guid: UUID
    Name: str = ""

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> AutomationPlanInfo:
        return cls(
            Guid=UUID(data["Guid"]) if isinstance(data["Guid"], str) else data["Guid"],
            Name=data.get("Name", ""),
        )


@dataclass
class AutomationPlan:
    Guid: UUID
    Name: str
    Revertable: bool
    Trigger: Trigger
    RuleSet: RuleSet | None
    Actions: list[Action]

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
    TriggerKey: str
    Properties: dict[str, Any] | None = None


@dataclass
class ActionDto:
    ActionKey: str
    Properties: dict[str, Any] | None = None


@dataclass
class RuleNodeDto:
    RuleKey: str | None = None
    Properties: dict[str, Any] | None = None
    Not: bool = False
    RuleSet: RuleSetDto | None = None


@dataclass
class RuleSetDto:
    SatisfyMode: RuleSetSatisfyMode
    Not: bool
    Rules: list[RuleNodeDto]


@dataclass
class CreateAutomationPlanRequest:
    Name: str
    Revertable: bool
    Trigger: TriggerDto
    RuleSet: RuleSetDto | None
    Actions: list[ActionDto]


@dataclass
class UpdateAutomationPlanRequest:
    Name: str
    Revertable: bool
    Trigger: TriggerDto
    RuleSet: RuleSetDto | None
    Actions: list[ActionDto]


# ============================================================================
# Lesson / ClassIsland stub types (from external ClassIsland.Shared.IPC NuGet)
# ============================================================================


@dataclass
class Subject:
    Name: str = ""
    Teacher: str = ""
    Room: str = ""
    StartTime: str = ""
    EndTime: str = ""

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
    StartTime: str = ""
    EndTime: str = ""
    TimeType: int = 0

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> TimeLayoutItem:
        return cls(
            StartTime=data.get("StartTime", ""),
            EndTime=data.get("EndTime", ""),
            TimeType=data.get("TimeType", 0),
        )


@dataclass
class ClassPlan:
    Classes: list[Subject] = field(default_factory=list)
    ValidTimeLayoutItems: list[TimeLayoutItem] = field(default_factory=list)

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
    Subjects: dict[str, Subject] = field(default_factory=dict)

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Profile:
        return cls(
            Subjects={
                k: Subject.from_dict(v) for k, v in data.get("Subjects", {}).items()
            },
        )
