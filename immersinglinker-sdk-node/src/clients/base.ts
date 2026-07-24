/** 客户端请求时抛出的通用错误 */
export class ImmersingLinkerError extends Error {
  /**
   * @param message 错误描述
   * @param statusCode HTTP 状态码
   * @param url 请求路径
   */
  constructor(
    message: string,
    public readonly statusCode: number,
    public readonly url: string,
  ) {
    super(message);
    this.name = 'ImmersingLinkerError';
  }
}

/** 请求超时错误 */
export class TimeoutError extends ImmersingLinkerError {
  constructor(url: string) {
    super(`Request timed out: ${url}`, 0, url);
    this.name = 'TimeoutError';
  }
}

/** 资源不存在错误（HTTP 404） */
export class NotFoundError extends ImmersingLinkerError {
  constructor(url: string) {
    super(`Resource not found: ${url}`, 404, url);
    this.name = 'NotFoundError';
  }
}

/** 资源冲突错误（HTTP 409） */
export class ConflictError extends ImmersingLinkerError {
  constructor(message: string, url: string) {
    super(message, 409, url);
    this.name = 'ConflictError';
  }
}

/** 请求参数错误（HTTP 400） */
export class BadRequestError extends ImmersingLinkerError {
  constructor(message: string, url: string) {
    super(message, 400, url);
    this.name = 'BadRequestError';
  }
}

/** 服务客户端基类，封装 HTTP GET/POST/PUT/DELETE 请求 */
export class ServiceClientBase {
  /** 服务器基础地址 */
  protected readonly _baseUrl: string;

  /** 请求超时时间（毫秒） */
  private readonly _timeout: number;

  /**
   * @param port 服务器端口号（数字或字符串）
   * @param timeout 请求超时时间（毫秒），默认 5000
   */
  constructor(port: string | number, timeout: number = 5000) {
    this._baseUrl = `http://localhost:${port}`;
    this._timeout = timeout;
  }

  /** 创建带超时的 AbortSignal，超时后自动中止请求 */
  protected _createSignal(): AbortSignal {
    const controller = new AbortController();
    setTimeout(() => controller.abort(), this._timeout);
    return controller.signal;
  }

  /** 统一处理 fetch 响应，超时时抛出 TimeoutError */
  protected async _fetchWithTimeout(
    path: string,
    init?: RequestInit,
  ): Promise<Response> {
    try {
      const response = await fetch(`${this._baseUrl}${path}`, {
        ...init,
        signal: this._createSignal(),
      });
      return response;
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        throw new TimeoutError(path);
      }
      throw error;
    }
  }

  /**
   * 发起 GET 请求，返回解析后的数据。
   * @throws {ImmersingLinkerError} 非 2xx 响应时抛出
   * @throws {TimeoutError} 请求超时时抛出
   */
  protected async _get<T>(path: string): Promise<T> {
    const response = await this._fetchWithTimeout(path);
    if (!response.ok) {
      throw new ImmersingLinkerError(
        `HTTP ${response.status}: ${response.statusText}`,
        response.status,
        path,
      );
    }
    const text = await response.text();
    return text ? (JSON.parse(text) as T) : (undefined as T);
  }

  /**
   * 发起 GET 请求，404 时返回 null。
   * @throws {ImmersingLinkerError} 非 404 错误时抛出
   * @throws {TimeoutError} 请求超时时抛出
   */
  protected async _getOrNull<T>(path: string): Promise<T | null> {
    const response = await this._fetchWithTimeout(path);
    if (response.status === 404) return null;
    if (!response.ok) {
      throw new ImmersingLinkerError(
        `HTTP ${response.status}: ${response.statusText}`,
        response.status,
        path,
      );
    }
    const text = await response.text();
    return text ? (JSON.parse(text) as T) : null;
  }

  /**
   * 发起 GET 请求，404 时返回空数组。
   * @throws {ImmersingLinkerError} 非 404 错误时抛出
   * @throws {TimeoutError} 请求超时时抛出
   */
  protected async _getOrEmpty<T>(path: string): Promise<T[]> {
    const response = await this._fetchWithTimeout(path);
    if (response.status === 404) return [];
    if (!response.ok) {
      throw new ImmersingLinkerError(
        `HTTP ${response.status}: ${response.statusText}`,
        response.status,
        path,
      );
    }
    const text = await response.text();
    return text ? (JSON.parse(text) as T[]) : [];
  }

  /**
   * 发起 POST 请求，返回解析后的响应体。
   * @throws {NotFoundError} 404 时抛出
   * @throws {ConflictError} 409 时抛出
   * @throws {BadRequestError} 400 时抛出
   * @throws {TimeoutError} 请求超时时抛出
   */
  protected async _post<T>(path: string, body?: unknown): Promise<T> {
    const response = await this._fetchWithTimeout(path, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    await this._handleError(response, path);
    const text = await response.text();
    return text ? (JSON.parse(text) as T) : (undefined as T);
  }

  /**
   * 发起 POST 请求，不关心响应体。
   * @throws {NotFoundError} 404 时抛出
   * @throws {BadRequestError} 400 时抛出
   * @throws {TimeoutError} 请求超时时抛出
   */
  protected async _postVoid(path: string): Promise<void> {
    const response = await this._fetchWithTimeout(path, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
    });
    await this._handleError(response, path);
  }

  /**
   * 发起 PUT 请求，返回解析后的响应体。
   * @throws {NotFoundError} 404 时抛出
   * @throws {ConflictError} 409 时抛出
   * @throws {BadRequestError} 400 时抛出
   * @throws {TimeoutError} 请求超时时抛出
   */
  protected async _put<T>(path: string, body: unknown): Promise<T> {
    const response = await this._fetchWithTimeout(path, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    await this._handleError(response, path);
    const text = await response.text();
    return text ? (JSON.parse(text) as T) : (undefined as T);
  }

  /**
   * 发起 DELETE 请求。
   * @throws {NotFoundError} 404 时抛出
   * @throws {ConflictError} 409 时抛出
   * @throws {BadRequestError} 400 时抛出
   * @throws {TimeoutError} 请求超时时抛出
   */
  protected async _delete(path: string): Promise<void> {
    const response = await this._fetchWithTimeout(path, {
      method: 'DELETE',
    });
    await this._handleError(response, path);
  }

  /** 统一处理非 2xx 响应 */
  private async _handleError(
    response: Response,
    path: string,
  ): Promise<void> {
    if (response.ok) return;
    switch (response.status) {
      case 404:
        throw new NotFoundError(path);
      case 409:
        throw new ConflictError(
          `Resource conflict at ${path}`,
          path,
        );
      case 400:
        throw new BadRequestError(
          `Bad request at ${path}`,
          path,
        );
      default:
        throw new ImmersingLinkerError(
          `HTTP ${response.status}: ${response.statusText}`,
          response.status,
          path,
        );
    }
  }
}