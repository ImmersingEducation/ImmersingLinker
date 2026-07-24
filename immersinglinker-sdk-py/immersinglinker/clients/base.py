"""基础工具模块，包含异常类、时间跨度解析和 JSON 序列化/反序列化工具。"""

from __future__ import annotations

import json
from datetime import timedelta
from enum import Enum
from typing import Any

from ..types import Class, ClassExtraProperty, ClassInfo, Student, StudentExtraProperty
from ..types import AutomationPlan, AutomationPlanInfo
from ..types import Subject, TimeLayoutItem, ClassPlan, Profile


class ImmersingLinkerError(Exception):
    """ImmersingLinker SDK 统一异常类型。"""


def _parse_timedelta(value: Any) -> timedelta:
    """将多种格式的时间跨度值解析为 timedelta。

    支持的格式：秒数（int/float）、JSON 对象、``HH:MM:SS`` 或 ``d.HH:MM:SS`` 字符串。
    """
    if isinstance(value, (int, float)):
        return timedelta(seconds=float(value))
    if isinstance(value, str):
        try:
            parsed = json.loads(value)
            if isinstance(parsed, dict):
                return timedelta(
                    days=parsed.get("Days", 0),
                    hours=parsed.get("Hours", 0),
                    minutes=parsed.get("Minutes", 0),
                    seconds=parsed.get("Seconds", 0),
                    milliseconds=parsed.get("Milliseconds", 0),
                )
        except (json.JSONDecodeError, TypeError):
            pass
        parts = value.split(":")
        if len(parts) == 3:
            h, m, s = parts
            sec_parts = s.split(".")
            sec = int(sec_parts[0])
            ms = int(sec_parts[1][:6].ljust(6, "0")) if len(sec_parts) > 1 else 0
            return timedelta(hours=int(h), minutes=int(m), seconds=sec, milliseconds=ms // 1000)
        if len(parts) == 4:
            d, h, m, s = parts
            sec_parts = s.split(".")
            sec = int(sec_parts[0])
            ms = int(sec_parts[1][:6].ljust(6, "0")) if len(sec_parts) > 1 else 0
            return timedelta(
                days=int(d), hours=int(h), minutes=int(m), seconds=sec, milliseconds=ms // 1000
            )
    if isinstance(value, dict):
        return timedelta(
            days=value.get("Days", 0),
            hours=value.get("Hours", 0),
            minutes=value.get("Minutes", 0),
            seconds=value.get("Seconds", 0),
            milliseconds=value.get("Milliseconds", 0),
        )
    raise ImmersingLinkerError(f"Cannot parse TimeSpan from: {value}")


def _to_json_serializable(obj: Any) -> Any:
    """递归将对象转换为 JSON 可序列化的形式。"""
    if obj is None:
        return None
    if isinstance(obj, (str, int, float, bool)):
        return obj
    if isinstance(obj, timedelta):
        return str(obj)
    if isinstance(obj, Enum):
        return obj.value
    if isinstance(obj, dict):
        return {k: _to_json_serializable(v) for k, v in obj.items()}
    if isinstance(obj, (list, tuple, set)):
        return [_to_json_serializable(v) for v in obj]
    if hasattr(obj, "to_dict"):
        return obj.to_dict()
    if hasattr(obj, "__dataclass_fields__"):
        from dataclasses import asdict

        return asdict(obj)
    return obj


def _deserialize_json_response(response_json: Any, target_type: type | str) -> Any:
    """将 JSON 响应反序列化为目标类型实例。"""
    if response_json is None:
        return None

    type_map: dict[str, type] = {
        "Class": Class,
        "ClassInfo": ClassInfo,
        "Student": Student,
        "ClassExtraProperty": ClassExtraProperty,
        "StudentExtraProperty": StudentExtraProperty,
        "AutomationPlan": AutomationPlan,
        "AutomationPlanInfo": AutomationPlanInfo,
        "Subject": Subject,
        "TimeLayoutItem": TimeLayoutItem,
        "ClassPlan": ClassPlan,
        "Profile": Profile,
    }

    if isinstance(target_type, str):
        target_type = type_map.get(target_type, target_type)

    if isinstance(target_type, type) and hasattr(target_type, "from_dict"):
        if isinstance(response_json, list):
            return [target_type.from_dict(item) for item in response_json]
        return target_type.from_dict(response_json)

    return response_json
