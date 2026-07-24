import { ServiceClientBase, NotFoundError, ConflictError } from './base.js';
import type {
  Class,
  ClassInfo,
  Student,
  ClassExtraProperty,
  StudentExtraProperty,
  GroupingRuleResponse,
  CreateClassRequest,
  UpdateClassRequest,
  CreateStudentRequest,
  UpdateStudentRequest,
  CreateExtraPropertyRequest,
  UpdateExtraPropertyRequest,
  CreateGroupingRuleRequest,
  UpdateGroupingRuleRequest,
  CreateGroupRequest,
  UpdateGroupRequest,
} from '../types/class.js';
import type { Application, Gender } from '../types/common.js';
import { v4 as uuidv4 } from 'uuid';

/** 班级管理客户端，提供班级/学生/扩展属性/分组规则的 CRUD 操作 */
export class ClassServiceClient extends ServiceClientBase {
  // #region GET

  /** 获取所有班级 */
  async getAllClasses(): Promise<Class[]> {
    return this._get<Class[]>('/class');
  }

  /** 获取所有班级摘要信息 */
  async getAllClassInfos(): Promise<ClassInfo[]> {
    return this._get<ClassInfo[]>('/class/infos');
  }

  /** 根据 GUID 获取指定班级，不存在时返回 null */
  async getClassByGuid(classGuid: string): Promise<Class | null> {
    return this._getOrNull<Class>(`/class/${classGuid}`);
  }

  /** 获取指定班级内的所有学生 */
  async getStudentsByClassGuid(classGuid: string): Promise<Student[]> {
    return this._getOrEmpty<Student>(`/class/${classGuid}/student`);
  }

  /** 根据学号获取指定班级内的指定学生，不存在时返回 null */
  async getStudentByStudentIdInClass(
    classGuid: string,
    studentId: number,
  ): Promise<Student | null> {
    return this._getOrNull<Student>(
      `/class/${classGuid}/student/${studentId}`,
    );
  }

  /** 获取指定班级内指定学生的所有扩展属性 */
  async getExtraPropertiesByStudentIdInClass(
    classGuid: string,
    studentId: number,
  ): Promise<StudentExtraProperty[]> {
    return this._getOrEmpty<StudentExtraProperty>(
      `/class/${classGuid}/student/${studentId}/extraProps`,
    );
  }

  /** 获取指定班级内指定学生、指定应用的扩展属性列表 */
  async getExtraPropertiesByStudentIdAndAppIdInClass(
    classGuid: string,
    studentId: number,
    appId: string,
  ): Promise<StudentExtraProperty[]> {
    return this._getOrEmpty<StudentExtraProperty>(
      `/class/${classGuid}/student/${studentId}/extraProps/${appId}`,
    );
  }

  /** 根据应用 ID 和属性名获取指定学生扩展属性，不存在时返回 null */
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

  /** 获取指定班级的所有扩展属性 */
  async getExtraPropertiesByClassGuid(
    classGuid: string,
  ): Promise<ClassExtraProperty[]> {
    return this._getOrEmpty<ClassExtraProperty>(
      `/class/${classGuid}/extraProps`,
    );
  }

  /** 获取指定班级内指定应用的扩展属性列表 */
  async getExtraPropertiesByAppIdInClass(
    classGuid: string,
    appId: string,
  ): Promise<ClassExtraProperty[]> {
    return this._getOrEmpty<ClassExtraProperty>(
      `/class/${classGuid}/extraProps/${appId}`,
    );
  }

  /** 根据应用 ID 和属性名获取班级扩展属性，不存在时返回 null */
  async getExtraPropertyByAppIdAndNameInClass(
    classGuid: string,
    appId: string,
    propName: string,
  ): Promise<ClassExtraProperty | null> {
    return this._getOrNull<ClassExtraProperty>(
      `/class/${classGuid}/extraProps/${appId}/${propName}`,
    );
  }

  /** 获取指定班级的所有分组规则 */
  async getGroupingRules(
    classGuid: string,
  ): Promise<GroupingRuleResponse[]> {
    return this._getOrEmpty<GroupingRuleResponse>(
      `/class/${classGuid}/groupingRule`,
    );
  }

  /** 获取指定班级的指定分组规则，不存在时返回 null */
  async getGroupingRule(
    classGuid: string,
    ruleGuid: string,
  ): Promise<GroupingRuleResponse | null> {
    return this._getOrNull<GroupingRuleResponse>(
      `/class/${classGuid}/groupingRule/${ruleGuid}`,
    );
  }

