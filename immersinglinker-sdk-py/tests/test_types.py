from __future__ import annotations

from uuid import UUID, uuid4

import pytest

from immersinglinker.enums import Gender, RuleSetSatisfyMode
from immersinglinker.types import (
    Action,
    ActionDto,
    Application,
    AutomationPlan,
    AutomationPlanInfo,
    Class,
    ClassExtraProperty,
    ClassInfo,
    ClassPlan,
    CreateAutomationPlanRequest,
    CreateClassRequest,
    CreateExtraPropertyRequest,
    CreateGroupRequest,
    CreateGroupingRuleRequest,
    CreateStudentRequest,
    Group,
    GroupingRule,
    GroupingRuleResponse,
    Profile,
    RuleBase,
    RuleNodeDto,
    RuleSet,
    RuleSetDto,
    Student,
    StudentExtraProperty,
    Subject,
    TimeLayoutItem,
    Trigger,
    TriggerDto,
    UnknownAction,
    UnknownRuleBase,
    UnknownTrigger,
    UpdateAutomationPlanRequest,
    UpdateClassRequest,
    UpdateExtraPropertyRequest,
    UpdateGroupRequest,
    UpdateGroupingRuleRequest,
    UpdateStudentRequest,
    UrlTrigger,
)

TEST_GUID = "11111111-1111-1111-1111-111111111111"


# ── Application ──────────────────────────────────────────────

def test_application_construction():
    app = Application(UniqueId="my-app", Name="My App")
    assert app.UniqueId == "my-app"
    assert app.Name == "My App"


# ── ClassInfo ────────────────────────────────────────────────

def test_class_info_construction():
    info = ClassInfo(Guid=UUID(TEST_GUID), Name="Class A")
    assert info.Guid == UUID(TEST_GUID)
    assert info.Name == "Class A"


# ── ClassExtraProperty ───────────────────────────────────────

def test_class_extra_property_roundtrip():
    prop = ClassExtraProperty(
        Application=Application(UniqueId="app1", Name="App1"),
        Name="score",
        Value=95,
    )
    d = prop.to_dict()
    restored = ClassExtraProperty.from_dict(d)
    assert restored.Application.UniqueId == "app1"
    assert restored.Name == "score"
    assert restored.Value == 95


def test_class_extra_property_value_none():
    prop = ClassExtraProperty(
        Application=Application(UniqueId="app1", Name=""),
        Name="empty",
    )
    d = prop.to_dict()
    assert d["Value"] is None


# ── StudentExtraProperty ─────────────────────────────────────

def test_student_extra_property_roundtrip():
    prop = StudentExtraProperty(
        Application=Application(UniqueId="app1", Name=""),
        Name="rank",
        Value="A",
    )
    d = prop.to_dict()
    restored = StudentExtraProperty.from_dict(d)
    assert restored.Value == "A"


# ── Student ──────────────────────────────────────────────────

def test_student_from_dict():
    data = {
        "Guid": TEST_GUID,
        "Name": "Alice",
        "StudentIdInClass": 1,
        "Gender": "Male",
        "ExtraProperties": [],
    }
    s = Student.from_dict(data)
    assert s.Guid == UUID(TEST_GUID)
    assert s.Name == "Alice"
    assert s.Gender is Gender.Male
    assert s.ExtraProperties == []


def test_student_from_dict_with_extra_props():
    data = {
        "Guid": TEST_GUID,
        "Name": "Bob",
        "StudentIdInClass": 2,
        "Gender": "Female",
        "ExtraProperties": [
            {
                "Application": {"UniqueId": "a", "Name": ""},
                "Name": "x",
                "Value": 10,
            }
        ],
    }
    s = Student.from_dict(data)
    assert len(s.ExtraProperties) == 1
    assert s.ExtraProperties[0].Value == 10


# ── Group / GroupingRule / GroupingRuleResponse ──────────────

