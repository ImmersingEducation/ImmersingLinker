import { ServiceClientBase } from './base.js';
import type {
  Subject,
  TimeState,
  TimeLayoutItem,
  ClassPlan,
  Profile,
} from '../types/lesson.js';

/** 课表/课程信息客户端，提供当前课程、上下节课、计时器、配置文件等查询接口 */
export class LessonServiceClient extends ServiceClientBase {
  // #region Current

  /** 获取当前课程科目，无课时返回 null */
  async getCurrentSubject(): Promise<Subject | null> {
    return this._getOrNull<Subject>('/lesson/current/subject');
  }

  /** 获取下一节课程的科目 */
  async getNextClassSubject(): Promise<Subject> {
    return this._get<Subject>('/lesson/current/next-class-subject');
  }

  /** 获取当前时间状态（上课/课间/无） */
  async getCurrentState(): Promise<TimeState> {
    return this._get<TimeState>('/lesson/current/state');
  }

  /** 获取当前时间布局项 */
  async getCurrentTimeLayoutItem(): Promise<TimeLayoutItem> {
    return this._get<TimeLayoutItem>('/lesson/current/time-layout-item');
  }

  /** 获取当前班级计划，无计划时返回 null */
  async getCurrentClassPlan(): Promise<ClassPlan | null> {
    return this._getOrNull<ClassPlan>('/lesson/current/class-plan');
  }

  /** 获取当前课表选中的索引 */
  async getCurrentSelectedIndex(): Promise<number> {
    return this._get<number>('/lesson/current/selected-index');
  }

  /** 获取当前班级计划是否启用 */
  async getIsClassPlanEnabled(): Promise<boolean> {
    return this._get<boolean>('/lesson/current/is-class-plan-enabled');
  }

  /** 获取当前班级计划是否已加载 */
  async getIsClassPlanLoaded(): Promise<boolean> {
    return this._get<boolean>('/lesson/current/is-class-plan-loaded');
  }

  /** 获取当前课程是否已确认 */
  async getIsLessonConfirmed(): Promise<boolean> {
    return this._get<boolean>('/lesson/current/is-lesson-confirmed');
  }

  // #endregion

  // #region Next

  /** 获取下一节课的时间布局项 */
  async getNextClassTimeLayoutItem(): Promise<TimeLayoutItem> {
    return this._get<TimeLayoutItem>('/lesson/next/class-time-layout-item');
  }

  /** 获取下一个课间的时间布局项 */
  async getNextBreakingTimeLayoutItem(): Promise<TimeLayoutItem> {
    return this._get<TimeLayoutItem>('/lesson/next/breaking-time-layout-item');
  }

  // #endregion

  // #region Previous

  /** 获取上一节课的科目，无上一节时返回 null */
  async getPreviousClassSubject(): Promise<Subject | null> {
    return this._getOrNull<Subject>('/lesson/previous/class-subject');
  }

  /** 获取上一节课的时间布局项 */
  async getPreviousClassTimeLayoutItem(): Promise<TimeLayoutItem> {
    return this._get<TimeLayoutItem>('/lesson/previous/class-time-layout-item');
  }

  /** 获取上一个课间的时间布局项 */
  async getPreviousBreakingTimeLayoutItem(): Promise<TimeLayoutItem> {
    return this._get<TimeLayoutItem>(
      '/lesson/previous/breaking-time-layout-item',
    );
  }

  // #endregion

  // #region Timer

  /** 获取当前课程剩余时间（ISO 8601 持续时间字符串） */
  async getOnClassLeftTime(): Promise<string> {
    return this._get<string>('/lesson/timer/on-class-left');
  }

  /** 获取当前课间剩余时间 */
  async getOnBreakingLeftTime(): Promise<string> {
    return this._get<string>('/lesson/timer/on-breaking-left');
  }

  /** 获取自上一节课程开始以来经过的时间 */
  async getElapsedSincePreviousClass(): Promise<string> {
    return this._get<string>('/lesson/timer/elapsed-since-previous-class');
  }

  /** 获取自上一课间开始以来经过的时间 */
  async getElapsedSincePreviousBreaking(): Promise<string> {
    return this._get<string>('/lesson/timer/elapsed-since-previous-breaking');
  }

  /** 获取自上一个任意状态开始以来经过的时间 */
  async getElapsedSincePreviousAny(): Promise<string> {
    return this._get<string>('/lesson/timer/elapsed-since-previous-any');
  }

  // #endregion

  // #region Profile

  /** 获取当前配置文件的路径 */
  async getCurrentProfilePath(): Promise<string> {
    return this._get<string>('/lesson/profile/current-profile-path');
  }

  /** 获取当前配置文件是否受信任 */
  async getIsCurrentProfileTrusted(): Promise<boolean> {
    return this._get<boolean>('/lesson/profile/is-trusted');
  }

  /** 获取当前完整配置文件 */
  async getProfile(): Promise<Profile> {
    return this._get<Profile>('/lesson/profile');
  }

  // #endregion
}