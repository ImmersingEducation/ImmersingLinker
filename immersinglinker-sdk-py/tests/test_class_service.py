from __future__ import annotations

import json
from unittest.mock import AsyncMock, MagicMock
from uuid import uuid4

import pytest

from immersinglinker.clients.class_service import ClassServiceClient
from immersinglinker.clients.base import ImmersingLinkerError
from immersinglinker.enums import Gender
from immersinglinker.types import (
    Class,
    ClassExtraProperty,
    ClassInfo,
    CreateClassRequest,
    CreateExtraPropertyRequest,
    CreateGroupRequest,
    CreateGroupingRuleRequest,
    CreateStudentRequest,
    GroupingRuleResponse,
    Student,
    StudentExtraProperty,
    UpdateClassRequest,
    UpdateExtraPropertyRequest,
    UpdateGroupRequest,
    UpdateGroupingRuleRequest,
    UpdateStudentRequest,
)
from tests.conftest import _ctx, _resp


def _mk(status=200, body=None, text=""):
    s = AsyncMock()
    s.close = AsyncMock()
    s.request = MagicMock(return_value=_ctx(_resp(status, body, text)))
    return s


def _guid():
    return str(uuid4())


def _student_json(**overrides):
    base = {"Guid": _guid(), "Name": "S", "StudentIdInClass": 1, "Gender": "Male"}
    base.update(overrides)
    return base


def _class_json(**overrides):
    base = {"Guid": _guid(), "Name": "C", "Students": [], "GroupingRules": [], "ExtraProperties": []}
    base.update(overrides)
    return base


def _rule_response_json(**overrides):
    base = {"Guid": _guid(), "Name": "Rule", "Groups": [], "UnassignedStudentGuids": []}
    base.update(overrides)
    return base


def _extra_prop_json(app_id="app1", name="score", value=None):
    return {
        "Application": {"UniqueId": app_id, "Name": "App"},
        "Name": name,
        "Value": value,
    }


# ── GET ──────────────────────────────────────────────────────

async def test_get_all_classes():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=[_class_json(), _class_json()])
    result = await c.get_all_classes()
    assert len(result) == 2
    assert isinstance(result[0], Class)


async def test_get_all_class_infos():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=[{"Guid": _guid(), "Name": "A"}])
    result = await c.get_all_class_infos()
    assert len(result) == 1
    assert isinstance(result[0], ClassInfo)


async def test_get_class_by_guid():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_class_json())
    result = await c.get_class_by_guid(_guid())
    assert result is not None


async def test_get_class_by_guid_not_found():
    c = ClassServiceClient("8080")
    c._session = _mk(404)
    result = await c.get_class_by_guid("bad")
    assert result is None


async def test_get_students_by_class_guid():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=[_student_json()])
    result = await c.get_students_by_class_guid(_guid())
    assert len(result) == 1


async def test_get_student_by_student_id():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_student_json())
    result = await c.get_student_by_student_id_in_class(_guid(), 1)
    assert result is not None


async def test_get_student_not_found():
    c = ClassServiceClient("8080")
    c._session = _mk(404)
    result = await c.get_student_by_student_id_in_class(_guid(), 999)
    assert result is None


async def test_get_extra_properties_by_student_id():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=[_extra_prop_json()])
    result = await c.get_extra_properties_by_student_id_in_class(_guid(), 1)
    assert len(result) == 1
    assert isinstance(result[0], StudentExtraProperty)


async def test_get_extra_properties_by_student_id_and_app():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=[_extra_prop_json()])
    result = await c.get_extra_properties_by_student_id_and_app_id_in_class(_guid(), 1, "app1")
    assert len(result) == 1


async def test_get_extra_property_by_name_student_id():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_extra_prop_json(value=100))
    result = await c.get_extra_property_by_name_and_student_id_in_class(_guid(), 1, "app1", "score")
    assert result is not None
    assert result.Value == 100


async def test_get_extra_properties_by_class_guid():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=[_extra_prop_json()])
    result = await c.get_extra_properties_by_class_guid(_guid())
    assert len(result) == 1
    assert isinstance(result[0], ClassExtraProperty)


async def test_get_extra_properties_by_app_id_in_class():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=[_extra_prop_json()])
    result = await c.get_extra_properties_by_app_id_in_class(_guid(), "app1")
    assert len(result) == 1


async def test_get_extra_property_by_app_id_and_name():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_extra_prop_json())
    result = await c.get_extra_property_by_app_id_and_name_in_class(_guid(), "app1", "score")
    assert result is not None


# ── POST ─────────────────────────────────────────────────────

async def test_create_class():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_class_json(Name="New"))
    result = await c.create_class(CreateClassRequest(Name="New"))
    assert result.Name == "New"


async def test_add_student():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_student_json(Name="Bob"))
    result = await c.add_student(
        _guid(), CreateStudentRequest(Name="Bob", StudentIdInClass=1, Gender=Gender.Male)
    )
    assert result.Name == "Bob"


async def test_add_student_not_found():
    c = ClassServiceClient("8080")
    c._session = _mk(404)
    with pytest.raises(ImmersingLinkerError, match="Not found"):
        await c.add_student(_guid(), CreateStudentRequest(Name="X", StudentIdInClass=1, Gender=Gender.Male))


async def test_add_student_conflict():
    c = ClassServiceClient("8080")
    c._session = _mk(409)
    with pytest.raises(ImmersingLinkerError, match="Conflict"):
        await c.add_student(_guid(), CreateStudentRequest(Name="X", StudentIdInClass=1, Gender=Gender.Male))


