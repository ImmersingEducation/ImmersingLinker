import { ServiceClientBase, NotFoundError, ConflictError } from './base.js';
import type {
  Class,
  ClassInfo,
  Student,
  ClassExtraProperty,
  StudentExtraProperty,
  CreateClassRequest,
  UpdateClassRequest,
  CreateStudentRequest,
  UpdateStudentRequest,
  CreateExtraPropertyRequest,
  UpdateExtraPropertyRequest,
} from '../types/class.js';
import type { Application, Gender } from '../types/common.js';
import { v4 as uuidv4 } from 'uuid';

export class ClassServiceClient extends ServiceClientBase {
  // #region GET

  async getAllClasses(): Promise<Class[]> {
    return this._get<Class[]>('/class');
  }

  async getAllClassInfos(): Promise<ClassInfo[]> {
    return this._get<ClassInfo[]>('/class/infos');
  }

  async getClassByGuid(classGuid: string): Promise<Class | null> {
    return this._getOrNull<Class>(`/class/${classGuid}`);
  }

  async getStudentsByClassGuid(classGuid: string): Promise<Student[]> {
    return this._getOrEmpty<Student>(`/class/${classGuid}/student`);
  }

  async getStudentByStudentIdInClass(
    classGuid: string,
    studentId: number,
  ): Promise<Student | null> {
    return this._getOrNull<Student>(
      `/class/${classGuid}/student/${studentId}`,
    );
  }

  async getExtraPropertiesByStudentIdInClass(
    classGuid: string,
    studentId: number,
  ): Promise<StudentExtraProperty[]> {
    return this._getOrEmpty<StudentExtraProperty>(
      `/class/${classGuid}/student/${studentId}/extraProps`,
    );
  }

  async getExtraPropertiesByStudentIdAndAppIdInClass(
    classGuid: string,
    studentId: number,
    appId: string,
  ): Promise<StudentExtraProperty[]> {
    return this._getOrEmpty<StudentExtraProperty>(
      `/class/${classGuid}/student/${studentId}/extraProps/${appId}`,
    );
  }

  async getExtraPropertyByNameAndStudentIdInClass(
    classGuid: string,
    studentId: number,
    appId: string,
    propName: string,
  ): Promise<StudentExtraProperty | null> {
    return this._getOrNull<StudentExtraProperty>(
      `/class/${classGuid}/student/${studentId}/extraProps/${appId}/${propName}`,
    );
  }

  async getExtraPropertiesByClassGuid(
    classGuid: string,
  ): Promise<ClassExtraProperty[]> {
    return this._getOrEmpty<ClassExtraProperty>(
      `/class/${classGuid}/extraProps`,
    );
  }

  async getExtraPropertiesByAppIdInClass(
    classGuid: string,
    appId: string,
  ): Promise<ClassExtraProperty[]> {
    return this._getOrEmpty<ClassExtraProperty>(
      `/class/${classGuid}/extraProps/${appId}`,
    );
  }

  async getExtraPropertyByAppIdAndNameInClass(
    classGuid: string,
    appId: string,
    propName: string,
  ): Promise<ClassExtraProperty | null> {
    return this._getOrNull<ClassExtraProperty>(
      `/class/${classGuid}/extraProps/${appId}/${propName}`,
    );
  }

  // #endregion

  // #region POST

  async createClass(request: CreateClassRequest): Promise<Class> {
    return this._post<Class>('/class', request);
  }

  async addStudent(
    classGuid: string,
    request: CreateStudentRequest,
  ): Promise<Student> {
    return this._post<Student>(`/class/${classGuid}/student`, request);
  }

  async addClassExtraProperty(
    classGuid: string,
    request: CreateExtraPropertyRequest,
  ): Promise<ClassExtraProperty> {
    return this._post<ClassExtraProperty>(
      `/class/${classGuid}/extraProps`,
      request,
    );
  }

  async addStudentExtraProperty(
    classGuid: string,
    studentId: number,
    request: CreateExtraPropertyRequest,
  ): Promise<StudentExtraProperty> {
    return this._post<StudentExtraProperty>(
      `/class/${classGuid}/student/${studentId}/extraProps`,
      request,
    );
  }

  // #endregion

  // #region PUT

  async updateClass(
    classGuid: string,
    request: UpdateClassRequest,
  ): Promise<Class> {
    return this._put<Class>(`/class/${classGuid}`, request);
  }

  async updateStudent(
    classGuid: string,
    studentId: number,
    request: UpdateStudentRequest,
  ): Promise<Student> {
    return this._put<Student>(
      `/class/${classGuid}/student/${studentId}`,
      request,
    );
  }

  async updateClassExtraProperty(
    classGuid: string,
    appId: string,
    propName: string,
    request: UpdateExtraPropertyRequest,
  ): Promise<ClassExtraProperty> {
    return this._put<ClassExtraProperty>(
      `/class/${classGuid}/extraProps/${appId}/${propName}`,
      request,
    );
  }

  async updateStudentExtraProperty(
    classGuid: string,
    studentId: number,
    appId: string,
    propName: string,
    request: UpdateExtraPropertyRequest,
  ): Promise<StudentExtraProperty> {
    return this._put<StudentExtraProperty>(
      `/class/${classGuid}/student/${studentId}/extraProps/${appId}/${propName}`,
      request,
    );
  }

  // #endregion

  // #region DELETE

  async deleteClass(classGuid: string): Promise<void> {
    return this._delete(`/class/${classGuid}`);
  }

  async deleteStudent(classGuid: string, studentId: number): Promise<void> {
    return this._delete(`/class/${classGuid}/student/${studentId}`);
  }

  async deleteClassExtraProperty(
    classGuid: string,
    appId: string,
    propName: string,
  ): Promise<void> {
    return this._delete(
      `/class/${classGuid}/extraProps/${appId}/${propName}`,
    );
  }

  async deleteStudentExtraProperty(
    classGuid: string,
    studentId: number,
    appId: string,
    propName: string,
  ): Promise<void> {
    return this._delete(
      `/class/${classGuid}/student/${studentId}/extraProps/${appId}/${propName}`,
    );
  }

  // #endregion

  // #region Offline factory methods

  static createClassOffline(name: string): Class {
    return {
      guid: uuidv4(),
      name,
      students: [],
      groupingRules: [],
      extraProperties: [],
    };
  }

  static createStudentOffline(
    name: string,
    studentIdInClass: number,
    gender: Gender,
  ): Student {
    return {
      guid: uuidv4(),
      name,
      studentIdInClass,
      gender,
      extraProperties: [],
    };
  }

  static createClassExtraPropertyOffline(
    appId: string,
    name: string,
    value: unknown,
  ): ClassExtraProperty {
    return {
      application: { uniqueId: appId, name: '' },
      name,
      value,
    };
  }

  static createStudentExtraPropertyOffline(
    appId: string,
    name: string,
    value: unknown,
  ): StudentExtraProperty {
    return {
      application: { uniqueId: appId, name: '' },
      name,
      value,
    };
  }

  // #endregion
}
