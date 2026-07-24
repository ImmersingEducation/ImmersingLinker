import { ServiceClientBase } from './base.js';

export class AppServiceClient extends ServiceClientBase {
  async testConnection(): Promise<boolean> {
    try {
      const response = await fetch(`${this._baseUrl}/app/hello`);
      return response.ok;
    } catch {
      return false;
    }
  }
}
