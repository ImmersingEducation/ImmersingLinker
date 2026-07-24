"""应用服务客户端，提供连接测试功能。"""

from __future__ import annotations

import aiohttp

from .base import ImmersingLinkerError


class AppServiceClient:
    """应用服务客户端，用于测试与 ImmersingLinker 服务的连接。

    用法::

        async with AppServiceClient(port) as client:
            ok = await client.test_connection()
    """

    def __init__(self, port: str) -> None:
        """初始化客户端。

        Args:
            port: ImmersingLinker 服务端口号。
        """
        self._base_url = f"http://localhost:{port}"
        self._session: aiohttp.ClientSession | None = None

    async def __aenter__(self) -> AppServiceClient:
        self._session = aiohttp.ClientSession(self._base_url)
        return self

    async def __aexit__(self, *args: object) -> None:
        if self._session:
            await self._session.close()
            self._session = None

    async def _ensure_session(self) -> aiohttp.ClientSession:
        if self._session is None:
            raise ImmersingLinkerError(
                "Client not opened. Use 'async with AppServiceClient(...) as client:'"
            )
        return self._session

    async def test_connection(self) -> bool:
        """测试与 ImmersingLinker 服务的连接是否正常。

        Returns:
            连接成功返回 ``True``，失败返回 ``False``。
        """
        try:
            session = await self._ensure_session()
            async with session.get("/app/hello") as response:
                return response.status == 200
        except Exception:
            return False

    async def close(self) -> None:
        """关闭客户端，释放 HTTP 会话资源。"""
        if self._session:
            await self._session.close()
            self._session = None
