from __future__ import annotations

from enum import Enum


class Gender(str, Enum):
    Male = "Male"
    Female = "Female"


class RuleSetSatisfyMode(str, Enum):
    AllSatisfied = "AllSatisfied"
    AnySatisfied = "AnySatisfied"
