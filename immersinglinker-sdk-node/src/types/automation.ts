import type { JsonElement } from './common.js';

/** 规则集满足模式 */
export enum RuleSetSatisfyMode {
  /** 所有子规则必须全部满足 */
  AllSatisfied = 0,
  /** 任一子规则满足即可 */
  AnySatisfied = 1,
}

/** 触发器 DTO（数据传输对象） */
export interface TriggerDto {
  /** 触发器键（如 "ilinker.UrlTrigger"） */
  triggerKey: string;
  /** 触发器属性（JSON 格式） */
  properties?: JsonElement;
}

/** 动作 DTO（数据传输对象） */
export interface ActionDto {
  /** 动作键（如 "some.action"） */
  actionKey: string;
  /** 动作属性（JSON 格式） */
  properties?: JsonElement;
}

/** 规则节点 DTO，叶子节点使用 ruleKey，非叶子节点使用 ruleSet */
export interface RuleNodeDto {
  /** 规则键（叶子规则使用，规则集节点为 null） */
  ruleKey?: string | null;
  /** 规则属性（JSON 格式） */
  properties?: JsonElement;
  /** 是否取反 */
  not?: boolean;
  /** 嵌套规则集（非叶子节点使用） */
  ruleSet?: RuleSetDto | null;
}

/** 规则集 DTO */
export interface RuleSetDto {
  /** 满足模式 */
  satisfyMode: RuleSetSatisfyMode;
  /** 是否取反 */
  not?: boolean;
  /** 子规则列表 */
  rules: RuleNodeDto[];
}

/** 创建自动化方案请求 */
export interface CreateAutomationPlanRequest {
  /** 方案名称 */
  name: string;
  /** 是否可回退 */
  revertable: boolean;
  /** 触发器 */
  trigger: TriggerDto;
  /** 规则集（可选） */
  ruleSet?: RuleSetDto | null;
  /** 动作列表 */
  actions: ActionDto[];
}

/** 更新自动化方案请求（结构与创建相同） */
export type UpdateAutomationPlanRequest = CreateAutomationPlanRequest;

/** 自动化方案摘要信息 */
export interface AutomationPlanInfo {
  /** 方案 GUID */
  guid: string;
  /** 方案名称 */
  name: string;
}

/** 触发器基类（映射 C# abstract Trigger） */
export interface Trigger {
  /** 触发事件回调 */
  onTriggerFired?: (sender: unknown, args: TriggerFiredEventArgs) => void;
}

/** 动作基类（映射 C# abstract Action） */
export interface Action {
  /** 是否可回退 */
  revertable: boolean;
  /** 执行动作 */
  invoke: () => Promise<void>;
  /** 回退动作 */
  revert: () => Promise<void>;
  /** 回退失败回调 */
  onRevertFailed?: (error: Error) => void;
}

/** 规则基类（映射 C# abstract RuleBase） */
export interface RuleBase {
  /** 规则 GUID */
  guid: string;
  /** 是否取反 */
  not: boolean;
  /** 是否满足 */
  isSatisfied: boolean;
}

/** 叶子规则（映射 C# abstract Rule） */
export interface Rule extends RuleBase {}

/** 规则集（映射 C# RuleSet） */
export interface RuleSet extends RuleBase {
  /** 满足模式 */
  satisfyMode: RuleSetSatisfyMode;
  /** 子规则列表 */
  rules: RuleBase[];
}

/** 自动化方案 */
export interface AutomationPlan {
  /** 方案 GUID */
  guid: string;
  /** 方案名称 */
  name: string;
  /** 是否可回退 */
  revertable: boolean;
  /** 触发器 */
  trigger: Trigger;
  /** 规则集 */
  ruleSet: RuleSet;
  /** 动作列表 */
  actions: Action[];
}

/** 自动化执行器 */
export interface AutomationRunner {
  /** 执行器 GUID */
  guid: string;
  /** 是否为回退模式 */
  revertMode: boolean;
  /** 动作列表 */
  actions: Action[];
  /** 当前步骤索引（-1 表示未开始） */
  currentStep: number;
}

/** 触发器触发事件参数 */
export interface TriggerFiredEventArgs {
  /** 关联的自动化方案 GUID */
  automationPlanGuid: string;
  /** 触发时间（ISO 字符串） */
  firedAt: string;
  /** 附加载荷 */
  payload?: unknown;
}

/** 方案触发事件参数 */
export interface PlanTriggeredEventArgs {
  /** 关联的自动化方案 */
  plan: AutomationPlan;
  /** 执行器 */
  runner: AutomationRunner;
}

/** 方案回退事件参数 */
export interface PlanRevertedEventArgs {
  /** 关联的自动化方案 */
  plan: AutomationPlan;
  /** 执行器 */
  runner: AutomationRunner;
}

/** 执行器完成事件参数 */
export interface RunnerCompletedEventArgs {
  /** 执行器 */
  runner: AutomationRunner;
}

/** 执行器失败事件参数 */
export interface RunnerFailedEventArgs {
  /** 执行器 */
  runner: AutomationRunner;
  /** 异常 */
  exception: Error;
}

/** URL 触发器（映射 C# UrlTrigger） */
export interface UrlTrigger extends Trigger {
  /** 触发标签 */
  tag: string;
}

/** 回退失败异常 */
export interface RevertFailedException extends Error {
  /** 失败的动作 */
  failedAction: Action;
  /** 步骤索引 */
  stepIndex: number;
}