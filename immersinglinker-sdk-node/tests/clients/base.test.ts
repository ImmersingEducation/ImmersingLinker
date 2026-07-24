import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import {
  ServiceClientBase,
  ImmersingLinkerError,
  TimeoutError,
  NotFoundError,
  ConflictError,
  BadRequestError,
} from '../../src/clients/base.js';

function createResponse(status: number, body?: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 404 ? 'Not Found' : 'OK',
    text: async () => (body !== undefined ? JSON.stringify(body) : ''),
    json: async () => body,
  } as Response;
}

class TestClient extends ServiceClientBase {
  async getTest<T>(path: string) {
    return this._get<T>(path);
  }
  async getOrNullTest<T>(path: string) {
    return this._getOrNull<T>(path);
  }
  async getOrEmptyTest<T>(path: string) {
    return this._getOrEmpty<T>(path);
  }
  async postTest<T>(path: string, body?: unknown) {
    return this._post<T>(path, body);
  }
  async postVoidTest(path: string) {
    return this._postVoid(path);
  }
  async putTest<T>(path: string, body: unknown) {
    return this._put<T>(path, body);
  }
  async deleteTest(path: string) {
    return this._delete(path);
  }
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn());
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('ImmersingLinkerError', () => {
  it('should create base error with status code and url', () => {
    const err = new ImmersingLinkerError('Something went wrong', 500, '/test');
    expect(err.message).toBe('Something went wrong');
    expect(err.statusCode).toBe(500);
    expect(err.url).toBe('/test');
    expect(err.name).toBe('ImmersingLinkerError');
    expect(err).toBeInstanceOf(Error);
  });

  it('should create NotFoundError', () => {
    const err = new NotFoundError('/api/resource');
    expect(err.statusCode).toBe(404);
    expect(err.name).toBe('NotFoundError');
    expect(err).toBeInstanceOf(ImmersingLinkerError);
  });

  it('should create ConflictError', () => {
    const err = new ConflictError('conflict', '/api/resource');
    expect(err.statusCode).toBe(409);
    expect(err.name).toBe('ConflictError');
    expect(err).toBeInstanceOf(ImmersingLinkerError);
  });

  it('should create BadRequestError', () => {
    const err = new BadRequestError('bad request', '/api/resource');
    expect(err.statusCode).toBe(400);
    expect(err.name).toBe('BadRequestError');
    expect(err).toBeInstanceOf(ImmersingLinkerError);
  });
});

