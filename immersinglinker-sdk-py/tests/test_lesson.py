from __future__ import annotations

import json
from unittest.mock import AsyncMock, MagicMock
from uuid import uuid4

import pytest

from immersinglinker.clients.base import ImmersingLinkerError
from immersinglinker.clients.lesson import LessonServiceClient
from immersinglinker.enums import TimeState
from immersinglinker.types import ClassPlan, Profile, Subject, TimeLayoutItem
from tests.conftest import _ctx, _resp


def _mk(status=200, body=None, text=""):
    s = AsyncMock()
    s.close = AsyncMock()
    s.get = MagicMock(return_value=_ctx(_resp(status, body, text)))
    return s


# ── Current ──────────────────────────────────────────────────

async def test_get_current_subject():
    c = LessonServiceClient("8080")
    c._session = _mk(200, body={"Name": "Math", "Teacher": "T", "Room": "R", "StartTime": "", "EndTime": ""})
    result = await c.get_current_subject()
    assert isinstance(result, Subject)
    assert result.Name == "Math"


async def test_get_current_subject_not_found():
    c = LessonServiceClient("8080")
    c._session = _mk(404)
    result = await c.get_current_subject()
    assert result is None


async def test_get_next_class_subject():
    c = LessonServiceClient("8080")
    c._session = _mk(200, body={"Name": "Eng", "Teacher": "", "Room": "", "StartTime": "", "EndTime": ""})
    result = await c.get_next_class_subject()
    assert result.Name == "Eng"


async def test_get_current_state():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="1")
    result = await c.get_current_state()
    assert isinstance(result, TimeState)
    assert result is TimeState.OnClass


async def test_get_current_state_breaking():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="3")
    result = await c.get_current_state()
    assert result is TimeState.Breaking


async def test_get_current_time_layout_item():
    c = LessonServiceClient("8080")
    c._session = _mk(200, body={"StartTime": "08:00", "EndTime": "09:00", "TimeType": 1})
    result = await c.get_current_time_layout_item()
    assert isinstance(result, TimeLayoutItem)
    assert result.TimeType == 1


async def test_get_current_class_plan():
    c = LessonServiceClient("8080")
    c._session = _mk(200, body={"Classes": [], "ValidTimeLayoutItems": []})
    result = await c.get_current_class_plan()
    assert isinstance(result, ClassPlan)


async def test_get_current_class_plan_not_found():
    c = LessonServiceClient("8080")
    c._session = _mk(404)
    result = await c.get_current_class_plan()
    assert result is None


async def test_get_current_selected_index():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="5")
    result = await c.get_current_selected_index()
    assert result == 5


# ── Boolean methods (JSON bool) ──────────────────────────────

async def test_get_is_class_plan_enabled_true():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="true")
    assert await c.get_is_class_plan_enabled() is True


async def test_get_is_class_plan_enabled_false():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="false")
    assert await c.get_is_class_plan_enabled() is False


async def test_get_is_class_plan_enabled_json_bool():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text=json.dumps(True))
    assert await c.get_is_class_plan_enabled() is True


async def test_get_is_class_plan_loaded():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="true")
    assert await c.get_is_class_plan_loaded() is True


async def test_get_is_lesson_confirmed():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text=json.dumps(False))
    assert await c.get_is_lesson_confirmed() is False


async def test_get_is_current_profile_trusted_true():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="true")
    assert await c.get_is_current_profile_trusted() is True


async def test_get_is_current_profile_trusted_json_bool():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text=json.dumps(True))
    assert await c.get_is_current_profile_trusted() is True


# ── Next / Previous ──────────────────────────────────────────

async def test_get_next_class_time_layout_item():
    c = LessonServiceClient("8080")
    c._session = _mk(200, body={"StartTime": "09:00", "EndTime": "10:00", "TimeType": 0})
    result = await c.get_next_class_time_layout_item()
    assert result.StartTime == "09:00"


