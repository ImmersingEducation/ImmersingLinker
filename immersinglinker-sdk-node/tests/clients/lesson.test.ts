import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { LessonServiceClient } from '../../src/clients/lesson.js';
import { ImmersingLinkerError } from '../../src/clients/base.js';

function createResponse(status: number, body?: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 404 ? 'Not Found' : 'OK',
    text: async () => (body !== undefined ? JSON.stringify(body) : ''),
  } as Response;
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn());
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('LessonServiceClient', () => {
  const client = new LessonServiceClient(5000);

  describe('Current', () => {
    it('getCurrentSubject returns Subject on 200', async () => {
      const subject = { name: 'Math', teacher: 'Mr. Smith' };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, subject));
      const result = await client.getCurrentSubject();
      expect(result).toEqual(subject);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/lesson/current/subject', expect.objectContaining({}));
    });

    it('getCurrentSubject returns null on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getCurrentSubject();
      expect(result).toBeNull();
    });

    it('getNextClassSubject returns Subject', async () => {
      const subject = { name: 'Physics' };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, subject));
      const result = await client.getNextClassSubject();
      expect(result).toEqual(subject);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/lesson/current/next-class-subject', expect.objectContaining({}));
    });

    it('getCurrentState returns TimeState', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, 1));
      const result = await client.getCurrentState();
      expect(result).toBe(1);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/lesson/current/state', expect.objectContaining({}));
    });

    it('getCurrentTimeLayoutItem returns TimeLayoutItem', async () => {
      const item = { startSecond: 0, endSecond: 3600, timeType: 1 };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, item));
      const result = await client.getCurrentTimeLayoutItem();
      expect(result).toEqual(item);
    });

    it('getCurrentClassPlan returns ClassPlan on 200', async () => {
      const plan = { name: 'Day Plan', isEnabled: true };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, plan));
      const result = await client.getCurrentClassPlan();
      expect(result).toEqual(plan);
    });

    it('getCurrentClassPlan returns null on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getCurrentClassPlan();
      expect(result).toBeNull();
    });

    it('getCurrentSelectedIndex returns number', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, 3));
      const result = await client.getCurrentSelectedIndex();
      expect(result).toBe(3);
    });

    it('getIsClassPlanEnabled returns boolean', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, true));
      const result = await client.getIsClassPlanEnabled();
      expect(result).toBe(true);
    });

    it('getIsClassPlanLoaded returns boolean', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, false));
      const result = await client.getIsClassPlanLoaded();
      expect(result).toBe(false);
    });

    it('getIsLessonConfirmed returns boolean', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, true));
      const result = await client.getIsLessonConfirmed();
      expect(result).toBe(true);
    });
  });

  describe('Next', () => {
    it('getNextClassTimeLayoutItem', async () => {
      const item = { startSecond: 3600, endSecond: 7200, timeType: 1 };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, item));
      const result = await client.getNextClassTimeLayoutItem();
      expect(result).toEqual(item);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/lesson/next/class-time-layout-item', expect.objectContaining({}));
    });

    it('getNextBreakingTimeLayoutItem', async () => {
      const item = { startSecond: 0, endSecond: 600, timeType: 0 };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, item));
      const result = await client.getNextBreakingTimeLayoutItem();
      expect(result).toEqual(item);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/lesson/next/breaking-time-layout-item', expect.objectContaining({}));
    });
  });

  describe('Previous', () => {
    it('getPreviousClassSubject returns Subject on 200', async () => {
      const subject = { name: 'History' };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, subject));
      const result = await client.getPreviousClassSubject();
      expect(result).toEqual(subject);
    });

    it('getPreviousClassSubject returns null on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getPreviousClassSubject();
      expect(result).toBeNull();
    });

    it('getPreviousClassTimeLayoutItem', async () => {
      const item = { startSecond: 0, endSecond: 3600, timeType: 1 };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, item));
      const result = await client.getPreviousClassTimeLayoutItem();
      expect(result).toEqual(item);
    });

    it('getPreviousBreakingTimeLayoutItem', async () => {
      const item = { startSecond: 0, endSecond: 600, timeType: 0 };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, item));
      const result = await client.getPreviousBreakingTimeLayoutItem();
      expect(result).toEqual(item);
    });
  });

  describe('Timer', () => {
    it('getOnClassLeftTime returns string', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, '00:45:00'));
      const result = await client.getOnClassLeftTime();
      expect(result).toBe('00:45:00');
    });

    it('getOnBreakingLeftTime returns string', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, '00:05:00'));
      const result = await client.getOnBreakingLeftTime();
      expect(result).toBe('00:05:00');
    });

    it('getElapsedSincePreviousClass returns string', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, '00:10:00'));
      const result = await client.getElapsedSincePreviousClass();
      expect(result).toBe('00:10:00');
    });

    it('getElapsedSincePreviousBreaking returns string', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, '00:03:00'));
      const result = await client.getElapsedSincePreviousBreaking();
      expect(result).toBe('00:03:00');
    });

    it('getElapsedSincePreviousAny returns string', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, '00:05:00'));
      const result = await client.getElapsedSincePreviousAny();
      expect(result).toBe('00:05:00');
    });
  });

  describe('Profile', () => {
    it('getCurrentProfilePath returns string', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, '/path/to/profile'));
      const result = await client.getCurrentProfilePath();
      expect(result).toBe('/path/to/profile');
    });

    it('getIsCurrentProfileTrusted returns boolean', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, true));
      const result = await client.getIsCurrentProfileTrusted();
      expect(result).toBe(true);
    });

    it('getProfile returns Profile', async () => {
      const profile = { name: 'Default', classPlans: [] };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, profile));
      const result = await client.getProfile();
      expect(result).toEqual(profile);
    });
  });

  describe('error handling', () => {
    it('should throw on 500', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(500));
      await expect(client.getCurrentState()).rejects.toThrow(ImmersingLinkerError);
    });
  });
});