describe('ServiceClientBase', () => {
  let client: TestClient;

  beforeEach(() => {
    client = new TestClient(5000);
  });

  describe('_get', () => {
    it('should return parsed JSON on success', async () => {
      const data = { id: 1, name: 'test' };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, data));
      const result = await client.getTest<typeof data>('/api/test');
      expect(result).toEqual(data);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/api/test', expect.objectContaining({}));
    });

    it('should return undefined for empty 200 response', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, undefined));
      const result = await client.getTest('/api/test');
      expect(result).toBeUndefined();
    });

    it('should throw on non-ok response', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(500));
      await expect(client.getTest('/api/test')).rejects.toThrow(ImmersingLinkerError);
    });
  });

  describe('_getOrNull', () => {
    it('should return parsed JSON on success', async () => {
      const data = { id: 1 };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, data));
      const result = await client.getOrNullTest<typeof data>('/api/test');
      expect(result).toEqual(data);
    });

    it('should return null on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getOrNullTest('/api/test');
      expect(result).toBeNull();
    });

    it('should return null for empty body non-404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, undefined));
      const result = await client.getOrNullTest('/api/test');
      expect(result).toBeNull();
    });

    it('should throw on non-404 error', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(500));
      await expect(client.getOrNullTest('/api/test')).rejects.toThrow(ImmersingLinkerError);
    });
  });

  describe('_getOrEmpty', () => {
    it('should return parsed array on success', async () => {
      const data = [{ id: 1 }, { id: 2 }];
      vi.mocked(fetch).mockResolvedValue(createResponse(200, data));
      const result = await client.getOrEmptyTest('/api/test');
      expect(result).toEqual(data);
    });

    it('should return empty array on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      const result = await client.getOrEmptyTest('/api/test');
      expect(result).toEqual([]);
    });

    it('should return empty array for empty 200', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200, undefined));
      const result = await client.getOrEmptyTest('/api/test');
      expect(result).toEqual([]);
    });

    it('should throw on non-404 error', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(500));
      await expect(client.getOrEmptyTest('/api/test')).rejects.toThrow(ImmersingLinkerError);
    });
  });

  describe('_post', () => {
    it('should post JSON body and return response', async () => {
      const body = { name: 'new' };
      const responseData = { id: 1, name: 'new' };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, responseData));

      const result = await client.postTest('/api/test', body);
      expect(result).toEqual(responseData);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/api/test', expect.objectContaining({
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      }));
    });

    it('should throw NotFoundError on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.postTest('/api/test', {})).rejects.toThrow(NotFoundError);
    });

    it('should throw ConflictError on 409', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(409));
      await expect(client.postTest('/api/test', {})).rejects.toThrow(ConflictError);
    });

    it('should throw BadRequestError on 400', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(400));
      await expect(client.postTest('/api/test', {})).rejects.toThrow(BadRequestError);
    });
  });

  describe('_postVoid', () => {
    it('should post without body and not throw on success', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(200));

      await expect(client.postVoidTest('/api/trigger')).resolves.toBeUndefined();
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/api/trigger', expect.objectContaining({
        method: 'POST',
      }));
    });

    it('should throw on error', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.postVoidTest('/api/trigger')).rejects.toThrow(NotFoundError);
    });
  });

  describe('_put', () => {
    it('should put JSON body and return response', async () => {
      const body = { name: 'updated' };
      const responseData = { id: 1, name: 'updated' };
      vi.mocked(fetch).mockResolvedValue(createResponse(200, responseData));

      const result = await client.putTest('/api/test', body);
      expect(result).toEqual(responseData);
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/api/test', expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify(body),
      }));
    });

    it('should throw NotFoundError on 404', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.putTest('/api/test', {})).rejects.toThrow(NotFoundError);
    });

    it('should throw ConflictError on 409', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(409));
      await expect(client.putTest('/api/test', {})).rejects.toThrow(ConflictError);
    });

    it('should throw BadRequestError on 400', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(400));
      await expect(client.putTest('/api/test', {})).rejects.toThrow(BadRequestError);
    });
  });

  describe('_delete', () => {
    it('should delete and not throw on success', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(204));

      await expect(client.deleteTest('/api/test')).resolves.toBeUndefined();
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/api/test', expect.objectContaining({
        method: 'DELETE',
      }));
    });

    it('should throw NotFoundError on 404 for delete', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(404));
      await expect(client.deleteTest('/api/test')).rejects.toThrow(NotFoundError);
    });

    it('should throw on other errors', async () => {
      vi.mocked(fetch).mockResolvedValue(createResponse(500));
      await expect(client.deleteTest('/api/test')).rejects.toThrow(ImmersingLinkerError);
    });
  });

  describe('timeout', () => {
    it('should throw TimeoutError when fetch aborts', async () => {
      vi.mocked(fetch).mockRejectedValue(new DOMException('The operation was aborted', 'AbortError'));
      await expect(client.getTest('/api/test')).rejects.toThrow(TimeoutError);
    });

    it('should not throw TimeoutError on normal fetch error', async () => {
      vi.mocked(fetch).mockRejectedValue(new TypeError('Failed to fetch'));
      await expect(client.getTest('/api/test')).rejects.not.toThrow(TimeoutError);
    });
  });

  describe('constructor', () => {
    it('should accept number port', async () => {
      const c = new TestClient(3000);
      vi.mocked(fetch).mockResolvedValue(createResponse(200, { ok: true }));
      await c.getTest('/test');
      expect(fetch).toHaveBeenCalledWith('http://localhost:3000/test', expect.objectContaining({}));
    });

    it('should accept string port', async () => {
      const c = new TestClient('8080');
      vi.mocked(fetch).mockResolvedValue(createResponse(200, { ok: true }));
      await c.getTest('/test');
      expect(fetch).toHaveBeenCalledWith('http://localhost:8080/test', expect.objectContaining({}));
    });

    it('should accept custom timeout', async () => {
      const c = new TestClient(5000, 10000);
      vi.mocked(fetch).mockResolvedValue(createResponse(200, { ok: true }));
      await c.getTest('/test');
      expect(fetch).toHaveBeenCalledWith('http://localhost:5000/test', expect.objectContaining({}));
    });
  });
});
