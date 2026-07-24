from __future__ import annotations

from datetime import timedelta
from uuid import UUID

import pytest

from immersinglinker.clients.base import (
    ImmersingLinkerError,
    _deserialize_json_response,
    _parse_timedelta,
    _to_json_serializable,
)
from immersinglinker.enums import Gender
from immersinglinker.types import ClassInfo, Subject, Student


# ── _parse_timedelta ─────────────────────────────────────────

def test_parse_timedelta_int():
    assert _parse_timedelta(3600) == timedelta(hours=1)


def test_parse_timedelta_float():
    assert _parse_timedelta(90.5) == timedelta(seconds=90.5)


def test_parse_timedelta_hhmmss():
    result = _parse_timedelta("01:30:00")
    assert result == timedelta(hours=1, minutes=30)


def test_parse_timedelta_hhmmss_with_ms():
    result = _parse_timedelta("00:05:30.123456")
    assert result == timedelta(minutes=5, seconds=30, milliseconds=123)


def test_parse_timedelta_ddhhmmss():
    result = _parse_timedelta("1:02:30:00")
    assert result == timedelta(days=1, hours=2, minutes=30)


def test_parse_timedelta_dict():
    result = _parse_timedelta({"Hours": 2, "Minutes": 15, "Seconds": 30})
    assert result == timedelta(hours=2, minutes=15, seconds=30)


def test_parse_timedelta_dict_with_days():
    result = _parse_timedelta({"Days": 1, "Hours": 3, "Minutes": 0, "Seconds": 0, "Milliseconds": 500})
    assert result == timedelta(days=1, hours=3, milliseconds=500)


def test_parse_timedelta_invalid_type():
    with pytest.raises(ImmersingLinkerError, match="Cannot parse TimeSpan"):
        _parse_timedelta([1, 2, 3])


# ── _to_json_serializable ────────────────────────────────────

def test_to_json_serializable_none():
    assert _to_json_serializable(None) is None


def test_to_json_serializable_primitives():
    assert _to_json_serializable("hello") == "hello"
    assert _to_json_serializable(42) == 42
    assert _to_json_serializable(3.14) == 3.14
    assert _to_json_serializable(True) is True


def test_to_json_serializable_timedelta():
    result = _to_json_serializable(timedelta(hours=1))
    assert isinstance(result, str)


def test_to_json_serializable_enum():
    result = _to_json_serializable(Gender.Male)
    assert result == "Male"


def test_to_json_serializable_dict():
    result = _to_json_serializable({"a": Gender.Male, "b": 1})
    assert result == {"a": "Male", "b": 1}


def test_to_json_serializable_list():
    result = _to_json_serializable([Gender.Female, 2])
    assert result == ["Female", 2]


def test_to_json_serializable_dataclass():
    info = ClassInfo(Guid=UUID("00000000-0000-0000-0000-000000000001"), Name="X")
    result = _to_json_serializable(info)
    assert result["Name"] == "X"


# ── _deserialize_json_response ───────────────────────────────

def test_deserialize_none():
    assert _deserialize_json_response(None, "Class") is None


def test_deserialize_single():
    data = {"Name": "Math", "Teacher": "", "Room": "", "StartTime": "", "EndTime": ""}
    result = _deserialize_json_response(data, Subject)
    assert isinstance(result, Subject)
    assert result.Name == "Math"


def test_deserialize_list():
    data = [
        {"Name": "Math", "Teacher": "", "Room": "", "StartTime": "", "EndTime": ""},
        {"Name": "Eng", "Teacher": "", "Room": "", "StartTime": "", "EndTime": ""},
    ]
    result = _deserialize_json_response(data, Subject)
    assert len(result) == 2


def test_deserialize_by_string_type():
    data = {"Name": "Math", "Teacher": "", "Room": "", "StartTime": "", "EndTime": ""}
    result = _deserialize_json_response(data, "Subject")
    assert isinstance(result, Subject)


def test_deserialize_by_string_type_unknown():
    data = {"foo": 1}
    result = _deserialize_json_response(data, "NonExistent")
    assert result == {"foo": 1}
