from __future__ import annotations

import json
from uuid import uuid4

import aiohttp

from ..enums import Gender
from ..types import (
    Class,
    ClassExtraProperty,
    ClassInfo,
    Student,
    StudentExtraProperty,
    Application,
    CreateClassRequest,
    UpdateClassRequest,
    CreateStudentRequest,
    UpdateStudentRequest,
    CreateExtraPropertyRequest,
    UpdateExtraPropertyRequest,
)
from .base import ImmersingLinkerError


class ClassServiceClient:
    def __init__(self, port: str) -> None:
        self._base_url = f"http://localhost:{port}"
        self._session: aiohttp.ClientSession | None = None

    async def __aenter__(self) -> ClassServiceClient:
        self._session = aiohttp.ClientSession(self._base_url)
        return self

    async def __aexit__(self, *args: object) -> None:
        if self._session:
            await self._session.close()
            self._session = None

    async def _ensure_session(self) -> aiohttp.ClientSession:
        if self._session is None:
            raise ImmersingLinkerError(
                "Client not opened. Use 'async with ClassServiceClient(...) as client:'"
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
            if response.status == 409:
                raise ImmersingLinkerError(f"Conflict: {text}")
            if not 200 <= response.status < 300:
                raise ImmersingLinkerError(f"HTTP {response.status}: {text}")
            return text

    @staticmethod
    def _serialize(obj: object) -> object:
        if hasattr(obj, "__dataclass_fields__"):
            from dataclasses import asdict

            return asdict(obj)
        if isinstance(obj, Gender):
            return obj.value
        return obj

    # region GET

    async def get_all_classes(self) -> list[Class]:
        text = await self._request("GET", "/class")
        data = json.loads(text) if text else []
        return [Class.from_dict(item) for item in data]

    async def get_all_class_infos(self) -> list[ClassInfo]:
        text = await self._request("GET", "/class/infos")
        data = json.loads(text) if text else []
        return [ClassInfo.from_dict(item) for item in data]

    async def get_class_by_guid(self, class_guid: str) -> Class | None:
        text = await self._request("GET", f"/class/{class_guid}")
        return Class.from_dict(json.loads(text)) if text else None

    async def get_students_by_class_guid(self, class_guid: str) -> list[Student]:
        text = await self._request("GET", f"/class/{class_guid}/student")
        data = json.loads(text) if text else []
        return [Student.from_dict(item) for item in data]

    async def get_student_by_student_id_in_class(
        self, class_guid: str, student_id: int
    ) -> Student | None:
        text = await self._request("GET", f"/class/{class_guid}/student/{student_id}")
        return Student.from_dict(json.loads(text)) if text else None

    async def get_extra_properties_by_student_id_in_class(
        self, class_guid: str, student_id: int
    ) -> list[StudentExtraProperty]:
        text = await self._request("GET", f"/class/{class_guid}/student/{student_id}/extraProps")
        data = json.loads(text) if text else []
        return [StudentExtraProperty.from_dict(item) for item in data]

    async def get_extra_properties_by_student_id_and_app_id_in_class(
        self, class_guid: str, student_id: int, app_id: str
    ) -> list[StudentExtraProperty]:
        text = await self._request(
            "GET", f"/class/{class_guid}/student/{student_id}/extraProps/{app_id}"
        )
        data = json.loads(text) if text else []
        return [StudentExtraProperty.from_dict(item) for item in data]

    async def get_extra_property_by_name_and_student_id_in_class(
        self, class_guid: str, student_id: int, app_id: str, prop_name: str
    ) -> StudentExtraProperty | None:
        text = await self._request(
            "GET",
            f"/class/{class_guid}/student/{student_id}/extraProps/{app_id}/{prop_name}",
        )
        return StudentExtraProperty.from_dict(json.loads(text)) if text else None

    async def get_extra_properties_by_class_guid(
        self, class_guid: str
    ) -> list[ClassExtraProperty]:
        text = await self._request("GET", f"/class/{class_guid}/extraProps")
        data = json.loads(text) if text else []
        return [ClassExtraProperty.from_dict(item) for item in data]

    async def get_extra_properties_by_app_id_in_class(
        self, class_guid: str, app_id: str
    ) -> list[ClassExtraProperty]:
        text = await self._request("GET", f"/class/{class_guid}/extraProps/{app_id}")
        data = json.loads(text) if text else []
        return [ClassExtraProperty.from_dict(item) for item in data]

    async def get_extra_property_by_app_id_and_name_in_class(
        self, class_guid: str, app_id: str, prop_name: str
    ) -> ClassExtraProperty | None:
        text = await self._request(
            "GET", f"/class/{class_guid}/extraProps/{app_id}/{prop_name}"
        )
        return ClassExtraProperty.from_dict(json.loads(text)) if text else None

    # endregion

    # region POST

    async def create_class(self, request: CreateClassRequest) -> Class:
        text = await self._request("POST", "/class", body=request)
        return Class.from_dict(json.loads(text))

    async def add_student(
        self, class_guid: str, request: CreateStudentRequest
    ) -> Student:
        text = await self._request(
            "POST", f"/class/{class_guid}/student", body=request, raise_on_404=True
        )
        return Student.from_dict(json.loads(text))

    async def add_class_extra_property(
        self, class_guid: str, request: CreateExtraPropertyRequest
    ) -> ClassExtraProperty:
        text = await self._request(
            "POST", f"/class/{class_guid}/extraProps", body=request, raise_on_404=True
        )
        return ClassExtraProperty.from_dict(json.loads(text))

    async def add_student_extra_property(
        self, class_guid: str, student_id: int, request: CreateExtraPropertyRequest
    ) -> StudentExtraProperty:
        text = await self._request(
            "POST",
            f"/class/{class_guid}/student/{student_id}/extraProps",
            body=request,
            raise_on_404=True,
        )
        return StudentExtraProperty.from_dict(json.loads(text))

    # endregion

    # region PUT

    async def update_class(
        self, class_guid: str, request: UpdateClassRequest
    ) -> Class:
        text = await self._request("PUT", f"/class/{class_guid}", body=request, raise_on_404=True)
        return Class.from_dict(json.loads(text))

    async def update_student(
        self, class_guid: str, student_id: int, request: UpdateStudentRequest
    ) -> Student:
        text = await self._request(
            "PUT", f"/class/{class_guid}/student/{student_id}", body=request, raise_on_404=True
        )
        return Student.from_dict(json.loads(text))

    async def update_class_extra_property(
        self,
        class_guid: str,
        app_id: str,
        prop_name: str,
        request: UpdateExtraPropertyRequest,
    ) -> ClassExtraProperty:
        text = await self._request(
            "PUT",
            f"/class/{class_guid}/extraProps/{app_id}/{prop_name}",
            body=request,
            raise_on_404=True,
        )
        return ClassExtraProperty.from_dict(json.loads(text))

    async def update_student_extra_property(
        self,
        class_guid: str,
        student_id: int,
        app_id: str,
        prop_name: str,
        request: UpdateExtraPropertyRequest,
    ) -> StudentExtraProperty:
        text = await self._request(
            "PUT",
            f"/class/{class_guid}/student/{student_id}/extraProps/{app_id}/{prop_name}",
            body=request,
            raise_on_404=True,
        )
        return StudentExtraProperty.from_dict(json.loads(text))

    # endregion

    # region DELETE

    async def delete_class(self, class_guid: str) -> None:
        await self._request("DELETE", f"/class/{class_guid}")

    async def delete_student(self, class_guid: str, student_id: int) -> None:
        await self._request(
            "DELETE", f"/class/{class_guid}/student/{student_id}", raise_on_404=True
        )

    async def delete_class_extra_property(
        self, class_guid: str, app_id: str, prop_name: str
    ) -> None:
        await self._request(
            "DELETE",
            f"/class/{class_guid}/extraProps/{app_id}/{prop_name}",
            raise_on_404=True,
        )

    async def delete_student_extra_property(
        self, class_guid: str, student_id: int, app_id: str, prop_name: str
    ) -> None:
        await self._request(
            "DELETE",
            f"/class/{class_guid}/student/{student_id}/extraProps/{app_id}/{prop_name}",
            raise_on_404=True,
        )

    # endregion

    # region Offline factories

    @staticmethod
    def create_class_offline(name: str) -> Class:
        return Class(Guid=uuid4(), Name=name)

    @staticmethod
    def create_student_offline(
        name: str, student_id_in_class: int, gender: Gender
    ) -> Student:
        return Student(
            Guid=uuid4(), Name=name, StudentIdInClass=student_id_in_class, Gender=gender
        )

    @staticmethod
    def create_class_extra_property_offline(
        app_id: str, name: str, value: object = None
    ) -> ClassExtraProperty:
        return ClassExtraProperty(
            Application=Application(UniqueId=app_id, Name=""),
            Name=name,
            Value=value,
        )

    @staticmethod
    def create_student_extra_property_offline(
        app_id: str, name: str, value: object = None
    ) -> StudentExtraProperty:
        return StudentExtraProperty(
            Application=Application(UniqueId=app_id, Name=""),
            Name=name,
            Value=value,
        )

    # endregion

    async def close(self) -> None:
        if self._session:
            await self._session.close()
            self._session = None
