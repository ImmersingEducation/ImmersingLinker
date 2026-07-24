/** 时间状态 */
export enum TimeState {
  /** 无 */
  None = 0,
  /** 上课 */
  OnClass = 1,
  /** 课间 */
  OnBreaking = 2,
}

/** 科目（映射 ClassIsland.Shared.Subject） */
export interface Subject {
  /** 科目名称 */
  name: string;
  /** 教师 */
  teacher?: string;
  /** 教室 */
  room?: string;
  /** 备注 */
  notes?: string;
  /** 起始时间 */
  startTime?: string;
  /** 结束时间 */
  endTime?: string;
}

/** 时间布局项（映射 ClassIsland.Shared.TimeLayoutItem） */
export interface TimeLayoutItem {
  /** 起始相对秒数 */
  startSecond: number;
  /** 结束相对秒数 */
  endSecond: number;
  /** 时间类型 */
  timeType: number;
  /** 备注 */
  notes?: string;
  /** 关联科目 */
  subject?: Subject;
}

/** 班级计划（映射 ClassIsland.Shared.ClassPlan） */
export interface ClassPlan {
  /** 计划名称 */
  name: string;
  /** 时间布局项列表 */
  timeLayouts?: TimeLayoutItem[];
  /** 关联的班级标识 */
  class?: string;
  /** 星期规则 */
  weekRule?: number;
  /** 是否启用 */
  isEnabled?: boolean;
  /** 创建日期 */
  createdDate?: string;
}

/** 配置文件（映射 ClassIsland.Shared.Profile） */
export interface Profile {
  /** 配置名称 */
  name: string;
  /** 班级计划列表 */
  classPlans?: ClassPlan[];
  /** 时间点列表 */
  timePoints?: unknown[];
  /** 全局设置 */
  settings?: Record<string, unknown>;
}