def test_group_from_dict():
    student_guid = str(uuid4())
    data = {
        "Guid": TEST_GUID,
        "Name": "Group 1",
        "Contains": [student_guid],
    }
    g = Group.from_dict(data)
    assert g.Guid == UUID(TEST_GUID)
    assert g.Name == "Group 1"
    assert UUID(student_guid) in g.Contains


def test_grouping_rule_from_dict():
    data = {
        "Guid": TEST_GUID,
        "Name": "Rule 1",
        "Groups": [
            {"Guid": str(uuid4()), "Name": "G1", "Contains": []}
        ],
    }
    r = GroupingRule.from_dict(data)
    assert r.Name == "Rule 1"
    assert len(r.Groups) == 1


def test_grouping_rule_response_from_dict():
    sg = str(uuid4())
    data = {
        "Guid": TEST_GUID,
        "Name": "RR",
        "Groups": [],
        "UnassignedStudentGuids": [sg],
    }
    rr = GroupingRuleResponse.from_dict(data)
    assert UUID(sg) in rr.UnassignedStudentGuids


# ── Class ────────────────────────────────────────────────────

def test_class_from_dict_full():
    data = {
        "Guid": TEST_GUID,
        "Name": "Class 1",
        "Students": [
            {
                "Guid": str(uuid4()),
                "Name": "S1",
                "StudentIdInClass": 1,
                "Gender": "Male",
            }
        ],
        "GroupingRules": [
            {
                "Guid": str(uuid4()),
                "Name": "R1",
                "Groups": [],
            }
        ],
        "ExtraProperties": [
            {
                "Application": {"UniqueId": "a", "Name": ""},
                "Name": "p",
                "Value": None,
            }
        ],
    }
    c = Class.from_dict(data)
    assert c.Name == "Class 1"
    assert len(c.Students) == 1
    assert len(c.GroupingRules) == 1
    assert len(c.ExtraProperties) == 1


def test_class_from_dict_empty_collections():
    data = {"Guid": TEST_GUID, "Name": "Empty"}
    c = Class.from_dict(data)
    assert c.Students == []
    assert c.GroupingRules == []
    assert c.ExtraProperties == []


# ── Request DTOs ─────────────────────────────────────────────

def test_create_class_request():
    r = CreateClassRequest(Name="A")
    assert r.Name == "A"


def test_update_student_request():
    r = UpdateStudentRequest(Name="X", Gender=Gender.Male, GroupInClass="G1")
    assert r.GroupInClass == "G1"


def test_create_grouping_rule_request():
    r = CreateGroupingRuleRequest(Name="Rules")
    assert r.Name == "Rules"


def test_create_group_request():
    r = CreateGroupRequest(Name="Team A")
    assert r.Name == "Team A"


# ── Automation abstractions ──────────────────────────────────

def test_trigger_base():
    t = Trigger()
    d = t.to_dict()
    assert d["$type"] == "Trigger"


def test_url_trigger_roundtrip():
    t = UrlTrigger(Tag="my-tag")
    d = t.to_dict()
    assert d["$type"] == "UrlTrigger"
    assert d["Tag"] == "my-tag"
    restored = UrlTrigger.from_dict(d)
    assert restored.Tag == "my-tag"


def test_unknown_trigger_preserves_raw():
    data = {"$type": "CustomTrigger", "Foo": 123}
    t = UnknownTrigger.from_dict(data)
    assert isinstance(t, UnknownTrigger)
    d = t.to_dict()
    assert d["Foo"] == 123


def test_action_base():
    a = Action(Revertable=True)
    assert a.Revertable is True
    d = a.to_dict()
    assert d["Revertable"] is True


def test_unknown_action_preserves_raw():
    data = {"$type": "Custom", "Revertable": True, "X": 1}
    a = UnknownAction.from_dict(data)
    assert a.Revertable is True


