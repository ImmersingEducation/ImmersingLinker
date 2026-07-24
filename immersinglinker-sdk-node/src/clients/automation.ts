import { ServiceClientBase } from './base.js';
import type {
  AutomationPlan,
  AutomationPlanInfo,
  CreateAutomationPlanRequest,
  UpdateAutomationPlanRequest,
  TriggerDto,
  ActionDto,
  RuleSetDto,
  RuleNodeDto,
  RuleSetSatisfyMode,
} from '../types/automation.js';
import { v4 as uuidv4 } from 'uuid';

/** 自动化管理客户端，提供自动化方案的 CRUD 及触发操作 */
export class AutomationServiceClient extends ServiceClientBase {
  // #region GET

  /** 获取所有自动化方案的摘要信息 */
  async getAllPlanInfos(): Promise<AutomationPlanInfo[]> {
    return this._get<AutomationPlanInfo[]>('/automation');
  }

  /** 根据 GUID 获取指定自动化方案，不存在时返回 null */
  async getPlanByGuid(planGuid: string): Promise<AutomationPlan | null> {
    return this._getOrNull<AutomationPlan>(`/automation/${planGuid}`);
  }

  // #endregion

  // #region POST

  /** 创建自动化方案 */
  async createPlan(
    request: CreateAutomationPlanRequest,
  ): Promise<AutomationPlan> {
    return this._post<AutomationPlan>('/automation', request);
  }

  /** 手动触发指定自动化方案 */
  async triggerPlan(planGuid: string): Promise<void> {
    return this._postVoid(`/automation/${planGuid}/trigger`);
  }

  /** 通过标签触发 UrlTrigger */
  async invokeUrlTrigger(tag: string): Promise<void> {
    return this._postVoid(`/automation/invoke/${tag}`);
  }

  // #endregion

  // #region PUT

  /** 更新指定自动化方案 */
  async updatePlan(
    planGuid: string,
    request: UpdateAutomationPlanRequest,
  ): Promise<AutomationPlan> {
    return this._put<AutomationPlan>(`/automation/${planGuid}`, request);
  }

  // #endregion

  // #region DELETE

  /** 删除指定自动化方案 */
  async deletePlan(planGuid: string): Promise<void> {
    return this._delete(`/automation/${planGuid}`);
  }

  // #endregion

  // #region Offline factory methods

  /** 创建一个离线触发器 DTO */
  static createTriggerDtoOffline(
    triggerKey: string,
    properties?: unknown,
  ): TriggerDto {
    return { triggerKey, properties };
  }

  /** 创建一个离线规则集 DTO */
  static createRuleSetDtoOffline(
    satisfyMode: RuleSetSatisfyMode,
    not: boolean,
    rules: RuleNodeDto[],
  ): RuleSetDto {
    return { satisfyMode, not, rules };
  }

  /** 创建一个离线规则节点 DTO */
  static createRuleNodeDtoOffline(
    ruleKey: string | null,
    properties: unknown | undefined,
    not: boolean,
    ruleSet: RuleSetDto | null,
  ): RuleNodeDto {
    return {
      ruleKey,
      properties,
      not,
      ruleSet,
    };
  }

  /** 创建一个离线动作 DTO */
  static createActionDtoOffline(
    actionKey: string,
    properties?: unknown,
  ): ActionDto {
    return { actionKey, properties };
  }

  /** 创建一个离线创建方案的请求对象 */
  static createPlanRequestOffline(
    name: string,
    revertable: boolean,
    trigger: TriggerDto,
    ruleSet: RuleSetDto | null,
    actions: ActionDto[],
  ): CreateAutomationPlanRequest {
    return { name, revertable, trigger, ruleSet, actions };
  }

  /** 创建一个离线更新方案的请求对象 */
  static updatePlanRequestOffline(
    name: string,
    revertable: boolean,
    trigger: TriggerDto,
    ruleSet: RuleSetDto | null,
    actions: ActionDto[],
  ): UpdateAutomationPlanRequest {
    return { name, revertable, trigger, ruleSet, actions };
  }

  // #endregion
}