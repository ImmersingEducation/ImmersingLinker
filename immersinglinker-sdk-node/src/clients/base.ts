export class ImmersingLinkerError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number,
    public readonly url: string,
  ) {
    super(message);
    this.name = 'ImmersingLinkerError';
  }
}

export class NotFoundError extends ImmersingLinkerError {
  constructor(url: string) {
    super(`Resource not found: ${url}`, 404, url);
    this.name = 'NotFoundError';
  }
}

export class ConflictError extends ImmersingLinkerError {
  constructor(message: string, url: string) {
    super(message, 409, url);
    this.name = 'ConflictError';
  }
}

export class BadRequestError extends ImmersingLinkerError {
  constructor(message: string, url: string) {
    super(message, 400, url);
    this.name = 'BadRequestError';
  }
}

export class ServiceClientBase {
  protected readonly _baseUrl: string;

  constructor(port: string | number) {
    this._baseUrl = `http://localhost:${port}`;
  }

  protected async _get<T>(path: string): Promise<T> {
    const response = await fetch(`${this._baseUrl}${path}`);
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

  protected async _getOrNull<T>(path: string): Promise<T | null> {
    const response = await fetch(`${this._baseUrl}${path}`);
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

  protected async _getOrEmpty<T>(path: string): Promise<T[]> {
    const response = await fetch(`${this._baseUrl}${path}`);
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

  protected async _post<T>(path: string, body?: unknown): Promise<T> {
    const response = await fetch(`${this._baseUrl}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    await this._handlePostPutError(response, path);
    const text = await response.text();
    return text ? (JSON.parse(text) as T) : (undefined as T);
  }

  protected async _postVoid(path: string): Promise<void> {
    const response = await fetch(`${this._baseUrl}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
    });
    await this._handlePostPutError(response, path);
  }

  protected async _put<T>(path: string, body: unknown): Promise<T> {
    const response = await fetch(`${this._baseUrl}${path}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    await this._handlePostPutError(response, path);
    const text = await response.text();
    return text ? (JSON.parse(text) as T) : (undefined as T);
  }

  protected async _delete(path: string): Promise<void> {
    const response = await fetch(`${this._baseUrl}${path}`, {
      method: 'DELETE',
    });
    if (response.status === 404) {
      const urlPath = path;
      throw new NotFoundError(urlPath);
    }
    if (!response.ok) {
      throw new ImmersingLinkerError(
        `HTTP ${response.status}: ${response.statusText}`,
        response.status,
        path,
      );
    }
  }

  private async _handlePostPutError(
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
