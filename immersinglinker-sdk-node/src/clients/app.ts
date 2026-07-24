import { ServiceClientBase } from './base.js';

/** 应用基础客户端，提供服务器连接检测等通用功能 */
export class AppServiceClient extends ServiceClientBase {
  /**
   * 测试与 ImmersingLinker 服务器的连接。
   * 请求 /app/hello 端点，根据响应状态返回 true/false。
   */
  async testConnection(): Promise<boolean> {
    try {
      const response = await this._fetchWithTimeout('/app/hello');
      return response.ok;
    } catch {
      return false;
    }
  }
}