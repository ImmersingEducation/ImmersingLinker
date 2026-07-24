import type { Gender, Application, ExtraPropertyBase, JsonElement } from './common.js';

/** 班级额外属性 */
export interface ClassExtraProperty extends ExtraPropertyBase {}

/** 学生额外属性 */
export interface StudentExtraProperty extends ExtraPropertyBase {}

/** 学生 */
export interface Student {
  /** 学生 GUID */
  guid: string;
  /** 学生姓名 */
  name: string;
  /** 班内学号 */
  studentIdInClass: number;
  /** 性别 */
  gender: Gender;
  /** 学生额外属性列表 */
  extraProperties: StudentExtraProperty[];
}

/** 分组 */
export interface Group {
  /** 分组 GUID */
  guid: string;
  /** 分组名称 */
  name: string;
  /** 包含的学生 GUID 集合 */
  contains: string[];
}

/** 分组规则 */
export interface GroupingRule {
  /** 分组规则 GUID */
  guid: string;
  /** 分组规则名称 */
  name: string;
  /** 分组列表 */
  groups: Group[];
}

/** 分组规则响应（含未分配学生） */
export interface GroupingRuleResponse {
  /** 分组规则 GUID */
  guid: string;
  /** 分组规则名称 */
  name: string;
  /** 分组列表 */
  groups: Group[];
  /** 未分配学生 GUID 列表 */
  unassignedStudentGuids: string[];
}

/** 班级 */
export interface Class {
  /** 班级 GUID */
  guid: string;
  /** 班级名称 */
  name: string;
  /** 学生列表 */
  students: Student[];
  /** 分组规则列表 */
  groupingRules: GroupingRule[];
  /** 班级额外属性列表 */
  extraProperties: ClassExtraProperty[];
}

/** 班级摘要信息 */
export interface ClassInfo {
  /** 班级 GUID */
  guid: string;
  /** 班级名称 */
  name: string;
}

/** 创建班级请求 */
export interface CreateClassRequest {
  /** 班级名称 */
  name: string;
}

/** 更新班级请求 */
export interface UpdateClassRequest {
  /** 班级名称 */
  name: string;
}

/** 创建学生请求 */
export interface CreateStudentRequest {
  /** 学生姓名 */
  name: string;
  /** 班内学号 */
  studentIdInClass: number;
  /** 性别 */
  gender: Gender;
}

/** 更新学生请求 */
export interface UpdateStudentRequest {
  /** 学生姓名 */
  name: string;
  /** 性别 */
  gender: Gender;
  /** 所在班级内分组 */
  groupInClass: string;
}

/** 创建额外属性请求 */
export interface CreateExtraPropertyRequest {
  /** 应用 ID */
  appId: string;
  /** 属性名称 */
  name: string;
  /** 属性值 */
  value: unknown;
}

/** 更新额外属性请求 */
export interface UpdateExtraPropertyRequest {
  /** 属性值 */
  value: unknown;
}

/** 创建分组规则请求 */
export interface CreateGroupingRuleRequest {
  /** 分组规则名称 */
  name: string;
}

/** 更新分组规则请求 */
export interface UpdateGroupingRuleRequest {
  /** 分组规则名称 */
  name: string;
}

/** 创建分组请求 */
export interface CreateGroupRequest {
  /** 分组名称 */
  name: string;
}

/** 更新分组请求 */
export interface UpdateGroupRequest {
  /** 分组名称 */
  name: string;
}
