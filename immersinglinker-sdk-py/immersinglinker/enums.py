from __future__ import annotations

from enum import Enum


class Gender(str, Enum):
    Male = "Male"
    Female = "Female"


class RuleSetSatisfyMode(str, Enum):
    AllSatisfied = "AllSatisfied"
    AnySatisfied = "AnySatisfied"


class TimeState(int, Enum):
    None_ = 0
    OnClass = 1
    PrepareOnClass = 2
    Breaking = 3
    AfterSchool = 4
