from __future__ import annotations

import json
from datetime import timedelta

import aiohttp

from ..enums import TimeState
from ..types import Subject, TimeLayoutItem, ClassPlan, Profile
from .base import ImmersingLinkerError, _parse_timedelta


class LessonServiceClient:
    def __init__(self, port: str) -> None:
        self._base_url = f"http://localhost:{port}"
        self._session: aiohttp.ClientSession | None = None

    async def __aenter__(self) -> LessonServiceClient:
        self._session = aiohttp.ClientSession(self._base_url)
        return self

    async def __aexit__(self, *args: object) -> None:
        if self._session:
            await self._session.close()
            self._session = None

    async def _ensure_session(self) -> aiohttp.ClientSession:
        if self._session is None:
            raise ImmersingLinkerError(
                "Client not opened. Use 'async with LessonServiceClient(...) as client:'"
            )
        return self._session

    async def _get(
        self, path: str, *, nullable: bool = False
    ) -> str:
        session = await self._ensure_session()
        async with session.get(path) as response:
            if nullable and response.status == 404:
                return ""
            if response.status != 200:
                text = await response.text()
                raise ImmersingLinkerError(f"HTTP {response.status}: {text}")
            return await response.text()

    # region Current

    async def get_current_subject(self) -> Subject | None:
        text = await self._get("/lesson/current/subject", nullable=True)
        return Subject.from_dict(json.loads(text)) if text else None

    async def get_next_class_subject(self) -> Subject:
        text = await self._get("/lesson/current/next-class-subject")
        return Subject.from_dict(json.loads(text))

    async def get_current_state(self) -> TimeState:
        return TimeState(int(json.loads(await self._get("/lesson/current/state"))))

    async def get_current_time_layout_item(self) -> TimeLayoutItem:
        text = await self._get("/lesson/current/time-layout-item")
        return TimeLayoutItem.from_dict(json.loads(text))

    async def get_current_class_plan(self) -> ClassPlan | None:
        text = await self._get("/lesson/current/class-plan", nullable=True)
        return ClassPlan.from_dict(json.loads(text)) if text else None

    async def get_current_selected_index(self) -> int:
        return int(await self._get("/lesson/current/selected-index"))

    async def get_is_class_plan_enabled(self) -> bool:
        raw = await self._get("/lesson/current/is-class-plan-enabled")
        value = json.loads(raw)
        if isinstance(value, bool):
            return value
        return str(value).strip().lower() == "true"

    async def get_is_class_plan_loaded(self) -> bool:
        raw = await self._get("/lesson/current/is-class-plan-loaded")
        value = json.loads(raw)
        if isinstance(value, bool):
            return value
        return str(value).strip().lower() == "true"

    async def get_is_lesson_confirmed(self) -> bool:
        raw = await self._get("/lesson/current/is-lesson-confirmed")
        value = json.loads(raw)
        if isinstance(value, bool):
            return value
        return str(value).strip().lower() == "true"

    # endregion

    # region Next

    async def get_next_class_time_layout_item(self) -> TimeLayoutItem:
        text = await self._get("/lesson/next/class-time-layout-item")
        return TimeLayoutItem.from_dict(json.loads(text))

    async def get_next_breaking_time_layout_item(self) -> TimeLayoutItem:
        text = await self._get("/lesson/next/breaking-time-layout-item")
        return TimeLayoutItem.from_dict(json.loads(text))

    # endregion

    # region Previous

    async def get_previous_class_subject(self) -> Subject | None:
        text = await self._get("/lesson/previous/class-subject", nullable=True)
        return Subject.from_dict(json.loads(text)) if text else None

    async def get_previous_class_time_layout_item(self) -> TimeLayoutItem:
        text = await self._get("/lesson/previous/class-time-layout-item")
        return TimeLayoutItem.from_dict(json.loads(text))

    async def get_previous_breaking_time_layout_item(self) -> TimeLayoutItem:
        text = await self._get("/lesson/previous/breaking-time-layout-item")
        return TimeLayoutItem.from_dict(json.loads(text))

    # endregion

    # region Timer

    async def get_on_class_left_time(self) -> timedelta:
        return _parse_timedelta(await self._get("/lesson/timer/on-class-left"))

    async def get_on_breaking_left_time(self) -> timedelta:
        return _parse_timedelta(await self._get("/lesson/timer/on-breaking-left"))

    async def get_elapsed_since_previous_class(self) -> timedelta:
        return _parse_timedelta(await self._get("/lesson/timer/elapsed-since-previous-class"))

    async def get_elapsed_since_previous_breaking(self) -> timedelta:
        return _parse_timedelta(await self._get("/lesson/timer/elapsed-since-previous-breaking"))

    async def get_elapsed_since_previous_any(self) -> timedelta:
        return _parse_timedelta(await self._get("/lesson/timer/elapsed-since-previous-any"))

    # endregion

    # region Profile

    async def get_current_profile_path(self) -> str:
        raw = await self._get("/lesson/profile/current-profile-path")
        return json.loads(raw)

    async def get_is_current_profile_trusted(self) -> bool:
        raw = await self._get("/lesson/profile/is-trusted")
        value = json.loads(raw)
        if isinstance(value, bool):
            return value
        return str(value).strip().lower() == "true"

    async def get_profile(self) -> Profile:
        text = await self._get("/lesson/profile")
        return Profile.from_dict(json.loads(text))

    # endregion

    async def close(self) -> None:
        if self._session:
            await self._session.close()
            self._session = None
