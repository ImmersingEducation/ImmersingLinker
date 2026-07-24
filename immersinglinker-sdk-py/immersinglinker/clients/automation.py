"""自动化服务客户端，提供自动化计划的 CRUD 和离线工厂方法。"""

from __future__ import annotations

import json
from uuid import uuid4

import aiohttp

from ..enums import RuleSetSatisfyMode
from ..types import (
    AutomationPlan,
    AutomationPlanInfo,
    Trigger,
    Action,
    RuleSet,
    RuleBase,
    TriggerDto,
    ActionDto,
    RuleNodeDto,
    RuleSetDto,
    CreateAutomationPlanRequest,
    UpdateAutomationPlanRequest,
)
from .base import ImmersingLinkerError


class AutomationServiceClient:
    """自动化服务客户端，用于管理自动化计划。

    用法::

        async with AutomationServiceClient(port) as client:
            plans = await client.get_all_plan_infos()
            plan = await client.get_plan_by_guid(guid)
    """
    def __init__(self, port: str) -> None:
        """初始化客户端。

        Args:
            port: ImmersingLinker 服务端口号。
        """
        self._base_url = f"http://localhost:{port}"
        self._session: aiohttp.ClientSession | None = None

    async def __aenter__(self) -> AutomationServiceClient:
        self._session = aiohttp.ClientSession(self._base_url)
        return self

    async def __aexit__(self, *args: object) -> None:
        if self._session:
            await self._session.close()
            self._session = None

    async def _ensure_session(self) -> aiohttp.ClientSession:
        if self._session is None:
            raise ImmersingLinkerError(
                "Client not opened. Use 'async with AutomationServiceClient(...) as client:'"
            )
        return self._session

    async def _request(
        self, method: str, path: str, body: object = None, *, raise_on_404: bool = False
    ) -> str:
        session = await self._ensure_session()
        kwargs: dict = {}
        if body is not None:
            kwargs["json"] = self._serialize(body)
        async with session.request(method, path, **kwargs) as response:
            text = await response.text()
            if response.status == 404:
                if raise_on_404:
                    raise ImmersingLinkerError(f"Not found: {text}")
                return ""
            if response.status == 400:
                raise ImmersingLinkerError(f"Bad request: {text}")
            if not 200 <= response.status < 300:
                raise ImmersingLinkerError(f"HTTP {response.status}: {text}")
            return text

    @staticmethod
    def _serialize(obj: object) -> object:
        if obj is None:
            return None
        if isinstance(obj, (str, int, float, bool)):
            return obj
        if isinstance(obj, dict):
            return {k: AutomationServiceClient._serialize(v) for k, v in obj.items()}
        if isinstance(obj, (list, tuple)):
            return [AutomationServiceClient._serialize(v) for v in obj]
        if hasattr(obj, "to_dict"):
            return obj.to_dict()  # type: ignore[union-attr]
        if hasattr(obj, "__dataclass_fields__"):
            from dataclasses import asdict

            return asdict(obj)  # type: ignore[call-overload]
        return str(obj)

    # region GET

    async def get_all_plan_infos(self) -> list[AutomationPlanInfo]:
        """获取所有自动化计划的简要信息列表。"""
        text = await self._request("GET", "/automation")
        data = json.loads(text) if text else []
        return [AutomationPlanInfo.from_dict(item) for item in data]

    async def get_plan_by_guid(self, plan_guid: str) -> AutomationPlan | None:
        """根据 GUID 获取完整的自动化计划。

        Args:
            plan_guid: 自动化计划 GUID。

        Returns:
            自动化计划，不存在时返回 ``None``。
        """
        text = await self._request("GET", f"/automation/{plan_guid}")
        return AutomationPlan.from_dict(json.loads(text)) if text else None

    # endregion

    # region POST

    async def create_plan(self, request: CreateAutomationPlanRequest) -> AutomationPlan:
        """创建新的自动化计划。"""
        text = await self._request("POST", "/automation", body=request)
        return AutomationPlan.from_dict(json.loads(text))

    async def trigger_plan(self, plan_guid: str) -> None:
        """手动触发指定的自动化计划。"""
        await self._request("POST", f"/automation/{plan_guid}/trigger", raise_on_404=True)

    async def invoke_url_trigger(self, tag: str) -> None:
        """通过标签调用 URL 触发器。"""
        await self._request("POST", f"/automation/invoke/{tag}")

    # endregion

    # region PUT

    async def update_plan(
        self, plan_guid: str, request: UpdateAutomationPlanRequest
    ) -> AutomationPlan:
        """更新指定的自动化计划。"""
        text = await self._request("PUT", f"/automation/{plan_guid}", body=request, raise_on_404=True)
        return AutomationPlan.from_dict(json.loads(text))

    # endregion

    # region DELETE

    async def delete_plan(self, plan_guid: str) -> None:
        """删除指定的自动化计划。"""
        await self._request("DELETE", f"/automation/{plan_guid}", raise_on_404=True)

    # endregion

    # region Offline factories

    @staticmethod
    def create_automation_plan_offline(
        name: str,
        revertable: bool,
        trigger: Trigger,
        rule_set: RuleSet | None,
        actions: list[Action],
    ) -> AutomationPlan:
        """离线创建 AutomationPlan 实例（无需网络请求）。"""
        return AutomationPlan(
            Guid=uuid4(),
            Name=name,
            Revertable=revertable,
            Trigger=trigger,
            RuleSet=rule_set
            or RuleSet(SatisfyMode=RuleSetSatisfyMode.AllSatisfied, Not=False),
            Actions=actions,
        )

    @staticmethod
    def create_trigger_dto_offline(
        trigger_key: str, properties: dict | None = None
    ) -> TriggerDto:
        """离线创建 TriggerDto 实例。"""
        return TriggerDto(TriggerKey=trigger_key, Properties=properties)

    @staticmethod
    def create_rule_set_offline(
        satisfy_mode: RuleSetSatisfyMode, not_: bool, rules: list[RuleNodeDto]
    ) -> RuleSetDto:
        """离线创建 RuleSetDto 实例。"""
        return RuleSetDto(SatisfyMode=satisfy_mode, Not=not_, Rules=rules)

    @staticmethod
    def create_rule_node_offline(
        rule_key: str | None = None,
        properties: dict | None = None,
        not_: bool = False,
        rule_set: RuleSetDto | None = None,
    ) -> RuleNodeDto:
        """离线创建 RuleNodeDto 实例。"""
        return RuleNodeDto(
            RuleKey=rule_key, Properties=properties, Not=not_, RuleSet=rule_set
        )

    @staticmethod
    def create_action_dto_offline(
        action_key: str, properties: dict | None = None
    ) -> ActionDto:
        """离线创建 ActionDto 实例。"""
        return ActionDto(ActionKey=action_key, Properties=properties)

    @staticmethod
    def create_plan_request_offline(
        name: str,
        revertable: bool,
        trigger: TriggerDto,
        rule_set: RuleSetDto | None,
        actions: list[ActionDto],
    ) -> CreateAutomationPlanRequest:
        """离线创建 CreateAutomationPlanRequest 实例。"""
        return CreateAutomationPlanRequest(
            Name=name,
            Revertable=revertable,
            Trigger=trigger,
            RuleSet=rule_set,
            Actions=actions,
        )

    @staticmethod
    def update_plan_request_offline(
        name: str,
        revertable: bool,
        trigger: TriggerDto,
        rule_set: RuleSetDto | None,
        actions: list[ActionDto],
    ) -> UpdateAutomationPlanRequest:
        """离线创建 UpdateAutomationPlanRequest 实例。"""
        return UpdateAutomationPlanRequest(
            Name=name,
            Revertable=revertable,
            Trigger=trigger,
            RuleSet=rule_set,
            Actions=actions,
        )

    # endregion

    async def close(self) -> None:
        """关闭客户端，释放 HTTP 会话资源。"""
        if self._session:
            await self._session.close()
            self._session = None