async def test_add_class_extra_property():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_extra_prop_json(value=100))
    result = await c.add_class_extra_property(
        _guid(), CreateExtraPropertyRequest(AppId="app1", Name="score", Value=100)
    )
    assert result.Value == 100


async def test_add_student_extra_property():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_extra_prop_json(value=100))
    result = await c.add_student_extra_property(
        _guid(), 1, CreateExtraPropertyRequest(AppId="app1", Name="score", Value=100)
    )
    assert result.Name == "score"


# ── PUT ──────────────────────────────────────────────────────

async def test_update_class():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_class_json(Name="Updated"))
    result = await c.update_class(_guid(), UpdateClassRequest(Name="Updated"))
    assert result.Name == "Updated"


async def test_update_student():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_student_json(Name="Alice"))
    result = await c.update_student(
        _guid(), 1, UpdateStudentRequest(Name="Alice", Gender=Gender.Female, GroupInClass="G1")
    )
    assert result.Name == "Alice"


async def test_update_class_extra_property():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_extra_prop_json(value=200))
    result = await c.update_class_extra_property(
        _guid(), "app1", "score", UpdateExtraPropertyRequest(Value=200)
    )
    assert result.Value == 200


async def test_update_student_extra_property():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_extra_prop_json(value=200))
    result = await c.update_student_extra_property(
        _guid(), 1, "app1", "rank", UpdateExtraPropertyRequest(Value=200)
    )
    assert result.Value == 200


# ── DELETE ───────────────────────────────────────────────────

async def test_delete_class():
    c = ClassServiceClient("8080")
    c._session = _mk(200)
    await c.delete_class(_guid())


async def test_delete_student():
    c = ClassServiceClient("8080")
    c._session = _mk(200)
    await c.delete_student(_guid(), 1)


async def test_delete_class_extra_property():
    c = ClassServiceClient("8080")
    c._session = _mk(200)
    await c.delete_class_extra_property(_guid(), "app1", "score")


async def test_delete_student_extra_property():
    c = ClassServiceClient("8080")
    c._session = _mk(200)
    await c.delete_student_extra_property(_guid(), 1, "app1", "score")


# ── Grouping Rules ───────────────────────────────────────────

async def test_get_all_grouping_rules():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=[_rule_response_json()])
    result = await c.get_all_grouping_rules(_guid())
    assert len(result) == 1
    assert isinstance(result[0], GroupingRuleResponse)


async def test_get_grouping_rule_by_guid():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_rule_response_json())
    result = await c.get_grouping_rule_by_guid(_guid(), _guid())
    assert result is not None


async def test_get_grouping_rule_not_found():
    c = ClassServiceClient("8080")
    c._session = _mk(404)
    result = await c.get_grouping_rule_by_guid(_guid(), "bad")
    assert result is None


async def test_add_grouping_rule():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_rule_response_json(Name="NewRule"))
    result = await c.add_grouping_rule(_guid(), CreateGroupingRuleRequest(Name="NewRule"))
    assert result.Name == "NewRule"


async def test_add_group():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_rule_response_json(Name="R", Groups=[
        {"Guid": _guid(), "Name": "G1", "Contains": []}
    ]))
    result = await c.add_group(_guid(), _guid(), CreateGroupRequest(Name="G1"))
    assert len(result.Groups) == 1


async def test_update_grouping_rule():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_rule_response_json(Name="Renamed"))
    result = await c.update_grouping_rule(_guid(), _guid(), UpdateGroupingRuleRequest(Name="Renamed"))
    assert result.Name == "Renamed"


async def test_update_group():
    c = ClassServiceClient("8080")
    c._session = _mk(200, body=_rule_response_json(Name="R", Groups=[
        {"Guid": _guid(), "Name": "Renamed", "Contains": []}
    ]))
    result = await c.update_group(_guid(), _guid(), _guid(), UpdateGroupRequest(Name="Renamed"))
    assert len(result.Groups) == 1


async def test_delete_grouping_rule():
    c = ClassServiceClient("8080")
    c._session = _mk(200)
    await c.delete_grouping_rule(_guid(), _guid())


async def test_delete_group():
    c = ClassServiceClient("8080")
    c._session = _mk(200)
    await c.delete_group(_guid(), _guid(), _guid())


# ── Offline factories ────────────────────────────────────────

def test_create_class_offline():
    cls = ClassServiceClient.create_class_offline("MyClass")
    assert cls.Name == "MyClass"
    assert cls.Guid is not None


def test_create_student_offline():
    s = ClassServiceClient.create_student_offline("Alice", 5, Gender.Female)
    assert s.Name == "Alice"
    assert s.StudentIdInClass == 5
    assert s.Gender is Gender.Female


def test_create_class_extra_property_offline():
    p = ClassServiceClient.create_class_extra_property_offline("app", "key", 42)
    assert p.Application.UniqueId == "app"
    assert p.Value == 42


def test_create_student_extra_property_offline():
    p = ClassServiceClient.create_student_extra_property_offline("app", "key", "val")
    assert p.Value == "val"


# ── Error handling ───────────────────────────────────────────

async def test_ensure_session_raises():
    c = ClassServiceClient("8080")
    with pytest.raises(ImmersingLinkerError, match="not opened"):
        await c.get_all_classes()
