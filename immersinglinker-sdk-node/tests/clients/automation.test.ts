import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { AutomationServiceClient } from '../../src/clients/automation.js';
import { NotFoundError, BadRequestError, ImmersingLinkerError } from '../../src/clients/base.js';
import { RuleSetSatisfyMode } from '../../src/types/automation.js';

function createResponse(status: number, body?: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 404 ? 'Not Found' : status === 400 ? 'Bad Request' : 'OK',
    text: async () => (body !== undefined ? JSON.stringify(body) : ''),
  } as Response;
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn());
});

afterEach(() => {
  vi.unstubAllGlobals();
});

const mockPlanInfo = { guid: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee', name: 'Test Plan' };

const mockPlan = {
  guid: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
  name: 'Test Plan',
  revertable: true,
  trigger: { tag: 'test' },
  ruleSet: { guid: '', not: false, isSatisfied: false, satisfyMode: 0, rules: [] },
  actions: [],
};

const mockRequest = {
  name: 'New Plan',
  revertable: false,
  trigger: { triggerKey: 'ilinker.UrlTrigger', properties: { tag: 'test' } },
  ruleSet: null,
  actions: [{ actionKey: 'test.action', properties: {} }],
};

describe('AutomationServiceClient', () => {
  const client = new AutomationServiceClient(5000);

  describe('GET endpoints', () => {
    it('getAllPlanInfos returns AutomationPlanInfo[]', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, [mockPlanInfo]));
      const result = await client.getAllPlanInfos();
      expect(result).toEqual([mockPlanInfo]);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/automation');
    });

    it('getAllPlanInfos throws on 500', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(500));
      await expect(client.getAllPlanInfos()).rejects.toThrow(ImmersingLinkerError);
    });

    it('getPlanByGuid returns AutomationPlan on 200', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockPlan));
      const result = await client.getPlanByGuid('test-guid');
      expect(result).toEqual(mockPlan);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/automation/test-guid');
    });

    it('getPlanByGuid returns null on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getPlanByGuid('nonexistent');
      expect(result).toBeNull();
    });
  });

  describe('POST endpoints', () => {
    it('createPlan sends request and returns AutomationPlan', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockPlan));
      const result = await client.createPlan(mockRequest);
      expect(result).toEqual(mockPlan);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/automation', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify(mockRequest),
      }));
    });

    it('createPlan throws BadRequestError on 400', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(400));
      await expect(client.createPlan(mockRequest)).rejects.toThrow(BadRequestError);
    });

    it('triggerPlan calls trigger endpoint', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200));
      await expect(client.triggerPlan('test-guid')).resolves.toBeUndefined();
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/automation/test-guid/trigger', expect.objectContaining({
        method: 'POST',
      }));
    });

    it('triggerPlan throws NotFoundError on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.triggerPlan('nonexistent')).rejects.toThrow(NotFoundError);
    });

    it('triggerPlan throws BadRequestError on 400', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(400));
      await expect(client.triggerPlan('test-guid')).rejects.toThrow(BadRequestError);
    });

    it('invokeUrlTrigger calls invoke endpoint', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200));
      await expect(client.invokeUrlTrigger('my-tag')).resolves.toBeUndefined();
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/automation/invoke/my-tag', expect.objectContaining({
        method: 'POST',
      }));
    });
  });

  describe('PUT endpoints', () => {
    it('updatePlan sends request and returns AutomationPlan', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, mockPlan));
      const result = await client.updatePlan('test-guid', mockRequest);
      expect(result).toEqual(mockPlan);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/automation/test-guid', expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify(mockRequest),
      }));
    });

    it('updatePlan throws NotFoundError on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.updatePlan('nonexistent', mockRequest)).rejects.toThrow(NotFoundError);
    });

    it('updatePlan throws BadRequestError on 400', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(400));
      await expect(client.updatePlan('test-guid', mockRequest)).rejects.toThrow(BadRequestError);
    });
  });

  describe('DELETE endpoints', () => {
    it('deletePlan deletes on success', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(204));
      await expect(client.deletePlan('test-guid')).resolves.toBeUndefined();
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/automation/test-guid', expect.objectContaining({
        method: 'DELETE',
      }));
    });

    it('deletePlan throws NotFoundError on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.deletePlan('nonexistent')).rejects.toThrow(NotFoundError);
    });
  });

  describe('Offline factory methods', () => {
    it('createTriggerDtoOffline creates TriggerDto', () => {
      const dto = AutomationServiceClient.createTriggerDtoOffline('ilinker.UrlTrigger', { tag: 'test' });
      expect(dto.triggerKey).toBe('ilinker.UrlTrigger');
      expect(dto.properties).toEqual({ tag: 'test' });
    });

    it('createRuleSetDtoOffline creates RuleSetDto', () => {
      const rules = [{ ruleKey: 'test.rule', not: false, ruleSet: null }];
      const dto = AutomationServiceClient.createRuleSetDtoOffline(RuleSetSatisfyMode.AllSatisfied, false, rules);
      expect(dto.satisfyMode).toBe(RuleSetSatisfyMode.AllSatisfied);
      expect(dto.not).toBe(false);
      expect(dto.rules).toEqual(rules);
    });

    it('createRuleNodeDtoOffline creates leaf node', () => {
      const node = AutomationServiceClient.createRuleNodeDtoOffline('test.rule', { value: 42 }, false, null);
      expect(node.ruleKey).toBe('test.rule');
      expect(node.properties).toEqual({ value: 42 });
      expect(node.not).toBe(false);
      expect(node.ruleSet).toBeNull();
    });

    it('createRuleNodeDtoOffline creates nested node', () => {
      const nested = AutomationServiceClient.createRuleSetDtoOffline(RuleSetSatisfyMode.AnySatisfied, true, []);
      const node = AutomationServiceClient.createRuleNodeDtoOffline(null, undefined, true, nested);
      expect(node.ruleKey).toBeNull();
      expect(node.not).toBe(true);
      expect(node.ruleSet).toBe(nested);
    });

    it('createActionDtoOffline creates ActionDto', () => {
      const dto = AutomationServiceClient.createActionDtoOffline('test.action', { param: 1 });
      expect(dto.actionKey).toBe('test.action');
      expect(dto.properties).toEqual({ param: 1 });
    });

    it('createPlanRequestOffline creates CreateAutomationPlanRequest', () => {
      const trigger = AutomationServiceClient.createTriggerDtoOffline('t', {});
      const actions = [AutomationServiceClient.createActionDtoOffline('a', {})];
      const req = AutomationServiceClient.createPlanRequestOffline('Plan', true, trigger, null, actions);
      expect(req.name).toBe('Plan');
      expect(req.revertable).toBe(true);
      expect(req.trigger).toBe(trigger);
      expect(req.ruleSet).toBeNull();
      expect(req.actions).toBe(actions);
    });

    it('updatePlanRequestOffline creates UpdateAutomationPlanRequest', () => {
      const trigger = AutomationServiceClient.createTriggerDtoOffline('t', {});
      const actions = [AutomationServiceClient.createActionDtoOffline('a', {})];
      const req = AutomationServiceClient.updatePlanRequestOffline('Updated', false, trigger, null, actions);
      expect(req.name).toBe('Updated');
      expect(req.revertable).toBe(false);
      expect(req.trigger).toBe(trigger);
      expect(req.actions).toEqual(actions);
    });
  });
});