async def test_get_next_breaking_time_layout_item():
    c = LessonServiceClient("8080")
    c._session = _mk(200, body={"StartTime": "10:00", "EndTime": "10:10", "TimeType": 1})
    result = await c.get_next_breaking_time_layout_item()
    assert result.TimeType == 1


async def test_get_previous_class_subject():
    c = LessonServiceClient("8080")
    c._session = _mk(200, body={"Name": "History", "Teacher": "", "Room": "", "StartTime": "", "EndTime": ""})
    result = await c.get_previous_class_subject()
    assert result.Name == "History"


async def test_get_previous_class_subject_not_found():
    c = LessonServiceClient("8080")
    c._session = _mk(404)
    result = await c.get_previous_class_subject()
    assert result is None


async def test_get_previous_class_time_layout_item():
    c = LessonServiceClient("8080")
    c._session = _mk(200, body={"StartTime": "07:30", "EndTime": "08:30", "TimeType": 0})
    result = await c.get_previous_class_time_layout_item()
    assert result.StartTime == "07:30"


async def test_get_previous_breaking_time_layout_item():
    c = LessonServiceClient("8080")
    c._session = _mk(200, body={"StartTime": "08:30", "EndTime": "08:40", "TimeType": 1})
    result = await c.get_previous_breaking_time_layout_item()
    assert result.TimeType == 1


# ── Timer ────────────────────────────────────────────────────

async def test_get_on_class_left_time():
    from datetime import timedelta
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="00:30:00")
    result = await c.get_on_class_left_time()
    assert isinstance(result, timedelta)
    assert result == timedelta(minutes=30)


async def test_get_on_breaking_left_time():
    from datetime import timedelta
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="00:10:00")
    result = await c.get_on_breaking_left_time()
    assert result == timedelta(minutes=10)


async def test_get_elapsed_since_previous_class():
    from datetime import timedelta
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="00:05:30")
    result = await c.get_elapsed_since_previous_class()
    assert result == timedelta(minutes=5, seconds=30)


async def test_get_elapsed_since_previous_breaking():
    from datetime import timedelta
    c = LessonServiceClient("8080")
    c._session = _mk(200, text="00:02:00")
    result = await c.get_elapsed_since_previous_breaking()
    assert result == timedelta(minutes=2)


async def test_get_elapsed_since_previous_any():
    from datetime import timedelta
    c = LessonServiceClient("8080")
    c._session = _mk(200, text=json.dumps({"Hours": 1, "Minutes": 0, "Seconds": 0}))
    result = await c.get_elapsed_since_previous_any()
    assert result == timedelta(hours=1)


# ── Profile ──────────────────────────────────────────────────

async def test_get_current_profile_path():
    c = LessonServiceClient("8080")
    c._session = _mk(200, text=json.dumps("/home/user/.classisland/profile.xml"))
    result = await c.get_current_profile_path()
    assert result == "/home/user/.classisland/profile.xml"


async def test_get_profile():
    c = LessonServiceClient("8080")
    c._session = _mk(200, body={
        "Subjects": {"math": {"Name": "Math", "Teacher": "", "Room": "", "StartTime": "", "EndTime": ""}}
    })
    result = await c.get_profile()
    assert isinstance(result, Profile)
    assert "math" in result.Subjects


# ── Error handling ───────────────────────────────────────────

async def test_ensure_session_raises():
    c = LessonServiceClient("8080")
    with pytest.raises(ImmersingLinkerError, match="not opened"):
        await c.get_current_subject()


async def test_http_error_raises():
    c = LessonServiceClient("8080")
    c._session = _mk(500, text="server error")
    with pytest.raises(ImmersingLinkerError, match="HTTP 500"):
        await c.get_current_subject()


# ── Close ────────────────────────────────────────────────────

async def test_close():
    c = LessonServiceClient("8080")
    c._session = _mk(200)
    await c.close()
    assert c._session is None


async def test_close_when_none():
    c = LessonServiceClient("8080")
    await c.close()