  // #endregion

  // #region POST

  /** 创建班级 */
  async createClass(request: CreateClassRequest): Promise<Class> {
    return this._post<Class>('/class', request);
  }

  /** 向指定班级添加学生 */
  async addStudent(
    classGuid: string,
    request: CreateStudentRequest,
  ): Promise<Student> {
    return this._post<Student>(`/class/${classGuid}/student`, request);
  }

  /** 向指定班级添加扩展属性 */
  async addClassExtraProperty(
    classGuid: string,
    request: CreateExtraPropertyRequest,
  ): Promise<ClassExtraProperty> {
    return this._post<ClassExtraProperty>(
      `/class/${classGuid}/extraProps`,
      request,
    );
  }

  /** 向指定班级的指定学生添加扩展属性 */
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

  /** 向指定班级添加分组规则 */
  async addGroupingRule(
    classGuid: string,
    request: CreateGroupingRuleRequest,
  ): Promise<GroupingRuleResponse> {
    return this._post<GroupingRuleResponse>(
      `/class/${classGuid}/groupingRules`,
      request,
    );
  }

  /** 向指定班级的指定分组规则中添加小组 */
  async addGroup(
    classGuid: string,
    ruleGuid: string,
    request: CreateGroupRequest,
  ): Promise<GroupingRuleResponse> {
    return this._post<GroupingRuleResponse>(
      `/class/${classGuid}/groupingRules/${ruleGuid}`,
      request,
    );
  }

  // #endregion

  // #region PUT

  /** 更新班级名称 */
  async updateClass(
    classGuid: string,
    request: UpdateClassRequest,
  ): Promise<Class> {
    return this._put<Class>(`/class/${classGuid}`, request);
  }

  /** 更新指定班级内的学生信息 */
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

  /** 更新指定班级扩展属性值 */
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

  /** 更新指定学生扩展属性值 */
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

  /** 更新分组规则名称 */
  async updateGroupingRule(
    classGuid: string,
    ruleGuid: string,
    request: UpdateGroupingRuleRequest,
  ): Promise<GroupingRuleResponse> {
    return this._put<GroupingRuleResponse>(
      `/class/${classGuid}/groupingRules/${ruleGuid}`,
      request,
    );
  }

  /** 更新指定小组名称 */
  async updateGroup(
    classGuid: string,
    ruleGuid: string,
    groupGuid: string,
    request: UpdateGroupRequest,
  ): Promise<GroupingRuleResponse> {
    return this._put<GroupingRuleResponse>(
      `/class/${classGuid}/groupingRules/${ruleGuid}/${groupGuid}`,
      request,
    );
  }

  // #endregion

  // #region DELETE

  /** 删除指定班级 */
  async deleteClass(classGuid: string): Promise<void> {
    return this._delete(`/class/${classGuid}`);
  }

  /** 删除指定班级内的指定学生 */
  async deleteStudent(classGuid: string, studentId: number): Promise<void> {
    return this._delete(`/class/${classGuid}/student/${studentId}`);
  }

  /** 删除指定班级扩展属性 */
  async deleteClassExtraProperty(
    classGuid: string,
    appId: string,
    propName: string,
  ): Promise<void> {
    return this._delete(
      `/class/${classGuid}/extraProps/${appId}/${propName}`,
    );
  }

  /** 删除指定学生扩展属性 */
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

  /** 删除指定分组规则 */
  async deleteGroupingRule(
    classGuid: string,
    ruleGuid: string,
  ): Promise<void> {
    return this._delete(`/class/${classGuid}/groupingRules/${ruleGuid}`);
  }

  /** 删除指定小组 */
  async deleteGroup(
    classGuid: string,
    ruleGuid: string,
    groupGuid: string,
  ): Promise<void> {
    return this._delete(
      `/class/${classGuid}/groupingRules/${ruleGuid}/${groupGuid}`,
    );
  }

  // #endregion

  // #region Offline factory methods

  /** 创建一个离线班级对象（不经过服务器） */
  static createClassOffline(name: string): Class {
    return {
      guid: uuidv4(),
      name,
      students: [],
      groupingRules: [],
      extraProperties: [],
    };
  }

  /** 创建一个离线学生对象（不经过服务器） */
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

  /** 创建一个离线班级扩展属性对象 */
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

  /** 创建一个离线学生扩展属性对象 */
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