/** 性别枚举 */
export enum Gender {
  /** 男 */
  Male = 0,
  /** 女 */
  Female = 1,
}

/** 应用标识 */
export interface Application {
  /** 应用唯一标识 */
  uniqueId: string;
  /** 应用名称 */
  name: string;
}

/** 额外属性基类 */
export interface ExtraPropertyBase {
  /** 所属应用 */
  application: Application;
  /** 属性名称 */
  name: string;
  /** 属性值 */
  value: unknown;
}

/**
 * JSON 序列化元素，对应 System.Text.Json.JsonElement。
 * 在运行时可能包含 { ValueKind, GetString(), GetInt32() } 等方法。
 */
export type JsonElement = unknown;