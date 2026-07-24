"""课程服务客户端，提供课表查询、时间管理和档案管理功能。"""

from __future__ import annotations

import json
from datetime import timedelta

import aiohttp

from ..enums import TimeState
from ..types import Subject, TimeLayoutItem, ClassPlan, Profile
from .base import ImmersingLinkerError, _parse_timedelta


class LessonServiceClient:
    """课程服务客户端，用于查询当前课程状态、时间布局和档案信息。

    用法::

        async with LessonServiceClient(port) as client:
            subject = await client.get_current_subject()
            state = await client.get_current_state()
    """

    def __init__(self, port: str) -> None:
        """初始化客户端。

        Args:
            port: ImmersingLinker 服务端口号。
        """
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
        """获取当前正在上课的科目信息。

        Returns:
            当前科目信息，无课时返回 ``None``。
        """
        text = await self._get("/lesson/current/subject", nullable=True)
        return Subject.from_dict(json.loads(text)) if text else None

    async def get_next_class_subject(self) -> Subject:
        """获取下节课的科目信息。

        Returns:
            下节课科目信息。
        """
        text = await self._get("/lesson/current/next-class-subject")
        return Subject.from_dict(json.loads(text))

    async def get_current_state(self) -> TimeState:
        """获取当前时间状态。

        Returns:
            当前时间状态枚举值。
        """
        return TimeState(int(json.loads(await self._get("/lesson/current/state"))))

    async def get_current_time_layout_item(self) -> TimeLayoutItem:
        """获取当前时间布局项。

        Returns:
            当前时间布局信息。
        """
        text = await self._get("/lesson/current/time-layout-item")
        return TimeLayoutItem.from_dict(json.loads(text))

    async def get_current_class_plan(self) -> ClassPlan | None:
        """获取当前课程表。

        Returns:
            当前课程表，未加载时返回 ``None``。
        """
        text = await self._get("/lesson/current/class-plan", nullable=True)
        return ClassPlan.from_dict(json.loads(text)) if text else None

    async def get_current_selected_index(self) -> int:
        """获取当前选中的课程索引。

        Returns:
            课程索引（从 0 开始）。
        """
        return int(await self._get("/lesson/current/selected-index"))

    async def get_is_class_plan_enabled(self) -> bool:
        """获取课程表功能是否启用。

        Returns:
            启用返回 ``True``。
        """
        raw = await self._get("/lesson/current/is-class-plan-enabled")
        value = json.loads(raw)
        if isinstance(value, bool):
            return value
        return str(value).strip().lower() == "true"

    async def get_is_class_plan_loaded(self) -> bool:
        """获取课程表是否已加载。

        Returns:
            已加载返回 ``True``。
        """
        raw = await self._get("/lesson/current/is-class-plan-loaded")
        value = json.loads(raw)
        if isinstance(value, bool):
            return value
        return str(value).strip().lower() == "true"

    async def get_is_lesson_confirmed(self) -> bool:
        """获取当前课程是否已确认。

        Returns:
            已确认返回 ``True``。
        """
        raw = await self._get("/lesson/current/is-lesson-confirmed")
        value = json.loads(raw)
        if isinstance(value, bool):
            return value
        return str(value).strip().lower() == "true"

    # endregion

    # region Next

    async def get_next_class_time_layout_item(self) -> TimeLayoutItem:
        """获取下节课的时间布局项。"""
        text = await self._get("/lesson/next/class-time-layout-item")
        return TimeLayoutItem.from_dict(json.loads(text))

    async def get_next_breaking_time_layout_item(self) -> TimeLayoutItem:
        """获取下个课间休息的时间布局项。"""
        text = await self._get("/lesson/next/breaking-time-layout-item")
        return TimeLayoutItem.from_dict(json.loads(text))

    # endregion

    # region Previous

    async def get_previous_class_subject(self) -> Subject | None:
        """获取上节课的科目信息。

        Returns:
            上节课科目信息，无上节课时返回 ``None``。
        """
        text = await self._get("/lesson/previous/class-subject", nullable=True)
        return Subject.from_dict(json.loads(text)) if text else None

    async def get_previous_class_time_layout_item(self) -> TimeLayoutItem:
        """获取上节课的时间布局项。"""
        text = await self._get("/lesson/previous/class-time-layout-item")
        return TimeLayoutItem.from_dict(json.loads(text))

    async def get_previous_breaking_time_layout_item(self) -> TimeLayoutItem:
        """获取上个课间休息的时间布局项。"""
        text = await self._get("/lesson/previous/breaking-time-layout-item")
        return TimeLayoutItem.from_dict(json.loads(text))

    # endregion

    # region Timer

    async def get_on_class_left_time(self) -> timedelta:
        """获取当前上课剩余时间。"""
        return _parse_timedelta(await self._get("/lesson/timer/on-class-left"))

    async def get_on_breaking_left_time(self) -> timedelta:
        """获取当前课间休息剩余时间。"""
        return _parse_timedelta(await self._get("/lesson/timer/on-breaking-left"))

    async def get_elapsed_since_previous_class(self) -> timedelta:
        """获取自上节课开始以来经过的时间。"""
        return _parse_timedelta(await self._get("/lesson/timer/elapsed-since-previous-class"))

    async def get_elapsed_since_previous_breaking(self) -> timedelta:
        """获取自上个课间休息开始以来经过的时间。"""
        return _parse_timedelta(await self._get("/lesson/timer/elapsed-since-previous-breaking"))

    async def get_elapsed_since_previous_any(self) -> timedelta:
        """获取自上一个时间段开始以来经过的时间。"""
        return _parse_timedelta(await self._get("/lesson/timer/elapsed-since-previous-any"))

    # endregion

    # region Profile

    async def get_current_profile_path(self) -> str:
        """获取当前档案文件路径。"""
        raw = await self._get("/lesson/profile/current-profile-path")
        return json.loads(raw)

    async def get_is_current_profile_trusted(self) -> bool:
        """获取当前档案是否受信任。"""
        raw = await self._get("/lesson/profile/is-trusted")
        value = json.loads(raw)
        if isinstance(value, bool):
            return value
        return str(value).strip().lower() == "true"

    async def get_profile(self) -> Profile:
        """获取当前档案信息。"""
        text = await self._get("/lesson/profile")
        return Profile.from_dict(json.loads(text))

    # endregion

    async def close(self) -> None:
        """关闭客户端，释放 HTTP 会话资源。"""
        if self._session:
            await self._session.close()
            self._session = None
