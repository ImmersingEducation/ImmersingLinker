from __future__ import annotations

import aiohttp

from .base import ImmersingLinkerError


class AppServiceClient:
    def __init__(self, port: str) -> None:
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
        try:
            session = await self._ensure_session()
            async with session.get("/app/hello") as response:
                return response.status == 200
        except Exception:
            return False

    async def close(self) -> None:
        if self._session:
            await self._session.close()
            self._session = None
