from __future__ import annotations

from immersinglinker.enums import Gender, RuleSetSatisfyMode, TimeState


def test_gender_values():
    assert Gender.Male == "Male"
    assert Gender.Female == "Female"


def test_gender_is_str_enum():
    assert isinstance(Gender.Male, str)
    assert Gender("Male") is Gender.Male
    assert Gender("Female") is Gender.Female


def test_ruleset_satisfy_mode_values():
    assert RuleSetSatisfyMode.AllSatisfied == "AllSatisfied"
    assert RuleSetSatisfyMode.AnySatisfied == "AnySatisfied"


def test_ruleset_satisfy_mode_is_str_enum():
    assert isinstance(RuleSetSatisfyMode.AllSatisfied, str)


def test_time_state_values():
    assert TimeState.None_ == 0
    assert TimeState.OnClass == 1
    assert TimeState.PrepareOnClass == 2
    assert TimeState.Breaking == 3
    assert TimeState.AfterSchool == 4


def test_time_state_is_int_enum():
    assert isinstance(TimeState.OnClass, int)
    assert TimeState(1) is TimeState.OnClass
    assert TimeState(3) is TimeState.Breaking
