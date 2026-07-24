from __future__ import annotations

import json
from unittest.mock import AsyncMock, MagicMock
from uuid import uuid4

import pytest

from immersinglinker.clients.automation import AutomationServiceClient
from immersinglinker.clients.base import ImmersingLinkerError
from immersinglinker.enums import RuleSetSatisfyMode
from immersinglinker.types import (
    ActionDto,
    CreateAutomationPlanRequest,
    TriggerDto,
    UrlTrigger,
)
from tests.conftest import _ctx, _resp


def _mk(status=200, body=None, text=""):
    s = AsyncMock()
    s.close = AsyncMock()
    s.request = MagicMock(return_value=_ctx(_resp(status, body, text)))
    return s


async def test_get_all_plan_infos():
    c = AutomationServiceClient("8080")
    c._session = _mk(200, body=[
        {"Guid": str(uuid4()), "Name": "Plan1"},
        {"Guid": str(uuid4()), "Name": "Plan2"},
    ])
    result = await c.get_all_plan_infos()
    assert len(result) == 2
    assert result[0].Name == "Plan1"


async def test_get_plan_by_guid():
    gid = str(uuid4())
    c = AutomationServiceClient("8080")
    c._session = _mk(200, body={
        "Guid": gid, "Name": "P", "Revertable": False,
        "Trigger": {"$type": "UrlTrigger", "Tag": "t"},
        "RuleSet": None, "Actions": [],
    })
    result = await c.get_plan_by_guid(gid)
    assert result is not None
    assert result.Name == "P"


async def test_get_plan_by_guid_not_found():
    c = AutomationServiceClient("8080")
    c._session = _mk(404)
    result = await c.get_plan_by_guid("bad")
    assert result is None


async def test_create_plan():
    c = AutomationServiceClient("8080")
    plan = {
        "Guid": str(uuid4()), "Name": "New", "Revertable": True,
        "Trigger": {"$type": "UrlTrigger", "Tag": "t"},
        "RuleSet": None, "Actions": [],
    }
    c._session = _mk(200, body=plan)
    req = CreateAutomationPlanRequest(
        Name="New", Revertable=True,
        Trigger=TriggerDto(TriggerKey="url"),
        RuleSet=None, Actions=[],
    )
    result = await c.create_plan(req)
    assert result.Name == "New"


async def test_create_plan_bad_request():
    c = AutomationServiceClient("8080")
    c._session = _mk(400, text="invalid")
    req = CreateAutomationPlanRequest(
        Name="Bad", Revertable=False,
        Trigger=TriggerDto(TriggerKey="x"),
        RuleSet=None, Actions=[],
    )
    with pytest.raises(ImmersingLinkerError, match="Bad request"):
        await c.create_plan(req)


async def test_trigger_plan():
    c = AutomationServiceClient("8080")
    c._session = _mk(200)
    await c.trigger_plan("guid123")


async def test_trigger_plan_not_found():
    c = AutomationServiceClient("8080")
    c._session = _mk(404)
    with pytest.raises(ImmersingLinkerError, match="Not found"):
        await c.trigger_plan("bad")


async def test_invoke_url_trigger():
    c = AutomationServiceClient("8080")
    c._session = _mk(200)
    await c.invoke_url_trigger("mytag")


async def test_update_plan():
    c = AutomationServiceClient("8080")
    c._session = _mk(200, body={
        "Guid": str(uuid4()), "Name": "Updated", "Revertable": False,
        "Trigger": {"$type": "UrlTrigger", "Tag": "t"},
        "RuleSet": None, "Actions": [],
    })
    req = CreateAutomationPlanRequest(
        Name="Updated", Revertable=False,
        Trigger=TriggerDto(TriggerKey="url"),
        RuleSet=None, Actions=[],
    )
    result = await c.update_plan("guid123", req)
    assert result.Name == "Updated"


async def test_delete_plan():
    c = AutomationServiceClient("8080")
    c._session = _mk(200)
    await c.delete_plan("guid123")


async def test_delete_plan_not_found():
    c = AutomationServiceClient("8080")
    c._session = _mk(404)
    with pytest.raises(ImmersingLinkerError, match="Not found"):
        await c.delete_plan("bad")


# ── Offline factories ────────────────────────────────────────

def test_create_automation_plan_offline():
    plan = AutomationServiceClient.create_automation_plan_offline(
        name="Test", revertable=True, trigger=UrlTrigger(Tag="t"),
        rule_set=None, actions=[],
    )
    assert plan.Name == "Test"
    assert plan.Revertable is True
    assert isinstance(plan.Trigger, UrlTrigger)
    assert plan.RuleSet is not None


def test_create_trigger_dto_offline():
    dto = AutomationServiceClient.create_trigger_dto_offline("key", {"p": 1})
    assert dto.TriggerKey == "key"


def test_create_rule_set_offline():
    dto = AutomationServiceClient.create_rule_set_offline(
        RuleSetSatisfyMode.AllSatisfied, False, []
    )
    assert dto.SatisfyMode is RuleSetSatisfyMode.AllSatisfied


def test_create_rule_node_offline():
    dto = AutomationServiceClient.create_rule_node_offline("rk", {"a": 1}, True)
    assert dto.RuleKey == "rk"
    assert dto.Not is True


def test_create_action_dto_offline():
    dto = AutomationServiceClient.create_action_dto_offline("ak", {"b": 2})
    assert dto.ActionKey == "ak"


def test_create_plan_request_offline():
    req = AutomationServiceClient.create_plan_request_offline(
        "P", True, TriggerDto(TriggerKey="t"), None, []
    )
    assert req.Name == "P"


def test_update_plan_request_offline():
    req = AutomationServiceClient.update_plan_request_offline(
        "P", False, TriggerDto(TriggerKey="t"), None, []
    )
    assert req.Revertable is False
