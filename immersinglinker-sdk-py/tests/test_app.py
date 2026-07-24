from __future__ import annotations

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from immersinglinker.clients.app import AppServiceClient
from immersinglinker.clients.base import ImmersingLinkerError
from tests.conftest import _ctx, _resp


async def test_test_connection_success():
    client = AppServiceClient("8080")
    session = MagicMock()
    session.get = MagicMock(return_value=_ctx(_resp(200)))
    session.close = AsyncMock()
    client._session = session
    assert await client.test_connection() is True


async def test_test_connection_failure_status():
    client = AppServiceClient("8080")
    session = MagicMock()
    session.get = MagicMock(return_value=_ctx(_resp(500)))
    session.close = AsyncMock()
    client._session = session
    assert await client.test_connection() is False


async def test_test_connection_exception():
    client = AppServiceClient("8080")
    session = AsyncMock()
    session.get = AsyncMock(side_effect=ConnectionError("refused"))
    session.close = AsyncMock()
    client._session = session
    assert await client.test_connection() is False


async def test_ensure_session_raises_without_session():
    client = AppServiceClient("8080")
    result = await client.test_connection()
    assert result is False


async def test_context_manager():
    client = AppServiceClient("8080")
    mock_session = AsyncMock()
    mock_session.close = AsyncMock()
    with patch("immersinglinker.clients.app.aiohttp.ClientSession", return_value=mock_session):
        async with client as c:
            assert c._session is mock_session
        mock_session.close.assert_awaited_once()


async def test_close():
    client = AppServiceClient("8080")
    session = AsyncMock()
    session.close = AsyncMock()
    client._session = session
    await client.close()
    assert client._session is None
