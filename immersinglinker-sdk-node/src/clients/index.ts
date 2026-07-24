/** 客户端基础类及错误类型 */
export { ServiceClientBase, ImmersingLinkerError, NotFoundError, ConflictError, BadRequestError } from './base.js';
/** 应用基础客户端 */
export { AppServiceClient } from './app.js';
/** 课表/课程信息客户端 */
export { LessonServiceClient } from './lesson.js';
/** 班级管理客户端 */
export { ClassServiceClient } from './class.js';
/** 自动化管理客户端 */
export { AutomationServiceClient } from './automation.js';