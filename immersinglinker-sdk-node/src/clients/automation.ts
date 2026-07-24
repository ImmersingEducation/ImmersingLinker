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

export class AutomationServiceClient extends ServiceClientBase {
  // #region GET

  async getAllPlanInfos(): Promise<AutomationPlanInfo[]> {
    return this._get<AutomationPlanInfo[]>('/automation');
  }

  async getPlanByGuid(planGuid: string): Promise<AutomationPlan | null> {
    return this._getOrNull<AutomationPlan>(`/automation/${planGuid}`);
  }

  // #endregion

  // #region POST

  async createPlan(
    request: CreateAutomationPlanRequest,
  ): Promise<AutomationPlan> {
    return this._post<AutomationPlan>('/automation', request);
  }

  async triggerPlan(planGuid: string): Promise<void> {
    return this._postVoid(`/automation/${planGuid}/trigger`);
  }

  async invokeUrlTrigger(tag: string): Promise<void> {
    return this._postVoid(`/automation/invoke/${tag}`);
  }

  // #endregion

  // #region PUT

  async updatePlan(
    planGuid: string,
    request: UpdateAutomationPlanRequest,
  ): Promise<AutomationPlan> {
    return this._put<AutomationPlan>(`/automation/${planGuid}`, request);
  }

  // #endregion

  // #region DELETE

  async deletePlan(planGuid: string): Promise<void> {
    return this._delete(`/automation/${planGuid}`);
  }

  // #endregion

  // #region Offline factory methods

  static createTriggerDtoOffline(
    triggerKey: string,
    properties?: unknown,
  ): TriggerDto {
    return { triggerKey, properties };
  }

  static createRuleSetDtoOffline(
    satisfyMode: RuleSetSatisfyMode,
    not: boolean,
    rules: RuleNodeDto[],
  ): RuleSetDto {
    return { satisfyMode, not, rules };
  }

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

  static createActionDtoOffline(
    actionKey: string,
    properties?: unknown,
  ): ActionDto {
    return { actionKey, properties };
  }

  static createPlanRequestOffline(
    name: string,
    revertable: boolean,
    trigger: TriggerDto,
    ruleSet: RuleSetDto | null,
    actions: ActionDto[],
  ): CreateAutomationPlanRequest {
    return { name, revertable, trigger, ruleSet, actions };
  }

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
