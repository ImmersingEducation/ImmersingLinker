from __future__ import annotations

import json
from typing import Any
from unittest.mock import AsyncMock, MagicMock

import aiohttp
import pytest


@pytest.fixture
def mock_session():
    session = AsyncMock(spec=aiohttp.ClientSession)
    session.request = AsyncMock()
    session.get = AsyncMock()
    return session


def _resp(status: int = 200, body: Any = None, text: str = "") -> AsyncMock:
    r = AsyncMock()
    r.status = status
    r.text = AsyncMock(return_value=json.dumps(body) if body is not None else text)
    return r


def _ctx(resp: AsyncMock) -> MagicMock:
    ctx = MagicMock()
    ctx.__aenter__ = AsyncMock(return_value=resp)
    ctx.__aexit__ = AsyncMock(return_value=False)
    return ctx


def make_response(status: int = 200, body: Any = None, text: str = "") -> MagicMock:
    return _ctx(_resp(status, body, text))


def json_body(data: Any) -> str:
    return json.dumps(data)
