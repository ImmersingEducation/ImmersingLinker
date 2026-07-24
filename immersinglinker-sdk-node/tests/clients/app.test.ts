import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { AppServiceClient } from '../../src/clients/app.js';

function createResponse(ok: boolean): Response {
  return {
    ok,
    status: ok ? 200 : 500,
    statusText: ok ? 'OK' : 'Internal Server Error',
    text: async () => '',
  } as Response;
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn());
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('AppServiceClient', () => {
  const client = new AppServiceClient(5000);

  describe('testConnection', () => {
    it('should return true on successful response', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(true));
      const result = await client.testConnection();
      expect(result).toBe(true);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/app/hello', expect.objectContaining({}));
    });

    it('should return false on non-ok response', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(false));
      const result = await client.testConnection();
      expect(result).toBe(false);
    });

    it('should return false on fetch error', async () => {
      vi.mocked(fetch).mockRejectedValue(new Error('Connection refused'));
      const result = await client.testConnection();
      expect(result).toBe(false);
    });
  });
});
