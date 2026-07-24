/** 时间状态 */
export enum TimeState {
  /** 无 */
  None = 0,
  /** 上课 */
  OnClass = 1,
  /** 课间 */
  OnBreaking = 2,
}

/** 科目 (来自 ClassIsland.Shared) */
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

/** 时间布局项 (来自 ClassIsland.Shared) */
export interface TimeLayoutItem {
  /** 起始时间 */
  startSecond: number;
  /** 结束时间 */
  endSecond: number;
  /** 时间类型 */
  timeType: number;
  /** 备注 */
  notes?: string;
  /** 科目 */
  subject?: Subject;
}

/** 班级计划 (来自 ClassIsland.Shared) */
export interface ClassPlan {
  /** 计划名称 */
  name: string;
  /** 时间布局项列表 */
  timeLayouts?: TimeLayoutItem[];
  /** 关联的班级 */
  class?: string;
  /** 星期规则 */
  weekRule?: number;
  /** 是否启用 */
  isEnabled?: boolean;
  /** 创建日期 */
  createdDate?: string;
}

/** 配置文件 (来自 ClassIsland.Shared) */
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