def test_rule_set_roundtrip():
    rs = RuleSet(
        SatisfyMode=RuleSetSatisfyMode.AnySatisfied,
        Not=True,
        Rules=[],
    )
    d = rs.to_dict()
    assert d["SatisfyMode"] == "AnySatisfied"
    assert d["Not"] is True
    restored = RuleSet.from_dict(d)
    assert restored.SatisfyMode is RuleSetSatisfyMode.AnySatisfied
    assert restored.Not is True


def test_unknown_rule_base():
    data = {"$type": "Foo", "Guid": str(uuid4()), "Not": False, "Custom": True}
    r = UnknownRuleBase.from_dict(data)
    assert r._raw["Custom"] is True


# ── Automation domain models ─────────────────────────────────

def test_automation_plan_info_from_dict():
    data = {"Guid": TEST_GUID, "Name": "Plan1"}
    info = AutomationPlanInfo.from_dict(data)
    assert info.Name == "Plan1"


def test_automation_plan_roundtrip():
    plan = AutomationPlan(
        Guid=uuid4(),
        Name="Test Plan",
        Revertable=True,
        Trigger=UrlTrigger(Tag="t1"),
        RuleSet=RuleSet(
            SatisfyMode=RuleSetSatisfyMode.AllSatisfied,
            Not=False,
        ),
        Actions=[Action(Revertable=False)],
    )
    d = plan.to_dict()
    restored = AutomationPlan.from_dict(d)
    assert restored.Name == "Test Plan"
    assert restored.Revertable is True
    assert isinstance(restored.Trigger, UrlTrigger)
    assert len(restored.Actions) == 1


def test_automation_plan_null_ruleset():
    plan = AutomationPlan(
        Guid=uuid4(),
        Name="No Rules",
        Revertable=False,
        Trigger=Trigger(),
        RuleSet=None,
        Actions=[],
    )
    d = plan.to_dict()
    assert d["RuleSet"] is None
    restored = AutomationPlan.from_dict(d)
    assert restored.RuleSet is None


# ── Automation request DTOs ──────────────────────────────────

def test_trigger_dto():
    dto = TriggerDto(TriggerKey="url", Properties={"path": "/x"})
    assert dto.TriggerKey == "url"


def test_rule_set_dto():
    dto = RuleSetDto(
        SatisfyMode=RuleSetSatisfyMode.AllSatisfied,
        Not=False,
        Rules=[],
    )
    assert dto.Not is False


def test_create_automation_plan_request():
    req = CreateAutomationPlanRequest(
        Name="P",
        Revertable=True,
        Trigger=TriggerDto(TriggerKey="t"),
        RuleSet=None,
        Actions=[],
    )
    assert req.Name == "P"


# ── Lesson / ClassIsland stub types ──────────────────────────

def test_subject_from_dict():
    data = {"Name": "Math", "Teacher": "Mr. X", "Room": "101", "StartTime": "08:00", "EndTime": "09:00"}
    s = Subject.from_dict(data)
    assert s.Name == "Math"
    assert s.Room == "101"


def test_subject_defaults():
    s = Subject()
    assert s.Name == ""
    assert s.Teacher == ""


def test_time_layout_item_from_dict():
    data = {"StartTime": "08:00", "EndTime": "09:00", "TimeType": 1}
    t = TimeLayoutItem.from_dict(data)
    assert t.TimeType == 1


def test_class_plan_from_dict():
    data = {
        "Classes": [{"Name": "Math"}],
        "ValidTimeLayoutItems": [{"StartTime": "08:00", "EndTime": "09:00", "TimeType": 0}],
    }
    cp = ClassPlan.from_dict(data)
    assert len(cp.Classes) == 1
    assert len(cp.ValidTimeLayoutItems) == 1


def test_profile_from_dict():
    data = {
        "Subjects": {
            "math": {"Name": "Math", "Teacher": "T", "Room": "R", "StartTime": "", "EndTime": ""}
        }
    }
    p = Profile.from_dict(data)
    assert "math" in p.Subjects
    assert p.Subjects["math"].Name == "Math"


def test_profile_empty():
    p = Profile.from_dict({})
    assert p.Subjects == {}
