"""枚举类型定义。

本模块包含 ImmersingLinker 使用的所有枚举类型，
涵盖性别、规则满足模式和课程时间状态。
"""

from __future__ import annotations

from enum import Enum


class Gender(str, Enum):
    """学生性别。"""

    Male = "Male"
    """男"""

    Female = "Female"
    """女"""


class RuleSetSatisfyMode(str, Enum):
    """自动化规则集满足模式。"""

    AllSatisfied = "AllSatisfied"
    """所有规则均满足时触发"""

    AnySatisfied = "AnySatisfied"
    """任一规则满足时触发"""


class TimeState(int, Enum):
    """当前所处的时间状态。

    对应 ClassIsland 的 ``TimeState`` 枚举，
    用于 ``LessonServiceClient.get_current_state()`` 的返回值。
    """

    None_ = 0
    """无"""

    OnClass = 1
    """上课"""

    PrepareOnClass = 2
    """准备上课（预留）"""

    Breaking = 3
    """课间休息"""

    AfterSchool = 4
    """放学"""
